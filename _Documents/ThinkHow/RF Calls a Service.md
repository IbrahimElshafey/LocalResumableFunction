﻿# When to call a service from a RF 
```C#
Wait UserApplicantRegistration();
var score = CalcScore();
if(score > 70)
	SendForReview();
```
* If the `CalcScore` or `SendForReview` failed how to recover and resume function execusion?