using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ImageSharingWithSecurity.Models;

public class ListByTagModel
{
    public int Id { get; set; }
    public IEnumerable<SelectListItem> Tags { get; set; }
}