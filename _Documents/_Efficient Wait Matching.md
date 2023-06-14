# Work On
* Test if mandatory expression make full match
	* Translate every mandatory part to true
	* Other parts to false
	* If result is true the full match
	* if else the partial match
* Write unit test for all waiting scenarios
* New Scan should not delete existing method wait and groups if exist
* Clear Hangfire jobs table in debug on start

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