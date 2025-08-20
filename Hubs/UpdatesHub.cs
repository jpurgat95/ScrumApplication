using Microsoft.AspNetCore.SignalR;
using ScrumApplication.Models;
using System.Threading.Tasks;

public class UpdatesHub : Hub
{
    // Wysyłanie informacji o nowym zadaniu
    public async Task TaskAdded(TaskItem task)
    {
        await Clients.All.SendAsync("TaskAdded", task);
    }

    // Wysyłanie informacji o usunięciu zadania
    public async Task TaskDeleted(int taskId)
    {
        await Clients.All.SendAsync("TaskDeleted", taskId);
    }

    // Wysyłanie informacji o nowym wydarzeniu
    public async Task EventAdded(ScrumEvent ev)
    {
        await Clients.All.SendAsync("EventAdded", ev);
    }

    // Wysyłanie informacji o usunięciu wydarzenia
    public async Task EventDeleted(int eventId)
    {
        await Clients.All.SendAsync("EventDeleted", eventId);
    }
}
