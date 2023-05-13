using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using ResumableFunctions.AspNetService.DisplayObject;
using System.Diagnostics;

namespace MVC.Controllers
{
    [Area("RF")]
    public class PushedCallController : Controller
    {
        private readonly ILogger<PushedCallController> _logger;

        public PushedCallController(ILogger<PushedCallController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }
    }
}