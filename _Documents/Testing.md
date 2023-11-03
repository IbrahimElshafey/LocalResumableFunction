# Test Shell Todo
* Get instances for pushed call
* Get matched waits for pushed call



# Write Unit Tests for basic scenarios:
* Sub functions tests
	* Pushed call activate one instance,what if same function and sub fnction wait the same wait


* Attributes test
* Validation, errors, and exceptions is work fine
* Scan results are correct

# Performance Test
* 1 Million active wait for same function
	* With mandatory part exist
	* Without mandatory part exist
* 500 active resumable function
* 100 resumable function activated by one pushed call
* 10000 pushed call per second test

# Problem to solve 
* We can't mock Resumable function class dependencies and use the mocks because of attributes checks
* Could we fake dll and then mock faked instances?
* No Fakes or Mokes libraries you should write your fake implementation for your interfaces

# Generate test code for resumable function
* I plan to auto-generate code for test