using FastExpressionCompiler;
using MessagePack;
using MessagePack.Resolvers;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
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
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace TestSomething
{
    internal class MessagePackTest
    {
        public void Test()
        {
            // Sample blob.
            var model = new Model
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
            Console.WriteLine(dynamicModel.Get("Name"));
            Console.WriteLine(dynamicModel.Get("BB"));
            Console.WriteLine(dynamicModel.Get("DT"));
            Console.WriteLine(dynamicModel.Get("Point.X"));
            Console.WriteLine(dynamicModel.Get("Comp.Po.X"));

            dynamicModel.Set("Point.X", 1000);
            dynamicModel.Set("Comp.Po.X", 2000);
            Console.WriteLine(dynamicModel.Get("Point.X"));
            Console.WriteLine(dynamicModel.Get("Comp.Po.X"));

            var modelBack = dynamicModel.ToObject<Model>();
            var modelBack2 = dynamicModel.ToObject(typeof(Model));
        }

        private Expression<Func<bool>> GetQuery()
        {
            return () => DateTime.Today.Day > 7;
        }
    }

    public static class ExpandoExtensions
    {
        public static object Get(this ExpandoObject _this, string path)
        {
            var root = (IDictionary<string, object>)_this;
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
    }


    public class Model
    {
        public string Name { get; set; }
        public int[] Items { get; set; }
        public byte BB { get; set; }
        public DateTime DT { get; set; }
        public Point Point { get; set; }
        public object Comp { get; set; }
    }
}
