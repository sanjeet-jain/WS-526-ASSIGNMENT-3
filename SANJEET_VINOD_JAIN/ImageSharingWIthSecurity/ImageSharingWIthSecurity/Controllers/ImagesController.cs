using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ImageSharingWithSecurity.DAL;
using ImageSharingWithSecurity.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ImageSharingWithSecurity.Controllers;

using SysIOFile = File;

[Authorize]
public class ImagesController : BaseController
{
    private readonly IWebHostEnvironment hostingEnvironment;

    private readonly ILogger<ImagesController> logger;

    // Dependency injection
    public ImagesController(UserManager<ApplicationUser> userManager,
        ApplicationDbContext db,
        IWebHostEnvironment environment,
        ILogger<ImagesController> logger)
        : base(userManager, db)
    {
        hostingEnvironment = environment;
        this.logger = logger;
    }

    protected void mkDirectories()
    {
        var dataDir = Path.Combine(hostingEnvironment.WebRootPath,
            "data", "images");
        if (!Directory.Exists(dataDir)) Directory.CreateDirectory(dataDir);
    }

    protected string imageDataFile(int id)
    {
        return Path.Combine(
            hostingEnvironment.WebRootPath,
            "data", "images", "img-" + id + ".jpg");
    }

    public static string imageContextPath(int id)
    {
        return "data/images/img-" + id + ".jpg";
    }


