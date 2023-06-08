* Remove use of value converters
* Remove NewtonSoft and use MessagePack
	* [Yes] Can deserialize data to dynamic object
	* [Yes] Can convert dynamic object to object back
	* [Yes] Does not need attributes
	* Does not need constructor constrains
	* [Yes] Can be shown as Json
	* Can use existing object and fill it
# When Wait Requested
* Calc WaitTemplate Hash(Match Expression & Set Data Expression HASH)
* Search if template is exist
	* MethodWaitTemplates.Where(Hash, FunctionId, MethodGroupId)
* If not exist add it,if exist use it
	* Return MethodWaitTemplateId
* Generate Wait Template
	* Rewrite match expression
	* Generate set data expression
	* Generate dynamic match expression
	* Generate dynamic set data expression
	* Generate get mandatory part form call expression
		* Warning if null
	* Generate get mandatory part form instance expression
		* Warning if null
* Use Wait Template to
	* Extract mandatory part from current function instance and assign to wait `SearchRefinementIdentifier` prop

# When process pushed call received for service
* Get MethodGroupId by MethodUrn
* Get Wait templates for method group
	* MethodWaitTemplates.Where(x => x.MethodGroupId)
	* Will return WaitTemplate list
* Apply CallMandatoryPartExpression extractor expression to pushed call
	* Will return mandatory part (Mandatory part string,Function Id)
	* If mandatory part is null or contains null it will be ignored
* Query waits table
	* Where (MandatoryPartValue & FunctionId & MethodGroupId)
	* Waits result will be marked as partial matches or full match based on template `IsMandatoryFullMatch` prop
* Will group waits by FunctionId
* For each group one match must be success
* Match evalution for each group
	* Deserialize pushed call to `InputOutput` strong type
	* For each Wait deserialize state object to `FunctionContainerClass` strong type
	* Check waits against static match expression if exist
	* Find first match
	* If match found >> set data using set data expression
	* The other not matched will be marked as canceled
* Continue processing matched wait

# Efficient Wait Matching
* Use Method Wait Template Table
* Don't use converters for serialization/deserialization
* Use MessagePack for fast serialization and deserialization of:
	* StateObject
	* Wait ExtraData
	* Pushed Call [Info/Data]
* Test if mandatory expression make full match
	* Translate every mandatory part to true
	* Other parts to false
	* If result is true the full match
	* if else the partial match

* Match vistor will return
	* MatchExpression
	* CallMandatoryPartExpression (Dynamic pushed call JObject)
	* InstanceMandatoryPartExtarctorExpression (Intance Class)
	* Current Mandatory Part 
		* After applying `InstanceMandatoryPartExtarctorExpression` to current instance
	* Mandatory is partial or full

* Optimize wait table indexes to enable fast wait insertion
	* Remove index 
		* [ParentWaitId] in [dbo].[Waits]

# Expression Serialization
* https://github.com/reaqtive/reaqtor/tree/main/Nuqleon/Core/LINQ/Nuqleon.Linq.Expressions.Bonsai

[SerializationConstructor]