using Microsoft.AspNetCore.Mvc;
using ResumableFunctions.Core.Attributes;

namespace TestApi2.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class TestController : ControllerBase
    {
        [HttpPost(nameof(ExtenalMethodTest))]
        [WaitMethod]
        public int ExtenalMethodTest(object o)
        {
           return Random.Shared.Next();
        }
    }
}