# Todo

* Delete first wait subwaits if group
* Validate waits
	* Wait same wait twice in group is not valid
	* Wait name can't duplicate in same function

* Time wait exact match

* Activate one start wait if multiple exist for same method

* Handle concurrency problems
	* optimistic or pessimistic fro cases below:
	* Two waits matched for same FunctionState
	* First wait closed but new request come before create new one
	* Update pushed methods calls counter


* Track code changes
	* GUID for methods for easy track 

* Remove uniqe for method hash

* Logging and handle exception

* Create nuget package

* Save function state all fields [public and non public]
* Find fast and best object serializer
* Move completed function instance to Recycle Bin
* Delete PushedMethodsCalls after processing background job
* Parameter check lib use
* Add UI Project
	* Monitor active resumable functions
		* Incoming waits
		* Past waits
		* Status
	* List completed functions
	* Verify scanned methods 
		* delete methods not in code
		* verify method signatures
		* verofy start waits exist in db
	* Register External Method
		* For example github web hook


* Speed Analysis	
	* https://learn.microsoft.com/en-us/ef/core/logging-events-diagnostics/event-counters?tabs=windows


* Hnagfire problem
Category: ResumableFunctions.Core.Scanner
EventId: 0

Error when scan [TestApi2]

Exception: 
System.InvalidOperationException: A second operation was started on this context instance before a previous operation completed. This is usually caused by different threads concurrently using the same instance of DbContext. For more information on how to avoid threading issues with DbContext, see https://go.microsoft.com/fwlink/?linkid=2097913.
   at Microsoft.EntityFrameworkCore.Infrastructure.Internal.ConcurrencyDetector.EnterCriticalSection()
   at Microsoft.EntityFrameworkCore.Query.Internal.SingleQueryingEnumerable`1.AsyncEnumerator.MoveNextAsync()
   at Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.ToListAsync[TSource](IQueryable`1 source, CancellationToken cancellationToken)
   at Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.ToListAsync[TSource](IQueryable`1 source, CancellationToken cancellationToken)
   at ResumableFunctions.Core.Data.MethodIdentifierRepository.GetMethodIdentifierFromDb(MethodData methodId) in C:\LocalResumableFunction\ResumableFunctions.Core\Data\MethodIdentifierRepository.cs:line 16
   at ResumableFunctions.Core.Scanner.AddMethodWait(MethodData methodData) in C:\LocalResumableFunction\ResumableFunctions.Core\Scanner.cs:line 208
   at ResumableFunctions.Core.Scanner.RegisterMethodWaitsInType(Type type) in C:\LocalResumableFunction\ResumableFunctions.Core\Scanner.cs:line 201
   at ResumableFunctions.Core.Scanner.Start() in C:\LocalResumableFunction\ResumableFunctions.Core\Scanner.cs:line 52
