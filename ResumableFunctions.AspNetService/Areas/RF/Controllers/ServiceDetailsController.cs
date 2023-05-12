using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MVC.Models;
using ResumableFunctions.AspNetService.DisplayObject;
using ResumableFunctions.Handler.UiService;
using System.Diagnostics;

namespace MVC.Controllers
{
    [Area("RF")]
    public class ServiceDetailsController : Controller
    {
        private readonly IUiService _uiService;
        private readonly ILogger<ServiceDetailsController> _logger;

        public ServiceDetailsController(ILogger<ServiceDetailsController> logger, IUiService uiService)
        {
            _logger = logger;
            _uiService = uiService;
        }

        public async Task<IActionResult> IndexAsync(int id)
        {
            var model = new ServiceDetailsModel();
            return View(model);
        }

        public IActionResult ServiceInfoView(int id)
        {
            return PartialView("");
        }
    }
}