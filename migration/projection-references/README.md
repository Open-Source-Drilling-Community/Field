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

## Deployment completion and retention

The migration was completed and verified on all three deployments on
2026-08-27. Post-migration API checks found 7 fields on dev, 2 on AWE, and 135
on app, with respectively 1, 2, and 131 non-null `ProjectionDefinitionID`
values and no legacy `CartographicProjectionID` properties.

An independent post-v2 logical snapshot was stored at
`C:\OSDC\FieldMigrationBackups\post-v2\20260827T130709Z`. Every HeavyData
record count matches its ID-list count. SHA-256 checksums are:

| Deployment | File | SHA-256 |
|---|---|---|
| app | `field-ids.json` | `CF74B85EE30D15D516BB936A57FDE1943B111555FAB656FEBDB2C2BB25D58FE7` |
| app | `fields-heavy.json` | `2EA0BC0B3320C719CB9D01503899A6AF2000505B4F32DCA1B42C5DEA229AD0EC` |
| AWE | `field-ids.json` | `E58FD345F5AE36A32F9DD86F281995D0F19A84F33D551030C65E59B8202D9BBA` |
| AWE | `fields-heavy.json` | `7F11563B3F5AD4905D4DBAE0B9812C52D2CF04552D57D31A2258D14F2F511128` |
| dev | `field-ids.json` | `0E468FDC497F367C83E49345F873449ACDDFDB9E9CE4975A0E75256005276D6A` |
| dev | `fields-heavy.json` | `E3F74CA27B423A95B242D807649FC6FC1BAB74944CEBFEF74C7CF8309361F9C0` |

Keep the migrator, its tests, and these reviewed mapping files through at least
one release cycle so that a pre-v2 database backup can still be restored. A v2
database skips legacy Field scanning and does not require mappings at startup;
normal table-structure validation still runs. Remove the recovery path only in
a later release that explicitly drops support for pre-v2 databases.
