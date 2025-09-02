using Microsoft.AspNetCore.SignalR;
using ScrumApplication.Models;

public class UpdatesHub : Hub
{
    public async Task TaskAdded(TaskItem task)
        => await Clients.All.SendAsync("TaskAdded", task);

    public async Task TaskDeleted(int taskId)
        => await Clients.All.SendAsync("TaskDeleted", taskId);

    public async Task TaskUpdated(TaskItem task)
        => await Clients.All.SendAsync("TaskUpdated", task);

    public async Task EventAdded(ScrumEvent ev)
        => await Clients.All.SendAsync("EventAdded", ev);

    public async Task EventDeleted(int eventId)
        => await Clients.All.SendAsync("EventDeleted", eventId);

    public async Task EventUpdated(ScrumEvent ev)
        => await Clients.All.SendAsync("EventUpdated", ev);

    public async Task EventUpdatesTask(int eventId, object taskDtoList)
        => await Clients.All.SendAsync("EventUpdatesTask", taskDtoList);

    public async Task BlockTaskEdit(int taskId)
        => await Clients.All.SendAsync("BlockTaskEdit", taskId);

    public async Task UnblockTaskEdit(int taskId)
        => await Clients.All.SendAsync("UnblockTaskEdit", taskId);

    public async Task ForceLogoutWithToast()
        => await Clients.All.SendAsync("ForceLogoutWithToast");

    public async Task ForcePasswordReset(string resetUrl)
        => await Clients.All.SendAsync("ForcePasswordReset", resetUrl);
    public async Task UserRegistered(string userName, string userId)    
       => await Clients.All.SendAsync("UserRegistered", userName, userId);
}
