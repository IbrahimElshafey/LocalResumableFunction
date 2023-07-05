using ResumableFunctions.Handler.UiService.InOuts;

namespace ResumableFunctions.AspNetService.DisplayObject
{
    public class HomePageModel
    {
        public MainMenuDisplay Menu { get; private set; }

        internal void SetMenu()
        {
            Menu = new MainMenuDisplay
            {
                Items = new[]
                    {
                        new MainMenuItem(
                            $"Services",
                            $"/RF/Home/{PartialNames.ServicesList}"),
                        new MainMenuItem(
                            $"Resumable Functions",
                             $"/RF/Home/{PartialNames.ResumableFunctions}"),
                        new MainMenuItem(
                            $"Method Groups",
                            $"/RF/Home/{PartialNames.MethodsList}"),
                        new MainMenuItem(
                            $"Pushed Calls",
                            $"/RF/Home/{PartialNames.PushedCalls}"),
                        new MainMenuItem(
                            "Logs",
                            $"/RF/Home/{PartialNames.LatestLogs}"),
                }
            };
        }
    }
}