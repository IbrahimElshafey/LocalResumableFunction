* Validate wait name duplication 
* Call same sub resumable function many times
	* Replay Cancel?
	* Replay Name Duplication?
	* Review Wait name duplication expected scenario 
- Same wait name in two sub resumable function
- Same wait name in same sub resumable function called twice

# Closure Bug
* Add scope continuation tests for:
	- Global Scope
	- Local Closure scope
	- In sequance
	- In group
	- In function
	- Within After Match Call [May update local or global state]
	- Within Cancel Call [May update local or global state]
	- Within Group match filter [May update local or global state]


- Multiple replay and local variables

