using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.Net;

public class ResetPasswordModel : PageModel
{
    private readonly UserManager<IdentityUser> _userManager;
    private readonly ILogger<ResetPasswordModel> _logger;
    private readonly SignInManager<IdentityUser> _signInManager;

    public ResetPasswordModel(UserManager<IdentityUser> userManager, ILogger<ResetPasswordModel> logger, 
                             SignInManager<IdentityUser> signInManager)
    {
        _userManager = userManager;
        _logger = logger;
        _signInManager = signInManager;
    }

    [BindProperty]
    public InputModel Input { get; set; }

    public class InputModel
    {
        [Required]
        public string UserId { get; set; }

        [Required]
        public string Token { get; set; }

        [Required(ErrorMessage = "Hasło jest wymagane.")]
        [DataType(DataType.Password)]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Hasło musi mieć co najmniej {2} znaków.")]
        public string NewPassword { get; set; }

        [DataType(DataType.Password)]
        [Compare("NewPassword", ErrorMessage = "Hasła się nie zgadzają.")]
        public string ConfirmPassword { get; set; }
    }

    public IActionResult OnGet(string userId, string token)
    {
        if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(token))
        {
            _logger.LogWarning("Brak userId lub tokenu w zapytaniu resetu hasła.");
            return BadRequest("Kod i użytkownik są wymagane do resetu hasła.");
        }

        Input = new InputModel
        {
            UserId = userId,
            Token = WebUtility.UrlDecode(token)
        };

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        _logger.LogInformation("Rozpoczęto reset hasła dla użytkownika {UserId}.", Input.UserId);

        if (!ModelState.IsValid)
        {
            _logger.LogWarning("Niepoprawny model podczas resetu hasła. UserId: {UserId}", Input.UserId);
            return Page();
        }

        var user = await _userManager.FindByIdAsync(Input.UserId);
        if (user == null)
        {
            _logger.LogWarning("Nie znaleziono użytkownika o Id {UserId}.", Input.UserId);
            ModelState.AddModelError("", "Nie znaleziono użytkownika.");
            return Page();
        }

        var result = await _userManager.ResetPasswordAsync(user, Input.Token, Input.NewPassword);

        if (result.Succeeded)
        {
            _logger.LogInformation("Reset hasła zakończony sukcesem dla użytkownika {UserId}.", Input.UserId);
            TempData["SuccessMessage"] = "Hasło zostało zmienione.";
            await _signInManager.SignOutAsync();
            return RedirectToPage("/Login");
        }

        foreach (var error in result.Errors)
        {
            _logger.LogWarning("Błąd resetu hasła dla użytkownika {UserId}: {ErrorCode} - {ErrorDescription}", Input.UserId, error.Code, error.Description);
            var key = string.Empty;
            var msg = error.Description;
            switch (error.Code)
            {
                case "PasswordTooShort":
                    key = "Input.NewPassword";
                    msg = $"Hasło jest za krótkie. Minimalna długość: {_userManager.Options.Password.RequiredLength}.";
                    break;
                case "PasswordRequiresDigit":
                    key = "Input.NewPassword";
                    msg = "Hasło musi zawierać co najmniej jedną cyfrę.";
                    break;
                case "PasswordRequiresUpper":
                    key = "Input.NewPassword";
                    msg = "Hasło musi zawierać co najmniej jedną wielką literę.";
                    break;
                case "PasswordRequiresLower":
                    key = "Input.NewPassword";
                    msg = "Hasło musi zawierać co najmniej jedną małą literę.";
                    break;
                case "PasswordRequiresNonAlphanumeric":
                    key = "Input.NewPassword";
                    msg = "Hasło musi zawierać co najmniej jeden znak specjalny.";
                    break;
            }
            ModelState.AddModelError(key, msg);
        }

        return Page();
    }
}
