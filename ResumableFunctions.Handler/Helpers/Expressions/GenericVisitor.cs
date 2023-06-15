using System.Linq.Expressions;

namespace ResumableFunctions.Handler.Helpers.Expressions;

public class GenericVisitor : ExpressionVisitor
{
    //todo:enhance this class by override it's methods 
    private readonly List<VisitNodeFunction> _visitors = new();
    public void AddVisitor(
        Func<Expression, bool> whenExpressionMatch,
        Func<Expression, Expression> visitFunction)
    {
        _visitors.Add(new VisitNodeFunction(whenExpressionMatch, visitFunction));
    }

    public void ClearVisitors() => _visitors.Clear();

    public override Expression Visit(Expression node)
    {
        foreach (var visitor in _visitors)
        {
            if (visitor.WhenExpressionMatch(node))
                return base.Visit(visitor.VisitFunction(node));
        }
        return base.Visit(node);
    }

    internal void OnVisitBinary(Func<BinaryExpression, Expression> binaryVisitFunc)
    {
        _visitors.Add(
           new VisitNodeFunction(
              ex => ex is BinaryExpression,
               (ex) => binaryVisitFunc((BinaryExpression)ex)));
    }

    internal void OnVisitParamter(Func<ParameterExpression, Expression> parameterVisitFunc)
    {
        _visitors.Add(
            new VisitNodeFunction(
                ex => ex is ParameterExpression,
                (ex) => parameterVisitFunc((ParameterExpression)ex)));
    }
    internal void OnVisitConstant(Func<ConstantExpression, Expression> constantVisitFunc)
    {
        _visitors.Add(
            new VisitNodeFunction(
                ex => ex is ConstantExpression,
                (ex) => constantVisitFunc((ConstantExpression)ex)));
    }

    internal void OnVisitUnary(Func<UnaryExpression, Expression> visitUnary)
    {
        _visitors.Add(
          new VisitNodeFunction(
             ex => ex is UnaryExpression,
              (ex) => visitUnary((UnaryExpression)ex)));
    }

    internal void OnVisitMember(Func<MemberExpression, Expression> visitMember)
    {
        _visitors.Add(
          new VisitNodeFunction(
             ex => ex is MemberExpression,
              (ex) => visitMember((MemberExpression)ex)));
    }

    internal void OnVisitCall(Func<MethodCallExpression, Expression> visitCall)
    {
        _visitors.Add(
          new VisitNodeFunction(
             ex => ex is MethodCallExpression,
              (ex) => visitCall((MethodCallExpression)ex)));
    }

    private class VisitNodeFunction
    {
        public VisitNodeFunction(Func<Expression, bool> whenExpressionMatch, Func<Expression, Expression> visitFunction)
        {
            WhenExpressionMatch = whenExpressionMatch;
            VisitFunction = visitFunction;
        }
        public Func<Expression, bool> WhenExpressionMatch { get; }
        public Func<Expression, Expression> VisitFunction { get; }
    }
}
