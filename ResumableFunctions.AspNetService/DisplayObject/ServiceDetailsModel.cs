namespace ResumableFunctions.AspNetService.DisplayObject
{
    public class ServiceDetailsModel
    {
        public class PartialNames
        {
            public const string ServiceDetails = "_ServiceInfo";
            public const string ScanLogs = "_ScanLogs";
            public const string ResumabelFunctions = "_ResumableFunctionsList";
            public const string MethodsList = "_MethodsList";
        }
        public int ServiceId { get; set; }
        public MainMenuDisplay Menu { get; private set; }

        internal void SetMenu()
        {
            Menu = new MainMenuDisplay
            {
                Items = new[]
                {
                    new MainMenuItem(
                        "TestApi1 Service Details",
                        $"/RF/ServiceDetails/{PartialNames.ServiceDetails}?serviceId={ServiceId}"),
                    new MainMenuItem(
                        "Scan Logs (0 Errors)",
                        $"/RF/ServiceDetails/{PartialNames.ScanLogs}?serviceId={ServiceId}"),
                    new MainMenuItem(
                        "Resumable Functions (10)",
                        $"/RF/ServiceDetails/{PartialNames.ResumabelFunctions}?serviceId={ServiceId}"),
                    new MainMenuItem(
                        "Methods (34)",
                        $"/RF/ServiceDetails/{PartialNames.MethodsList}?serviceId={ServiceId}"),
                }
            };
        }
    }
}