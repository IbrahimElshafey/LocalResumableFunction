using Hangfire;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using ResumableFunctions.Handler.Helpers;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
namespace ResumableFunctions.Handler.InOuts;

public class MethodWaitTemplate : IEntity
{

    public int Id { get; internal set; }
    public int FunctionId { get; internal set; }
    public int MethodGroupId { get; internal set; }
    public string AssemblyName { get; internal set; }
    public MethodsGroup MethodGroup { get; internal set; }
    public byte[] HashId { get; internal set; }
    public DateTime Created { get; internal set; }

    private string _matchExpression;
    private string _matchExpressionDynamic;
    private string _callMandatoryPartExpression;
    private string _waitMandatoryPartExpression;
    private string _setDataExpression;
    private string _setDataExpressionDynamic;

    [NotMapped]
    public LambdaExpression MatchExpression { get; internal set; }

    [NotMapped]
    public Expression<Func<ExpandoAccessor, ExpandoAccessor, bool>> MatchExpressionDynamic { get; internal set; }

    [NotMapped]
    public Expression<Func<ExpandoAccessor, string[]>> CallMandatoryPartExpressionDynamic { get; internal set; }

    [NotMapped]
    public LambdaExpression CallMandatoryPartExpression { get; internal set; }

    [NotMapped]
    public Expression<Func<ExpandoAccessor, string>> WaitMandatoryPartExpressionDynamic { get; internal set; }

    [NotMapped]
    public LambdaExpression WaitMandatoryPartExpression { get; internal set; }

    [NotMapped]
    public LambdaExpression SetDataExpression { get; internal set; }

    [NotMapped]
    public Expression<Action<ExpandoAccessor, ExpandoAccessor>> SetDataExpressionDynamic { get; internal set; }

    private Assembly FunctionAssembly =>
       Assembly.LoadFile($"{AppContext.BaseDirectory}{AssemblyName}.dll") ??
        Assembly.GetEntryAssembly();

    internal void LoadExpressions()
    {
        var serializer = new ExpressionSerializer();
        MatchExpression = (LambdaExpression)serializer.Deserialize(_matchExpression).ToExpression();
        SetDataExpression = (LambdaExpression)serializer.Deserialize(_setDataExpression).ToExpression();
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
}
