using Xunit;
using Moq;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using System.Security.Claims;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
namespace ScrumApplicationTests
{
    public class LoginTests
    {
        private Mock<SignInManager<IdentityUser>> CreateSignInManagerMock()
        {
            var userManagerMock = new Mock<UserManager<IdentityUser>>(
                Mock.Of<IUserStore<IdentityUser>>(), null, null, null, null, null, null, null, null);

            var contextAccessor = new Mock<IHttpContextAccessor>();
            var claimsFactory = new Mock<IUserClaimsPrincipalFactory<IdentityUser>>();

            return new Mock<SignInManager<IdentityUser>>(
                userManagerMock.Object, contextAccessor.Object, claimsFactory.Object, null, null, null, null);
        }

        private void ValidateModel(object model, PageModel page)
        {
            var validationContext = new ValidationContext(model);
            var validationResults = new List<ValidationResult>();
            Validator.TryValidateObject(model, validationContext, validationResults, true);

            foreach (var validationResult in validationResults)
            {
                foreach (var memberName in validationResult.MemberNames)
                {
                    page.ModelState.AddModelError(memberName, validationResult.ErrorMessage);
                }
            }
        }

        private void SetupContextAndUrlHelper(PageModel model)
        {
            var urlHelperMock = new Mock<IUrlHelper>();
            urlHelperMock.Setup(u => u.Content(It.IsAny<string>())).Returns<string>(content => content);
            model.Url = urlHelperMock.Object;

            model.PageContext = new PageContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, "testuser") }))
                }
            };
        }

        [Fact]
        public void OnGet_ShouldInitializeReturnUrl()
        {
            var signInManagerMock = CreateSignInManagerMock();

            var model = new LoginModel(signInManagerMock.Object);
            SetupContextAndUrlHelper(model);

            model.OnGet("/someUrl");
            Assert.Equal("/someUrl", model.ReturnUrl);

            model.OnGet();
            Assert.Equal("~/", model.ReturnUrl);
        }

        [Fact]
        public async Task OnPostAsync_InvalidModel_ShouldReturnPage()
        {
            var signInManagerMock = CreateSignInManagerMock();

            var model = new LoginModel(signInManagerMock.Object)
            {
                Input = new LoginModel.InputModel() // pusty input - walidacja nie przejdzie
            };
            SetupContextAndUrlHelper(model);
            ValidateModel(model.Input, model);

            var result = await model.OnPostAsync();

            Assert.IsType<PageResult>(result);
            Assert.False(model.ModelState.IsValid);
            Assert.True(model.ModelState.ContainsKey(nameof(model.Input.Email)));
            Assert.True(model.ModelState.ContainsKey(nameof(model.Input.Password)));
        }

        [Fact]
        public async Task OnPostAsync_SuccessfulLogin_ShouldRedirect()
        {
            var signInManagerMock = CreateSignInManagerMock();

            signInManagerMock.Setup(s =>
                s.PasswordSignInAsync("email@example.com", "Password1!", false, false))
                .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Success);

            var model = new LoginModel(signInManagerMock.Object)
            {
                Input = new LoginModel.InputModel
                {
                    Email = "email@example.com",
                    Password = "Password1!",
                    RememberMe = false
                }
            };

            SetupContextAndUrlHelper(model);

            model.ReturnUrl = "/returnUrl";

            var result = await model.OnPostAsync(model.ReturnUrl);

            var redirectResult = Assert.IsType<LocalRedirectResult>(result);
            Assert.Equal("/returnUrl", redirectResult.Url);
        }

        [Fact]
        public async Task OnPostAsync_LockedOut_ShouldReturnPageWithError()
        {
            var signInManagerMock = CreateSignInManagerMock();

            signInManagerMock.Setup(s =>
                s.PasswordSignInAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), false))
                .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.LockedOut);

            var model = new LoginModel(signInManagerMock.Object)
            {
                Input = new LoginModel.InputModel
                {
                    Email = "user@example.com",
                    Password = "Password1!"
                }
            };

            SetupContextAndUrlHelper(model);

            var result = await model.OnPostAsync();

            Assert.IsType<PageResult>(result);
            Assert.Contains(model.ModelState[string.Empty].Errors, e => e.ErrorMessage.Contains("Twoje konto zostało zablokowane"));
        }

        [Fact]
        public async Task OnPostAsync_NotAllowed_ShouldReturnPageWithError()
        {
            var signInManagerMock = CreateSignInManagerMock();

            signInManagerMock.Setup(s =>
                s.PasswordSignInAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), false))
                .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.NotAllowed);

            var model = new LoginModel(signInManagerMock.Object)
            {
                Input = new LoginModel.InputModel
                {
                    Email = "user@example.com",
                    Password = "Password1!"
                }
            };

            SetupContextAndUrlHelper(model);

            var result = await model.OnPostAsync();

            Assert.IsType<PageResult>(result);
            Assert.Contains(model.ModelState[string.Empty].Errors, e => e.ErrorMessage.Contains("Logowanie jest niedozwolone"));
        }

        [Fact]
        public async Task OnPostAsync_FailedLogin_ShouldReturnPageWithError()
        {
            var signInManagerMock = CreateSignInManagerMock();

            signInManagerMock.Setup(s =>
                s.PasswordSignInAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), false))
                .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Failed);

            var model = new LoginModel(signInManagerMock.Object)
            {
                Input = new LoginModel.InputModel
                {
                    Email = "user@example.com",
                    Password = "Password1!"
                }
            };

            SetupContextAndUrlHelper(model);

            var result = await model.OnPostAsync();

            Assert.IsType<PageResult>(result);
            Assert.Contains(model.ModelState[string.Empty].Errors, e => e.ErrorMessage.Contains("Nieprawidłowy login lub hasło"));
        }
    }
}