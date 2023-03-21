# Keywords
* Engine: component responsible for running and resume function execution.
* Event Provider: is a component that push events to the engine.
* Queung service: is a way to separate engine and providers.
* Event: Plain object but contains a property for it's provider.



# What are the expected types and resources for events?
* Any implementation for `IEventProvider` interface that push events to the engine such as:
* A WEB proxy listen to server in outs HTTP calls.
* File watcher.
* Long pooling service that monitor a database table.
* Timer service.