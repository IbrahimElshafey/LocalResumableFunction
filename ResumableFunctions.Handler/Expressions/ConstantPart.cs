using System.Linq.Expressions;

namespace ResumableFunctions.Handler.Expressions;
public partial class MatchExpressionWriter
{
    internal class ConstantPart
    {
        public ConstantPart(
            ExpressionType op,
            Expression propPathExpression,
            Expression constantExpression,
            object value,
            Expression constantOriginalExpression)
        {
            PropPathExpression = propPathExpression;
            ConstantExpression = constantExpression;
            ConstantOriginalExpression = constantOriginalExpression;
            Value = value;
            Operator = op;
        }

        public Expression PropPathExpression { get; set; }
        public Expression ConstantExpression { get; set; }
        public Expression ConstantOriginalExpression { get; set; }
        public ExpressionType Operator { get; set; }
        public object Value { get; set; }
        public bool IsMandatory { get; set; }
    }
}
