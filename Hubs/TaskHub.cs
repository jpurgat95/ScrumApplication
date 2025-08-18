using Microsoft.AspNetCore.SignalR;

public class TaskHub : Hub
{
    // Metoda może wysyłać powiadomienie do wszystkich podłączonych klientów
    public async Task RefreshTasks()
    {
        await Clients.All.SendAsync("ReceiveTaskUpdate");
    }
}
