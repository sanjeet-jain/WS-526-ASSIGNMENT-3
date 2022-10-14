using System.ComponentModel.DataAnnotations;

namespace ImageSharingWithSecurity.Models;

public class UserView
{
    public string Id { get; set; }

    [Required]
    [RegularExpression(@"[a-zA-Z0-9_]+")]
    public string Username { get; set; }

    public string Password { get; set; }

    [Required] public bool ADA { get; set; }
}