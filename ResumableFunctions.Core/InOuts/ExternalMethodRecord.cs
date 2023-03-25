﻿
namespace ResumableFunctions.Core.InOuts;

public class ExternalMethodRecord
{
    public int Id { get; internal set; }
    public MethodData MethodData { get; set; }

    public byte[] MethodHash { get; internal set; }

    public byte[] OriginalMethodHash { get; internal set; }
}
