# Work on Closures
* does closure may be diffrent for class in same wait??
* Clear scan locks for service on scan start
* Match expression tests
	* Mandatory parts rewrite and tests
		* inOut.prop + loclas.Prop == value
		* instance.Method(inOut.prop) == value
	* Warning of closure in math that not in a resumable function or embed it
	* Test complex match expressions

* Can we access closure in wait constructor?
* Closure in loops not saved immediately after creating the wait

* Test what happen when
	* yield return ExternalClass.MethodWait();

# Todo
* Use same dll in two different services must be allowed
* How to check that pushed calls processed by all affected services
* Roslyn Analyzer