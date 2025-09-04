using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Logging;
using Moq;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Threading.Tasks;
using Xunit;
namespace ScrumApplicationTests
{
    public class ResetPasswordTests
    {
        private Mock<UserManager<IdentityUser>> CreateUserManagerMock()
        {
            var store = new Mock<IUserStore<IdentityUser>>();
            var mgr = new Mock<UserManager<IdentityUser>>(store.Object, null, null, null, null, null, null, null, null);
            return mgr;
        }

        private Mock<ILogger<ResetPasswordModel>> CreateLoggerMock()
        {
            return new Mock<ILogger<ResetPasswordModel>>();
        }

        private Mock<SignInManager<IdentityUser>> CreateSignInManagerMock(Mock<UserManager<IdentityUser>> userManager)
        {
            var contextAccessor = new Mock<Microsoft.AspNetCore.Http.IHttpContextAccessor>();
            var claimsFactory = new Mock<IUserClaimsPrincipalFactory<IdentityUser>>();
            return new Mock<SignInManager<IdentityUser>>(userManager.Object, contextAccessor.Object, claimsFactory.Object, null, null, null, null);
        }

        private void InitializeTempData(PageModel model)
        {
            var httpContext = new DefaultHttpContext();
            var tempDataProvider = new Mock<ITempDataProvider>().Object;
            model.TempData = new TempDataDictionary(httpContext, tempDataProvider);
        }

        private void ValidateModel(object model, PageModel page)
        {
            var validationContext = new ValidationContext(model);
            var validationResults = new List<ValidationResult>();
            Validator.TryValidateObject(model, validationContext, validationResults, true);
            foreach (var validationResult in validationResults)
                foreach (var memberName in validationResult.MemberNames)
                    page.ModelState.AddModelError(memberName, validationResult.ErrorMessage);
        }

        [Fact]
        public void OnGet_InvalidParams_ShouldReturnBadRequest()
        {
            var userManagerMock = CreateUserManagerMock();
            var loggerMock = CreateLoggerMock();
            var signInManagerMock = CreateSignInManagerMock(userManagerMock);

            var model = new ResetPasswordModel(userManagerMock.Object, loggerMock.Object, signInManagerMock.Object);

            var result = model.OnGet(null, null);

            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Kod i użytkownik są wymagane do resetu hasła.", badRequestResult.Value);
        }

        [Fact]
        public void OnGet_ValidParams_ShouldPopulateInputAndReturnPage()
        {
            var userManagerMock = CreateUserManagerMock();
            var loggerMock = CreateLoggerMock();
            var signInManagerMock = CreateSignInManagerMock(userManagerMock);

            var model = new ResetPasswordModel(userManagerMock.Object, loggerMock.Object, signInManagerMock.Object);

            string userId = "user1";
            string token = WebUtility.UrlEncode("testtoken");

            var result = model.OnGet(userId, token);

            Assert.IsType<PageResult>(result);
            Assert.NotNull(model.Input);
            Assert.Equal(userId, model.Input.UserId);
            Assert.Equal("testtoken", model.Input.Token);
        }

        [Fact]
        public async Task OnPostAsync_InvalidModel_ShouldReturnPage()
        {
            var userManagerMock = CreateUserManagerMock();
            var loggerMock = CreateLoggerMock();
            var signInManagerMock = CreateSignInManagerMock(userManagerMock);

            var model = new ResetPasswordModel(userManagerMock.Object, loggerMock.Object, signInManagerMock.Object)
            {
                Input = new ResetPasswordModel.InputModel() // pusty input => walidacja zawiedzie
            };
            InitializeTempData(model);
            ValidateModel(model.Input, model);

            var result = await model.OnPostAsync();

            Assert.IsType<PageResult>(result);
            Assert.False(model.ModelState.IsValid);
        }

        [Fact]
        public async Task OnPostAsync_UserNotFound_ShouldReturnPageWithError()
        {
            var userManagerMock = CreateUserManagerMock();
            userManagerMock.Setup(u => u.FindByIdAsync(It.IsAny<string>())).ReturnsAsync((IdentityUser)null);

            var loggerMock = CreateLoggerMock();
            var signInManagerMock = CreateSignInManagerMock(userManagerMock);

            var model = new ResetPasswordModel(userManagerMock.Object, loggerMock.Object, signInManagerMock.Object)
            {
                Input = new ResetPasswordModel.InputModel
                {
                    UserId = "user1",
                    Token = "token",
                    NewPassword = "Password1!",
                    ConfirmPassword = "Password1!"
                }
            };

            InitializeTempData(model);

            var result = await model.OnPostAsync();

            Assert.IsType<PageResult>(result);
            Assert.True(model.ModelState.ContainsKey(string.Empty));
            Assert.Contains(model.ModelState[string.Empty].Errors, e => e.ErrorMessage.Contains("Nie znaleziono użytkownika."));
        }

