using Microsoft.AspNetCore.Mvc;
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
        [PublishMethod("PublisherController.Method123", "TestApi1", "TestApi2")]
        public string Method123(string input)
        {
            return $"{nameof(Method123)} called with input [{input}]";
        }

        [HttpGet(nameof(MethodNotExist))]
        [PublishMethod("PublisherController.MethodNotExist", "TestApi2")]//not exist in TestApi2
        public string MethodNotExist(string input)
        {
            return $"{nameof(MethodNotExist)} called with input [{input}]";
        }

        [HttpGet(nameof(TwoParamsMethod))]
        [PublishMethod("PublisherController.TwoParamsMethod", "TestApi1")]//will throw exception
        public string TwoParamsMethod(string input, string t2)
        {
            return $"{nameof(TwoParamsMethod)} called with input [{input + t2}]";
        }
    }
}