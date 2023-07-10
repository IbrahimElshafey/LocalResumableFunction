namespace ResumableFunctions.Handler.InOuts
{
    public class CleanDatabaseSettings
    {
        public TimeSpan CompletedInstanceRetentionPeriod { get; set; } = TimeSpan.FromDays(3);
        public TimeSpan PushedCallRetentionPeriod { get; set; } = TimeSpan.FromDays(10);
    }
}