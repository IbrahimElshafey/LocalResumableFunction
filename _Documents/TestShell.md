# Todo
* RegsiterAssemblies(names)
* RegsiterTypes(types)
* RegsiterMethods(methods)
* Simulate Method Call (Method, input, output)
* Get instances for pushed call
* Update instance data and save it back
* Get matched waits for pushed call
* Query Waits DB
     * Get Waits
     * Get Pushed Calls
     * Get States
* Query Hangfire DB
     * Get Jobs

# Critical problems
* How to know that hangfire background job finished
    * Check the JobStorage.Current.GetMonitoringApi()//https://discuss.hangfire.io/t/checking-for-a-job-state/57/7
* 
