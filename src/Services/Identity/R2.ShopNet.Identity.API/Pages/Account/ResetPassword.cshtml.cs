using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using R2.ShopNet.Identity.Domain.Entities;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace R2.ShopNet.Identity.API.Pages.Account;

public class ResetPasswordModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ILogger<ResetPasswordModel> _logger;

    public ResetPasswordModel(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        ILogger<ResetPasswordModel> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _logger = logger;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public string? ErrorMessage { get; set; }
    public bool IsSuccess { get; set; }
    public string? ReturnUrl { get; set; }

    public class InputModel
    {
        [Required]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Token { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required")]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [Display(Name = "Confirm password")]
        [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    public IActionResult OnGet(string? token = null, string? email = null, string? returnUrl = null)
    {
        ReturnUrl = returnUrl;

        if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(email))
        {
            ErrorMessage = "Invalid password reset link. Please request a new password reset.";
            return Page();
        }

        Input.Token = token;
        Input.Email = email;

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        ReturnUrl = returnUrl ?? Url.Content("~/");

        if (!ModelState.IsValid)
        {
            return Page();
        }

        if (string.IsNullOrEmpty(Input.Token) || string.IsNullOrEmpty(Input.Email))
        {
            ErrorMessage = "Invalid password reset link. Please request a new password reset.";
            return Page();
        }

        var user = await _userManager.FindByEmailAsync(Input.Email);
        if (user == null)
        {
            // Don't reveal that the user doesn't exist
            _logger.LogWarning("Password reset attempted for non-existent user: {Email}", Input.Email);
            ErrorMessage = "Invalid password reset link or the link has expired. Please request a new password reset.";
            return Page();
        }

        try
        {
            var result = await _userManager.ResetPasswordAsync(user, Input.Token, Input.Password);

            if (result.Succeeded)
            {
                _logger.LogInformation("Password reset successful for user: {Email}", Input.Email);

                // Automatically sign in the user after successful password reset
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.Name, user.UserName!),
                    new Claim(ClaimTypes.Email, user.Email!)
                };

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);

                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = false,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(30)
                };

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    claimsPrincipal,
                    authProperties);

                _logger.LogInformation("User {Email} automatically signed in after password reset", Input.Email);

                // Redirect to the original return URL
                return LocalRedirect(ReturnUrl);
            }

            foreach (var error in result.Errors)
            {
                _logger.LogWarning("Password reset error for {Email}: {Error}", Input.Email, error.Description);

                if (error.Code == "InvalidToken")
                {
                    ErrorMessage = "Invalid or expired password reset link. Please request a new password reset.";
                }
                else
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            if (string.IsNullOrEmpty(ErrorMessage))
            {
                ErrorMessage = "Failed to reset password. Please check the errors and try again.";
            }

            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting password for user: {Email}", Input.Email);
            ErrorMessage = "An error occurred while resetting your password. Please try again later.";
            return Page();
        }
    }
}
