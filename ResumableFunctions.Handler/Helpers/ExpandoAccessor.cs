using System.Dynamic;

namespace ResumableFunctions.Handler.Helpers
{
    public class ExpandoAccessor
    {
        private readonly IDictionary<string, object> _root;
        public ExpandoAccessor(ExpandoObject expandoObject)
        {
            _root = expandoObject;
        }
        //todo: handle array item access
        public object this[string index]
        {
            get
            {
                var parts = index.Split('.');
                object? result = _root[parts[0]];
                var parent = parts.Length > 1 ? (IDictionary<object, object>)_root[parts[0]] : null;
                for (int i = 1; i < parts.Length; i++)
                {
                    var currentProp = parts[i];
                    result = parent[currentProp];
                    parent = result as IDictionary<object, object>;
                }
                return result;
            }

            set
            {
                var parts = index.Split('.');
                if (parts.Length == 1)
                    _root[index] = value;
                else
                {
                    var parent = (IDictionary<object, object>)_root[parts[0]];
                    for (int i = 1; i < parts.Length; i++)
                    {
                        var currentProp = parts[i];
                        if (i == parts.Length - 1)
                            parent[currentProp] = value;
                        parent = parent[currentProp] as IDictionary<object, object>;
                    }
                }
            }
        }
    }
}