using Xunit;
using Moq;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
namespace ScrumApplicationTests
{
    public class LogoutTests
    {
        private Mock<SignInManager<IdentityUser>> CreateSignInManagerMock()
        {
            var userManagerMock = new Mock<UserManager<IdentityUser>>(
                Mock.Of<IUserStore<IdentityUser>>(), null, null, null, null, null, null, null, null);

            var contextAccessor = new Mock<Microsoft.AspNetCore.Http.IHttpContextAccessor>();
            var claimsFactory = new Mock<IUserClaimsPrincipalFactory<IdentityUser>>();

            return new Mock<SignInManager<IdentityUser>>(
                userManagerMock.Object,
                contextAccessor.Object,
                claimsFactory.Object,
                null, null, null, null);
        }

        [Fact]
        public async Task OnGetAsync_ShouldSignOutAndRedirect()
        {
            var signInManagerMock = CreateSignInManagerMock();

            signInManagerMock.Setup(s => s.SignOutAsync()).Returns(Task.CompletedTask).Verifiable();

            var model = new LogoutModel(signInManagerMock.Object);

            var result = await model.OnGetAsync();

            signInManagerMock.Verify(s => s.SignOutAsync(), Times.Once);
            var redirectResult = Assert.IsType<RedirectToPageResult>(result);
            Assert.Equal("/Login", redirectResult.PageName);
        }

        [Fact]
        public async Task OnPostAsync_ShouldSignOutAndRedirect()
        {
            var signInManagerMock = CreateSignInManagerMock();

            signInManagerMock.Setup(s => s.SignOutAsync()).Returns(Task.CompletedTask).Verifiable();

            var model = new LogoutModel(signInManagerMock.Object);

            var result = await model.OnPostAsync();

            signInManagerMock.Verify(s => s.SignOutAsync(), Times.Once);
            var redirectResult = Assert.IsType<RedirectToPageResult>(result);
            Assert.Equal("/Login", redirectResult.PageName);
        }
    }
}