using System;
using System.Threading.Tasks;
using ImageSharingWithSecurity.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ImageSharingWithSecurity.DAL;

public class ApplicationDbInitializer
{
    private readonly ApplicationDbContext db;
    private readonly ILogger<ApplicationDbInitializer> logger;

    public ApplicationDbInitializer(ApplicationDbContext db, ILogger<ApplicationDbInitializer> logger)
    {
        this.db = db;
        this.logger = logger;
    }

    public async Task SeedDatabase(IServiceProvider serviceProvider)
    {
        await db.Database.MigrateAsync();

        db.RemoveRange(db.Images);
        db.RemoveRange(db.Tags);
        db.RemoveRange(db.Users);
        await db.SaveChangesAsync();

        logger.LogDebug("Adding role: User");
        var idResult = await CreateRole(serviceProvider, "User");
        if (!idResult.Succeeded) logger.LogDebug("Failed to create User role!");

        // TODO-DONE add other roles
        idResult = await CreateRole(serviceProvider, "Admin");
        if (!idResult.Succeeded) logger.LogDebug("Failed to create Admin role!");

        idResult = await CreateRole(serviceProvider, "Approver");
        if (!idResult.Succeeded) logger.LogDebug("Failed to create Approver role!");

        logger.LogDebug("Adding user: admin");
        idResult = await CreateAccount(serviceProvider, "admin@admin.org", "admin123", "Admin");
        if (!idResult.Succeeded) logger.LogDebug("Failed to create jfk user!");

        logger.LogDebug("Adding user: approver");
        idResult = await CreateAccount(serviceProvider, "approver@approver.org", "approver123", "Approver");
        if (!idResult.Succeeded) logger.LogDebug("Failed to create nixon user!");

        // TODO-DONE add other users and assign more roles
        logger.LogDebug("Adding user: sanjeet");
        idResult = await CreateAccount(serviceProvider, "sanjeet@gmail.com", "sanjeet123", "User");
        if (!idResult.Succeeded) logger.LogDebug("Failed to create sanjeet user!");


        var portrait = new Tag { Name = "portrait" };
        db.Tags.Add(portrait);
        var architecture = new Tag { Name = "architecture" };
        db.Tags.Add(architecture);

        // TODO-DONE add other tags
        var custom = new Tag { Name = "custom" };
        db.Tags.Add(custom);

        await db.SaveChangesAsync();
    }

    public static async Task<IdentityResult> CreateRole(IServiceProvider provider,
        string role)
    {
        var roleManager = provider
            .GetRequiredService
                <RoleManager<IdentityRole>>();
        var idResult = IdentityResult.Success;
        if (await roleManager.FindByNameAsync(role) == null)
            idResult = await roleManager.CreateAsync(new IdentityRole(role));
        return idResult;
    }

    public static async Task<IdentityResult> CreateAccount(IServiceProvider provider,
        string email,
        string password,
        string role)
    {
        var userManager = provider
            .GetRequiredService
                <UserManager<ApplicationUser>>();
        var idResult = IdentityResult.Success;

        if (await userManager.FindByNameAsync(email) == null)
        {
            var user = new ApplicationUser { UserName = email, Email = email };
            idResult = await userManager.CreateAsync(user, password);

            if (idResult.Succeeded) idResult = await userManager.AddToRoleAsync(user, role);
        }

        return idResult;
    }
}