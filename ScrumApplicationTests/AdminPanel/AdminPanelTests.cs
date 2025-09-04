using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.SignalR;
using Moq;
using ScrumApplication.Models;

namespace ScrumApplicationTests
{
    public class AdminPanelTests
    {
        private Mock<UserManager<IdentityUser>> CreateUserManagerMock()
        {
            var store = new Mock<IUserStore<IdentityUser>>();
            return new Mock<UserManager<IdentityUser>>(store.Object, null, null, null, null, null, null, null, null);
        }

        private Mock<IEventRepository> CreateEventRepoMock()
        {
            return new Mock<IEventRepository>();
        }

        private Mock<ITaskRepository> CreateTaskRepoMock()
        {
            return new Mock<ITaskRepository>();
        }

        private class FakeHubClients : IHubClients
        {
            private readonly IClientProxy _clientProxy;
            public FakeHubClients(IClientProxy clientProxy) => _clientProxy = clientProxy;
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

        private Mock<IClientProxy> CreateClientProxyMock()
        {
            var mock = new Mock<IClientProxy>();
            mock.Setup(p => p.SendCoreAsync(It.IsAny<string>(), It.IsAny<object[]>(), default))
                .Returns(Task.CompletedTask);
            return mock;
        }

        [Fact]
        public async Task OnGetAsync_ShouldLoadNonAdminUsers()
        {
            var userManagerMock = CreateUserManagerMock();

            var users = new List<IdentityUser>
        {
            new IdentityUser { Id = "1", UserName = "user1" },
            new IdentityUser { Id = "2", UserName = "adminUser" }
        }.AsQueryable();

            userManagerMock.Setup(u => u.Users).Returns(users);
            userManagerMock.Setup(u => u.GetRolesAsync(It.Is<IdentityUser>(usr => usr.Id == "1")))
                           .ReturnsAsync(new List<string> { "User" });
            userManagerMock.Setup(u => u.GetRolesAsync(It.Is<IdentityUser>(usr => usr.Id == "2")))
                           .ReturnsAsync(new List<string> { "Admin" });

            var eventRepoMock = CreateEventRepoMock();
            var taskRepoMock = CreateTaskRepoMock();

            var clientProxyMock = CreateClientProxyMock();
            var hubClients = new FakeHubClients(clientProxyMock.Object);
            var hubContextMock = new Mock<IHubContext<UpdatesHub>>();
            hubContextMock.Setup(h => h.Clients).Returns(hubClients);

            var model = new AdminPanelModel(userManagerMock.Object, eventRepoMock.Object, taskRepoMock.Object, hubContextMock.Object);

            await model.OnGetAsync();

            Assert.Single(model.Users);
            Assert.Equal("user1", model.Users[0].UserName);
            Assert.DoesNotContain(model.Users, u => u.UserName == "adminUser");
        }

        [Fact]
        public async Task OnPostForcePasswordResetAsync_InvalidUserId_ReturnsPageWithError()
        {
            var userManagerMock = CreateUserManagerMock();
            var eventRepoMock = CreateEventRepoMock();
            var taskRepoMock = CreateTaskRepoMock();
            var clientProxyMock = CreateClientProxyMock();
            var hubClients = new FakeHubClients(clientProxyMock.Object);
            var hubContextMock = new Mock<IHubContext<UpdatesHub>>();
            hubContextMock.Setup(h => h.Clients).Returns(hubClients);

            var model = new AdminPanelModel(userManagerMock.Object, eventRepoMock.Object, taskRepoMock.Object, hubContextMock.Object);

            var result = await model.OnPostForcePasswordResetAsync(null);

            Assert.False(model.ModelState.IsValid);
            var pageResult = Assert.IsType<PageResult>(result);
            Assert.True(model.ModelState.ContainsKey(string.Empty));
            Assert.Contains(model.ModelState[string.Empty].Errors, e => e.ErrorMessage == "Id użytkownika jest wymagane.");
        }

