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

    private byte[] _matchExpression;
    private byte[] _matchExpressionDynamic;
    private byte[] _mandatoryPartExtractorExpression;
    private byte[] _waitMandatoryPartExpression;
    private byte[] _setDataExpression;
    private byte[] _setDataExpressionDynamic;

    [NotMapped]
    public LambdaExpression MatchExpression { get; internal set; }
    [NotMapped]
    public Expression<Func<ExpandoAccessor, ExpandoAccessor, bool>> MatchExpressionDynamic { get; internal set; }
    [NotMapped]
    public Expression<Func<ExpandoAccessor, string[]>> MandatoryPartExtractorExpression { get; internal set; }
    [NotMapped]
    public Expression<Func<ExpandoAccessor, string>> WaitMandatoryPartExpression { get; internal set; }
    [NotMapped]
    public LambdaExpression SetDataExpression { get; internal set; }
    [NotMapped]
    public Expression<Action<ExpandoAccessor, ExpandoAccessor>> SetDataExpressionDynamic { get; internal set; }

    private Assembly FunctionAssembly =>
       Assembly.LoadFile($"{AppContext.BaseDirectory}{AssemblyName}.dll") ??
        Assembly.GetEntryAssembly();

    internal void LoadExpressions()
    {
        MatchExpression = (LambdaExpression)
            ExpressionToJsonConverter.JsonToExpression(
                TextCompressor.DecompressString(_matchExpression), FunctionAssembly);
        SetDataExpression = (LambdaExpression)
            ExpressionToJsonConverter.JsonToExpression(
                TextCompressor.DecompressString(_setDataExpression), FunctionAssembly);
    }

    public static byte[] CalcHash(LambdaExpression matchExpression, LambdaExpression setDataExpression, Assembly assembly)
    {
        var matchBytes = Encoding.ASCII.GetBytes(ExpressionToJsonConverter.ExpressionToJson(matchExpression, assembly));
        var setDataBytes = Encoding.ASCII.GetBytes(ExpressionToJsonConverter.ExpressionToJson(setDataExpression, assembly));
        using (System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create())
        {
            var mergedHash = new byte[matchBytes.Length + setDataBytes.Length];
            Array.Copy(matchBytes, mergedHash, matchBytes.Length);
            Array.Copy(setDataBytes, 0, mergedHash, matchBytes.Length, setDataBytes.Length);

            return md5.ComputeHash(mergedHash);
        }
    }
}
