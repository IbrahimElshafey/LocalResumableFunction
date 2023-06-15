using Newtonsoft.Json.Linq;
using ResumableFunctions.Handler.InOuts;
using System.Dynamic;
using System.IO;
using System.Linq.Expressions;
using static ResumableFunctions.Handler.Helpers.Expressions.MatchExpressionWriter;
using static System.Linq.Expressions.Expression;

namespace ResumableFunctions.Handler.Helpers.Expressions
{
    internal class MandatoryPartVisitor : ExpressionVisitor
    {
        private readonly List<ConstantPart> _mandatoryParts;
        private readonly LambdaExpression _matchExpression;
        public LambdaExpression CallMandatoryPartExpression { get; internal set; }
        public Expression<Func<ExpandoObject, string[]>> CallMandatoryPartExpressionDynamic { get; internal set; }
        public LambdaExpression InstanceMandatoryPartExpression { get; internal set; }
        public MandatoryPartVisitor(LambdaExpression matchExpression, List<ConstantPart> constantParts)
        {
            _matchExpression = matchExpression;
            _mandatoryParts =
               constantParts
              .Where(x => x.IsMandatory)
              .OrderBy(x => x.PropPathExpression.ToString())
              .ToList();
            CallMandatoryPartExpression = GetCallMandatoryPartExpression();
            CallMandatoryPartExpressionDynamic = GetCallMandatoryPartExpressionDynamic();
            InstanceMandatoryPartExpression = GetInstanceMandatoryPartExpression();
        }



        private LambdaExpression GetCallMandatoryPartExpression()
        {
            //return null;
            return Lambda(NewArrayInit(typeof(string), TranslateParts()), _matchExpression.Parameters[0], _matchExpression.Parameters[1]);

            IEnumerable<Expression> TranslateParts()
            {
                foreach (var part in _mandatoryParts)
                {
                    yield return Call(part.PropPathExpression, typeof(object).GetMethod("ToString"));
                }
            }
        }

        private Expression<Func<ExpandoObject, string[]>> GetCallMandatoryPartExpressionDynamic()
        {
            var call = Parameter(typeof(ExpandoObject), "call");
            var parts = TranslateParts()?.ToList();
            if (parts != null && parts.Count == _mandatoryParts.Count)
                return Lambda<Func<ExpandoObject, string[]>>(NewArrayInit(typeof(string), parts), call);
            return null;

            IEnumerable<Expression> TranslateParts()
            {
                foreach (var part in _mandatoryParts)
                {
                    var exp = part.PropPathExpression;
                    if (exp is UnaryExpression convert && exp.NodeType == ExpressionType.Convert)
                        exp = convert.Operand;
                    var canInclude = exp is MemberExpression || exp is ParameterExpression;
                    if (!canInclude) break;
                    yield return
                        Call(
                            Call(
                                typeof(ExpandoExtensions).GetMethod("Get", 0, new[] { typeof(ExpandoObject), typeof(string) }),
                                call,
                                Constant(exp.ToString())
                            ),
                            typeof(object).GetMethod("ToString")
                        );
                }
            }
        }

        private LambdaExpression GetInstanceMandatoryPartExpression()
        {
            //return null;
            return Lambda(NewArrayInit(typeof(string), TranslateParts()), _matchExpression.Parameters[2]);

            IEnumerable<Expression> TranslateParts()
            {
                foreach (var part in _mandatoryParts)
                {
                    yield return Call(part.ConstantOriginalExpression, typeof(object).GetMethod("ToString"));
                }
            }
        }
    }
}