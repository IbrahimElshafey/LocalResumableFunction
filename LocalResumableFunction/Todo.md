# Plan
* Precompile task to add first waits:
	* Find method with attribute [ResumableMethodStart] in class derived from `ResumableFunctionLocal`
* Use fody to wrap methods that push events
	* https://github.com/vescon/MethodBoundaryAspect.Fody [will be used]
	* https://github.com/Fody/MethodDecorator
* Database
	* Function runtime data 
	* Waits
