using ImageSharingWithSecurity.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ImageSharingWithSecurity.DAL;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Image> Images { get; set; }

    //    public DbSet<User> Users { get; set; }

    public DbSet<Tag> Tags { get; set; }
}