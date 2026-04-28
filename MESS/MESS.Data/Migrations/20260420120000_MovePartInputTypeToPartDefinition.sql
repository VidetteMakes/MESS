-- =============================================================================
-- EF migration equivalent: MovePartInputTypeToPartDefinition (Up)
-- Apply in pgAdmin 4: Query Tool -> Paste -> Execute (F5)
-- Targets: PostgreSQL (Npgsql / quoted identifiers like EF Core)
-- =============================================================================

BEGIN;

-- 1) Add traceability type on PartDefinitions (0 = SerialNumber enum default)
ALTER TABLE "PartDefinitions"
    ADD COLUMN IF NOT EXISTS "InputType" integer NOT NULL DEFAULT 0;

-- 2) Backfill from PartNodes (one row per PartDefinition: pick lowest PartNode Id)
UPDATE "PartDefinitions" pd
SET "InputType" = sub."InputType"
FROM (
    SELECT DISTINCT ON ("PartDefinitionId") "PartDefinitionId", "InputType"
    FROM "PartNodes"
    ORDER BY "PartDefinitionId", "Id"
) sub
WHERE pd."Id" = sub."PartDefinitionId";

-- 3) Remove column from PartNodes (no longer stored per node)
ALTER TABLE "PartNodes" DROP COLUMN IF EXISTS "InputType";

COMMIT;

-- =============================================================================
-- Optional: record migration as applied (only if you use __EFMigrationsHistory
-- and are applying this SQL INSTEAD of `dotnet ef database update`)
-- =============================================================================
-- INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
-- VALUES ('20260420120000_MovePartInputTypeToPartDefinition', '10.0.0')
-- ON CONFLICT DO NOTHING;

-- =============================================================================
-- DOWN / rollback (run only if you need to reverse the change)
-- =============================================================================
-- BEGIN;
-- ALTER TABLE "PartNodes" ADD COLUMN IF NOT EXISTS "InputType" integer NOT NULL DEFAULT 0;
-- UPDATE "PartNodes" pn
-- SET "InputType" = pd."InputType"
-- FROM "PartDefinitions" pd
-- WHERE pn."PartDefinitionId" = pd."Id";
-- ALTER TABLE "PartDefinitions" DROP COLUMN IF EXISTS "InputType";
-- COMMIT;
-- DELETE FROM "__EFMigrationsHistory"
-- WHERE "MigrationId" = '20260420120000_MovePartInputTypeToPartDefinition';
