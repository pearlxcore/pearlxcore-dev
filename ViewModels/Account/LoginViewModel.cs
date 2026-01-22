using System.ComponentModel.DataAnnotations;

namespace pearlxcore.dev.ViewModels.Account;

public class LoginViewModel
{
    [Display(Name = "Enter your email")]
    public string Email { get; set; } = null!;
    [Display(Name = "Enter your password")]
    public string Password { get; set; } = null!;
    public bool RememberMe { get; set; }
    public string? ReturnUrl { get; set; }
}
