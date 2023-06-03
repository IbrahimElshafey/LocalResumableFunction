using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace ResumableFunctions.Handler.Helpers;

public class IntListToStringConverter : ValueConverter<List<int>, string>
{
    public IntListToStringConverter() :
        base(
        intList => IntListToString(intList),
        s => StringToIntList(s))
    {
    }

    private static List<int> StringToIntList(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return new List<int>();
        return new List<int>(s.Split(',').Select(int.Parse));
    }

    private static string IntListToString(List<int> intList)
    {
        return
            intList == null || !intList.Any() ?
            "" :
            intList.Select(x => x.ToString()).Aggregate((x, y) => $"{x},{y}");
    }
}
