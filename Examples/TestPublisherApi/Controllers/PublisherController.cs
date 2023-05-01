using Microsoft.AspNetCore.Mvc;
using ResumableFunctions.Publisher;

namespace TestPublisherApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class PublisherController : ControllerBase
    {
        

        private readonly ILogger<PublisherController> _logger;

        public PublisherController(ILogger<PublisherController> logger)
        {
            _logger = logger;
        }

        [HttpGet(nameof(Method123))]
        [PublishMethod("PublisherController.Method123")]
        public string Method123(string input)
        {
            return $"{nameof(Method123)} called with input `{input}`";
        }
    }
}