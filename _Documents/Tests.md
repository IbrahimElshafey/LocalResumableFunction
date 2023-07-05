# Write Unit Tests for basic scenarios:


* Sub functions tests
	* Pushed call activate one instance,what if same function and sub fnction wait the same wait

* Test loops

* Write test cases for expressions rewrite because it's critical part
* Attributes test
* Scan results is correct

# Performance Test
* 1 Million active wait for same function
	* With mandatory part exist
	* Without mandatory part exist
* 1000 resumable function test
* 1000 pushed call per second test
* One method activate 1000 function type

# Problem to solve 
* We can't mock Resumable function class dependencies and use the mocks because of attributes checks
* Could we fake dll and then mock faked instances?
* No Fakes or Mokes libraries you should write your fake implementation for your interfaces