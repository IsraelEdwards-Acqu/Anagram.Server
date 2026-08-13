using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace Anagram.Server.Hubs
{
    public class ChatHub : Hub
    {
        public async Task SendMessage(string user, string message)
        {
            // Broadcast to all connected clients
            await Clients.All.SendAsync("ReceiveMessage", user, message);
        }
    }
}
