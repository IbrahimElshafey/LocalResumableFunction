
# Write Unit Tests for basic scenarios:
* Sequence
* Wait all
* Wait all as first 
* Wait first in many
* Wait first in many as first
* Wait function
* Wait many functions
* Wait first function
* Replay [to,after,before] for types:
	* To
	* After
	* Before
* Replay in sub functions
* Multiple sub functions levels
* Test loops
* Test Replay
* Write test cases for expressions rewrite because it's critical part
* Sub functions tests
	* Pushed call activate one instance,what if same function and sub fnction wait the same wait
* Attributes test
* Scan results exist
# Performance Test
* 1 Million active wait for same function
	* With mandatory part exist
	* Without mandatory part exist
* 1000 resumable function test
* 1000 pushed call per second test
* One method activate 1000 function type

# Problem to solve 
* We can't mock Resumable function class dependacies and use the mocks because off attributes checks
* Could we fake dll and then mock faked instances
* No Fakes or Mokes libraries you should write your fake implementation for your interfaces