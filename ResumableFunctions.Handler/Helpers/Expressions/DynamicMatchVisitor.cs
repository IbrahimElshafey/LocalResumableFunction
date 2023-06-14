﻿using Newtonsoft.Json.Linq;
using ResumableFunctions.Handler.InOuts;
using System.Dynamic;
using System.IO;
using System.Linq.Expressions;
using System.Reflection;
using System.Xml.Linq;
using static ResumableFunctions.Handler.Helpers.Expressions.MatchExpressionWriter;
using static System.Linq.Expressions.Expression;

namespace ResumableFunctions.Handler.Helpers.Expressions
{
    internal class DynamicMatchVisitor : ExpressionVisitor
    {
        private LambdaExpression _matchExpression;
        private ParameterExpression _inputOutput;
        private ParameterExpression _instance;
        private bool _stop;
        public Expression<Func<ExpandoObject, ExpandoObject, bool>> Result { get; internal set; }

        public DynamicMatchVisitor(LambdaExpression matchExpression)
        {
            _matchExpression = matchExpression;
            _inputOutput = Parameter(typeof(ExpandoObject), "inputOutput");
            _instance = Parameter(typeof(ExpandoObject), "instance");
            var result = Visit(_matchExpression.Body);
            if (!_stop)
                Result = Lambda<Func<ExpandoObject, ExpandoObject, bool>>(result, _inputOutput, _instance);
        }

        protected override Expression VisitMember(MemberExpression node)
        {
            var paramter = GetParamter(node.ToString());
            if (CanConvert(node.Type))
            {
                var getValueMi = typeof(ExpandoExtensions).GetMethods().First(x => x.Name == "Get" && x.IsGenericMethod).MakeGenericMethod(node.Type);
                return Call(
                    getValueMi,
                    paramter.ParameterExpression,
                    paramter.Path
                );
            }
            else
                _stop = true;
            return base.VisitMember(node);
        }

        private (ParameterExpression ParameterExpression, ConstantExpression Path) GetParamter(string path)
        {
            if (path.StartsWith("input.") || path.StartsWith("output."))
                return (_inputOutput, Constant(path));
            if (path.StartsWith("functionInstance."))
                return (_instance, Constant(path.Substring(17)));
            throw new Exception($"Can't access to `{path}`");
        }

        public override Expression Visit(Expression node)
        {
            if (_stop) return node;
            return base.Visit(node);
        }

        private bool CanConvert(Type type)
        {
            return type.IsConstantType() || type == typeof(DateTime) || type == typeof(Guid) || type.IsEnum;
        }
    }
}