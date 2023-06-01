# WaitForCall
public int ServiceId { get; internal set; }
public int FunctionId { get; internal set; }

# Efficient Wait Matching
* Auto generate computed instance ID
	* Find parts that is mandatory in the match expression
		* Convert each part to true except the one we evaluate will be false
		* Compile and get expression value
		* If false the the part is mandatory
		* If true then it's optional
	* Collect these parts and construct json object where key is pushed call prop and value is the value you get
	* Keys are ordered and values are strings only
	* This column is indexed in waits table
	* Save keys to method group `MethodMatchKeys` table
	* MethodMatchKeys table row contains (MethodGroupId,MatchKeys string array,FunctionId)
	* MatchKeys not duplicate for same (MethodGroupId,FunctionId)
* One Call for Service (No use `ProcessMatchedWait(int waitId, int pushedCallId)` but `ProcessCall(int pushedCallId)`
	* Query waits to get which services will be called
	* Call `ProcessCall(int pushedCallId)` for each service
	* The service will evaluate match and set data if matched for each wait
	* For waits that use the same input/output types the deserialization will made once
	* Keep in mind that "One wait match per call per function
* Evaluate match does not need data load
* Serialize pushed call once for same group methods

* Fast serialization and deserialization for [Use MessagePack] 
	* StateObject https://github.com/rikimaru0345/Ceras
	* Wait ExtraData
	* Expressions
# Separate store for state objects
* Use NoSql databases to store states

* Fast JSON serialization and deserialization for
	* StateObject https://github.com/rikimaru0345/Ceras
	* Wait ExtraData
	* Expressions




* Evaluate expression tree as where clause
	* Translate expression tree to Mongo query
	* https://stackoverflow.com/questions/7391450/simple-where-clause-in-expression-tree

* Expresssion Tree To readable string
	* https://agileobjects.co.uk/readable-expression-trees-debug-visualizer



## In current
* Expression Serialization
	* https://github.com/reaqtive/reaqtor/tree/main/Nuqleon/Core/LINQ/Nuqleon.Linq.Expressions.Bonsai.Serialization
* Expression Trees Serialization
	* https://github.com/esskar/Serialize.Linq/blob/master/src/Serialize.Linq.Tests/ExpressionSerializerTests.cs
* Project that serialize expression trees
	* https://reaqtive.net/blog/2021/05/sequences-linq-rx-reaqtor-part-05-remotable-expressions

* Query to SQL 'DataContext.GetCommand(IQueryable)'
	* https://learn.microsoft.com/en-us/dotnet/api/system.data.linq.datacontext.getcommand?view=netframework-4.8.1

