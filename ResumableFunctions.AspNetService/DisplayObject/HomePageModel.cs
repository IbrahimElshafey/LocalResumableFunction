using ResumableFunctions.AspNetService.DisplayObject;
using ResumableFunctions.Handler.UiService.InOuts;

namespace MVC.Models
{
    public class HomePageModel
    {
        public MainMenuDisplay Menu { get; private set; }

        internal void SetMenu(MainStatistics mainStatistics)
        {
            Menu = new MainMenuDisplay
            {
                Items = new[]
                    {
                        new MainMenuItem(
                            $"Services ({mainStatistics.Services})",
                            "/RF/Home/ServicesView"),
                        new MainMenuItem(
                            $"Resumable Functions ({mainStatistics.ResumableFunctions}) & ({mainStatistics.ResumableFunctionsInstances}) Instances",
                            "./_ResumableFunctionsList"),
                        new MainMenuItem(
                            $"Pushed Calls ({mainStatistics.PushedCalls})",
                            "./_LatestCalls"),
                        new MainMenuItem(
                            $"Latest Logs ({mainStatistics.LatestLogErrors} New Error)",
                            "./_LatestLogs"),
                }
            };
        }
    }
}