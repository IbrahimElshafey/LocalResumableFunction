using System.ComponentModel.DataAnnotations.Schema;
using System.Linq.Expressions;
using System.Security.Cryptography;
using System.Text;
using ResumableFunctions.Handler.Helpers.Expressions;

namespace ResumableFunctions.Handler.InOuts;

public class WaitTemplate : IEntity, IOnSaveEntity
{

    internal static class FieldsNames
    {
        public const string MatchExpression = nameof(_matchExpression);
        public const string CallMandatoryPartExpression = nameof(_callMandatoryPartExpression);
        //public const string CallMandatoryPartExpressionDynamic = nameof(_callMandatoryPartExpressionDynamic);
        public const string InstanceMandatoryPartExpression = nameof(_instanceMandatoryPartExpression);
        public const string SetDataExpression = nameof(_setDataExpression);
    }
    public int Id { get; internal set; }
    public int FunctionId { get; internal set; }
    public int MethodId { get; internal set; }
    public int MethodGroupId { get; internal set; }
    public MethodsGroup MethodGroup { get; internal set; }
    public byte[] BaseHash { get; internal set; }
    public DateTime Created { get; internal set; }
    public bool IsMandatoryPartFullMatch { get; internal set; }

    private string _matchExpression;
    private string _callMandatoryPartExpression;
    //private string _callMandatoryPartExpressionDynamic;
    private string _instanceMandatoryPartExpression;
    private string _setDataExpression;


    public static Expression<Func<WaitTemplate, WaitTemplate>> BasicMatchSelector =>
        waitTemplate => new WaitTemplate
        {
            _matchExpression = waitTemplate._matchExpression,
            _setDataExpression = waitTemplate._setDataExpression,
            Id = waitTemplate.Id,
            FunctionId = waitTemplate.FunctionId,
            MethodId = waitTemplate.MethodId,
            MethodGroupId = waitTemplate.MethodGroupId,
            ServiceId = waitTemplate.ServiceId
        };

    public static Expression<Func<WaitTemplate, WaitTemplate>> CallMandatoryPartSelector =>
        waitTemplate => new WaitTemplate
        {
            _callMandatoryPartExpression = waitTemplate._callMandatoryPartExpression,
            Id = waitTemplate.Id,
            FunctionId = waitTemplate.FunctionId,
            MethodId = waitTemplate.MethodId,
            MethodGroupId = waitTemplate.MethodGroupId,
            IsMandatoryPartFullMatch = waitTemplate.IsMandatoryPartFullMatch,
            ServiceId = waitTemplate.ServiceId
        };
    public static Expression<Func<WaitTemplate, WaitTemplate>> InstanceMandatoryPartSelector =>
        waitTemplate => new WaitTemplate
        {
            _instanceMandatoryPartExpression = waitTemplate._instanceMandatoryPartExpression,
            Id = waitTemplate.Id,
            FunctionId = waitTemplate.FunctionId,
            MethodId = waitTemplate.MethodId,
            MethodGroupId = waitTemplate.MethodGroupId,
            IsMandatoryPartFullMatch = waitTemplate.IsMandatoryPartFullMatch,
            ServiceId = waitTemplate.ServiceId
        };

    [NotMapped]
    public LambdaExpression MatchExpression { get; internal set; }


    //[NotMapped]
    //public Expression<Func<ExpandoObject, string[]>> CallMandatoryPartExpressionDynamic { get; internal set; }

    [NotMapped]
    public LambdaExpression CallMandatoryPartExpression { get; internal set; }

    [NotMapped]
    public LambdaExpression InstanceMandatoryPartExpression { get; internal set; }



    [NotMapped]
    public LambdaExpression SetDataExpression { get; internal set; }

    [NotMapped]
    public LambdaExpression SetDataExpressionDynamic { get; internal set; }

    public int? ServiceId { get; set; }


    bool expressionsLoaded;
    internal void LoadExpressions(bool forceReload = false)
    {
        var serializer = new ExpressionSerializer();
        if (expressionsLoaded && !forceReload) return;

        if (_matchExpression != null)
            MatchExpression = (LambdaExpression)serializer.Deserialize(_matchExpression).ToExpression();
        if (_setDataExpression != null)
            SetDataExpression = (LambdaExpression)serializer.Deserialize(_setDataExpression).ToExpression();
        if (_callMandatoryPartExpression != null)
            CallMandatoryPartExpression = (LambdaExpression)serializer.Deserialize(_callMandatoryPartExpression).ToExpression();
        if (_instanceMandatoryPartExpression != null)
            InstanceMandatoryPartExpression = (LambdaExpression)serializer.Deserialize(_instanceMandatoryPartExpression).ToExpression();
        //if (_callMandatoryPartExpressionDynamic != null)
        //    CallMandatoryPartExpressionDynamic = (Expression<Func<ExpandoObject, string[]>>)
        //        serializer.Deserialize(_callMandatoryPartExpressionDynamic).ToExpression();

        expressionsLoaded = true;
    }

    public static byte[] CalcHash(LambdaExpression matchExpression, LambdaExpression setDataExpression)
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
            _matchExpression = serializer.Serialize(MatchExpression.ToExpressionSlim());
        if (SetDataExpression != null)
            _setDataExpression = serializer.Serialize(SetDataExpression.ToExpressionSlim());
        //if (CallMandatoryPartExpressionDynamic != null)
        //    _callMandatoryPartExpressionDynamic = serializer.Serialize(CallMandatoryPartExpressionDynamic.ToExpressionSlim());
        if (CallMandatoryPartExpression != null)
        {
            _callMandatoryPartExpression = serializer.Serialize(CallMandatoryPartExpression.ToExpressionSlim());
            //using var md5 = MD5.Create();
            //MandatoryPartHash = md5.ComputeHash(Encoding.UTF8.GetBytes(_callMandatoryPartExpression));
        }
        if (InstanceMandatoryPartExpression != null)
            _instanceMandatoryPartExpression = serializer.Serialize(InstanceMandatoryPartExpression.ToExpressionSlim());
    }
}
