# Write ahead log
* Record Structure
	* Type (New Record,Old Record,Delete Record)
	* Record Type 16 Byte Hash
	* Record ID 16 Byte Hash
	* Record Length 8 Bytes
	* Variable Length Byte array for Record Content
* Memory mapped file is used to simulate  file
* Data will written to disk if it exceeded on miga byte or 30 seconds passed after last record insertion
* Compaction process run when insertions goes down for 30 seconds
* While compaction you can provide aggregation function for the a type
* Compaction read log from buttom to top
* While compaction no apped process done