    [HttpGet]
    public ActionResult Upload()
    {
        CheckAda();

        ViewBag.Message = "";
        var imageView = new ImageView();
        imageView.Tags = new SelectList(db.Tags, "Id", "Name", 1);
        return View(imageView);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> Upload(ImageView imageView)
    {
        CheckAda();

        await TryUpdateModelAsync(imageView);

        if (!ModelState.IsValid)
        {
            ViewBag.ImageErrorMessage = "No image file specified!";
            ViewBag.ImageNotUploaded = true;
            if (ModelState["DateTaken"]?.Errors.Count > 0)
            {
                ModelState["DateTaken"].Errors.Clear();
                ModelState.AddModelError("DateTaken", "Please Enter Valid Date");
            }

            imageView.Tags = new SelectList(db.Tags, "Id", "Name", 1);
            return View(imageView);
        }


        if (imageView.ImageFile == null || imageView.ImageFile.Length <= 0)
        {
            ViewBag.ImageErrorMessage = "No image file specified!";
            ViewBag.ImageNotUploaded = true;
            imageView.Tags = new SelectList(db.Tags, "Id", "Name", 1);
            return View(imageView);
        }

        var user = await GetLoggedInUser();
        if (user == null) return RedirectToAction("AccessDenied", "Account");
        // TODO-DONE save image metadata in the database 
        var selectedTag = await db.Tags.SingleOrDefaultAsync(x => x.Id.Equals(imageView.TagId));

        Image image = null;
        image = new Image
        {
            Caption = imageView.Caption,
            Description = imageView.Description,
            DateTaken = imageView.DateTaken,
            UserId = user.Id,
            TagId = selectedTag.Id,
            User = user,
            Tag = selectedTag
        };
        await db.Images.AddAsync(image);
        await db.SaveChangesAsync();
        // end TODO

        mkDirectories();

        // TODO-DONE save image file on disk
        using (var DestinationStream = SysIOFile.Create(imageDataFile(image.Id)))
        {
            await imageView.ImageFile.CopyToAsync(DestinationStream);
        }

        // end TODO

        return RedirectToAction("Details", new { image.Id });
    }

    [HttpGet]
    public ActionResult Query()
    {
        CheckAda();

        ViewBag.Message = "";
        return View();
    }

    [HttpGet]
    public ActionResult Details(int Id)
    {
        CheckAda();

        var image = db.Images.Find(Id);
        if (image == null) return RedirectToAction("Error", "Home", new { ErrId = "Details:" + Id });

        var imageView = new ImageView();
        imageView.Id = image.Id;
        imageView.Caption = image.Caption;
        imageView.Description = image.Description;
        imageView.DateTaken = image.DateTaken;
        /*
         * Eager loading of related entities
         */
        var imageEntry = db.Entry(image);
        imageEntry.Reference(i => i.Tag).Load();
        imageEntry.Reference(i => i.User).Load();
        imageView.TagName = image.Tag.Name;
        imageView.Username = image.User.UserName;
        return View(imageView);
    }

    [HttpGet]
    public async Task<ActionResult> Edit(int Id)
    {
        CheckAda();
        var user = await GetLoggedInUser();

        var image = db.Images.Find(Id);
        if (image == null) return RedirectToAction("Error", "Home", new { ErrId = "EditNotFound" });

        db.Entry(image).Reference(im => im.User).Load(); // Eager load of user
        if (!image.User.UserName.Equals(user.UserName))
            return RedirectToAction("Error", "Home", new { ErrId = "EditNotAuth" });

        ViewBag.Message = "";

        var imageView = new ImageView();
        imageView.Tags = new SelectList(db.Tags, "Id", "Name", image.TagId);
        imageView.Id = image.Id;
        imageView.TagId = image.TagId;
        imageView.Caption = image.Caption;
        imageView.Description = image.Description;
        imageView.DateTaken = image.DateTaken;

        return View("Edit", imageView);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> DoEdit(int Id, ImageView imageView)
    {
        CheckAda();
        var user = await GetLoggedInUser();

        if (!ModelState.IsValid)
        {
            ViewBag.Message = "Please correct the errors on the page";
            imageView.Id = Id;
            imageView.Tags = new SelectList(db.Tags, "Id", "Name", imageView.TagId);
            return View("Edit", imageView);
        }

        logger.LogDebug("Saving changes to image " + Id);
        var image = db.Images.Find(Id);
        if (image == null) return RedirectToAction("Error", "Home", new { ErrId = "EditNotFound" });

        db.Entry(image).Reference(im => im.User).Load(); // Explicit load of user
        if (!image.User.UserName.Equals(user.UserName))
            return RedirectToAction("Error", "Home", new { ErrId = "EditNotAuth" });

        image.TagId = imageView.TagId;
        image.Caption = imageView.Caption;
        image.Description = imageView.Description;
        image.DateTaken = imageView.DateTaken;
        db.Entry(image).State = EntityState.Modified;
        await db.SaveChangesAsync();
        return RedirectToAction("Details", new { Id });
    }

    [HttpGet]
    public async Task<ActionResult> Delete(int Id)
    {
        CheckAda();
        var user = await GetLoggedInUser();

        var image = await db.Images.FindAsync(Id);
        if (image == null) return RedirectToAction("Error", "Home", new { ErrId = "Delete" });

        db.Entry(image).Reference(im => im.User).Load(); // Explicit load of user
        if (!image.User.UserName.Equals(user.UserName))
            return RedirectToAction("Error", "Home", new { ErrId = "DeleteNotAuth" });

        var imageView = new ImageView();
        imageView.Id = image.Id;
        imageView.Caption = image.Caption;
        imageView.Description = image.Description;
        imageView.DateTaken = image.DateTaken;
        /*
         * Eager loading of related entities
         */
        db.Entry(image).Reference(i => i.Tag).Load();
        imageView.TagName = image.Tag.Name;
        imageView.Username = image.User.UserName;
        return View(imageView);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> DoDelete(int Id)
    {
        CheckAda();
        var user = await GetLoggedInUser();

        var image = db.Images.Find(Id);
        if (image == null) return RedirectToAction("Error", "Home", new { ErrId = "DeleteNotFound" });

        db.Entry(image).Reference(im => im.User).Load(); // Explicit load of user
        if (!image.User.UserName.Equals(user.UserName))
            return RedirectToAction("Error", "Home", new { ErrId = "DeleteNotAuth" });

        //db.Entry(imageEntity).State = EntityState.Deleted;
        db.Images.Remove(image);
        await db.SaveChangesAsync();
        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public async Task<ActionResult> ListAll()
    {
        CheckAda();
        var user = await GetLoggedInUser();

        IList<Image> images = ApprovedImages().Include(im => im.User).Include(im => im.Tag).ToList();
        ViewBag.Username = user.UserName;
        return View(images);
    }

    [HttpGet]
    public async Task<IActionResult> ListByUser()
    {
        CheckAda();

        // TODO-DONE Return form for selecting a user from a drop-down list
        var userView = new ListByUserModel();
        userView.Users = new SelectList(db.Users, "Id", "UserName", 1);

        return View(userView);
        // End TODO
    }

    [HttpGet]
    public async Task<ActionResult> DoListByUser(ListByUserModel userView)
    {
        CheckAda();
        var loggedinuser = await GetLoggedInUser();
        if (loggedinuser == null) return RedirectToAction("AccessDenied", "Account");

        // TODO-DONE list all images uploaded by the user in userView (see List By Tag)
        var user = await db.Users.FindAsync(userView.Id);
        if (user == null) return RedirectToAction("Error", "Home", new { ErrId = "ListByUser" });
        ViewBag.Username = loggedinuser.UserName;
        /*
         * Eager loading of related entities
         */
        var images = db.Entry(user).Collection(t => t.Images).Query().Where(im => im.Approved).Include(im => im.User).Include(t => t.Tag)
            .ToList();
        return View("ListAll", user.Images);
        // End TODO
    }

    [HttpGet]
    public ActionResult ListByTag()
    {
        CheckAda();

        var tagView = new ListByTagModel();
        tagView.Tags = new SelectList(db.Tags, "Id", "Name", 1);
        return View(tagView);
    }

    [HttpGet]
    public async Task<ActionResult> DoListByTag(ListByTagModel tagView)
    {
        CheckAda();
        var user = await GetLoggedInUser();

        var tag = db.Tags.Find(tagView.Id);
        if (tag == null) return RedirectToAction("Error", "Home", new { ErrId = "ListByTag" });

        ViewBag.Username = user.UserName;
        /*
         * Eager loading of related entities
         */
        var images = db.Entry(tag).Collection(t => t.Images).Query().Where(im => im.Approved).Include(im => im.User)
            .ToList();
        return View("ListAll", tag.Images);
    }


    [HttpGet]
    [Authorize(Roles = "Approver")]
    public IActionResult Approve()
    {
        CheckAda();

        var items = new List<SelectListItem>();
        foreach (var im in db.Images)
        {
            var item = new SelectListItem { Text = im.Caption, Value = im.Id.ToString(), Selected = im.Approved };
            items.Add(item);
        }

        ViewBag.message = "";
        var model = new ApproveModel { Images = items };
        return View(model);
    }

    [HttpPost]
    [Authorize(Roles = "Approver")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(ApproveModel model)
    {
        CheckAda();

        foreach (var item in model.Images.ToList())
        {
            var image = db.Images.Find(int.Parse(item.Value));

            if (item.Selected)
            {
                image.Approved = true;
                model.Images.Remove(item);
            }
            else
            {
                item.Text = image.Caption;
            }
        }

        await db.SaveChangesAsync();

        ViewBag.message = "Images approved!";

        return View(model);
    }
}