* Dont use converters for serialization
	* Shadow property and set/get on demand
	* https://learn.microsoft.com/en-us/dotnet/api/microsoft.entityframeworkcore.diagnostics.imaterializationinterceptor?view=efcore-7.0

# WaitTemplate
* Match Expression (Need load state)
* Set Data Expression (Need load state)
* Hash for (Match Expression & Set Data Expression)
* Extract Mandatory Part Expression
* Function Id
* MethodGroupId Id

# When process call
* Get MethodGroupId
* Get Mandatory Part Expressions
	* Will return WaitTemplate
	* WaitTemplate.Where(x => x.MethodGroupId = MethodGroupId)
* Apply expression to pushed call
	* Will return condition parts (Mandatory part string,Function Id)
* Will query waits to get
	* WHEN MandatoryPartValue & FunctionId & MethodGroupId
* Will group waits by FunctionId
* For each group one match must be success
* For each group 
	* Deserialize pushed call once for same group methods
	* The state object will be deserialized to class type
	* The match expression will be evaluated to fiund the first match
	* The other will be marked as unmateched
	* The set data will be called
	* Continue processing the wait ...

# Efficient Wait Matching
* Evaluate match does not need data load


# Serialization
* Fast serialization and deserialization for [Use MessagePack] 
	* StateObject https://github.com/rikimaru0345/Ceras
	* Wait ExtraData
	* Pushed Call [Info/Data]
* Mandatory part extractor will need to be updated

* Warning if wait template not have mandatory part
* Warning if pushed call mandatory part missing

# Expression Serialization [Will be separate project]
	* https://github.com/reaqtive/reaqtor/tree/main/Nuqleon/Core/LINQ/Nuqleon.Linq.Expressions.Bonsai
	* NodesStack: Node types as array int/byte
	* Varaibels array:Varaibels as array object
	* Varaibels references stack int array

