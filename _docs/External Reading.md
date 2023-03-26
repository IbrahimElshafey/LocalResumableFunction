
# Workflow Patterns
* http://www.Functionpatterns.com/patterns/control/
* http://www.Functionpatterns.com/patterns/
* https://www.ariscommunity.com/users/sstein/2010-07-20-bpmn-2-Workflow-patterns





# Code generation
* https://github.com/Testura/Testura.Code
# Generate swagger definition for created API


# Hangfire
* https://code-maze.com/hangfire-with-asp-net-core/
# Event Types
* API Called [HTTP Listener]
* API Call to the engine [WebHook]
* RabbitMQ or any service bus [Subscribe to event]
* File/Folder Events [Watcher]
* Time Event [Timer Service Event]
	* https://www.hangfire.io/
* Any implementation for IEventProvider interface



Proxy Creation
https://stackoverflow.com/questions/15733900/dynamically-creating-a-proxy-class
https://devblogs.microsoft.com/dotnet/migrating-realproxy-usage-to-dispatchproxy/

# Async 
* Lazy Task
* https://itnext.io/writing-lazy-task-using-new-features-of-c-7-7e9b3f2fda07
* 

# IL Rewrite 
* https://github.com/Fody/Fody
* MonoCeceil

# Expression Trees
* [Converting Expression Trees to Source Code]https://bagoum.medium.com/c-heresy-converting-expression-trees-to-source-code-1082ba8963a6
* [FastExpressionCompiler]https://github.com/dadhi/FastExpressionCompiler
* STAR[Expression Tree Visualizer]https://github.com/zspitz/ExpressionTreeVisualizer

# Get all method calls
* https://stackoverflow.com/questions/57118269/get-all-method-calls

# Database to save FunctionData
https://www.litedb.org/


# Generic Varince
https://agirlamonggeeks.com/2019/06/04/cannot-implicitly-convert-type-abc-to-iabc-contravariance-vs-covariance-part-2/



# Resolve by name
https://stackoverflow.com/questions/39072001/dependency-injection-resolving-by-name

# RPC
* CoreRPC https://github.com/kekekeks/CoreRPC
* https://github.com/RandomEngy/PipeMethodCalls

* Listen to local in outs http and tcp calls
	* https://www.meziantou.net/observing-all-http-requests-in-a-dotnet-application.htm
	* https://github.com/justcoding121/titanium-web-proxy


* Evaluate expression tree as where clause
	* Translate expression tree to Mongo query
	* https://stackoverflow.com/questions/7391450/simple-where-clause-in-expression-tree
* Save expression trees to database
	* https://stackoverflow.com/questions/23253399/serialize-expression-tree
	* https://github.com/esskar/Serialize.Linq

* Generic host
* https://learn.microsoft.com/en-us/aspnet/core/fundamentals/host/generic-host?view=aspnetcore-7.0

* How to: Examine and Instantiate Generic Types with Reflection
https://learn.microsoft.com/en-us/dotnet/framework/reflection-and-codedom/how-to-examine-and-instantiate-generic-types-with-reflection


* Use app domain
https://stackoverflow.com/questions/6258160/unloading-the-assembly-loaded-with-assembly-loadfrom
Note that .NET Core supports only a single application domain.


# assemblies-load-in-dotnet
* https://michaelscodingspot.com/assemblies-load-in-dotnet/

# EF Core
* [Multiple Providers]https://blog.jetbrains.com/dotnet/2022/08/24/entity-framework-core-and-multiple-database-providers/
* [Tools Commands] https://learn.microsoft.com/en-us/ef/core/cli/powershell
* [Using a Separate Migrations Project] https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/projects?tabs=dotnet-core-cli
* [Converters] https://learn.microsoft.com/en-us/ef/core/modeling/value-conversions?tabs=data-annotations
* [EF 7 Features] https://learn.microsoft.com/en-us/ef/core/what-is-new/ef-core-7.0/whatsnew#json-columns




* Background tasks with hosted services in ASP.NET Core
https://docs.microsoft.com/en-us/aspnet/core/fundamentals/host/hosted-services?view=aspnetcore-6.0&tabs=visual-studio
# Dynamic class loading (we will not need this)
* [Using reflection] (https://www.codeproject.com/Articles/13747/Dynamically-load-a-class-and-execute-a-method-in-N)
* [Using dynamic runtime] ()
* Using Expresssion Trees
	* https://agileobjects.co.uk/readable-expression-trees-debug-visualizer

	
# Find a service bus
	*Zebus https://github.com/Abc-Arbitrage/Zebus (no broker)

	* Silverback https://silverback-messaging.net/ A simple but feature-rich message bus "Broker" for .NET core (Apache Kafka, MQTT and RabbitMQ)
	
	* [*]SlimMessageBus https://github.com/zarusz/SlimMessageBus (Apache Kafka, Azure EventHub, MQTT/Mosquitto, Redis Pub/Sub),and provides request-response implementation over message queues.
		* wit Mosquitto http://www.steves-internet-guide.com/install-mosquitto-broker/#manual
		* http://www.steves-internet-guide.com/mosquitto_pub-sub-clients/

	* https://github.com/zarusz/SlimMessageBus
	* Use https://github.com/Cysharp/MessagePipe to send events to the engine
	* https://stackoverflow.com/questions/58549763/how-should-ipc-be-handled-in-net-core
