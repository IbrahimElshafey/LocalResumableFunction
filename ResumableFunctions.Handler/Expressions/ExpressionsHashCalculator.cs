using System.Linq.Expressions;
using System.Security.Cryptography;
using System.Text;

namespace ResumableFunctions.Handler.Expressions;

public class ExpressionsHashCalculator : ExpressionVisitor
{
    private readonly LambdaExpression _matchExpression;
    private readonly string _afterMatchAction;
    private readonly string _cancelMethodAction;

    public ExpressionsHashCalculator(
        LambdaExpression matchExpression,
        string afterMatchAction,
        string cancelAction)
    {
        try
        {
            _matchExpression = matchExpression;
            _afterMatchAction = afterMatchAction;
            _cancelMethodAction = cancelAction;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    public byte[] GetHash()
    {
        var sb = new StringBuilder();
        if (_matchExpression != null)
            sb.Append(_matchExpression.ToString());

        if (_afterMatchAction != null)
            sb.Append(_afterMatchAction);

        if (_cancelMethodAction != null)
            sb.Append(_cancelMethodAction);

        return MD5.HashData(Encoding.Unicode.GetBytes(sb.ToString()));
    }

}
