using System.ComponentModel.DataAnnotations.Schema;
using System.Linq.Expressions;
using System.Security.Cryptography;
using System.Text;
using ResumableFunctions.Handler.Expressions;

namespace ResumableFunctions.Handler.InOuts.Entities;

public class WaitTemplate : IEntity<int>, IOnSaveEntity
{

    public int Id { get; set; }
    public int FunctionId { get; set; }
    public int? MethodId { get; set; }
    public int MethodGroupId { get; set; }
    public MethodsGroup MethodGroup { get; set; }
    public byte[] Hash { get; set; }
    public DateTime Created { get; set; }
    public DateTime DeactivationDate { get; set; }
    public bool IsMandatoryPartFullMatch { get; set; }

    internal string MatchExpressionValue { get; set; }
    internal string CallMandatoryPartExpressionValue { get; set; }

    internal string InstanceMandatoryPartExpressionValue { get; set; }

    public string CancelMethodAction { get; set; }

    [NotMapped]
    public LambdaExpression MatchExpression { get; set; }

    [NotMapped]
    public LambdaExpression CallMandatoryPartExpression { get; set; }

    [NotMapped]
    public LambdaExpression InstanceMandatoryPartExpression { get; set; }



    public string AfterMatchAction { get; set; }


    public int? ServiceId { get; set; }
    public int InCodeLine { get; set; }
    public int IsActive { get; set; } = 1;

    bool expressionsLoaded;
    internal void LoadUnmappedProps(bool forceReload = false)
    {
        try
        {
            var serializer = new ExpressionSerializer();
            if (expressionsLoaded && !forceReload) return;

            if (MatchExpressionValue != null)
                MatchExpression = (LambdaExpression)serializer.Deserialize(MatchExpressionValue).ToExpression();
            if (CallMandatoryPartExpressionValue != null)
                CallMandatoryPartExpression = (LambdaExpression)serializer.Deserialize(CallMandatoryPartExpressionValue).ToExpression();
            if (InstanceMandatoryPartExpressionValue != null)
                InstanceMandatoryPartExpression = (LambdaExpression)serializer.Deserialize(InstanceMandatoryPartExpressionValue).ToExpression();

        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
        expressionsLoaded = true;
    }

    internal static byte[] CalcHash(LambdaExpression matchExpression, LambdaExpression setDataExpression)
    {
        var serializer = new ExpressionSerializer();
        var matchBytes = Encoding.UTF8.GetBytes(serializer.Serialize(matchExpression.ToExpressionSlim()));
        var setDataBytes = Encoding.UTF8.GetBytes(serializer.Serialize(setDataExpression.ToExpressionSlim()));
        using var md5 = MD5.Create();
        var mergedHash = new byte[matchBytes.Length + setDataBytes.Length];
        Array.Copy(matchBytes, mergedHash, matchBytes.Length);
        Array.Copy(setDataBytes, 0, mergedHash, matchBytes.Length, setDataBytes.Length);

        return md5.ComputeHash(mergedHash);
    }

    public void OnSave()
    {
        var serializer = new ExpressionSerializer();
        if (MatchExpression != null)
            MatchExpressionValue = serializer.Serialize(MatchExpression.ToExpressionSlim());
        if (CallMandatoryPartExpression != null)
            CallMandatoryPartExpressionValue = serializer.Serialize(CallMandatoryPartExpression.ToExpressionSlim());
        if (InstanceMandatoryPartExpression != null)
            InstanceMandatoryPartExpressionValue = serializer.Serialize(InstanceMandatoryPartExpression.ToExpressionSlim());
    }
}
