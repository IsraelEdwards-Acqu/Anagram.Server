using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using Anagram.Server.Data;
using Anagram.Server.Hubs;

var builder = WebApplication.CreateBuilder(args);

// ✅ Add services
builder.Services.AddControllers();
builder.Services.AddSignalR()
    .AddJsonProtocol(options =>
    {
        options.PayloadSerializerOptions.PropertyNamingPolicy = null;
    });

// ✅ Configure EF Core with Neon (Postgres)
builder.Services.AddDbContext<AnagramDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// ✅ Swagger for API docs
builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthorization();

// ✅ Map controllers + hubs
app.MapControllers();
app.MapHub<ChatHub>("/hubs/chat");
app.MapHub<FileHub>("/hubs/files");
app.MapHub<CallHub>("/hubs/calls");
app.MapHub<VoiceNoteHub>("/hubs/voicenotes");

app.Run();
