

# Efficient Wait Matching
* Create table wait template
* The service will evaluate match and set data if matched for each wait
* For waits that use the same input/output types the deserialization will made once
* Keep in mind that "One wait match per call per function
* Evaluate match does not need data load
* Serialize pushed call once for same group methods

# Serialization
* Fast serialization and deserialization for [Use MessagePack/BinaryPack] 
	* StateObject https://github.com/rikimaru0345/Ceras
	* Wait ExtraData
	* Pushed Call [Info/Data]

# Expression Serialization
	* https://github.com/reaqtive/reaqtor/tree/main/Nuqleon/Core/LINQ/Nuqleon.Linq.Expressions.Bonsai

# Separate store for state objects
* Use NoSql databases to store states

* Fast JSON serialization and deserialization for
	* StateObject https://github.com/rikimaru0345/Ceras
	* Wait ExtraData
	* Expressions


* Evaluate expression tree as where clause
	* Translate expression tree to Mongo query
	* https://stackoverflow.com/questions/7391450/simple-where-clause-in-expression-tree





## In current
* Expression Serialization
	* https://github.com/reaqtive/reaqtor/tree/main/Nuqleon/Core/LINQ/Nuqleon.Linq.Expressions.Bonsai.Serialization
* Expression Trees Serialization
	* https://github.com/esskar/Serialize.Linq/blob/master/src/Serialize.Linq.Tests/ExpressionSerializerTests.cs

* Query to SQL 'DataContext.GetCommand(IQueryable)'
	* https://learn.microsoft.com/en-us/dotnet/api/system.data.linq.datacontext.getcommand?view=netframework-4.8.1

