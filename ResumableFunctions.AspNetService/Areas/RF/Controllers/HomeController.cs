using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MVC.Models;
using ResumableFunctions.AspNetService.DisplayObject;
using System.Diagnostics;

namespace MVC.Controllers
{
    [Area("RF")]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            var model = new HomePageModel
            {
                Menu = new MainMenuDisplay
                {
                    Items = new[]
                    {
                        new MainMenuItem("Services (2)","./_Services"),
                        new MainMenuItem("All Resumable Functions (12)","./_ResumableFunctionsList"),
                        new MainMenuItem("Pushed Calls (1256)","./_LatestCalls"),
                        new MainMenuItem("Latest Logs (7 New Error)","./_LatestLogs"),
                    }
                }
            };
            return View(model);
        }

        public IActionResult ServiceDetails()
        {
            return View();
        }

        public IActionResult PushedCallDetails()
        {
            return View();
        }

        public IActionResult MethodsInGroup()
        {
            return View();
        }

        public IActionResult ResumableFunctionInstances()
        {
            return View();
        }

        public IActionResult MethodWaits()
        {
            return View();
        }

        public IActionResult ResumableFunctionInstanceHistory()
        {
            return View();
        }

        //[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        //public IActionResult Error()
        //{
        //    return View(new HomePageModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        //}
    }
}