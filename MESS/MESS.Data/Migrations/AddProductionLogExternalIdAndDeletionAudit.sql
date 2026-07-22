-- Migration: AddProductionLogExternalIdAndDeletionAudit
-- Apply with: psql -d <your_db> -f AddProductionLogExternalIdAndDeletionAudit.sql
-- Or paste into your preferred PostgreSQL client.

BEGIN;

-- Add import-tracking columns to ProductionLogs
ALTER TABLE "ProductionLogs"
    ADD COLUMN IF NOT EXISTS "ExternalId"  integer               NULL,
    ADD COLUMN IF NOT EXISTS "ImportedBy"  text                  NULL,
    ADD COLUMN IF NOT EXISTS "ImportedOn"  timestamp with time zone NULL;

-- Filtered index for ExternalId lookups (only non-NULL rows)
CREATE INDEX IF NOT EXISTS "IX_ProductionLogs_ExternalId"
    ON "ProductionLogs" ("ExternalId")
    WHERE "ExternalId" IS NOT NULL;

-- Audit table written whenever production logs are bulk-deleted
CREATE TABLE IF NOT EXISTS "ProductionLogDeletionAudits" (
    "Id"          SERIAL PRIMARY KEY,
    "DeletedBy"   text                      NOT NULL,
    "DeletedOn"   timestamp with time zone  NOT NULL,
    "LogCount"    integer                   NOT NULL,
    "LogIds"      integer[]                 NOT NULL,
    "ExportFile"  text                      NOT NULL
);

COMMIT;
