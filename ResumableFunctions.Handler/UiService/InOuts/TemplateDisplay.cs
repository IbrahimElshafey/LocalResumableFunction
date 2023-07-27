using ResumableFunctions.Handler.InOuts;
using System.Linq.Expressions;
using System.Text;
using ResumableFunctions.Handler.Expressions;

namespace ResumableFunctions.Handler.UiService.InOuts
{
    public class TemplateDisplay
    {
        private readonly ExpressionSerializer _serializer;
      
        public string MatchExpression { get; }
        public string SetDataExpression { get; }
        public string MandatoryPartExpression { get; }

        public TemplateDisplay(WaitTemplate waitTemplate):
            this(waitTemplate.MatchExpressionValue, waitTemplate.InstanceMandatoryPartExpressionValue)
        {
        }

        public TemplateDisplay(string matchExpressionValue, string instanceMandatoryPartExpressionValue)
        {
            _serializer = new ExpressionSerializer();
            MatchExpression = GetMatch(matchExpressionValue);
            //SetDataExpression = GetSetData(setDataExpressionValue);
            MandatoryPartExpression = GetMandatoryParts(instanceMandatoryPartExpressionValue);
        }

        string GetMatch(string matchExpressionValue)
        {
            if (matchExpressionValue == null) return string.Empty;
            var result = _serializer.Deserialize(matchExpressionValue).ToCSharpString();
            if (result.Length > 37)
                result = result.Substring(37);
            return result;
        }
        string GetMandatoryParts(string instanceMandatoryPartExpressionValue)
        {
            if (instanceMandatoryPartExpressionValue == null) return string.Empty;
            var result = _serializer.Deserialize(instanceMandatoryPartExpressionValue).ToCSharpString();
            result = result.Replace("(input, output) => new object[]", "");
            result = result.Replace("functionInstance => new object[]", "");
            result = result.Replace("(object)", "");
            return result;
        }
        //string GetSetData(string setDataExpressionValue)
        //{
        //    if (setDataExpressionValue == null) return string.Empty;
        //    var setDataExp = _serializer.Deserialize(setDataExpressionValue);
        //    var result = new StringBuilder();
        //    if (setDataExp is LambdaExpressionSlim lambdaExpression &&
        //        lambdaExpression.Body is BlockExpressionSlim blockExpression)
        //    {
        //        foreach (var exp in blockExpression.Expressions)
        //        {
        //            if (exp.NodeType == ExpressionType.Default) continue;
        //            var expStr = exp.ToCSharpString();
        //            result.AppendLine(expStr);
        //            result.AppendLine("<br>");
        //        }
        //    }
        //    return result.ToString();
        //}
    }
}
