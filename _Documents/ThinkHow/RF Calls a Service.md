# When to call a service from a RF 
```C#
Wait UserApplicantRegistration();
var score = CalcScore();
if(score > 70)
	SendForReview();
```
* If the `CalcScore` or `SendForReview` failed how to recover and resume function excecution?
* I'll use MassTransit to call the service and use the `Request-Response` pattern to get the result.
* I'll use MassTransit on clinet side to call the service and on the server side to receive the call.
* MassTransit will handle the retry policy and the failure recovery.