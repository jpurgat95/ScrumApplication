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
    public class EditEventModelTests
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

        // Helper do ustawienia TempData w modelu
        private void InitializeTempData(PageModel model)
        {
            var httpContext = new DefaultHttpContext();
            var tempDataProvider = new Mock<ITempDataProvider>().Object;
            var tempData = new TempDataDictionary(httpContext, tempDataProvider);
            model.TempData = tempData;
        }

        // FakeHubClients jak wcześniej, do mockowania IHubClients
        public class FakeHubClients : IHubClients
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

        [Fact]
        public async Task OnGetAsync_WithValidId_ShouldLoadEvent()
        {
            var ev = new ScrumEvent
            {
                Id = 1,
                Title = "Title1",
                Description = "Desc1",
                StartDate = System.DateTime.Now,
                EndDate = System.DateTime.Now.AddHours(1)
            };

            var mockEventRepo = new Mock<IEventRepository>();
            var mockUserRoleRepo = new Mock<IUserRoleRepository>();
            var mockHubContext = new Mock<IHubContext<UpdatesHub>>();

            var userId = "user1";
            var isAdmin = false;

            mockEventRepo.Setup(r => r.GetEventByIdAsync(ev.Id, userId, isAdmin)).ReturnsAsync(ev);

            var model = new EditEventModel(mockEventRepo.Object, mockUserRoleRepo.Object, mockHubContext.Object);

            model.PageContext = new PageContext()
            {
                HttpContext = new DefaultHttpContext { User = CreateUserPrincipal(userId, isAdmin) }
            };
            InitializeTempData(model);

            var result = await model.OnGetAsync(ev.Id);

            Assert.IsType<PageResult>(result);
            Assert.Equal(ev.Id, model.Id);
            Assert.Equal(ev.Title, model.Title);
            Assert.Equal(ev.Description, model.Description);
            Assert.Equal(ev.StartDate, model.StartDate);
            Assert.Equal(ev.EndDate, model.EndDate);
        }

        [Fact]
        public async Task OnGetAsync_WithInvalidId_ShouldReturnNotFound()
        {
            var mockEventRepo = new Mock<IEventRepository>();
            var mockUserRoleRepo = new Mock<IUserRoleRepository>();
            var mockHubContext = new Mock<IHubContext<UpdatesHub>>();

            mockEventRepo.Setup(r => r.GetEventByIdAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<bool>()))
                .ReturnsAsync((ScrumEvent)null);

            var model = new EditEventModel(mockEventRepo.Object, mockUserRoleRepo.Object, mockHubContext.Object);

            model.PageContext = new PageContext()
            {
                HttpContext = new DefaultHttpContext { User = CreateUserPrincipal("user1", false) }
            };
            InitializeTempData(model);

            var result = await model.OnGetAsync(999);

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task OnPostAsync_WithInvalidModel_ShouldReturnPage()
        {
            var mockEventRepo = new Mock<IEventRepository>();
            var mockUserRoleRepo = new Mock<IUserRoleRepository>();
            var mockHubContext = new Mock<IHubContext<UpdatesHub>>();

            var model = new EditEventModel(mockEventRepo.Object, mockUserRoleRepo.Object, mockHubContext.Object);

            // Nie ustawiamy wymaganych pól, więc model jest niepoprawny
            model.ModelState.AddModelError("Title", "Required");

            var result = await model.OnPostAsync();

            Assert.IsType<PageResult>(result);
        }

        [Fact]
        public async Task OnPostAsync_WithEndDateBeforeStartDate_ShouldReturnPageWithModelError()
        {
            var mockEventRepo = new Mock<IEventRepository>();
            var mockUserRoleRepo = new Mock<IUserRoleRepository>();
            var mockHubContext = new Mock<IHubContext<UpdatesHub>>();

            var model = new EditEventModel(mockEventRepo.Object, mockUserRoleRepo.Object, mockHubContext.Object)
            {
                Id = 1,
                Title = "title",
                Description = "desc",
                StartDate = System.DateTime.Now,
                EndDate = System.DateTime.Now.AddHours(-1) // EndDate przed StartDate
            };

            var result = await model.OnPostAsync();

            Assert.False(model.ModelState.IsValid);
            Assert.True(model.ModelState.ContainsKey(nameof(model.EndDate)));
            Assert.IsType<PageResult>(result);
        }

        [Fact]
        public async Task OnPostAsync_WithValidModelAsAdmin_ShouldUpdateEventAndNotifyUser()
        {
            var ev = new ScrumEvent
            {
                Id = 1,
                UserId = "user1",
                User = new IdentityUser { Id = "user1", UserName = "TestUser" },
                Title = "Old Title",
                Description = "Old Desc",
                StartDate = System.DateTime.Now,
                EndDate = System.DateTime.Now.AddHours(1),
                IsDone = false
            };

            var mockEventRepo = new Mock<IEventRepository>();
            var mockUserRoleRepo = new Mock<IUserRoleRepository>();
            var mockClientProxy = new Mock<IClientProxy>();
            var fakeHubClients = new FakeHubClients(mockClientProxy.Object);
            var mockHubContext = new Mock<IHubContext<UpdatesHub>>();

            var userId = ev.UserId;
            var adminRoleId = "98954494-ef5f-4a06-87e4-22ef31417c9c";
            var adminIds = new List<string> { "admin1", "admin2" };

            mockEventRepo.Setup(r => r.GetEventByIdAsync(ev.Id, userId, true)).ReturnsAsync(ev);
            mockEventRepo.Setup(r => r.UpdateEventAsync(ev)).Returns(Task.CompletedTask);
            mockUserRoleRepo.Setup(r => r.GetUserIdsInRoleAsync(adminRoleId)).ReturnsAsync(adminIds);
            mockClientProxy.Setup(proxy => proxy.SendCoreAsync(It.IsAny<string>(), It.IsAny<object[]>(), default))
                .Returns(Task.CompletedTask);
            mockHubContext.Setup(hub => hub.Clients).Returns(fakeHubClients);

            var model = new EditEventModel(mockEventRepo.Object, mockUserRoleRepo.Object, mockHubContext.Object)
            {
                Id = ev.Id,
                Title = "New Title",
                Description = "New Desc",
                StartDate = ev.StartDate,
                EndDate = ev.EndDate
            };

            model.PageContext = new PageContext()
            {
                HttpContext = new DefaultHttpContext()
                {
                    User = CreateUserPrincipal(userId, true)
                }
            };
            InitializeTempData(model);

            var result = await model.OnPostAsync();

            Assert.IsType<RedirectToPageResult>(result);
            mockEventRepo.Verify(r => r.UpdateEventAsync(It.Is<ScrumEvent>(e => e.Title == "New Title" && e.Description == "New Desc")), Times.Once);
            mockClientProxy.Verify(proxy => proxy.SendCoreAsync(
                "EventUpdated",
                It.IsAny<object[]>(),
                default), Times.Once);
        }

        [Fact]
        public async Task OnPostAsync_WithValidModelAsUser_ShouldUpdateEventAndNotifyAdmins()
        {
            var ev = new ScrumEvent
            {
                Id = 1,
                UserId = "user1",
                User = new IdentityUser { Id = "user1", UserName = "TestUser" },
                Title = "Old Title",
                Description = "Old Desc",
                StartDate = System.DateTime.Now,
                EndDate = System.DateTime.Now.AddHours(1),
                IsDone = false
            };

            var mockEventRepo = new Mock<IEventRepository>();
            var mockUserRoleRepo = new Mock<IUserRoleRepository>();
            var mockClientProxy = new Mock<IClientProxy>();
            var fakeHubClients = new FakeHubClients(mockClientProxy.Object);
            var mockHubContext = new Mock<IHubContext<UpdatesHub>>();

            var userId = ev.UserId;
            var adminRoleId = "98954494-ef5f-4a06-87e4-22ef31417c9c";
            var adminIds = new List<string> { "admin1", "admin2" };

            mockEventRepo.Setup(r => r.GetEventByIdAsync(ev.Id, userId, false)).ReturnsAsync(ev);
            mockEventRepo.Setup(r => r.UpdateEventAsync(ev)).Returns(Task.CompletedTask);
            mockUserRoleRepo.Setup(r => r.GetUserIdsInRoleAsync(adminRoleId)).ReturnsAsync(adminIds);
            mockClientProxy.Setup(proxy => proxy.SendCoreAsync(It.IsAny<string>(), It.IsAny<object[]>(), default))
                .Returns(Task.CompletedTask);
            mockHubContext.Setup(hub => hub.Clients).Returns(fakeHubClients);

            var model = new EditEventModel(mockEventRepo.Object, mockUserRoleRepo.Object, mockHubContext.Object)
            {
                Id = ev.Id,
                Title = "New Title",
                Description = "New Desc",
                StartDate = ev.StartDate,
                EndDate = ev.EndDate
            };

            model.PageContext = new PageContext()
            {
                HttpContext = new DefaultHttpContext()
                {
                    User = CreateUserPrincipal(userId, false)
                }
            };
            InitializeTempData(model);

            var result = await model.OnPostAsync();

            Assert.IsType<RedirectToPageResult>(result);
            mockEventRepo.Verify(r => r.UpdateEventAsync(It.Is<ScrumEvent>(e => e.Title == "New Title" && e.Description == "New Desc")), Times.Once);
            mockClientProxy.Verify(proxy => proxy.SendCoreAsync(
                "EventUpdated",
                It.IsAny<object[]>(),
                default), Times.Once);
        }
    }
}