        [Fact]
        public async Task OnPostForcePasswordResetAsync_UserNotFound_ReturnsPageWithError()
        {
            var userManagerMock = CreateUserManagerMock();
            userManagerMock.Setup(u => u.FindByIdAsync(It.IsAny<string>())).ReturnsAsync((IdentityUser)null);

            var eventRepoMock = CreateEventRepoMock();
            var taskRepoMock = CreateTaskRepoMock();
            var clientProxyMock = CreateClientProxyMock();
            var hubClients = new FakeHubClients(clientProxyMock.Object);
            var hubContextMock = new Mock<IHubContext<UpdatesHub>>();
            hubContextMock.Setup(h => h.Clients).Returns(hubClients);

            var model = new AdminPanelModel(userManagerMock.Object, eventRepoMock.Object, taskRepoMock.Object, hubContextMock.Object);

            var result = await model.OnPostForcePasswordResetAsync("user1");

            Assert.False(model.ModelState.IsValid);
            var pageResult = Assert.IsType<PageResult>(result);
            Assert.True(model.ModelState.ContainsKey(string.Empty));
            Assert.Contains(model.ModelState[string.Empty].Errors, e => e.ErrorMessage == "Użytkownik nie znaleziony.");
        }

        [Fact]
        public async Task OnPostDeleteUserAsync_UserNotFound_ShouldRedirectWithError()
        {
            var userManagerMock = CreateUserManagerMock();
            userManagerMock.Setup(u => u.FindByIdAsync(It.IsAny<string>())).ReturnsAsync((IdentityUser)null);

            var eventRepoMock = CreateEventRepoMock();
            var taskRepoMock = CreateTaskRepoMock();
            var clientProxyMock = CreateClientProxyMock();
            var hubClients = new FakeHubClients(clientProxyMock.Object);
            var hubContextMock = new Mock<IHubContext<UpdatesHub>>();
            hubContextMock.Setup(h => h.Clients).Returns(hubClients);

            var model = new AdminPanelModel(userManagerMock.Object, eventRepoMock.Object, taskRepoMock.Object, hubContextMock.Object);

            // Inicjalizacja TempData - bardzo ważne
            var httpContext = new DefaultHttpContext();
            var tempDataProvider = new Mock<ITempDataProvider>().Object;
            model.TempData = new TempDataDictionary(httpContext, tempDataProvider);

            var result = await model.OnPostDeleteUserAsync("userid");

            var redirectResult = Assert.IsType<RedirectToPageResult>(result);
            Assert.Null(redirectResult.PageName);
            Assert.Equal("Użytkownik nie istnieje.", model.TempData["ErrorMessage"]);
        }

        [Fact]
        public async Task OnPostDeleteUserAsync_ShouldDeleteUserAndRelatedDataAndLogout()
        {
            var userManagerMock = CreateUserManagerMock();
            var user = new IdentityUser { Id = "userid", UserName = "user1" };
            userManagerMock.Setup(u => u.FindByIdAsync(user.Id)).ReturnsAsync(user);
            userManagerMock.Setup(u => u.DeleteAsync(user)).ReturnsAsync(IdentityResult.Success);

            var events = new List<ScrumEvent>
    {
        new ScrumEvent { Id = 1, UserId = user.Id },
        new ScrumEvent { Id = 2, UserId = user.Id }
    };

            var tasks = new List<TaskItem>
    {
        new TaskItem { Id = 1, UserId = user.Id },
        new TaskItem { Id = 2, UserId = user.Id }
    };

            var eventRepoMock = CreateEventRepoMock();
            eventRepoMock.Setup(r => r.GetEventsAsync(user.Id, true)).ReturnsAsync(events);
            eventRepoMock.Setup(r => r.DeleteEventAsync(It.IsAny<ScrumEvent>())).Returns(Task.CompletedTask);

            var taskRepoMock = CreateTaskRepoMock();
            taskRepoMock.Setup(r => r.GetTasksAsync(user.Id, true)).ReturnsAsync(tasks);
            taskRepoMock.Setup(r => r.DeleteTaskAsync(It.IsAny<TaskItem>())).Returns(Task.CompletedTask);

            // Mock IClientProxy z SendCoreAsync poprawnie zwracającym Task
            var clientProxyMock = new Mock<IClientProxy>();
            clientProxyMock.Setup(p => p.SendCoreAsync("ForceLogoutWithToast", It.IsAny<object[]>(), default))
                .Returns(Task.CompletedTask);

            // Mock IHubClients, który dla User(user.Id) zwróci mock clientProxy
            var fakeHubClientsMock = new Mock<IHubClients>();
            fakeHubClientsMock.Setup(c => c.User(user.Id)).Returns(clientProxyMock.Object);

            var hubContextMock = new Mock<IHubContext<UpdatesHub>>();
            hubContextMock.Setup(h => h.Clients).Returns(fakeHubClientsMock.Object);

            var model = new AdminPanelModel(userManagerMock.Object, eventRepoMock.Object, taskRepoMock.Object, hubContextMock.Object);

            // Inicjalizacja TempData
            var httpContext = new DefaultHttpContext();
            var tempDataProvider = new Mock<ITempDataProvider>().Object;
            model.TempData = new TempDataDictionary(httpContext, tempDataProvider);

            var result = await model.OnPostDeleteUserAsync(user.Id);

            userManagerMock.Verify(u => u.DeleteAsync(user), Times.Once);
            eventRepoMock.Verify(r => r.DeleteEventAsync(It.IsAny<ScrumEvent>()), Times.Exactly(events.Count));
            taskRepoMock.Verify(r => r.DeleteTaskAsync(It.IsAny<TaskItem>()), Times.Exactly(tasks.Count));
            clientProxyMock.Verify(p => p.SendCoreAsync("ForceLogoutWithToast", It.IsAny<object[]>(), default), Times.Once);

            var redirectResult = Assert.IsType<RedirectToPageResult>(result);
            Assert.Null(redirectResult.PageName);
            Assert.Equal($"Użytkownik {user.UserName} wraz z powiązanymi danymi został usunięty.", model.TempData["SuccessMessage"]);
        }
        public class TestUrlHelper : IUrlHelper
        {
            public ActionContext ActionContext { get; }

