using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using R2.ShopNet.Identity.Application.Services;
using R2.ShopNet.Identity.Domain.Entities;
using System.ComponentModel.DataAnnotations;

namespace R2.ShopNet.Identity.API.Pages.Account;

public class ForgotPasswordModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IEmailService _emailService;
    private readonly ILogger<ForgotPasswordModel> _logger;

    public ForgotPasswordModel(
        UserManager<ApplicationUser> userManager,
        IEmailService emailService,
        ILogger<ForgotPasswordModel> logger)
    {
        _userManager = userManager;
        _emailService = emailService;
        _logger = logger;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public string? ErrorMessage { get; set; }
    public string? ReturnUrl { get; set; }

    public class InputModel
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address")]
        public string Email { get; set; } = string.Empty;
    }

    public void OnGet(string? returnUrl = null)
    {
        ReturnUrl = returnUrl;
    }

    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var user = await _userManager.FindByEmailAsync(Input.Email);

        ReturnUrl = returnUrl;

        // Always redirect to confirmation page for security reasons
        // Don't reveal whether the user exists or not
        if (user == null || !(await _userManager.IsEmailConfirmedAsync(user)))
        {
            _logger.LogWarning("Password reset requested for non-existent or unconfirmed email: {Email}", Input.Email);
            return RedirectToPage("./ForgotPasswordConfirmation", new { returnUrl });
        }

        try
        {
            // Generate password reset token
            var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);

            // Build the reset URL with returnUrl parameter
            var resetUrl = Url.Page(
                "./ResetPassword",
                pageHandler: null,
                values: new { returnUrl },
                protocol: Request.Scheme,
                host: Request.Host.ToString());

            if (string.IsNullOrEmpty(resetUrl))
            {
                _logger.LogError("Failed to generate reset URL");
                ErrorMessage = "An error occurred. Please try again later.";
                return Page();
            }

            // Send password reset email
            await _emailService.SendPasswordResetEmailAsync(
                Input.Email,
                resetToken,
                resetUrl);

            _logger.LogInformation("Password reset email sent to {Email}", Input.Email);

            return RedirectToPage("./ForgotPasswordConfirmation", new { returnUrl });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending password reset email to {Email}", Input.Email);
            ErrorMessage = "An error occurred while sending the reset email. Please try again later.";
            return Page();
        }
    }
}
