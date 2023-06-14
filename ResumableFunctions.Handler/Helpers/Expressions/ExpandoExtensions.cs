using MessagePack.Resolvers;
using MessagePack;
using System.Dynamic;
using FastExpressionCompiler;

namespace ResumableFunctions.Handler.Helpers.Expressions
{
    public static class ExpandoExtensions
    {
        public static object Get(this ExpandoObject _this, string path)
        {
            var comparer = StringComparer.OrdinalIgnoreCase;
            var root = new Dictionary<string, object>(_this, comparer);
            var parts = path.Split('.');
            object result = root[parts[0]];
            var parent = parts.Length > 1 ? (IDictionary<object, object>)root[parts[0]] : null;
            for (int i = 1; i < parts.Length; i++)
            {
                var currentProp = parts[i];
                result = parent[currentProp];
                parent = result as IDictionary<object, object>;
            }
            return result;
        }

        public static void Set(this ExpandoObject _this, string index, object value)
        {
            var root = (IDictionary<string, object>)_this;
            var parts = index.Split('.');
            if (parts.Length == 1)
                root[index] = value;
            else
            {
                var parent = (IDictionary<object, object>)root[parts[0]];
                for (int i = 1; i < parts.Length; i++)
                {
                    var currentProp = parts[i];
                    if (i == parts.Length - 1)
                        parent[currentProp] = value;
                    parent = parent[currentProp] as IDictionary<object, object>;
                }
            }
        }

        public static T ToObject<T>(this ExpandoObject _this)
        {
            var blob = MessagePackSerializer.Serialize(_this, ContractlessStandardResolver.Options);
            return MessagePackSerializer.Deserialize<T>(blob, ContractlessStandardResolver.Options);
        }

        public static object ToObject(this ExpandoObject _this, Type type)
        {
            var blob = MessagePackSerializer.Serialize(_this, ContractlessStandardResolver.Options);
            return MessagePackSerializer.Deserialize(type, blob, ContractlessStandardResolver.Options);
        }

        public static ExpandoObject ToExpando(this object _this)
        {
            var blob = MessagePackSerializer.Serialize(_this, ContractlessStandardResolver.Options);
            return MessagePackSerializer.Deserialize<ExpandoObject>(blob, ContractlessStandardResolver.Options);
        }
    }
}