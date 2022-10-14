using System.Collections.Generic;
using Microsoft.AspNetCore.Identity;

namespace ImageSharingWithSecurity.Models;

public class ApplicationUser : IdentityUser
{
    public ApplicationUser()
    {
        Active = true;
        ADA = false;
        Images = new List<Image>();
    }

    public ApplicationUser(string u)
    {
        Active = true;
        UserName = u;
        Email = u;
        ADA = false;
        Images = new List<Image>();
    }

    public ApplicationUser(string u, bool isADA)
    {
        Active = true;
        UserName = u;
        Email = u;
        ADA = isADA;
        Images = new List<Image>();
    }

    public virtual bool ADA { get; set; }
    public virtual bool Active { get; set; }

    public virtual ICollection<Image> Images { get; set; }
}