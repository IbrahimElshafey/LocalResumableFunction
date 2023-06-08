﻿using Newtonsoft.Json.Linq;
using ResumableFunctions.Handler.Helpers;
using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;

namespace ResumableFunctions.Handler.InOuts;
public class PushedCall : IEntityWithDelete, IEntityInService, IOnSaveEntity, ILoadUnMapped
{
    public int Id { get; internal set; }
    [NotMapped]
    public MethodData MethodDatan { get; internal set; }
    public byte[] MethodDataValue { get; internal set; }
    [NotMapped]
    public InputOutput Datan { get; internal set; } = new();
    public byte[] DataValue { get; internal set; }
    public int? ServiceId { get; set; }
    public List<WaitForCall> WaitsForCall { get; internal set; } = new();

    //todo: will be deleted or used by external calls only
    public string RefineMatchModifier
    {
        get
        {
            var name = nameof(MethodWait.RefineMatchModifier);
            if (Data.Input is string s && s.StartsWith(name))
                return s.Substring(name.Length);
            if (Data.Output is string output && output.StartsWith(name))
                return output.Substring(name.Length);
            if (Data.Input is JObject inputJson && inputJson[name] != null)
                return inputJson[name].ToString();
            if (Data.Output is JObject outputJson && outputJson[name] != null)
                return outputJson[name].ToString();
            return null;
        }
    }

    public DateTime Created { get; internal set; }

    public bool IsDeleted { get; internal set; }

    public void OnSave()
    {
        var converter = new NewtonsoftBinaryToObjectConverter();
        DataValue = converter.ConvertToBinary(Datan);
        MethodDataValue = converter.ConvertToBinary(MethodDatan);
    }

    public void LoadUnmappedProps(params object[] args)
    {
        var converter = new NewtonsoftBinaryToObjectConverter();
        Datan = converter.ConvertToObject<InputOutput>(DataValue);
        MethodDatan = converter.ConvertToObject<MethodData>(MethodDataValue);
    }
}

public class InputOutput
{
    public object Input { get; set; }
    public object Output { get; set; }
}
