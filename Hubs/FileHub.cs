using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace Anagram.Server.Hubs
{
    public class FileHub : Hub
    {
        public async Task InitiateFileTransfer(string sender, string receiver, string fileName, long fileSize)
        {
            // Notify receiver about incoming file
            await Clients.User(receiver).SendAsync("FileIncoming", sender, fileName, fileSize);
        }

        public async Task SendFileChunk(string receiver, byte[] chunk, int chunkNumber)
        {
            // Send file chunk to receiver
            await Clients.User(receiver).SendAsync("ReceiveFileChunk", chunk, chunkNumber);
        }
    }
}
