using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ImageSharingWithSecurity.DAL;
using ImageSharingWithSecurity.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Logging;

namespace ImageSharingWithSecurity.Controllers;

[Authorize]
public class AccountController : BaseController
{
    public enum PasswordMessageId
    {
        ChangePasswordSuccess,
        SetPasswordSuccess,
        RemoveLoginSuccess
    }

    private readonly ILogger<AccountController> logger;
    protected SignInManager<ApplicationUser> signInManager;

    // Dependency injection of DB context and user/signin managers
    public AccountController(UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        ApplicationDbContext db,
        ILogger<AccountController> logger)
        : base(userManager, db)
    {
        this.signInManager = signInManager;
        this.logger = logger;
    }

    [HttpGet]
    [AllowAnonymous]
    public ActionResult Register()
    {
        CheckAda();
        return View();
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> Register(RegisterModel model)
    {
        CheckAda();

        if (ModelState.IsValid)
        {
            logger.LogDebug("Registering user: " + model.Email);
            IdentityResult result = null;
            // TODO-DONE register the user from the model, and log them in
            var user = new ApplicationUser(model.Email, model.ADA);
            result = await userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                SaveADACookie(model.ADA);
                await signInManager.SignInAsync(user, false);
                return RedirectToAction("Index", "Home", new { model.Email });
            }
        }

        // If we got this far, something failed, redisplay form
        return View(model);
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login(string returnUrl)
    {
        CheckAda();
        ViewBag.ReturnUrl = returnUrl;
        return View();
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginModel model, string returnUrl)
    {
        CheckAda();
        if (!ModelState.IsValid) return View(model);

        // TODO-DONE log in the user from the model (make sure they are still active)
        var result = await signInManager.PasswordSignInAsync(model.UserName, model.Password, model.RememberMe, false);
        if (result.Succeeded)
        {
            var user = await userManager.FindByNameAsync(model.UserName);
            SaveADACookie(user.ADA);
            return Redirect(returnUrl ?? "/");
        }

        return View(model);
    }

    //
    // GET: /Account/Password

    [HttpGet]
    public ActionResult Password(PasswordMessageId? message)
    {
        CheckAda();
        ViewBag.Message =
            message == PasswordMessageId.ChangePasswordSuccess ? "Your password has been changed."
            : message == PasswordMessageId.SetPasswordSuccess ? "Your password has been set."
            : message == PasswordMessageId.RemoveLoginSuccess ? "The external login was removed."
            : "";
        ViewBag.ReturnUrl = Url.Action("Password");
        return View();
    }

    //
    // POST: /Account/Password

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> Password(LocalPasswordModel model)
    {
        CheckAda();

        ViewBag.ReturnUrl = Url.Action("Password");
        if (ModelState.IsValid)
        {
            IdentityResult idResult = null;
            ;

            // TODO-DONE change the password
            var user = await GetLoggedInUser();
            if (user == null) return RedirectToAction("AccessDenied");

            var checkPassword = await userManager.CheckPasswordAsync(user, model.OldPassword);
            if (checkPassword)
            {
                var resetToken = await userManager.GeneratePasswordResetTokenAsync(user);
                idResult = await userManager.ResetPasswordAsync(user, resetToken, model.NewPassword);

                if (idResult.Succeeded)
                    return RedirectToAction("Password", new { Message = PasswordMessageId.ChangePasswordSuccess });
                ModelState.AddModelError("", "The new password is invalid.");
            }
            else
            {
                ModelState.AddModelError("OldPassword", "The old password is invalid.");
            }
        }

        // If we got this far, something failed, redisplay form
        return View(model);
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public IActionResult Manage()
    {
        CheckAda();

        var users = new List<SelectListItem>();
        foreach (var u in db.Users)
        {
            var item = new SelectListItem { Text = u.UserName, Value = u.Id, Selected = u.Active };
            users.Add(item);
        }

        ViewBag.message = "";
        var model = new ManageModel { Users = users };
        return View(model);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Manage(ManageModel model)
    {
        CheckAda();

        foreach (var userItem in model.Users)
        {
            var user = await userManager.FindByIdAsync(userItem.Value);

            // Need to reset user name in view model before returning to user, it is not posted back
            userItem.Text = user.UserName;

            if (user.Active && !userItem.Selected)
            {
                var images = db.Entry(user).Collection(u => u.Images).Query().ToList();
                foreach (var image in images) db.Images.Remove(image);
                user.Active = false;
            }
            else if (!user.Active && userItem.Selected)
            {
                /*
                 * Reactivate a user
                 */
                user.Active = true;
            }
        }

        await db.SaveChangesAsync();

        ViewBag.message = "Users successfully deactivated/reactivated";

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Logoff()
    {
        await signInManager.SignOutAsync();
        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public async Task<IActionResult> AccessDenied()
    {
        CheckAda();
        return View();
    }

    protected void SaveADACookie(bool value)
    {
        // TODO-DONE save the value in a cookie field key
        var options = new CookieOptions
            { IsEssential = true, Secure = true, SameSite = SameSiteMode.None, Expires = DateTime.Now.AddMonths(3) };
        Response.Cookies.Append("ADA", value.ToString().ToLower(), options);
    }
}