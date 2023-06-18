using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace ResumableFunctions.AspNetService.Areas.RF.Controllers
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