# Plan
* Precompile task to add first waits:
	* Find method with attribute [ResumableMethodStart] in class derived from `ResumableFunctionLocal`
* Use fody to wrap methods that push events
	* https://github.com/vescon/MethodBoundaryAspect.Fody [will be used]
	* https://github.com/Fody/MethodDecorator
* Database
	* Function runtime data 
	* Waits


# To copy from ServerResumableFunction
* IWaitsRepository _waitsRepository;
* EngineDataContext _context;
* WhenProviderPushEvent(PushedEvent pushedEvent)

# Find 
* Best local DB
* Run some code after build


https://weblog.west-wind.com/posts/2022/Jun/07/Runtime-CSharp-Code-Compilation-Revisited-for-Roslyn#compiling-code-with-raw-roslyn