            public TestUrlHelper()
            {
                ActionContext = new ActionContext
                {
                    HttpContext = new DefaultHttpContext(),
                    RouteData = new RouteData(),
                    ActionDescriptor = new Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor()
                };
            }

            public string Page(string pageName, string pageHandler, object values, string protocol)
                => "https://example.com/resetpassword?userId=user1&token=token";

            public string RouteUrl(UrlRouteContext routeContext) => "https://example.com/resetpassword?userId=user1&token=token";

            // Pozostałe metody niezaimplementowane (throw NotImplementedException)
            public string Action(UrlActionContext actionContext) => throw new NotImplementedException();
            public string Content(string contentPath) => throw new NotImplementedException();
            public bool IsLocalUrl(string url) => throw new NotImplementedException();
            public string Link(string routeName, object values) => throw new NotImplementedException();
            public string RouteUrl(string routeName, object values, string protocol, string host, string fragment) => throw new NotImplementedException();
        }
        [Fact]
        public async Task OnPostForcePasswordResetAsync_ShouldSendSignalRNotificationAndRedirect()
        {
            var userManagerMock = CreateUserManagerMock();
            var user = new IdentityUser { Id = "user1", UserName = "user1" };
            userManagerMock.Setup(u => u.FindByIdAsync("user1")).ReturnsAsync(user);
            userManagerMock.Setup(u => u.GeneratePasswordResetTokenAsync(user)).ReturnsAsync("token");

            var eventRepoMock = CreateEventRepoMock();
            var taskRepoMock = CreateTaskRepoMock();

            var clientProxyMock = CreateClientProxyMock();
            var hubClients = new FakeHubClients(clientProxyMock.Object);
            var hubContextMock = new Mock<IHubContext<UpdatesHub>>();
            hubContextMock.Setup(h => h.Clients).Returns(hubClients);

            var model = new AdminPanelModel(userManagerMock.Object, eventRepoMock.Object, taskRepoMock.Object, hubContextMock.Object);

            // Inicjalizacja TempData
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Scheme = "https";
            var tempDataProvider = new Mock<ITempDataProvider>().Object;
            model.TempData = new TempDataDictionary(httpContext, tempDataProvider);

            // Ustawienie PageContext
            model.PageContext = new PageContext { HttpContext = httpContext };

            // Przypisanie niestandardowego UrlHelper z działającą metodą Page()
            model.Url = new TestUrlHelper();

            var result = await model.OnPostForcePasswordResetAsync("user1");

            clientProxyMock.Verify(p => p.SendCoreAsync("ForcePasswordReset", It.IsAny<object[]>(), default), Times.Once);

            var redirectResult = Assert.IsType<RedirectToPageResult>(result);
            Assert.Null(redirectResult.PageName);
            Assert.Equal($"Wymuszono reset hasła dla użytkownika {user.UserName}.", model.TempData["SuccessMessage"]);
        }

    }
}
