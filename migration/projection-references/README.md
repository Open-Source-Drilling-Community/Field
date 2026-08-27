# Field projection-reference migration

These files were derived from the verified logical backup at
`C:\OSDC\Backups\Field\20260827T074126Z`. The three authoritative ED50/UTM
mappings were also verified live against each EarthCartographicProjection
deployment on 2026-08-27. No remote Field record was changed.

Before deploying the new Field service, provide the applicable mapping as the
dedicated `/home/Field.ProjectionMappings.json`, set
`FIELD_PROJECTION_MAPPINGS_CONFIG` to another mounted path, or set the Helm
chart's `projectionDefinitionIdMappings` value to the inner object from the
applicable JSON file. This keeps the one-time migration input separate from
MCP hub and other service settings. On startup the service scans every stored
Field before changing anything. It refuses to start
if a non-empty legacy `CartographicProjectionID` has no reviewed mapping or if
an existing `ProjectionDefinitionID` conflicts with the mapping. If the plan is
complete, it creates a SQLite backup named `Field.pre-v2.<UTC>.db`, updates all
Field JSON rows in one transaction, removes the obsolete calculation-case
table, and sets schema version 2.

All three mapping files are complete. The two legacy definitions that could not
be inferred were explicitly assigned by Eric Cayeux on 2026-08-27: app field
`ayombero` uses WGS 84 / UTM zone 18N (EPSG:32618), and AWE field `The field`
uses ED50 / UTM zone 31N (EPSG:23031). Both authoritative definitions were
verified live before the assignments were recorded in `assessment.json`.
