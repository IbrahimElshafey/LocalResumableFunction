using Ceras;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace TestSomething
{
    class Person { public string Name; public int Age; }
    internal class CerasTest
    {
        public void Run()
        {

            var p = new Person { Name = "riki", Age = 5 };
            var ceras = new CerasSerializer();
            var bytes = ceras.Serialize(p);
            var p2 = ceras.Deserialize<Person>(bytes);
            Expression exp = () => 10 + 12;
            bytes = ceras.Serialize(exp);
            var exp2=ceras.Deserialize<LambdaExpression>(bytes);
        }
    }
}
