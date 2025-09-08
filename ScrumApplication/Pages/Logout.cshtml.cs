using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

public class LogoutModel : PageModel
{
    private readonly SignInManager<IdentityUser> _signInManager;

    public LogoutModel(SignInManager<IdentityUser> signInManager)
    {
        _signInManager = signInManager;
    }
    public async Task<IActionResult> OnGetAsync()
    {
        try
        {
            await _signInManager.SignOutAsync();
            return RedirectToPage("/Login");
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Wystąpił błąd podczas wylogowania: {ex.Message} Spróbuj ponownie.";
            return RedirectToPage("/Login");
        }
    }

    public async Task<IActionResult> OnPostAsync()
    {
        try
        {
            await _signInManager.SignOutAsync();
            return RedirectToPage("/Login");
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Wystąpił błąd podczas wylogowania: {ex.Message} Spróbuj ponownie.";
            return RedirectToPage("/Login");
        }
    }

}
