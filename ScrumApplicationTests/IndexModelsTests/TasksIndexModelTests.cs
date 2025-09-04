using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.SignalR;
using Moq;
using ScrumApplication.Data;
using ScrumApplication.Models;
using ScrumApplication.Pages.Tasks;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;
namespace ScrumApplicationTests
{
    public class TasksIndexModelTests
    {
        private ClaimsPrincipal CreateUserPrincipal(string userId, bool isAdmin)
        {
            var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, userId) };
            if (isAdmin)
                claims.Add(new Claim(ClaimTypes.Role, "Admin"));
            return new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
        }

        private void InitializeTempData(PageModel model)
        {
            var httpContext = new DefaultHttpContext();
            var tempDataProvider = new Mock<ITempDataProvider>().Object;
            model.TempData = new TempDataDictionary(httpContext, tempDataProvider);
        }

        public class FakeHubClients : IHubClients
        {
            private readonly IClientProxy _clientProxy;
            public FakeHubClients(IClientProxy proxy) => _clientProxy = proxy;
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
        public async Task OnGetAsync_ShouldLoadTasksAndEvents()
        {
            var userId = "user1";
            var isAdmin = false;

            var mockTaskRepo = new Mock<ITaskRepository>();
            var mockEventRepo = new Mock<IEventRepository>();
            var mockUserRoleRepo = new Mock<IUserRoleRepository>();
            var mockHubContext = new Mock<IHubContext<UpdatesHub>>();

            mockTaskRepo.Setup(r => r.GetTasksAsync(userId, isAdmin))
                .ReturnsAsync(new List<TaskItem> { new TaskItem { Id = 1, Title = "Task1" } });
            mockEventRepo.Setup(r => r.GetEventsAsync(userId, isAdmin))
                .ReturnsAsync(new List<ScrumEvent> { new ScrumEvent { Id = 2, Title = "Event1" } });

            var model = new IndexModel(mockTaskRepo.Object, mockEventRepo.Object, mockUserRoleRepo.Object, mockHubContext.Object);

            model.PageContext = new PageContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = CreateUserPrincipal(userId, isAdmin)
                }
            };
            InitializeTempData(model);

            await model.OnGetAsync();

            Assert.Single(model.Tasks);
            Assert.Single(model.Events);
            Assert.Equal("Task1", model.Tasks[0].Title);
            Assert.Equal("Event1", model.Events[0].Title);
        }
        [Fact]
        public async Task OnPostAsync_WithInvalidModel_ShouldReturnPageWithError()
        {
            var mockTaskRepo = new Mock<ITaskRepository>();
            var mockEventRepo = new Mock<IEventRepository>();
            var mockUserRoleRepo = new Mock<IUserRoleRepository>();
            var mockHubContext = new Mock<IHubContext<UpdatesHub>>();

            var model = new IndexModel(mockTaskRepo.Object, mockEventRepo.Object, mockUserRoleRepo.Object, mockHubContext.Object);

            // Ustawienie użytkownika (bez roli admin)
            model.PageContext = new PageContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                    {
                new Claim(ClaimTypes.NameIdentifier, "user1")
            }))
                }
            };

            // Inicjalizacja TempData
            var tempDataProvider = new Mock<ITempDataProvider>().Object;
            model.TempData = new TempDataDictionary(model.PageContext.HttpContext, tempDataProvider);

            // Walidacja modelu zawiedzie bo brak wymaganych danych
            model.ModelState.AddModelError("Title", "Required");

            var result = await model.OnPostAsync();

            Assert.IsType<PageResult>(result);
            Assert.True(model.ModelState.ErrorCount > 0);
        }

        [Fact]
        public async Task OnPostAsync_WithEndDateBeforeStartDate_ShouldReturnPageWithModelError()
        {
            var mockTaskRepo = new Mock<ITaskRepository>();
            var mockEventRepo = new Mock<IEventRepository>();
            var mockUserRoleRepo = new Mock<IUserRoleRepository>();
            var mockHubContext = new Mock<IHubContext<UpdatesHub>>();

            var model = new IndexModel(mockTaskRepo.Object, mockEventRepo.Object, mockUserRoleRepo.Object, mockHubContext.Object)
            {
                Title = "Title",
                Description = "Desc",
                StartDate = System.DateTime.Now,
                EndDate = System.DateTime.Now.AddHours(-1),
                EventId = 1
            };

            // Ustawienie użytkownika (bez roli admin)
            model.PageContext = new PageContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                    {
                new Claim(ClaimTypes.NameIdentifier, "user1")
            }))
                }
            };

            // Inicjalizacja TempData
            var tempDataProvider = new Mock<ITempDataProvider>().Object;
            model.TempData = new TempDataDictionary(model.PageContext.HttpContext, tempDataProvider);

            var result = await model.OnPostAsync();

            Assert.IsType<PageResult>(result);
            Assert.True(model.ModelState.ContainsKey(nameof(model.EndDate)));
        }



        [Fact]
        public async Task OnPostAsync_WithInvalidEvent_ShouldReturnPageWithModelError()
        {
            var userId = "user1";
            var isAdmin = false;

            var mockTaskRepo = new Mock<ITaskRepository>();
            var mockEventRepo = new Mock<IEventRepository>();
            var mockUserRoleRepo = new Mock<IUserRoleRepository>();
            var mockHubContext = new Mock<IHubContext<UpdatesHub>>();

            mockEventRepo.Setup(r => r.GetEventByIdAsync(It.IsAny<int>(), userId, isAdmin))
                .ReturnsAsync((ScrumEvent)null);

            var model = new IndexModel(mockTaskRepo.Object, mockEventRepo.Object, mockUserRoleRepo.Object, mockHubContext.Object)
            {
                Title = "Title",
                Description = "Desc",
                StartDate = System.DateTime.Now,
                EndDate = System.DateTime.Now.AddHours(1),
                EventId = 1
            };

            model.PageContext = new PageContext
            {
                HttpContext = new DefaultHttpContext { User = CreateUserPrincipal(userId, isAdmin) }
            };
            InitializeTempData(model);

            var result = await model.OnPostAsync();

            Assert.IsType<PageResult>(result);
            Assert.True(model.ModelState.ContainsKey(nameof(model.EventId)));
        }
        [Fact]
        public async Task OnPostAsync_WithValidModel_ShouldAddTaskAndNotifyAdmins()
        {
            var userId = "user1";
            var isAdmin = false;
            var adminRoleId = "98954494-ef5f-4a06-87e4-22ef31417c9c";
            var adminIds = new List<string> { "admin1", "admin2" };
            var eventDate = System.DateTime.Now;

            var selectedEvent = new ScrumEvent
            {
                Id = 2,
                StartDate = eventDate,
                EndDate = eventDate.AddHours(2)
            };

            var mockTaskRepo = new Mock<ITaskRepository>();
            var mockEventRepo = new Mock<IEventRepository>();
            var mockUserRoleRepo = new Mock<IUserRoleRepository>();
            var mockClientProxy = new Mock<IClientProxy>();
            var fakeHubClients = new FakeHubClients(mockClientProxy.Object);
            var mockHubContext = new Mock<IHubContext<UpdatesHub>>();

            mockEventRepo.Setup(r => r.GetEventByIdAsync(2, userId, isAdmin)).ReturnsAsync(selectedEvent);
            mockTaskRepo.Setup(r => r.AddTaskAsync(It.IsAny<TaskItem>())).Returns(Task.CompletedTask);

            // Używamy It.IsAny<List<string>> i wymuszamy List<string> jako argument
            mockUserRoleRepo
                .Setup(r => r.GetUserIdsInRoleAsync(It.IsAny<string>()))
                .ReturnsAsync(adminIds);

            mockUserRoleRepo
                .Setup(r => r.GetUserIdsNotInRolesAsync(It.IsAny<List<string>>()))
                .ReturnsAsync(new List<string> { userId });

            mockHubContext.Setup(h => h.Clients).Returns(fakeHubClients);

            mockClientProxy.Setup(proxy =>
                proxy.SendCoreAsync(It.IsAny<string>(), It.IsAny<object[]>(), default))
                .Returns(Task.CompletedTask);

            var model = new IndexModel(mockTaskRepo.Object, mockEventRepo.Object, mockUserRoleRepo.Object, mockHubContext.Object)
            {
                Title = "New Task",
                Description = "Task Desc",
                StartDate = eventDate,
                EndDate = eventDate.AddHours(1),
                EventId = 2
            };

            model.PageContext = new PageContext
            {
                HttpContext = new DefaultHttpContext { User = CreateUserPrincipal(userId, isAdmin) }
            };
            InitializeTempData(model);

            var result = await model.OnPostAsync();

            Assert.IsType<RedirectToPageResult>(result);
            mockTaskRepo.Verify(r => r.AddTaskAsync(It.IsAny<TaskItem>()), Times.Once);
            mockUserRoleRepo.Verify(r => r.GetUserIdsInRoleAsync(It.IsAny<string>()), Times.Once);
            mockUserRoleRepo.Verify(r => r.GetUserIdsNotInRolesAsync(It.Is<List<string>>(l => true)), Times.Once);
            mockClientProxy.Verify(proxy => proxy.SendCoreAsync(It.IsAny<string>(), It.IsAny<object[]>(), default), Times.Once);
        }

        [Fact]
        public async Task OnPostToggleDoneAsync_WithValidTask_ShouldToggleAndNotify()
        {
            var userId = "user1";
            var isAdmin = true;
            var adminRoleId = "adminRoleId";
            var adminIds = new List<string> { "admin1", "admin2" };
            var taskId = 1;

            var task = new TaskItem
            {
                Id = taskId,
                UserId = userId,
                User = new IdentityUser { Id = userId, UserName = "TestUser" },
                IsDone = false,
                StartDate = System.DateTime.Now,
                EndDate = System.DateTime.Now.AddHours(1)
            };

            var mockTaskRepo = new Mock<ITaskRepository>();
            var mockEventRepo = new Mock<IEventRepository>();
            var mockUserRoleRepo = new Mock<IUserRoleRepository>();
            var mockClientProxy = new Mock<IClientProxy>();
            var fakeHubClients = new FakeHubClients(mockClientProxy.Object);
            var mockHubContext = new Mock<IHubContext<UpdatesHub>>();

            mockTaskRepo.Setup(r => r.GetTaskByIdAsync(taskId, userId, isAdmin)).ReturnsAsync(task);
            mockTaskRepo.Setup(r => r.UpdateTaskAsync(task)).Returns(Task.CompletedTask);
            mockUserRoleRepo.Setup(r => r.GetUserIdsInRoleAsync(adminRoleId)).ReturnsAsync(adminIds);
            mockHubContext.Setup(h => h.Clients).Returns(fakeHubClients);

            mockClientProxy.Setup(proxy => proxy.SendCoreAsync(It.IsAny<string>(), It.IsAny<object[]>(), default))
                .Returns(Task.CompletedTask);

            var model = new IndexModel(mockTaskRepo.Object, mockEventRepo.Object, mockUserRoleRepo.Object, mockHubContext.Object);

            model.PageContext = new PageContext
            {
                HttpContext = new DefaultHttpContext { User = CreateUserPrincipal(userId, isAdmin) }
            };
            InitializeTempData(model);

            var result = await model.OnPostToggleDoneAsync(taskId);

            Assert.IsType<RedirectToPageResult>(result);
            mockTaskRepo.Verify(r => r.UpdateTaskAsync(task), Times.Once);
            mockClientProxy.Verify(c => c.SendCoreAsync(It.IsAny<string>(), It.IsAny<object[]>(), default), Times.AtLeastOnce);
        }

        [Fact]
        public async Task OnPostDeleteAsync_WithValidTask_ShouldDeleteAndNotify()
        {
            var userId = "user1";
            var isAdmin = true;
            var adminRoleId = "adminRoleId";
            var adminIds = new List<string> { "admin1", "admin2" };
            var taskId = 1;

            var task = new TaskItem
            {
                Id = taskId,
                UserId = userId,
                IsDone = false
            };

            var mockTaskRepo = new Mock<ITaskRepository>();
            var mockEventRepo = new Mock<IEventRepository>();
            var mockUserRoleRepo = new Mock<IUserRoleRepository>();
            var mockClientProxy = new Mock<IClientProxy>();
            var fakeHubClients = new FakeHubClients(mockClientProxy.Object);
            var mockHubContext = new Mock<IHubContext<UpdatesHub>>();

            mockTaskRepo.Setup(r => r.GetTaskByIdAsync(taskId, userId, isAdmin)).ReturnsAsync(task);
            mockTaskRepo.Setup(r => r.DeleteTaskAsync(task)).Returns(Task.CompletedTask);
            mockUserRoleRepo.Setup(r => r.GetUserIdsInRoleAsync(It.IsAny<string>())).ReturnsAsync(adminIds);
            mockHubContext.Setup(h => h.Clients).Returns(fakeHubClients);

            mockClientProxy.Setup(proxy => proxy.SendCoreAsync(It.IsAny<string>(), It.IsAny<object[]>(), default))
                .Returns(Task.CompletedTask);

            var model = new IndexModel(mockTaskRepo.Object, mockEventRepo.Object, mockUserRoleRepo.Object, mockHubContext.Object);

            model.PageContext = new PageContext
            {
                HttpContext = new DefaultHttpContext { User = CreateUserPrincipal(userId, isAdmin) }
            };
            InitializeTempData(model);

            var result = await model.OnPostDeleteAsync(taskId);

            Assert.IsType<RedirectToPageResult>(result);
            mockTaskRepo.Verify(r => r.DeleteTaskAsync(task), Times.Once);
            mockClientProxy.Verify(c => c.SendCoreAsync(It.IsAny<string>(), It.IsAny<object[]>(), default), Times.Once);
        }
    }
}