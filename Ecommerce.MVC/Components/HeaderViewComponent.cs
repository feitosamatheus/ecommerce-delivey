using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace Ecommerce.MVC.Components;

public class HeaderViewComponent: ViewComponent
{
    public HeaderViewComponent() { }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        return View();
    }
}