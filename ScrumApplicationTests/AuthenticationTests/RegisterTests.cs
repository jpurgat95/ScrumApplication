using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.SignalR;
using Moq;
using ScrumApplication.Data;
using ScrumApplication.Models;
using ScrumApplication.Pages; // dostosuj namespace
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;
namespace ScrumApplicationTests
{
    public class RegisterTests
    {
        // Helpers do mockowania UserManager i SignInManager
        private Mock<UserManager<IdentityUser>> CreateUserManagerMock()
        {
            var store = new Mock<IUserStore<IdentityUser>>();
            var mgr = new Mock<UserManager<IdentityUser>>(store.Object, null, null, null, null, null, null, null, null);
            mgr.Setup(x => x.CreateAsync(It.IsAny<IdentityUser>(), It.IsAny<string>())).ReturnsAsync(IdentityResult.Success);
            mgr.Setup(x => x.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync((IdentityUser)null);
            return mgr;
        }

        private Mock<SignInManager<IdentityUser>> CreateSignInManagerMock(Mock<UserManager<IdentityUser>> userManager)
        {
            var contextAccessor = new Mock<IHttpContextAccessor>();
            var claimsFactory = new Mock<IUserClaimsPrincipalFactory<IdentityUser>>();
            return new Mock<SignInManager<IdentityUser>>(userManager.Object, contextAccessor.Object, claimsFactory.Object, null, null, null, null);
        }

        private void InitializeTempData(PageModel model)
        {
            var httpContext = new DefaultHttpContext();
            var tempDataProvider = new Mock<ITempDataProvider>().Object;
            model.TempData = new TempDataDictionary(httpContext, tempDataProvider);
        }

        // FakeHubClients do mockowania IHubClients w SignalR
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
        public async Task OnPostAsync_WithInvalidModel_ShouldReturnPage()
        {
            var userManagerMock = CreateUserManagerMock();
            var signInManagerMock = CreateSignInManagerMock(userManagerMock);
            var mockUserRoleRepo = new Mock<IUserRoleRepository>();
            var mockClientProxy = new Mock<IClientProxy>();
            var fakeHubClients = new FakeHubClients(mockClientProxy.Object);
            var mockHubContext = new Mock<IHubContext<UpdatesHub>>();
            mockHubContext.Setup(h => h.Clients).Returns(fakeHubClients);

            var model = new RegisterModel(userManagerMock.Object, signInManagerMock.Object, mockHubContext.Object, mockUserRoleRepo.Object)
            {
                Input = new RegisterModel.InputModel() // pusty Input
            };

            InitializeTempData(model);

            model.PageContext = new PageContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, "testuser") }))
                }
            };

            // Wymuszenie walidacji
            var validationContext = new ValidationContext(model.Input);
            var validationResults = new List<ValidationResult>();
            Validator.TryValidateObject(model.Input, validationContext, validationResults, true);
            foreach (var validationResult in validationResults)
            {
                foreach (var memberName in validationResult.MemberNames)
                {
                    model.ModelState.AddModelError(memberName, validationResult.ErrorMessage);
                }
            }

            var result = await model.OnPostAsync();

            Assert.NotNull(result);
            Assert.IsType<PageResult>(result);
            Assert.False(model.ModelState.IsValid); // Teraz będzie false, test przejdzie
        }


        [Fact]
        public async Task OnPostAsync_UserAlreadyExists_ShouldAddModelError()
        {
            var userManagerMock = CreateUserManagerMock();
            var signInManagerMock = CreateSignInManagerMock(userManagerMock);
            var mockUserRoleRepo = new Mock<IUserRoleRepository>();
            var mockClientProxy = new Mock<IClientProxy>();
            var fakeHubClients = new FakeHubClients(mockClientProxy.Object);
            var mockHubContext = new Mock<IHubContext<UpdatesHub>>();
            mockHubContext.Setup(h => h.Clients).Returns(fakeHubClients);

            var existingUser = new IdentityUser { UserName = "test@example.com", Email = "test@example.com" };

            userManagerMock.Setup(u => u.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync(existingUser);

            var model = new RegisterModel(userManagerMock.Object, signInManagerMock.Object, mockHubContext.Object, mockUserRoleRepo.Object)
            {
                Input = new RegisterModel.InputModel
                {
                    Email = "test@example.com",
                    Password = "Password123!",
                    ConfirmPassword = "Password123!"
                }
            };

            InitializeTempData(model);

            model.PageContext = new PageContext()
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, "testuser") }))
                }
            };

            var result = await model.OnPostAsync();

            Assert.IsType<PageResult>(result);
            Assert.True(model.ModelState.ContainsKey("Input.Email"));
            Assert.Contains(model.ModelState["Input.Email"].Errors, e => e.ErrorMessage.Contains("już istnieje"));
        }
        [Fact]
        public async Task OnPostAsync_ValidModel_CreatesUserAssignsRoleAndSignsIn()
        {
            var userManagerMock = CreateUserManagerMock();
            var signInManagerMock = CreateSignInManagerMock(userManagerMock);
            var mockUserRoleRepo = new Mock<IUserRoleRepository>();
            var mockClientProxy = new Mock<IClientProxy>();
            var fakeHubClients = new FakeHubClients(mockClientProxy.Object);
            var mockHubContext = new Mock<IHubContext<UpdatesHub>>();
            mockHubContext.Setup(h => h.Clients).Returns(fakeHubClients);

            userManagerMock.Setup(u => u.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync((IdentityUser)null);
            userManagerMock.Setup(u => u.CreateAsync(It.IsAny<IdentityUser>(), It.IsAny<string>())).ReturnsAsync(IdentityResult.Success);
            userManagerMock.Setup(u => u.AddToRoleAsync(It.IsAny<IdentityUser>(), "User")).ReturnsAsync(IdentityResult.Success);
            mockUserRoleRepo.Setup(r => r.GetUserIdsInRoleAsync(It.IsAny<string>())).ReturnsAsync(new List<string> { "admin1" });
            signInManagerMock.Setup(s => s.SignInAsync(It.IsAny<IdentityUser>(), false, null)).Returns(Task.CompletedTask);
            mockClientProxy.Setup(p => p.SendCoreAsync(It.IsAny<string>(), It.IsAny<object[]>(), default)).Returns(Task.CompletedTask);

            var model = new RegisterModel(userManagerMock.Object, signInManagerMock.Object, mockHubContext.Object, mockUserRoleRepo.Object)
            {
                Input = new RegisterModel.InputModel
                {
                    Email = "newuser@example.com",
                    Password = "Password123!",
                    ConfirmPassword = "Password123!"
                }
            };

            InitializeTempData(model);

            model.PageContext = new PageContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, "newuser") }))
                }
            };

            var result = await model.OnPostAsync();

            Assert.IsType<RedirectToPageResult>(result);

            userManagerMock.Verify(u => u.CreateAsync(It.IsAny<IdentityUser>(), It.IsAny<string>()), Times.Once);
            userManagerMock.Verify(u => u.AddToRoleAsync(It.IsAny<IdentityUser>(), "User"), Times.Once);
            signInManagerMock.Verify(s => s.SignInAsync(It.IsAny<IdentityUser>(), false, null), Times.Once);
            mockClientProxy.Verify(p => p.SendCoreAsync("UserRegistered", It.IsAny<object[]>(), default), Times.Once);
        }
    }
}