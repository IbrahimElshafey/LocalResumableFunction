# Distributed Services and Resumable Function
**Work on docs**
* You can wait method in another service
``` C#
//you will create empty implementation for method you want to wait from the external
public class ExternalServiceClass
{
	//The [ExternalWaitMethod] attribute used to exactly point to external method you want to wait
	//The class name is the full class name in the external service
	//The AssemblyName is the assembly name for the external service
	//The method name must be the same as the on in the external service
	//The method return type name and input type name must be the same as the on in the external service
	[ExternalWaitMethod(ClassName = "External.IManagerFiveApproval",AssemblyName ="SomeAssembly")]
	public bool ManagerFiveApproveProject(ApprovalDecision args)
	{
		return default;
	}
}
/// you can wait it in your code normally
yield return
	Wait<ApprovalDecision, bool>("Manager Five Approve Project External Method", 
	new ExternalServiceClass().ManagerFiveApproveProject)//here
		.MatchIf((input, output) => input.ProjectId == CurrentProject.Id)
		.SetData((input, output) => ManagerFiveApproval == output);
```
* Wait method in another service [browse exmaples folder in source code. I'll add docs later.]


