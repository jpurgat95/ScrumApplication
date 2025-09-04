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
    public class EditTasksModelTests
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
        public async Task OnGetAsync_ExistingTaskAndNotDoneEvent_ShouldPopulateProperties()
        {
            var userId = "user1";
            var isAdmin = false;

            var scrumEvent = new ScrumEvent { Id = 100, IsDone = false };

            var task = new TaskItem
            {
                Id = 1,
                Title = "Task 1",
                Description = "Desc 1",
                StartDate = System.DateTime.Now,
                EndDate = System.DateTime.Now.AddHours(1),
                ScrumEvent = scrumEvent
            };

            var mockTaskRepo = new Mock<ITaskRepository>();
            var mockUserRoleRepo = new Mock<IUserRoleRepository>();
            var mockHubContext = new Mock<IHubContext<UpdatesHub>>();

            mockTaskRepo.Setup(r => r.GetTaskByIdAsync(task.Id, userId, isAdmin)).ReturnsAsync(task);

            var model = new EditModel(mockTaskRepo.Object, mockUserRoleRepo.Object, mockHubContext.Object);

            model.PageContext = new PageContext
            {
                HttpContext = new DefaultHttpContext { User = CreateUserPrincipal(userId, isAdmin) }
            };
            InitializeTempData(model);

            var result = await model.OnGetAsync(task.Id);

            Assert.IsType<PageResult>(result);
            Assert.Equal(task.Id, model.Id);
            Assert.Equal(task.Title, model.Title);
            Assert.Equal(task.Description, model.Description);
            Assert.Equal(task.StartDate, model.StartDate);
            Assert.Equal(task.EndDate, model.EndDate);
        }

        [Fact]
        public async Task OnGetAsync_TaskWithDoneEvent_ShouldRedirectWithWarning()
        {
            var userId = "user1";
            var isAdmin = false;

            var scrumEvent = new ScrumEvent { Id = 100, IsDone = true };

            var task = new TaskItem
            {
                Id = 1,
                ScrumEvent = scrumEvent
            };

            var mockTaskRepo = new Mock<ITaskRepository>();
            var mockUserRoleRepo = new Mock<IUserRoleRepository>();
            var mockHubContext = new Mock<IHubContext<UpdatesHub>>();

            mockTaskRepo.Setup(r => r.GetTaskByIdAsync(task.Id, userId, isAdmin)).ReturnsAsync(task);

            var model = new EditModel(mockTaskRepo.Object, mockUserRoleRepo.Object, mockHubContext.Object);

            model.PageContext = new PageContext
            {
                HttpContext = new DefaultHttpContext { User = CreateUserPrincipal(userId, isAdmin) }
            };
            InitializeTempData(model);

            var result = await model.OnGetAsync(task.Id);

            var redirectResult = Assert.IsType<RedirectToPageResult>(result);
            Assert.Equal("/Tasks/Index", redirectResult.PageName);
            Assert.Equal("Nie można edytować zadania, ponieważ powiązane wydarzenie zostało oznaczone jako wykonane.", model.TempData["ToastMessage"]);
            Assert.Equal("warning", model.TempData["ToastType"]);
        }

        [Fact]
        public async Task OnPostAsync_InvalidModel_ShouldReturnPage()
        {
            var mockTaskRepo = new Mock<ITaskRepository>();
            var mockUserRoleRepo = new Mock<IUserRoleRepository>();
            var mockHubContext = new Mock<IHubContext<UpdatesHub>>();

            var model = new EditModel(mockTaskRepo.Object, mockUserRoleRepo.Object, mockHubContext.Object);

            model.ModelState.AddModelError("Title", "Required");

            var result = await model.OnPostAsync();

            Assert.IsType<PageResult>(result);
        }

        [Fact]
        public async Task OnPostAsync_EndDateBeforeStartDate_ShouldReturnPageWithModelError()
        {
            var mockTaskRepo = new Mock<ITaskRepository>();
            var mockUserRoleRepo = new Mock<IUserRoleRepository>();
            var mockHubContext = new Mock<IHubContext<UpdatesHub>>();

            var model = new EditModel(mockTaskRepo.Object, mockUserRoleRepo.Object, mockHubContext.Object)
            {
                Id = 1,
                Title = "title",
                Description = "desc",
                StartDate = System.DateTime.Now,
                EndDate = System.DateTime.Now.AddHours(-1),
            };

            var result = await model.OnPostAsync();

            Assert.False(model.ModelState.IsValid);
            Assert.True(model.ModelState.ContainsKey(nameof(model.EndDate)));
            Assert.IsType<PageResult>(result);
        }

        [Fact]
        public async Task OnPostAsync_ValidModelAsAdmin_ShouldUpdateTaskAndNotifyUser()
        {
            var userId = "user1";
            var isAdmin = true;
            var adminRoleId = "98954494-ef5f-4a06-87e4-22ef31417c9c";
            var adminIds = new List<string> { "admin1", "admin2" };

            var task = new TaskItem
            {
                Id = 1,
                UserId = userId,
                User = new IdentityUser { Id = userId, UserName = "TestUser" },
                Title = "Old Title",
                Description = "Old Desc",
                StartDate = System.DateTime.Now,
                EndDate = System.DateTime.Now.AddHours(1),
                IsDone = false
            };

            var mockTaskRepo = new Mock<ITaskRepository>();
            var mockUserRoleRepo = new Mock<IUserRoleRepository>();
            var mockClientProxy = new Mock<IClientProxy>();
            var fakeHubClients = new FakeHubClients(mockClientProxy.Object);
            var mockHubContext = new Mock<IHubContext<UpdatesHub>>();

            mockTaskRepo.Setup(r => r.GetTaskByIdAsync(task.Id, userId, isAdmin)).ReturnsAsync(task);
            mockTaskRepo.Setup(r => r.UpdateTaskAsync(task)).Returns(Task.CompletedTask);
            mockUserRoleRepo.Setup(r => r.GetUserIdsInRoleAsync(adminRoleId)).ReturnsAsync(adminIds);
            mockClientProxy.Setup(proxy => proxy.SendCoreAsync(It.IsAny<string>(), It.IsAny<object[]>(), default)).Returns(Task.CompletedTask);
            mockHubContext.Setup(hub => hub.Clients).Returns(fakeHubClients);

            var model = new EditModel(mockTaskRepo.Object, mockUserRoleRepo.Object, mockHubContext.Object)
            {
                Id = task.Id,
                Title = "New Title",
                Description = "New Desc",
                StartDate = task.StartDate,
                EndDate = task.EndDate
            };

            model.PageContext = new PageContext
            {
                HttpContext = new DefaultHttpContext { User = CreateUserPrincipal(userId, isAdmin) }
            };

            InitializeTempData(model);

            var result = await model.OnPostAsync();

            Assert.IsType<RedirectToPageResult>(result);
            mockTaskRepo.Verify(r => r.UpdateTaskAsync(It.Is<TaskItem>(t => t.Title == "New Title" && t.Description == "New Desc")), Times.Once);
            mockClientProxy.Verify(proxy => proxy.SendCoreAsync("TaskUpdated", It.IsAny<object[]>(), default), Times.Once);
        }

        [Fact]
        public async Task OnPostAsync_ValidModelAsUser_ShouldUpdateTaskAndNotifyAdmins()
        {
            var userId = "user1";
            var isAdmin = false;
            var adminRoleId = "98954494-ef5f-4a06-87e4-22ef31417c9c";
            var adminIds = new List<string> { "admin1", "admin2" };

            var task = new TaskItem
            {
                Id = 1,
                UserId = userId,
                User = new IdentityUser { Id = userId, UserName = "TestUser" },
                Title = "Old Title",
                Description = "Old Desc",
                StartDate = System.DateTime.Now,
                EndDate = System.DateTime.Now.AddHours(1),
                IsDone = false
            };

            var mockTaskRepo = new Mock<ITaskRepository>();
            var mockUserRoleRepo = new Mock<IUserRoleRepository>();
            var mockClientProxy = new Mock<IClientProxy>();
            var fakeHubClients = new FakeHubClients(mockClientProxy.Object);
            var mockHubContext = new Mock<IHubContext<UpdatesHub>>();

            mockTaskRepo.Setup(r => r.GetTaskByIdAsync(task.Id, userId, isAdmin)).ReturnsAsync(task);
            mockTaskRepo.Setup(r => r.UpdateTaskAsync(task)).Returns(Task.CompletedTask);
            mockUserRoleRepo.Setup(r => r.GetUserIdsInRoleAsync(adminRoleId)).ReturnsAsync(adminIds);
            mockClientProxy.Setup(proxy => proxy.SendCoreAsync(It.IsAny<string>(), It.IsAny<object[]>(), default)).Returns(Task.CompletedTask);
            mockHubContext.Setup(hub => hub.Clients).Returns(fakeHubClients);

            var model = new EditModel(mockTaskRepo.Object, mockUserRoleRepo.Object, mockHubContext.Object)
            {
                Id = task.Id,
                Title = "New Title",
                Description = "New Desc",
                StartDate = task.StartDate,
                EndDate = task.EndDate
            };

            model.PageContext = new PageContext
            {
                HttpContext = new DefaultHttpContext { User = CreateUserPrincipal(userId, isAdmin) }
            };

            InitializeTempData(model);

            var result = await model.OnPostAsync();

            Assert.IsType<RedirectToPageResult>(result);
            mockTaskRepo.Verify(r => r.UpdateTaskAsync(It.Is<TaskItem>(t => t.Title == "New Title" && t.Description == "New Desc")), Times.Once);
            mockClientProxy.Verify(proxy => proxy.SendCoreAsync("TaskUpdated", It.IsAny<object[]>(), default), Times.Once);
        }
    }
}