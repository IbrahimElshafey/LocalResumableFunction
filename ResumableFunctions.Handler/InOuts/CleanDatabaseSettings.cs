using Hangfire;

namespace ResumableFunctions.Handler.InOuts
{
    public class CleanDatabaseSettings
    {
        public string RunCleaningCron { get; set; } = Cron.Daily();
        public string MarkUnusedWaitTemplatesCron { get; set; } = Cron.Daily();

        public TimeSpan CompletedInstanceRetention { get; set; } = TimeSpan.FromDays(3);
        public TimeSpan PushedCallRetention { get; set; } = TimeSpan.FromDays(10);
        public TimeSpan DeactivatedWaitTemplateRetention { get; set; } = TimeSpan.FromDays(10);
    }
}