DECLARE db_cursor CURSOR FOR
SELECT name
FROM master.dbo.sysdatabases
WHERE name LIKE '%_test' OR name LIKE '%_HangfireDb' OR name IN ('ResumableFunctionsData')

DECLARE @dbname NVARCHAR(1000)

OPEN db_cursor
FETCH NEXT FROM db_cursor INTO @dbname

WHILE @@FETCH_STATUS = 0
BEGIN
    -- Set the database to single user mode to close existing connections
    DECLARE @alterSql NVARCHAR(1000)
    SET @alterSql = 'ALTER DATABASE ' + QUOTENAME(@dbname) + ' SET SINGLE_USER WITH ROLLBACK IMMEDIATE'
    EXEC(@alterSql)

    -- Drop the database
    DECLARE @dropSql NVARCHAR(1000)
    SET @dropSql = 'DROP DATABASE ' + QUOTENAME(@dbname)
    EXEC(@dropSql)

    FETCH NEXT FROM db_cursor INTO @dbname
END

CLOSE db_cursor
DEALLOCATE db_cursor
