using FastExpressionCompiler;
using MessagePack;
using MessagePack.Resolvers;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace TestSomething
{
    internal class MessagePackTest
    {
        public void Test()
        {
            // Sample blob.
            var model = new
            {
                Name = "foobar",
                Items = new[] { 1, 10, 100, 1000 },
                BB = (byte)10,
                DT = DateTime.Today,
                Point = new Point { X = 100, Y = 200 },
                Comp = new { Po = new Point { X = 300, Y = 400 } }
            };
            var blob = MessagePackSerializer.Serialize(model, ContractlessStandardResolver.Options);
            var json = MessagePackSerializer.ConvertToJson(blob);
            // Dynamic ("untyped")
            var dynamicModel = MessagePackSerializer.Deserialize<ExpandoObject>(blob, ContractlessStandardResolver.Options);

            // You can access the data using array/dictionary indexers, as shown above
            //Console.WriteLine(dynamicModel["Name"]); 
            //Console.WriteLine(dynamicModel["Items"][0]);
            //Console.WriteLine(dynamicModel["BB"].GetType());
            //Console.WriteLine(dynamicModel["DT"].GetType());
            var m = new ExpandoAccessor(dynamicModel);
            Console.WriteLine(m["Name"]);
            Console.WriteLine(m["BB"]);
            Console.WriteLine(m["DT"]);
            Console.WriteLine(m["Point.X"]);
            Console.WriteLine(m["Comp.Po.X"]);
          
            m["Point.X"] = 1000;
            m["Comp.Po.X"] = 2000;
            Console.WriteLine(m["Point.X"]);
            Console.WriteLine(m["Comp.Po.X"]);

            Expression<Func<ExpandoAccessor, bool>> exp = x => Convert.ToInt32(x["Point.X"]) + 90 == 190;
            var compiledExp = exp.CompileFast();
            var result = compiledExp.Invoke(m);
            Console.WriteLine(result);
        }

        private Expression<Func<bool>> GetQuery()
        {
            return () => DateTime.Today.Day > 7;
        }
    }

    public class ExpandoAccessor
    {
        //todo: handle array item access
        public object this[string index]
        {
            get
            {
                var parts = index.Split('.');
                object? result = _dictionary[parts[0]];
                var parent = parts.Length > 1 ? (IDictionary<object, object>)_dictionary[parts[0]] : null;
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
                    _dictionary[index] = value;
                else
                {
                    var parent = (IDictionary<object, object>)_dictionary[parts[0]];
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
        private readonly IDictionary<string, object> _dictionary;
        public ExpandoAccessor(ExpandoObject expandoObject)
        {
            _dictionary = (IDictionary<string, object>)expandoObject;
        }
    }
}
