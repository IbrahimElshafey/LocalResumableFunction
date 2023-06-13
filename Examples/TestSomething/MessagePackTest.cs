using MessagePack;
using MessagePack.Resolvers;
using System.Drawing;
using System.Dynamic;
using System.Linq.Expressions;

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
}
