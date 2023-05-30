* Continue work on `Evaluate match expression against json`
* Add new table `Types Schemas` used for
	* Resumable Function Classes
	* Pushed call schema for a method group
* Add to method group table
	* Method signature
	* Pushed call schema
	* List of Match Expressions (in wait)
	* List of Set Data Expressions

# Dynamic class creation
* https://www.dotnetcurry.com/csharp/dynamic-class-creation-roslyn
* https://code-maze.com/create-class-dynamically-csharp/
* https://weblog.west-wind.com/posts/2022/Jun/07/Runtime-CSharp-Code-Compilation-Revisited-for-Roslyn
* https://stackoverflow.com/questions/3862226/how-to-dynamically-create-a-class

* Json to srong type class generated at runtime
* Fast JSON serialization and deserialization for
	* StateObject https://github.com/rikimaru0345/Ceras
	* Wait ExtraData
	* Expressions


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



## In current


* String to query expression
	* https://github.com/zzzprojects/System.Linq.Dynamic.Core
* Fast Lightweight Expression Evaluator
	* https://github.com/mparlak/Flee
* Expression Serialization
	* https://github.com/reaqtive/reaqtor/tree/main/Nuqleon/Core/LINQ/Nuqleon.Linq.Expressions.Bonsai.Serialization