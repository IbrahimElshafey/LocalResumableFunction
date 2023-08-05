using Microsoft.AspNetCore.Mvc;
using ResumableFunctions.Publisher;
using ResumableFunctions.Publisher.Helpers;

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
        [PublishMethod("PublisherController.Method123", ToService = "TestApi1")]
        public string Method123(string input)
        {
            return $"{nameof(Method123)} called with input [{input}]";
        }

        [HttpGet(nameof(Method456))]
        [PublishMethod("PublisherController.Method456", ToService = "TestApi2")]//not exist in TestApi2
        public string Method456(string input)
        {
            return $"{nameof(Method456)} called with input [{input}]";
        }
    }
}