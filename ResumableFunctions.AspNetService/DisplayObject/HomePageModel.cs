using ResumableFunctions.Handler.UiService.InOuts;

namespace ResumableFunctions.AspNetService.DisplayObject
{
    public class HomePageModel
    {
        public class PartialNames
        {
            public const string ServicesList = "_Services";
            public const string ResumableFunctions = "_ResumableFunctionsList";
            public const string LatestCalls = "_LatestCalls";
            public const string LatestLogs = "_LatestLogs";

        }
        public MainMenuDisplay Menu { get; private set; }

        internal void SetMenu(MainStatistics mainStatistics)
        {
            Menu = new MainMenuDisplay
            {
                Items = new[]
                    {
                        new MainMenuItem(
                            $"Services ({mainStatistics.Services})",
                            $"/RF/Home/{PartialNames.ServicesList}"),
                        new MainMenuItem(
                            $"Resumable Functions ({mainStatistics.ResumableFunctions}) & ({mainStatistics.ResumableFunctionsInstances}) Instances",
                            $"/RF/Home/{PartialNames.ResumableFunctions}"),
                        new MainMenuItem(
                            $"Pushed Calls ({mainStatistics.PushedCalls})",
                            $"/RF/Home/{PartialNames.LatestCalls}"),
                        new MainMenuItem(
                            $"Latest Logs ({mainStatistics.LatestLogErrors} New Error)",
                            $"/RF/Home/{PartialNames.LatestLogs}"),
                }
            };
        }
    }
}