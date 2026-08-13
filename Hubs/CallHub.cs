using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace Anagram.Server.Hubs
{
    public class CallHub : Hub
    {
        public async Task InitiateCall(string caller, string receiver)
        {
            // Notify receiver of incoming call
            await Clients.User(receiver).SendAsync("IncomingCall", caller);
        }

        public async Task SendSignal(string receiver, string signalData)
        {
            // Forward WebRTC signaling data (offer/answer/ICE candidates)
            await Clients.User(receiver).SendAsync("ReceiveSignal", signalData);
        }

        public async Task EndCall(string receiver)
        {
            // Notify receiver that call has ended
            await Clients.User(receiver).SendAsync("CallEnded");
        }
    }
}
