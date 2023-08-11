using ResumableFunctions.Handler.Helpers;
using System.Linq.Expressions;
using static ResumableFunctions.Handler.Expressions.MatchExpressionWriter;
using static System.Linq.Expressions.Expression;

namespace ResumableFunctions.Handler.Expressions
{
    internal class MandatoryPartExpressionsGenerator : ExpressionVisitor
    {
        private readonly List<ExpressionPart> _mandatoryParts;
        private readonly LambdaExpression _matchExpression;
        public LambdaExpression CallMandatoryPartExpression { get; }
        public LambdaExpression InstanceMandatoryPartExpression { get; }
        public MandatoryPartExpressionsGenerator(LambdaExpression matchExpression, List<ExpressionPart> expressionParts)
        {
            _matchExpression = matchExpression;
            _mandatoryParts =
               expressionParts
              .Where(x => x.IsMandatory)
              .OrderBy(x => x.InputOutputPart.ToString())
              .ToList();
            CallMandatoryPartExpression = GetCallMandatoryPartExpression();
            InstanceMandatoryPartExpression = GetInstanceMandatoryPartExpression();
        }



        private LambdaExpression GetCallMandatoryPartExpression()
        {
            return Lambda(NewArrayInit(
                typeof(object),
                _mandatoryParts.Select(x => Convert(x.InputOutputPart, typeof(object)))),
                _matchExpression.Parameters[0],
                _matchExpression.Parameters[1]);
        }


        private LambdaExpression GetInstanceMandatoryPartExpression()
        {
            //return null;
            return Lambda(NewArrayInit(
                typeof(object),
                _mandatoryParts.Select(x => Convert(ValuePart(x.ValuePart), typeof(object)))),
                _matchExpression.Parameters[2],
                _matchExpression.Parameters[3]);

            Expression ValuePart(Expression valuePart)
            {
                var changeClosureVarsVisitor = new GenericVisitor();
                changeClosureVarsVisitor.OnVisitConstant(node =>
                {
                    if (node.Type.Name.StartsWith(Constants.CompilerClosurePrefix))
                    {
                        return _matchExpression.Parameters[3];
                    }
                    return base.VisitConstant(node);
                });
                return changeClosureVarsVisitor.Visit(valuePart);
            }
        }
    }
}