# Empty database (schema only)

This folder is the **empty hospital database** for GitHub.

It has **tables, functions, and indexes only**.  
It does **not** include patients, donors, blood bags, or requests.

## Create a new empty database

1. Open pgAdmin (or `psql`) as `postgres`.
2. Connect to the default `postgres` database and run:

```sql
CREATE DATABASE his_empty;
```

3. Connect to `his_empty` and run the file `empty_schema.sql`.

**pgAdmin:** right-click `his_empty` → Query Tool → Open File → `empty_schema.sql` → Execute.

**Command line** (PowerShell):

```powershell
$env:PGPASSWORD = "your_postgres_password"
& "C:\Program Files\PostgreSQL\17\bin\psql.exe" -h localhost -U postgres -d postgres -c "CREATE DATABASE his_empty;"
& "C:\Program Files\PostgreSQL\17\bin\psql.exe" -h localhost -U postgres -d his_empty -f ".\empty_schema.sql"
```

4. In `KMU.HisOrder.MVC/appsettings.Development.json`, set the database name to `his_empty`.

The app will start with no patients and no Blood Bank records.
