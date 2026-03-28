using System.ComponentModel.DataAnnotations;

namespace ImmichFrame.WebApi.Models;

public class AdminAuthSessionDto
{
    public bool IsConfigured { get; set; }
    public bool IsAuthenticated { get; set; }
    public string? Username { get; set; }
}

public class AdminLoginRequest
{
    [Required]
    public string Username { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}
