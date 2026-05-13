using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace project.Pages.Auth;

[IgnoreAntiforgeryToken]
public class SessionModel : PageModel
{
    public IActionResult OnGet(string email, string name)
    {
        HttpContext.Session.SetString("user_email", email);
        HttpContext.Session.SetString("user_name", name);
        return Redirect("/");
    }
}