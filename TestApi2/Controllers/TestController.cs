using Microsoft.AspNetCore.Mvc;
using ReferenceLibrary;
using ResumableFunctions.Core.Attributes;

namespace TestApi2.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class TestController : ControllerBase
    {
        [HttpPost(nameof(ExternalMethodTest))]
        [WaitMethod]
        public int ExternalMethodTest(object o)
        {
           return Random.Shared.Next();
        }

        [HttpPost(nameof(ExternalMethodTest2))]
        [WaitMethod]
        public int ExternalMethodTest2(string o)
        {
            return Random.Shared.Next();
        }

        [HttpGet(nameof(SayHello_MethodInDll))]
        public string SayHello_MethodInDll(string userName)
        {
            return new CodeInDllTest().SayHello(userName);
        }
    }
}