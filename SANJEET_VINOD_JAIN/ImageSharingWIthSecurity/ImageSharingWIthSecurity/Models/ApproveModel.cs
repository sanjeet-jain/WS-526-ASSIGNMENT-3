﻿using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ImageSharingWithSecurity.Models;

public class ApproveModel
{
    public IList<SelectListItem> Images { get; set; }
}