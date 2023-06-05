# WaitTemplates Table
* Function Id
* MethodGroupId Id (Index)
* WaitTemplate Hash(Match Expression & Set Data Expression hashing)
* Match Expression (Need load state)
* Set Data Expression (Need load state)
* Extract Mandatory Part From Pushed Call Expression (Work on dynamic pushed call)
	* Return string array
* Extract Mandatory Part From Function Instance Expression (Work on dynamic pushed call)
	* Return string with '#' separator
* Dynamic Match Expression (Work on dynamic objects pushed call dynamic,state object dynamic)
	* Func<ExpandoAccessor instance,ExpandoAccessor pushedCall,bool>
* Dynamic Set Data Expression (Work on dynamic objects pushed call dynamic,state object dynamic)
	* Action<ExpandoAccessor instance,ExpandoAccessor pushedCall>

# When Wait Requested
* Calc WaitTemplate Hash(Match Expression & Set Data Expression HASH)
* Search if template is exist ,Where(FunctionId, MethodGroupId)
* If not exist generate it,if exist use it
* Generate Wait Template
	* Rewite match expression
	* Generate set data expression
	* Generate dynamic match expression
	* Generate dynamic set data expression
	* Generate get mandatory part form call expression
		* Warning if null
	* Generate get mandatory part form instance expression
		* Warning if null
* Use Wait Template to
	* Extract mandatory part from current function instance and assign to wait `SearchRefinementIdentifier` prop

# When pushed call received
* Get MethodGroupId
* Get Wait templates for method group
	* WaitTemplates.Where(x => x.MethodGroupId)
	* Will return WaitTemplate list
* Apply Mandatory part expression to pushed call
	* Will return mandatory part (Mandatory part string,Function Id)
	* If mandatory part is null or contains null it will be ignored
* Query waits table
	* Where (MandatoryPartValue & FunctionId & MethodGroupId)
	* Waits result will be marked as partial matches
* Will group waits by FunctionId
* For each group one match must be success
* Dynamic evalution for each group (If dymaic match != null)
	* Check waits against dynamic match expression if exist
	* Find first match
	* If match found and set data using dynamic expression not null >> set data
		* If the value that will be set is null >> throw exception
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


# Expression Serialization [Will be separate project]
* https://github.com/reaqtive/reaqtor/tree/main/Nuqleon/Core/LINQ/Nuqleon.Linq.Expressions.Bonsai
* NodesStack: Node types as array int/byte
* Varaibels array:Varaibels as array object
* Varaibels references stack int array

