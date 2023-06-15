# Work On
* Time wait custom processing
* Write unit test for all waiting scenarios


# When process pushed call received for service (Each service process it's waits)
* This will be in `WaitsService.GetWaitsIdsForMethodCall`
* Get MethodGroupId by MethodUrn
* Get Wait templates for method group
	* MethodWaitTemplates.Where(x => x.MethodGroupId)
	* Will return WaitTemplate list
* Group templates by MethodId
* Apply CallMandatoryPartExpressionDynamic extractor expression to pushed call
	* Will return mandatory part (Mandatory part string,Function Id)
	* If mandatory part is null or contains null it will translated to string NULL
* Query waits table
	* Where (MandatoryPartValue in Values & MethodId & FunctionId & MethodGroupId) pairs
	* Waits result will be marked as partial matches or full match based on template `IsMandatoryPartFullMatch` prop

# Process of same type for a function
* Will group waits by `WaitTemplateId`
* For each group one match must be success
* Match evalution for each group
	* Deserialize pushed call to `InputOutput` strong type
	* For each Wait deserialize state object to `FunctionContainerClass` strong type
	* Check waits against static match expression if exist
	* Find first match
	* If match found >> set data using set data expression
	* The other not matched will be marked as canceled in same GroupId
* Continue processing matched wait

# Efficient Wait Matching
* Use Method Wait Template Table

* Optimize wait table indexes to enable fast wait insertion
	* Remove index 
		* [ParentWaitId] in [dbo].[Waits]