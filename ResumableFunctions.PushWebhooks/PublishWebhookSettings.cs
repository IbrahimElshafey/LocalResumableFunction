﻿namespace ResumableFunctions.Publisher
{
    public class PublishWebhookSettings
    {
        public string ConsumerServiceUrl { get; set; }
        public string DllsToInclude { get; set; }
        public ScanOption ScanOption { get; set; }
    }

    public enum ScanOption
    {
        ScanIfDllsChanged,
        ScanEveryStartup,
        NoScan
    }
}
