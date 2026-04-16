# Fixing Missing EF Core Migrations Without Data Loss (PostgreSQL)

## A rare, but possible problem

You may encounter a situation where:

* `dotnet ef migrations list` shows **more migrations** than your database has applied
* The `__EFMigrationsHistory` table is **missing entries**
* Running:

  ```bash
  dotnet ef database update
  ```

  reports:

  ```
  No migrations were applied. The database is already up to date.
  ```
* Your application fails to start due to **missing schema changes**

---

## Problem Root Cause

Entity Framework Core determines whether migrations need to be applied by comparing:

* The **current model snapshot** (in code)
* The **actual database schema**

NOT strictly the `__EFMigrationsHistory` table.

This can result in a broken state where:

* The database schema is **out of sync**
* EF believes everything is **already applied**

---

## Solution Overview

We manually:

1. Identified missing migrations
2. Generated a SQL script for those migrations
3. Verified the database state
4. Applied the script manually
5. (Which in turn) syncs the migration history table

---

## Step 1: Identify Missing Migrations

Run:

```bash
dotnet ef migrations list
```

Then check the database:

```pgsql
SELECT * FROM "__EFMigrationsHistory";
```

Compare both lists and identify which migrations are missing.

---

## Step 2: Generate Migration Script

Generate a SQL script from the last applied migration to the latest:

```bash
dotnet ef migrations script <LAST_APPLIED> <LATEST> --idempotent -o fix_migrations.sql
```

Example:

```bash
dotnet ef migrations script 20260313154150_PartDefinitionAndWorkInstructionsUniqueIndex 20260325163434_PartRelationshipsView --idempotent -o fix_migrations.sql
```

---

## Step 3: Backup the Database (IMPORTANT)

Before making changes:

```bash
pg_dump mess_production > backup.sql
```
Or in a more verbose fashion (could be useful)...
```bash
pg_dump -h localhost -p 5432 -U postgres mess_production > backup.sql
```

To restore if needed:

```bash
psql -U postgres mess_production < backup.sql
```

---

## Step 4 (Optional, Usually Not Needed): Inspect Current Database State

If you suspect that you may have partially applied certain migrations, you can run  diagnostic query similar to the ones
below to see which tables and indices exist and which do not. This will be very much situational depending on the migrations
you missed. If you are confident that your generated script from above will work as expected with no modifications needed
(true most of the time), you can skip this step.

```pgsql
-- Tables
SELECT table_name
FROM information_schema.tables
WHERE table_name IN ('SerializablePartRelationship', 'SerializablePartRelationships');

-- Constraints
SELECT conname, conrelid::regclass AS table_name
FROM pg_constraint
WHERE conrelid::regclass::text IN ('SerializablePartRelationship', 'SerializablePartRelationships');

-- Indexes
SELECT indexname, tablename
FROM pg_indexes
WHERE indexname LIKE 'IX_SerializablePartRelationship%';

-- Columns
SELECT column_name
FROM information_schema.columns
WHERE table_name = 'PartDefinitions'
  AND column_name = 'IsSerialNumberUnique';

SELECT column_name
FROM information_schema.columns
WHERE table_name = 'AspNetUsers'
  AND column_name = 'DarkMode';

-- Views
SELECT table_name
FROM information_schema.views
WHERE table_name = 'PartRelationshipsView';
```

---

## Step 5: Interpret Results

### ✅ Ideal Case (What We Had)

* Old table exists (`SerializablePartRelationship`)
* New table does NOT exist
* Old indexes exist
* Constraints exist
* New columns do NOT exist
* View does NOT exist

**Note**: This means migrations were **never applied** → safe to run script as-is

---

### ⚠️ Partial Migration Case

If you see a mix of:

* New + old table names
* Missing constraints or indexes
* Some columns already exist

**Note**: You must **harden the script** (add `IF EXISTS` guards)

---

### ❌ Fully Applied But Missing History

If everything exists already:

👉 DO NOT run the script
Instead, manually insert missing rows into `__EFMigrationsHistory`

---

## 🚀 Step 6: Apply the Script

Run the generated SQL:

```bash
psql -U postgres mess_production < fix_migrations.sql
```

Or paste into your SQL tool.

---

## Step 7: Verify

Check migration history:

```pgsql
SELECT * FROM "__EFMigrationsHistory";
```

Ensure all migrations are present.

---

## Step 8: Test Application

Start your app:

```bash
dotnet run
```

It should now start without schema errors.

---

## ⚠️ Common Pitfalls

### Trusting `database update`

EF may incorrectly report the database is up to date.

---

### Running migrations blindly

Always inspect schema state first.

---

### Skipping backup

Schema migrations can fail — always have a rollback.

---

## Prevention Tips

* Always run:

  ```bash
  dotnet ef database update
  ```

  immediately after adding migrations

* Avoid copying databases between environments without applying migrations

* Consider adding automatic migration checks on startup

---

## Summary

| Step | Action                      |
|------|-----------------------------|
| 1    | Identify missing migrations |
| 2    | Generate SQL script         |
| 3    | Backup database             |
| 4    | Inspect schema              |
| 5    | Validate state              |
| 6    | Apply script                |
| 7    | Verify history              |
| 8    | Run app                     |

---

## Final Notes

This issue is not uncommon in systems using EF Core with multiple environments or manual database handling.

The key takeaway:

> EF trusts the schema more than migration history — when those diverge, manual intervention is required.

---
