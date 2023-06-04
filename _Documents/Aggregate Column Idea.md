# Aggregate Column Feature (will be separate project)
* Create table `AggregateDefinition` with columns
	* EntityName (sush as Orders)
	* AggregateName (such as FailedOrdersCount,TotalPayments)
	* AggregateFunction (such as SUM, COUNT, AVG, LAST,...) or user defined
	* ResetValue (such as -100 default null)
	* KeepValuesAfterAggregation (true or false)
* Create table `AggregateValues` with columns 'No update just insersion and delete'
	* AggregateDefinitionId
	* Number Value
	* CreationDate
	* IsAggregation (boolean)

## Example
* Define Aggregate `DefineAggregate(forTable: "Post",name: "LikesCount",aggregateFunction: "SUM")`
* Use when like button click `post.AddAggregateValue("LikesCount",1)`
* Use when unlike button clicked `post.AddAggregateValue("LikesCount",-1)`
* When user totally chnaged the content of the post `post.ResetAggregate("LikesCount")`
* When you wanty to display like counts `post.GetAggregate("LikesCount")`

# Table File Log
* This will be a separate test project to know more about reading/writing to files
* How database ACID work
* Use BinaryPack to save record
