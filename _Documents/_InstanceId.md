


modelBuilder
   .Entity<MethodWait>()  
   .HasIndex(entity => new { MethodWaitId, entity.COL2 }).IsUnique();

RequestBody.SelectToken("complex.path.forProp").ToString()
https://www.newtonsoft.com/json/help/html/SelectToken.htm


* Reducing Serialized JSON Size
https://www.newtonsoft.com/json/help/html/ReducingSerializedJSONSize.htm
* LINQ to JSON
* https://www.newtonsoft.com/json/help/html/LINQtoJSON.htm
* Expression Trees Serialization
https://github.com/esskar/Serialize.Linq/blob/master/src/Serialize.Linq.Tests/ExpressionSerializerTests.cs

Edit Expressions
https://github.com/mcintyre321/metalinq
Fast Compiling
https://github.com/dadhi/FastExpressionCompiler