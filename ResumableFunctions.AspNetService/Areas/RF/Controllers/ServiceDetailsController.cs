using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MVC.Models;
using ResumableFunctions.AspNetService.DisplayObject;
using System.Diagnostics;

namespace MVC.Controllers
{
    [Area("RF")]
    public class ServiceDetailsController : Controller
    {
        private readonly ILogger<ServiceDetailsController> _logger;

        public ServiceDetailsController(ILogger<ServiceDetailsController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }
    }
}