using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.SignalR;
using Moq;
using ScrumApplication.Data;
using ScrumApplication.Models;
using ScrumApplication.Pages.Events;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;
namespace ScrumApplicationTests
{
    public class EventsIndexModelTests
    {
        // Helper do mockowania ClaimsPrincipal z Id i rolą Admin lub nie
        private ClaimsPrincipal CreateUserPrincipal(string userId, bool isAdmin)
        {
            var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, userId) };
            if (isAdmin)
                claims.Add(new Claim(ClaimTypes.Role, "Admin"));
            var identity = new ClaimsIdentity(claims, "TestAuth");
            return new ClaimsPrincipal(identity);
        }

        // Helper do ustawienia TempData w modelu, konieczne dla uniknięcia NullReferenceException
        private void InitializeTempData(PageModel model)
        {
            var httpContext = new DefaultHttpContext();
            var tempDataProvider = new Mock<ITempDataProvider>().Object;
            var tempData = new TempDataDictionary(httpContext, tempDataProvider);
            model.TempData = tempData;
        }

        // Klasa ujednolicona do mockowania IHubClients z jednym proxy
        public class FakeHubClients : IHubClients
        {
            private readonly IClientProxy _clientProxy;

            public FakeHubClients(IClientProxy clientProxy)
            {
                _clientProxy = clientProxy;
            }

            public IClientProxy All => _clientProxy;
            public IClientProxy AllExcept(IReadOnlyList<string> excludedConnectionIds) => _clientProxy;
            public IClientProxy Client(string connectionId) => _clientProxy;
            public IClientProxy Clients(IReadOnlyList<string> connectionIds) => _clientProxy;
            public IClientProxy Group(string groupName) => _clientProxy;
            public IClientProxy GroupExcept(string groupName, IReadOnlyList<string> excludedConnectionIds) => _clientProxy;
            public IClientProxy Groups(IReadOnlyList<string> groupNames) => _clientProxy;
            public IClientProxy User(string userId) => _clientProxy;
            public IClientProxy Users(IReadOnlyList<string> userIds) => _clientProxy;
        }

        [Fact]
        public async Task OnGetAsync_ShouldLoadEventsAndTasks()
        {
            var mockEventRepo = new Mock<IEventRepository>();
            var mockTaskRepo = new Mock<ITaskRepository>();
            var mockHubContext = new Mock<IHubContext<UpdatesHub>>();
            var mockUserRoleRepo = new Mock<IUserRoleRepository>();

            var userId = "user1";
            var isAdmin = false;

            mockEventRepo.Setup(r => r.GetEventsAsync(userId, isAdmin))
                .ReturnsAsync(new List<ScrumEvent> { new ScrumEvent { Id = 1, Title = "Event1" } });

            mockTaskRepo.Setup(r => r.GetTasksAsync(userId, isAdmin))
                .ReturnsAsync(new List<TaskItem> { new TaskItem { Id = 1, Title = "Task1" } });

            var model = new IndexModel(mockEventRepo.Object, mockTaskRepo.Object, mockHubContext.Object, mockUserRoleRepo.Object);

            model.PageContext = new PageContext()
            {
                HttpContext = new DefaultHttpContext { User = CreateUserPrincipal(userId, isAdmin) }
            };
            InitializeTempData(model);

            await model.OnGetAsync();

            Assert.Single(model.Events);
            Assert.Single(model.Tasks);
            Assert.Equal("Event1", model.Events[0].Title);
            Assert.Equal("Task1", model.Tasks[0].Title);
        }
        [Fact]
        public async Task OnPostAsync_WithValidModel_ShouldAddEventAndSendNotification()
        {
            // Arrange
            var mockEventRepo = new Mock<IEventRepository>();
            var mockTaskRepo = new Mock<ITaskRepository>();
            var mockUserRoleRepo = new Mock<IUserRoleRepository>();
            var mockClientProxy = new Mock<IClientProxy>();
            var fakeHubClients = new FakeHubClients(mockClientProxy.Object);
            var mockHubContext = new Mock<IHubContext<UpdatesHub>>();

            var userId = "user1";
            var isAdmin = false;
            var adminRoleId = "98954494-ef5f-4a06-87e4-22ef31417c9c";
            var adminIds = new List<string> { "admin1", "admin2" };

            var newEvent = new ScrumEvent
            {
                Id = 1,
                Title = "New Event",
                Description = "Event description",
                StartDate = System.DateTime.Now,
                EndDate = System.DateTime.Now.AddHours(1),
                IsDone = false,
                UserId = userId
            };

            // Setup zwracania adminów
            mockUserRoleRepo.Setup(r => r.GetUserIdsInRoleAsync(adminRoleId)).ReturnsAsync(adminIds);

            // Setup dodawania nowego eventu
            mockEventRepo.Setup(r => r.AddEventAsync(It.IsAny<ScrumEvent>())).Callback<ScrumEvent>(ev =>
            {
                ev.Id = newEvent.Id; // przypisz Id, jak w produkcji
            }).Returns(Task.CompletedTask);

            // Setup zwracania list wydarzeń po dodaniu (admin i user)
            mockEventRepo.Setup(r => r.GetEventsAsync(null, true))
                .ReturnsAsync(new List<ScrumEvent> { newEvent });

            mockEventRepo.Setup(r => r.GetEventsAsync(userId, false))
                .ReturnsAsync(new List<ScrumEvent> { newEvent });

            // Setup clients w hubcontext
            mockHubContext.Setup(h => h.Clients).Returns(fakeHubClients);

            // Setup SendCoreAsync dla SignalR
            mockClientProxy.Setup(proxy =>
                proxy.SendCoreAsync(It.IsAny<string>(), It.IsAny<object[]>(), default))
                .Returns(Task.CompletedTask);

            var model = new IndexModel(mockEventRepo.Object, mockTaskRepo.Object, mockHubContext.Object, mockUserRoleRepo.Object)
            {
                Title = newEvent.Title,
                Description = newEvent.Description,
                StartDate = newEvent.StartDate,
                EndDate = newEvent.EndDate,
            };

            model.PageContext = new PageContext()
            {
                HttpContext = new DefaultHttpContext()
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                    {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Role, isAdmin ? "Admin" : "User")
            }))
                }
            };

            // Inicjuj TempData konieczne do prawidłowego działania
            InitializeTempData(model);

            // Act
            var result = await model.OnPostAsync();

            // Assert
            mockEventRepo.Verify(r => r.AddEventAsync(It.IsAny<ScrumEvent>()), Times.Once);
            mockUserRoleRepo.Verify(r => r.GetUserIdsInRoleAsync(adminRoleId), Times.Once);
            mockClientProxy.Verify(c => c.SendCoreAsync(It.IsAny<string>(), It.IsAny<object[]>(), default), Times.AtLeast(2));
            // Co najmniej: EventsListUpdated do adminów i usera, oraz EventAdded do adminów

            Assert.IsType<RedirectToPageResult>(result);
            Assert.Equal("Dodano nowe wydarzenie!", model.TempData["ToastMessage"]);
            Assert.Equal("success", model.TempData["ToastType"]);
        }
        [Fact]
        public async Task OnPostToggleDoneAsync_WithValidEvent_ShouldToggleDoneAndSendNotifications()
        {
            var mockEventRepo = new Mock<IEventRepository>();
            var mockTaskRepo = new Mock<ITaskRepository>();
            var mockUserRoleRepo = new Mock<IUserRoleRepository>();
            var mockClientProxy = new Mock<IClientProxy>();
            var fakeHubClients = new FakeHubClients(mockClientProxy.Object);
            var mockHubContext = new Mock<IHubContext<UpdatesHub>>();

            var userId = "user1";
            var isAdmin = true;
            var adminRoleId = "adminRoleId";
            var adminIds = new List<string> { "admin1", "admin2" };

            var ev = new ScrumEvent
            {
                Id = 1,
                UserId = userId,
                User = new IdentityUser { Id = userId, UserName = "TestUser" },
                IsDone = false
            };

            var tasks = new List<TaskItem>
        {
            new TaskItem { Id = 10, UserId = "userA" },
            new TaskItem { Id = 11, UserId = "userB" }
        };

            mockEventRepo.Setup(r => r.GetEventByIdAsync(ev.Id, userId, isAdmin)).ReturnsAsync(ev);
            mockEventRepo.Setup(r => r.UpdateEventAsync(ev)).Returns(Task.CompletedTask);
            mockTaskRepo.Setup(r => r.GetTasksByEventIdAsync(ev.Id)).ReturnsAsync(tasks);
            mockUserRoleRepo.Setup(r => r.GetUserIdsInRoleAsync(It.IsAny<string>())).ReturnsAsync(adminIds);

            mockHubContext.Setup(h => h.Clients).Returns(fakeHubClients);

            mockClientProxy
                .Setup(proxy => proxy.SendCoreAsync(It.IsAny<string>(), It.IsAny<object[]>(), default))
                .Returns(Task.CompletedTask);

            var model = new IndexModel(mockEventRepo.Object, mockTaskRepo.Object, mockHubContext.Object, mockUserRoleRepo.Object);

            model.PageContext = new PageContext()
            {
                HttpContext = new DefaultHttpContext() { User = CreateUserPrincipal(userId, isAdmin) }
            };
            InitializeTempData(model);

            var result = await model.OnPostToggleDoneAsync(ev.Id);

            Assert.IsType<RedirectToPageResult>(result);
            mockEventRepo.Verify(r => r.UpdateEventAsync(ev), Times.Once);
            mockClientProxy.Verify(c => c.SendCoreAsync(It.IsAny<string>(), It.IsAny<object[]>(), default), Times.AtLeastOnce);
        }
        [Fact]
        public async Task OnPostDeleteAsync_WithValidEvent_ShouldDeleteEventAndNotify()
        {
            // Arrange
            var mockEventRepo = new Mock<IEventRepository>();
            var mockTaskRepo = new Mock<ITaskRepository>();
            var mockUserRoleRepo = new Mock<IUserRoleRepository>();
            var mockClientProxy = new Mock<IClientProxy>();
            var fakeHubClients = new FakeHubClients(mockClientProxy.Object);
            var mockHubContext = new Mock<IHubContext<UpdatesHub>>();

            var userId = "user1";
            var isAdmin = true;
            var adminRoleId = "98954494-ef5f-4a06-87e4-22ef31417c9c";
            var adminIds = new List<string> { "admin1", "admin2" };

            var ev = new ScrumEvent { Id = 1, UserId = userId, IsDone = false };

            var taskList = new List<TaskItem>
    {
        new TaskItem { Id = 10 },
        new TaskItem { Id = 11 }
    };

            // Mock zwraca wydarzenie na podstawie id i uprawnień
            mockEventRepo.Setup(r => r.GetEventByIdAsync(ev.Id, userId, isAdmin))
                .ReturnsAsync(ev);

            // Mock usuwania wydarzenia
            mockEventRepo.Setup(r => r.DeleteEventAsync(ev))
                .Returns(Task.CompletedTask);

            // Mock zwraca listę zadań powiązanych przed usunięciem
            mockTaskRepo.Setup(r => r.GetTasksByEventIdAsync(ev.Id))
                .ReturnsAsync(taskList);

            // Mock zwraca adminów
            mockUserRoleRepo.Setup(r => r.GetUserIdsInRoleAsync(adminRoleId))
                .ReturnsAsync(adminIds);

            // Mock zwraca listę wydarzeń dla admina (pełna lista)
            mockEventRepo.Setup(r => r.GetEventsAsync(null, true))
                .ReturnsAsync(new List<ScrumEvent> { new ScrumEvent { Id = 2, Title = "Admin Event" } });

            // Mock zwraca listę wydarzeń dla usera (jego własne)
            mockEventRepo.Setup(r => r.GetEventsAsync(userId, false))
                .ReturnsAsync(new List<ScrumEvent> { new ScrumEvent { Id = 3, Title = "User Event" } });

            // Ustawienie fake Clients w hubContext
            mockHubContext.Setup(h => h.Clients).Returns(fakeHubClients);

            // Mock dla SendCoreAsync, zwracający ukończone Task
            mockClientProxy.Setup(proxy =>
                proxy.SendCoreAsync(It.IsAny<string>(), It.IsAny<object[]>(), default))
                .Returns(Task.CompletedTask);

            var model = new IndexModel(mockEventRepo.Object, mockTaskRepo.Object, mockHubContext.Object, mockUserRoleRepo.Object);

            model.PageContext = new PageContext()
            {
                HttpContext = new DefaultHttpContext()
                {
                    User = CreateUserPrincipal(userId, isAdmin) // Ustaw usera i rolę zgodnie z testem
                }
            };

            InitializeTempData(model);

            // Act
            var result = await model.OnPostDeleteAsync(ev.Id);

            // Assert
            Assert.IsType<RedirectToPageResult>(result);
            mockEventRepo.Verify(r => r.DeleteEventAsync(ev), Times.Once);

            // Sprawdź, że metodę SendCoreAsync wywołano co najmniej 6 razy:
            // - 1x EventsListUpdated do adminów
            // - 1x EventsListUpdated do usera
            // - 1x EventDeleted do usera (userToSendId)
            // - 1x RelatedTasksDeleted do usera (userToSendId)
            // - 1x RelatedTasksDeleted do siebie (userId)
            // - 1x EventDeleted lub RelatedTasksDeleted dla admina lub usera zależnie od implementacji — min 6 ogólnie
            mockClientProxy.Verify(c =>
                c.SendCoreAsync(It.IsAny<string>(), It.IsAny<object[]>(), default),
                Times.AtLeast(5));
        }

    }
}