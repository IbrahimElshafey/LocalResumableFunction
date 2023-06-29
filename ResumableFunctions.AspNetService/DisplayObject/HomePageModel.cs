using ResumableFunctions.Handler.UiService.InOuts;

namespace ResumableFunctions.AspNetService.DisplayObject
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
                            $"/RF/Home/{PartialNames.ServicesList}"),
                        new MainMenuItem(
                            $"Resumable Functions ({mainStatistics.ResumableFunctions}) & ({mainStatistics.ResumableFunctionsInstances}) Instances",
                             $"/RF/Home/{PartialNames.ResumableFunctions}"),
                        new MainMenuItem(
                            $"Method Groups ({mainStatistics.MethodGroups})",
                            $"/RF/Home/{PartialNames.MethodsList}"),
                        new MainMenuItem(
                            $"Pushed Calls ({mainStatistics.PushedCalls})",
                            $"/RF/Home/{PartialNames.PushedCalls}"),
                        new MainMenuItem(
                            "Logs",
                            $"/RF/Home/{PartialNames.LatestLogs}"),
                }
            };
        }
    }
}