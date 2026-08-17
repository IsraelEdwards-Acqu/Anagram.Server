using Microsoft.AspNetCore.SignalR;

public class VoiceNoteHub : Hub
{
    public async Task SendVoiceNote(string sender, string receiver, byte[] audio, double duration)
    {
        await Clients.All.SendAsync("ReceiveVoiceNote", sender, receiver, audio, duration);
    }

    public async Task OnVoiceNoteReceived(string sender)
    {
        await Clients.All.SendAsync("VoiceNoteReceived", sender);
    }
}
