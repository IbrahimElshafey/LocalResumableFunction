using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MVC.Models;
using System.Diagnostics;

namespace MVC.Controllers
{
    public class RfUiController : Controller
    {
        private readonly ILogger<RfUiController> _logger;

        public RfUiController(ILogger<RfUiController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
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

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}