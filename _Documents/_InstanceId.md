* Delay processing if the scan is in progress
* Process same service waits in bulk
	* Add `ServiceId` to `PushedCallWaits` table
* Wait default behavior is to activate one instance of function type
	* Add bool ActivateOneInstance to method wait

# Instance Id


* When method group matched the paths will be evaluated and expected instance Ids will be extracted
* The waits search will be done like this:
	* MethodGroup == PushedCall.MethodGroup
	* Status == Waiting
	* InstanceId in (PushedCall.ExpectedInstanceIds)
	* Use function Id also
	* 
* Mehtod wait will have a prop for `InstanceId Path` that filled after evaluating SetData expression


* When rewrite match expression you will generate InstanceId like `#input.ProjectId==190#`
* If multiple waits it will be like `#input.ProjectId==190##input.UserId==889#` duplication will be killed
* Instance will be match if instance Id contains the calculated PushedCall.ExpectedInstanceId
* We can name it `ComputedId`
modelBuilder
   .Entity<Entity>()  
   .HasIndex(entity => new { entity.COL1, entity.COL2 }).IsUnique();

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