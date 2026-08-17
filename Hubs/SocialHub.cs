using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Anagram.Server.Hubs
{
    [Authorize]
    public class SocialHub : Hub
    {
        // This hub is used for friend/follow notifications.
        // Methods are primarily server -> client notifications via IHubContext<SocialHub>.
    }
}
