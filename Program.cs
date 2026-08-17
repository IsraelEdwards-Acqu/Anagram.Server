using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Anagram.Server.Data;
using Anagram.Server.Hubs;
using Anagram.Server.Services;
using Microsoft.AspNetCore.SignalR;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// -------------------- Configuration --------------------
// Load configuration files without reloadOnChange to avoid inotify issues in some hosts
builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
                     .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: false)
                     .AddEnvironmentVariables();

// -------------------- Services --------------------
builder.Services.AddControllers()
    .AddJsonOptions(opts =>
    {
        // Keep property names as declared (no camelCase conversion) for SignalR payload parity
        opts.JsonSerializerOptions.PropertyNamingPolicy = null;
    });

// SignalR with JSON protocol options
builder.Services.AddSignalR()
    .AddJsonProtocol(options =>
    {
        options.PayloadSerializerOptions.PropertyNamingPolicy = null;
    });

// Response compression for hubs and API responses
builder.Services.AddResponseCompression();

// Health checks
builder.Services.AddHealthChecks();

// DbContext (Postgres)
builder.Services.AddDbContext<AnagramDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Register application services
builder.Services.AddScoped<SocialService>();

// -------------------- CORS --------------------
// Read allowed origins from configuration (appsettings)
var corsOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("DefaultCorsPolicy", policy =>
    {
        policy.WithOrigins(corsOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials(); // required for SignalR
    });
});

// -------------------- Authentication (JWT Bearer) --------------------
var jwtSection = builder.Configuration.GetSection("Jwt");
var jwtKey = jwtSection["Key"] ?? throw new InvalidOperationException("Jwt:Key not configured");
var issuer = jwtSection["Issuer"];
var audience = jwtSection["Audience"];

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = !string.IsNullOrEmpty(issuer),
        ValidateAudience = !string.IsNullOrEmpty(audience),
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = issuer,
        ValidAudience = audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
    };

    // Allow SignalR to receive access token from query string for WebSocket connections
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"].FirstOrDefault();
            var path = context.HttpContext.Request.Path;
            if (!string.IsNullOrEmpty(accessToken) &&
                (path.StartsWithSegments("/hubs/updates") || path.StartsWithSegments("/hubs/social") ||
                 path.StartsWithSegments("/hubs/chat") || path.StartsWithSegments("/hubs/files")))
            {
                context.Token = accessToken;
            }
            return Task.CompletedTask;
        }
    };
});

// -------------------- SignalR user identifier mapping (use username) --------------------
// Default SignalR UserIdentifier uses ClaimTypes.NameIdentifier. If you want to use username,
// register a custom IUserIdProvider that returns ClaimTypes.Name (username).
builder.Services.AddSingleton<IUserIdProvider, UsernameUserIdProvider>();

// -------------------- Rate limiting (basic) --------------------
// Protect high-frequency endpoints (likes, friend requests) with a simple policy.
// Adjust token limit and replenishment to your needs.
builder.Services.AddRateLimiter(options =>
{
    options.AddPolicy("ShortActionsPolicy", context =>
        RateLimitPartition.GetTokenBucketLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "anon",
            factory: _ => new TokenBucketRateLimiterOptions
            {
                TokenLimit = 20,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 10,
                ReplenishmentPeriod = TimeSpan.FromSeconds(60),
                TokensPerPeriod = 20,
                AutoReplenishment = true
            }));
});

// -------------------- Build --------------------
var app = builder.Build();

// -------------------- Middleware pipeline --------------------

// Forwarded headers (when behind proxies/load balancers)
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

// Developer exception page in Development
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    // HSTS in production
    app.UseHsts();
}

// HTTPS redirection only when appropriate (development or when not behind LB that terminates TLS)
var disableHttpsRedirect = builder.Configuration.GetValue<bool>("DisableHttpsRedirection");
if (!disableHttpsRedirect)
{
    app.UseHttpsRedirection();
}

// Use response compression
app.UseResponseCompression();

// Use static files if you serve any
app.UseStaticFiles();

// Use routing
app.UseRouting();

// CORS must be before Authentication/Authorization and before SignalR endpoints
app.UseCors("DefaultCorsPolicy");

// Authentication & Authorization
app.UseAuthentication();
app.UseAuthorization();

// Rate limiter middleware (apply globally or selectively)
app.UseRateLimiter();

// Health checks endpoint
app.MapHealthChecks("/healthz");

// Map controllers and hubs
app.MapControllers();

// Map SignalR hubs (use consistent hub paths)
app.MapHub<ChatHub>("/hubs/chat");
app.MapHub<FileHub>("/hubs/files");
app.MapHub<CallHub>("/hubs/calls");
app.MapHub<VoiceNoteHub>("/hubs/voicenotes");
app.MapHub<UpdatesHub>("/hubs/updates");
app.MapHub<ProfileHub>("/hubs/profile");
app.MapHub<SocialHub>("/hubs/social");

// Final run
app.Run();


// -------------------- Helper classes --------------------

// Map SignalR user identifier to username claim (ClaimTypes.Name)
public class UsernameUserIdProvider : IUserIdProvider
{
    public string? GetUserId(HubConnectionContext connection)
    {
        // Prefer ClaimTypes.Name (username). Fallback to NameIdentifier if needed.
        var nameClaim = connection.User?.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value;
        if (!string.IsNullOrEmpty(nameClaim)) return nameClaim;
        return connection.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
    }
}
