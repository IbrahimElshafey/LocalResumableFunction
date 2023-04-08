# Simple Stupid Locking
* Table with no index and Insert/Delete only (No Update)
* Indexed string column with fixed length,Creation date column
* Process that may cause lock add row at starting contains (EntityName-EntityID)
* Process check if row exist the entity is locked
* If no row exist then process can start
* After process finished the row will be deleted
* Background process to delete dead locks