using ResumableFunctions.Handler.Helpers;
using ResumableFunctions.Handler.Helpers.Expressions;
using System.ComponentModel.DataAnnotations.Schema;
using System.Dynamic;
using System.Linq.Expressions;
using System.Text;
namespace ResumableFunctions.Handler.InOuts;

public class MethodWaitTemplate : IEntity, IOnSaveEntity
{

    internal static class FieldsNames
    {
        public const string MatchExpression = nameof(_matchExpression);
        public const string CallMandatoryPartExpression = nameof(_callMandatoryPartExpression);
        public const string CallMandatoryPartExpressionDynamic = nameof(_callMandatoryPartExpressionDynamic);
        public const string InstanceMandatoryPartExpression = nameof(_instanceMandatoryPartExpression);
        public const string SetDataExpression = nameof(_setDataExpression);
    }
    public int Id { get; internal set; }
    public int FunctionId { get; internal set; }
    public int MethodId { get; internal set; }
    public int MethodGroupId { get; internal set; }
    public MethodsGroup MethodGroup { get; internal set; }
    public byte[] Hash { get; internal set; }
    public DateTime Created { get; internal set; }
    public bool IsMandatoryPartFullMatch { get; internal set; }

    private string _matchExpression;
    private string _callMandatoryPartExpression;
    private string _callMandatoryPartExpressionDynamic;
    private string _instanceMandatoryPartExpression;
    private string _setDataExpression;


    public static Expression<Func<MethodWaitTemplate, MethodWaitTemplate>> BasicMatchSelector =>
        waitTemplate => new MethodWaitTemplate
        {
            _matchExpression = waitTemplate._matchExpression,
            _setDataExpression = waitTemplate._setDataExpression,
            Id = waitTemplate.Id,
            FunctionId = waitTemplate.FunctionId,
            MethodId = waitTemplate.MethodId,
            MethodGroupId = waitTemplate.MethodGroupId,
        };

    public static Expression<Func<MethodWaitTemplate, MethodWaitTemplate>> CallMandatoryPartSelector =>
        waitTemplate => new MethodWaitTemplate
        {
            _callMandatoryPartExpression = waitTemplate._callMandatoryPartExpression,
            _callMandatoryPartExpressionDynamic = waitTemplate._callMandatoryPartExpressionDynamic,
            Id = waitTemplate.Id,
            FunctionId = waitTemplate.FunctionId,
            MethodId = waitTemplate.MethodId,
            MethodGroupId = waitTemplate.MethodGroupId,
            IsMandatoryPartFullMatch = waitTemplate.IsMandatoryPartFullMatch,
        };

    [NotMapped]
    public LambdaExpression MatchExpression { get; internal set; }


    [NotMapped]
    public Expression<Func<ExpandoObject, string[]>> CallMandatoryPartExpressionDynamic { get; internal set; }

    [NotMapped]
    public LambdaExpression CallMandatoryPartExpression { get; internal set; }

    [NotMapped]
    public LambdaExpression InstanceMandatoryPartExpression { get; internal set; }



    [NotMapped]
    public LambdaExpression SetDataExpression { get; internal set; }

    [NotMapped]
    public LambdaExpression SetDataExpressionDynamic { get; internal set; }


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
        if (_callMandatoryPartExpressionDynamic != null)
            CallMandatoryPartExpressionDynamic = (Expression<Func<ExpandoObject, string[]>>)
                serializer.Deserialize(_callMandatoryPartExpressionDynamic).ToExpression();

        expressionsLoaded = true;
    }

    public static byte[] CalcHash(LambdaExpression matchExpression, LambdaExpression setDataExpression)
    {
        var serializer = new ExpressionSerializer();
        var matchBytes = Encoding.UTF8.GetBytes(serializer.Serialize(matchExpression.ToExpressionSlim()));
        var setDataBytes = Encoding.UTF8.GetBytes(serializer.Serialize(setDataExpression.ToExpressionSlim()));
        using (System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create())
        {
            var mergedHash = new byte[matchBytes.Length + setDataBytes.Length];
            Array.Copy(matchBytes, mergedHash, matchBytes.Length);
            Array.Copy(setDataBytes, 0, mergedHash, matchBytes.Length, setDataBytes.Length);

            return md5.ComputeHash(mergedHash);
        }
    }

    public void OnSave()
    {
        var serializer = new ExpressionSerializer();
        if (MatchExpression != null)
            _matchExpression = serializer.Serialize(MatchExpression.ToExpressionSlim());
        if (SetDataExpression != null)
            _setDataExpression = serializer.Serialize(SetDataExpression.ToExpressionSlim());
        if (CallMandatoryPartExpressionDynamic != null)
            _callMandatoryPartExpressionDynamic = serializer.Serialize(CallMandatoryPartExpressionDynamic.ToExpressionSlim());
        if (CallMandatoryPartExpression != null)
            _callMandatoryPartExpression = serializer.Serialize(CallMandatoryPartExpression.ToExpressionSlim());
        if (InstanceMandatoryPartExpression != null)
            _instanceMandatoryPartExpression = serializer.Serialize(InstanceMandatoryPartExpression.ToExpressionSlim());
    }
}
