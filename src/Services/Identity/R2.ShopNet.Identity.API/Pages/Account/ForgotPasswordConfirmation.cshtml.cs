using Microsoft.AspNetCore.Mvc.RazorPages;

namespace R2.ShopNet.Identity.API.Pages.Account;

public class ForgotPasswordConfirmationModel : PageModel
{
    public string? ReturnUrl { get; set; }

    public void OnGet(string? returnUrl = null)
    {
        ReturnUrl = returnUrl;
    }
}
