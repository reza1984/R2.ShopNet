using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using R2.ShopNet.Identity.Domain.Entities;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace R2.ShopNet.Identity.API.Pages.Account;

public class LoginModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ILogger<LoginModel> _logger;

    public LoginModel(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        ILogger<LoginModel> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _logger = logger;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public string? ReturnUrl { get; set; }
    public string? ErrorMessage { get; set; }
    public bool ShowPasskeyOption { get; set; }

    public class InputModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Display(Name = "Remember me?")]
        public bool RememberMe { get; set; }
    }

    public async Task<IActionResult> OnGetAsync(string? returnUrl = null)
    {
        ReturnUrl = returnUrl ?? Url.Content("~/");

        Console.WriteLine("=== Login Page GET Request ===");
        Console.WriteLine($"ReturnUrl: {ReturnUrl}");
        Console.WriteLine($"Request Path: {HttpContext.Request.Path}");
        Console.WriteLine($"Request Query: {HttpContext.Request.QueryString}");

        _logger.LogInformation("=== Login Page GET Request ===");
        _logger.LogInformation("ReturnUrl: {ReturnUrl}", ReturnUrl);
        _logger.LogInformation("Request Path: {Path}", HttpContext.Request.Path);
        _logger.LogInformation("Request Query: {Query}", HttpContext.Request.QueryString);

        // Clear existing external cookie
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        // Check if browser supports WebAuthn (we'll handle this in JavaScript)
        ShowPasskeyOption = true;

        _logger.LogInformation("Login page displayed successfully");

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        ReturnUrl = returnUrl ?? Url.Content("~/");

        _logger.LogInformation("=== Login Page POST Request ===");
        _logger.LogInformation("ReturnUrl: {ReturnUrl}", ReturnUrl);
        _logger.LogInformation("Email: {Email}", Input.Email);

        if (!ModelState.IsValid)
        {
            _logger.LogWarning("ModelState is invalid");
            return Page();
        }

        var user = await _userManager.FindByEmailAsync(Input.Email);
        if (user == null)
        {
            _logger.LogWarning("User not found for email: {Email}", Input.Email);
            ModelState.AddModelError(string.Empty, "Invalid email or password.");
            return Page();
        }

        _logger.LogInformation("User found: {UserId}, checking password", user.Id);

        var result = await _signInManager.CheckPasswordSignInAsync(user, Input.Password, lockoutOnFailure: true);

        if (result.Succeeded)
        {
            _logger.LogInformation("Password check succeeded for user {Email}", Input.Email);

            // Create claims for the authentication cookie
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.UserName!),
                new Claim(ClaimTypes.Email, user.Email!)
            };

            _logger.LogInformation("Creating authentication cookie with claims: {Claims}",
                string.Join(", ", claims.Select(c => $"{c.Type}={c.Value}")));

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);

            var authProperties = new AuthenticationProperties
            {
                IsPersistent = Input.RememberMe,
                ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(30)
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                claimsPrincipal,
                authProperties);

            _logger.LogInformation("User signed in successfully. Redirecting to: {ReturnUrl}", ReturnUrl);

            return LocalRedirect(ReturnUrl);
        }

        if (result.IsLockedOut)
        {
            _logger.LogWarning("User {Email} account locked out.", Input.Email);
            ModelState.AddModelError(string.Empty, "Account locked out. Please try again later.");
            return Page();
        }

        if (result.RequiresTwoFactor)
        {
            _logger.LogInformation("User {Email} requires two-factor authentication", Input.Email);
            // Implement 2FA if needed
            return RedirectToPage("./LoginWith2fa", new { ReturnUrl = returnUrl, RememberMe = Input.RememberMe });
        }

        _logger.LogWarning("Password check failed for user {Email}", Input.Email);
        ModelState.AddModelError(string.Empty, "Invalid email or password.");
        return Page();
    }
}
