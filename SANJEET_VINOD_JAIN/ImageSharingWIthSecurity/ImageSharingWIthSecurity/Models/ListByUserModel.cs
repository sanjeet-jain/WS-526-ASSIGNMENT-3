using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ImageSharingWithSecurity.Models;

public class ListByUserModel
{
    public string Id { get; set; }
    public IEnumerable<SelectListItem> Users { get; set; }
}