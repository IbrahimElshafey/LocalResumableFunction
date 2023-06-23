using System.Reflection;
using ClientOnboarding.InOuts;
using ClientOnboarding.Services;
using ClientOnboarding.Workflow;
using Microsoft.Extensions.DependencyInjection;
using ResumableFunctions.Handler;
using ResumableFunctions.Handler.Attributes;
using ResumableFunctions.Handler.Helpers;
using ResumableFunctions.Handler.InOuts;
using ResumableFunctions.Handler.TestShell;

namespace Tests
{
    public partial class RemoveDbs
    {
        [Fact(Skip = "After Tests Only")]
        public void TestTimeWaitAtStart_Test()
        {
            var tests = Assembly
                .Load("Tests")
                .GetTypes()
                .SelectMany(type => type.GetMethods())
                .Where(methodInfo => methodInfo.GetCustomAttribute(typeof(FactAttribute)) != null)
                .Select(testMethodName => testMethodName.Name)
                .ToList();

            tests.ForEach(TestCase.DeleteDb);
            Assert.True(true);
        }


    }

}