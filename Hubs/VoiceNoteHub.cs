using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace Anagram.Server.Hubs
{
    public class VoiceNoteHub : Hub
    {
        public async Task SendVoiceNote(string sender, string receiver, byte[] audioData, double duration)
        {
            // Deliver voice note to receiver
            await Clients.User(receiver).SendAsync("ReceiveVoiceNote", sender, audioData, duration);
        }
    }
}