        [Fact]
        public async Task OnPostAsync_ResetPasswordSucceeds_ShouldSignOutAndRedirect()
        {
            var userManagerMock = CreateUserManagerMock();

            var user = new IdentityUser { Id = "user1" };
            userManagerMock.Setup(u => u.FindByIdAsync("user1")).ReturnsAsync(user);
            userManagerMock.Setup(u => u.ResetPasswordAsync(user, "token", "Password1!")).ReturnsAsync(IdentityResult.Success);

            var loggerMock = CreateLoggerMock();

            var signInManagerMock = CreateSignInManagerMock(userManagerMock);
            signInManagerMock.Setup(s => s.SignOutAsync()).Returns(Task.CompletedTask).Verifiable();

            var model = new ResetPasswordModel(userManagerMock.Object, loggerMock.Object, signInManagerMock.Object)
            {
                Input = new ResetPasswordModel.InputModel
                {
                    UserId = "user1",
                    Token = "token",
                    NewPassword = "Password1!",
                    ConfirmPassword = "Password1!"
                }
            };

            InitializeTempData(model);

            var result = await model.OnPostAsync();

            signInManagerMock.Verify(s => s.SignOutAsync(), Times.Once);

            var redirectResult = Assert.IsType<RedirectToPageResult>(result);
            Assert.Equal("/Login", redirectResult.PageName);
            Assert.Equal("Hasło zostało zmienione.", model.TempData["SuccessMessage"]);
        }

        [Fact]
        public async Task OnPostAsync_ResetPasswordFails_ShouldAddModelErrors()
        {
            var userManagerMock = CreateUserManagerMock();
            var user = new IdentityUser { Id = "user1" };
            userManagerMock.Setup(u => u.FindByIdAsync("user1")).ReturnsAsync(user);

            var loggerMock = CreateLoggerMock();

            var signInManagerMock = CreateSignInManagerMock(userManagerMock);

            var errors = new List<IdentityError>
        {
            new IdentityError { Code = "PasswordTooShort", Description = "Too short" },
            new IdentityError { Code = "PasswordRequiresDigit", Description = "Needs digit" },
            new IdentityError { Code = "PasswordRequiresUpper", Description = "Needs uppercase" },
            new IdentityError { Code = "PasswordRequiresLower", Description = "Needs lowercase" },
            new IdentityError { Code = "PasswordRequiresNonAlphanumeric", Description = "Needs special character" },
            new IdentityError { Code = "OtherError", Description = "Other error" }
        };

            userManagerMock.Setup(u => u.ResetPasswordAsync(user, "token", "Password1!")).ReturnsAsync(IdentityResult.Failed(errors.ToArray()));

            var model = new ResetPasswordModel(userManagerMock.Object, loggerMock.Object, signInManagerMock.Object)
            {
                Input = new ResetPasswordModel.InputModel
                {
                    UserId = "user1",
                    Token = "token",
                    NewPassword = "Password1!",
                    ConfirmPassword = "Password1!"
                }
            };

            InitializeTempData(model);

            var result = await model.OnPostAsync();

            Assert.IsType<PageResult>(result);

            Assert.True(model.ModelState.ContainsKey("Input.NewPassword"));
            Assert.Contains(model.ModelState["Input.NewPassword"].Errors, e => e.ErrorMessage.Contains("Minimalna długość"));
            Assert.Contains(model.ModelState["Input.NewPassword"].Errors, e => e.ErrorMessage.Contains("co najmniej jedną cyfrę"));
            Assert.Contains(model.ModelState["Input.NewPassword"].Errors, e => e.ErrorMessage.Contains("co najmniej jedną wielką literę"));
            Assert.Contains(model.ModelState["Input.NewPassword"].Errors, e => e.ErrorMessage.Contains("co najmniej jedną małą literę"));
            Assert.Contains(model.ModelState["Input.NewPassword"].Errors, e => e.ErrorMessage.Contains("co najmniej jeden znak specjalny"));
            Assert.Contains(model.ModelState[string.Empty].Errors, e => e.ErrorMessage.Contains("Other error"));
        }
    }
}