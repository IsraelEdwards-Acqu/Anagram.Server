using Microsoft.AspNetCore.SignalR;

public class CallHub : Hub
{
    public async Task InitiateCall(string caller, string receiver)
    {
        await Clients.All.SendAsync("IncomingCall", caller, receiver);
    }

    public async Task EndCall(string receiver)
    {
        await Clients.All.SendAsync("CallEnded", receiver);
    }

    public async Task OnCallStarted(string caller, string receiver)
    {
        await Clients.All.SendAsync("CallStarted", caller, receiver);
    }
}
