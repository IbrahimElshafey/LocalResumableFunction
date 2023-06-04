

# WaitTemplate
* Match Expression (Need load state)
* Set Data Expression (Need load state)
* Function Id
* MethodGroupId Id (Index)
* ##Hash Id (Match Expression & Set Data Expression hashing)
* Extract Mandatory Part From Pushed Call Expression (Work on dynamic pushed call)
* Extract Mandatory Part From Function Instance Expression (Work on dynamic pushed call)
* Dynamic Match Expression (Work on dynamic objects pushed call dynamic,state object dynamic)
* Dynamic Set Data Expression (Work on dynamic objects pushed call dynamic,state object dynamic)

# When process call
* Get MethodGroupId
* Get Mandatory Part Expressions
	* WaitTemplate.Where(x => x.MethodGroupId)
	* Will return WaitTemplate
* Apply Mandatory part expression to pushed call
	* Will return mandatory part (Mandatory part string,Function Id)
* Query waits table
	* Where (MandatoryPartValue & FunctionId & MethodGroupId)
	* Waits result will be marked as partial matches
* Will group waits by FunctionId
* For each group one match must be success
* Dynamic evalution for each group (If dymaic match != null)
	* Check waits against dynamic match expression if exist
	* Find first match
	* If match found and set data using dynamic expression not null >> set data
	* The other not matched will be marked as cancled
* Static evalution for each group
	* Deserialize pushed call to `InputOutput` strong type
	* For each Wait deserialize state object to `FunctionContainerClass` strong type
	* Check waits against static match expression if exist
	* Find first match
	* If match found >> set data using set data expression
	* The other not matched will be marked as cancled
* Continue processing matched wait



# Efficient Wait Matching
* Dont use converters for serialization

# Serialization
* Fast serialization and deserialization for [Use MessagePack] 
	* StateObject https://github.com/rikimaru0345/Ceras
	* Wait ExtraData
	* Pushed Call [Info/Data]


* Warning if wait template not have mandatory part
* Warning if pushed call mandatory part missing

# Expression Serialization [Will be separate project]
	* https://github.com/reaqtive/reaqtor/tree/main/Nuqleon/Core/LINQ/Nuqleon.Linq.Expressions.Bonsai
	* NodesStack: Node types as array int/byte
	* Varaibels array:Varaibels as array object
	* Varaibels references stack int array

