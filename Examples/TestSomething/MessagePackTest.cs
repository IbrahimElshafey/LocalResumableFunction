using MessagePack;
using MessagePack.Resolvers;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
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
            var model = new { Name = "foobar", Items = new[] { (int)1, 10, 100, 1000 }, BB = (byte)10, DT = DateTime.Today };
            var blob = MessagePackSerializer.Serialize(model, ContractlessStandardResolver.Options);
            var json = MessagePackSerializer.ConvertToJson(blob);
            // Dynamic ("untyped")
            var dynamicModel = MessagePackSerializer.Deserialize<dynamic>(blob, ContractlessStandardResolver.Options);

            // You can access the data using array/dictionary indexers, as shown above
            Console.WriteLine(dynamicModel["Name"]); 
            Console.WriteLine(dynamicModel["Items"][0]);
            Console.WriteLine(dynamicModel["BB"].GetType()); // 100
            Console.WriteLine(dynamicModel["DT"].GetType()); // 100
        }


    }
}
