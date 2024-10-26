# How it works
The library uses an [IAsyncEnumerable](https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.iasyncenumerable-1?view=net-7.0) generated state machine to implement a method that can be paused and resumed. An IAsyncEnumerable is a type that provides a sequence of values that can be enumerated asynchronously. A state machine is a data structure that keeps track of the current state of a system. In this case, the state machine keeps track of the current state of where function execution reached.

The library saves a serialized object of the class that contains the resumable function for each resumable function instance. The serialized object is restored when a wait is matched and the function is resumed.

One instance created each time the first wait matched for a resumable function.

If the match expression is not strict, then a single call may activate multiple waits. However, only one wait will be selected for each function. This means that pushed call will activate one instance per function, but they can activate multiple instances for different functions.
