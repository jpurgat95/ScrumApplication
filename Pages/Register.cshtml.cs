using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

public class RegisterModel : PageModel
{
    private readonly UserManager<IdentityUser> _userManager;
    private readonly SignInManager<IdentityUser> _signInManager;

    public RegisterModel(UserManager<IdentityUser> userManager, SignInManager<IdentityUser> signInManager)
    {
        _userManager = userManager;
        _signInManager = signInManager;
    }

    [BindProperty]
    public InputModel Input { get; set; }

    public class InputModel
    {
        [Required(ErrorMessage = "Adres e-mail jest wymagany.")]
        [EmailAddress(ErrorMessage = "Podaj poprawny adres e-mail.")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Hasło jest wymagane.")]
        [DataType(DataType.Password)]
        [StringLength(100, ErrorMessage = "Hasło musi mieć co najmniej {2} znaków.", MinimumLength = 6)]
        public string Password { get; set; }

        [Required(ErrorMessage = "Potwierdź hasło.")]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Hasła muszą być takie same.")]
        public string ConfirmPassword { get; set; }
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
            return Page();

        // sprawdzanie czy użytkownik już istnieje
        var existingUser = await _userManager.FindByEmailAsync(Input.Email);
        if (existingUser != null)
        {
            ModelState.AddModelError("Input.Email", "Użytkownik z tym adresem e-mail już istnieje.");
            return Page();
        }

        var user = new IdentityUser { UserName = Input.Email, Email = Input.Email };
        var result = await _userManager.CreateAsync(user, Input.Password);

        if (result.Succeeded)
        {
            await _signInManager.SignInAsync(user, isPersistent: false);
            return RedirectToPage("/Index");
        }

        // Polskie komunikaty dla błędów z Identity
        foreach (var error in result.Errors)
        {
            // Domyślnie pokaż opis z Identity (fallback)
            var key = string.Empty;
            var msg = error.Description;

            switch (error.Code)
            {
                case "DuplicateUserName":
                case "DuplicateEmail":
                    key = "Input.Email";
                    msg = "Użytkownik z tym adresem e-mail już istnieje.";
                    break;

                case "PasswordTooShort":
                    key = "Input.Password";
                    msg = $"Hasło jest za krótkie. Minimalna długość: {_userManager.Options.Password.RequiredLength}.";
                    break;

                case "PasswordRequiresDigit":
                    key = "Input.Password";
                    msg = "Hasło musi zawierać co najmniej jedną cyfrę.";
                    break;

                case "PasswordRequiresUpper":
                    key = "Input.Password";
                    msg = "Hasło musi zawierać co najmniej jedną wielką literę.";
                    break;

                case "PasswordRequiresLower":
                    key = "Input.Password";
                    msg = "Hasło musi zawierać co najmniej jedną małą literę.";
                    break;

                case "PasswordRequiresNonAlphanumeric":
                    key = "Input.Password";
                    msg = "Hasło musi zawierać co najmniej jeden znak specjalny.";
                    break;
            }

            ModelState.AddModelError(key, msg);
        }
        return Page();
    }
}
