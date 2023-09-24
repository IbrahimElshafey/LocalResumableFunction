# Side by Side Versions
* Add new version addd all active waits will be assigned to the old version
* When call pushed the system passes the matched waits to the corresponding versions
* Each version has a puplish date and deactivation date
* Wait that created in range between puplish date and deactivation date will be handled by the version corresponding
* The current version dectivation date is Date.Max
* I will use service fabric to host services
* When to mark version as dead? when no active waits exist and we don't need this

# Publish new version to production
* What about function class migration that is serialized
* What happedn when resumable function database schema changed with a new version
* What about HangfireDb schema
* How to receive calls while upgrading?