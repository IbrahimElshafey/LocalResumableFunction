using System.Drawing;
using System.Transactions;
using System.Linq;
using System.Reflection;
using LocalResumableFunction.Helpers;
using LocalResumableFunction.InOuts;

public abstract class ResumableFunctionLocal
{
    public Dictionary<string, object> State { get; set; }

    public MethodWait<Input, Output> When<Input, Output>(string name, Func<Input, Output> method)
    {
        var result = new MethodWait<Input, Output>(method)
        {
            Name = name,
            CurrntFunction = this,
            IsSingle = true,
            IsNode = true
        };
        return result;
    }

    public ManyMethodsWait When(string name, params MethodWait[] manyMethodsWait)
    {
        var result = new ManyMethodsWait
        {
            Name = name,
            WaitingMethods = manyMethodsWait.ToList(),
            IsSingle = false,
            IsNode = true
        };
        foreach (var item in result.WaitingMethods)
        {
            item.ParentGroupId = result.Id;
            item.IsNode = false;
        }
        return result;
    }

    private static List<Wait> _activeWaits = new List<Wait>();
    /// <summary>
    /// When method called and finished
    /// </summary>
    internal static async Task EventReceived(PushCalledMethod pushedEvent)
    {
        var waits = _activeWaits.Where(x => x.CallerMethodInfo == pushedEvent.CallerMethodInfo).ToList();
        foreach (var currentWait in waits)
        {
            //check if pushed event is matched against waits
            //currentWait.UpdateFunctionData();
            //get next wait if (IsSingleEvent(currentWait) || await IsGroupLastWait(currentWait))
            //load state and status from database

        }
    }

    internal static async Task WaitRequested(Wait wait)
    {
        _activeWaits.Add(wait);
    }
}
