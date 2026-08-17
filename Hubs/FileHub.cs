using Microsoft.AspNetCore.SignalR;

public class FileHub : Hub
{
    public async Task SendFileChunk(string sender, byte[] chunk, int sequence)
    {
        await Clients.All.SendAsync("ReceiveFileChunk", sender, chunk, sequence);
    }

    public async Task OnFileReceived(string sender, string fileName)
    {
        await Clients.All.SendAsync("FileReceived", sender, fileName);
    }
}
