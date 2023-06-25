using ResumableFunctions.Handler.UiService.InOuts;

namespace ResumableFunctions.AspNetService.DisplayObject
{
    public class ServiceDetailsModel
    {
        public int ServiceId { get; set; }
        public MainMenuDisplay Menu { get; private set; }

        internal void SetMenu(ServiceStatistics serviceStatistics)
        {
            Menu = new MainMenuDisplay
            {
                Items = new[]
                {
                    new MainMenuItem(
                        $"{serviceStatistics.ServiceName} Service Details",
                        $"/RF/ServiceDetails/{PartialNames.ServiceDetails}?serviceId={ServiceId}"),
                    new MainMenuItem(
                        $"Service GetLogs ({serviceStatistics.ErrorCounter} Errors)",
                        $"/RF/ServiceDetails/{PartialNames.ServiceLogs}?serviceId={ServiceId}"),
                    new MainMenuItem(
                        $"Resumable Functions ({serviceStatistics.FunctionsCount})",
                        $"/RF/ServiceDetails/{PartialNames.ResumableFunctions}?serviceId={ServiceId}"),
                    new MainMenuItem(
                        $"Methods ({serviceStatistics.MethodsCount})",
                        $"/RF/ServiceDetails/{PartialNames.MethodsList}?serviceId={ServiceId}"),
                }
            };

            Menu.BackLinkText = "Back to Services";
            Menu.BackLink = "/RF#view=0";
        }
    }
}