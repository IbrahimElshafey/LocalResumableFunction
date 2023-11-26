DECLARE db_cursor CURSOR FOR
SELECT name
FROM master.dbo.sysdatabases
WHERE name LIKE '%_test' or name like '%_HangfireDb' or name in ('ResumableFunctionsData')

DECLARE @dbname NVARCHAR(1000)

OPEN db_cursor
FETCH NEXT FROM db_cursor INTO @dbname

WHILE @@FETCH_STATUS = 0
BEGIN
    DECLARE @sql NVARCHAR(1000)
    SET @sql = 'DROP DATABASE ' + QUOTENAME(@dbname)
    EXEC(@sql)

    FETCH NEXT FROM db_cursor INTO @dbname
END

CLOSE db_cursor
DEALLOCATE db_cursor
