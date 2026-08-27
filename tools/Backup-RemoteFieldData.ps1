[CmdletBinding()]
param(
    [string]$BackupRoot = 'C:\OSDC\Backups\Field'
)

$ErrorActionPreference = 'Stop'
$timestamp = [DateTime]::UtcNow.ToString('yyyyMMddTHHmmssZ')
$destination = Join-Path $BackupRoot $timestamp
$environments = [ordered]@{
    'dev.digiwells.no' = 'https://dev.digiwells.no'
    'awe.web.intra.norceresearch.no' = 'https://awe.web.intra.norceresearch.no'
    'app.digiwells.no' = 'https://app.digiwells.no'
}
$catalogs = @(
    'FieldDelineationLineType',
    'FieldFeatureCategory',
    'FieldIdentity',
    'FieldMembershipCategory'
)

function Download-RawJson {
    param([string]$Uri, [string]$Path)
    Invoke-WebRequest -Uri $Uri -Method Get -UseBasicParsing -TimeoutSec 120 -OutFile $Path
    $null = Get-Content -Raw -LiteralPath $Path | ConvertFrom-Json
}

function Get-PropertyValue {
    param([object]$Object, [string]$Name)
    if ($null -eq $Object) { return $null }
    $property = $Object.PSObject.Properties | Where-Object { $_.Name -ieq $Name } | Select-Object -First 1
    if ($null -eq $property) { return $null }
    return $property.Value
}

function Get-RecordId {
    param([object]$Record)
    $metaInfo = Get-PropertyValue $Record 'MetaInfo'
    return Get-PropertyValue $metaInfo 'ID'
}

function Normalize-Ids {
    param([object[]]$Ids)
    return @($Ids | ForEach-Object { ([Guid]$_).ToString('D').ToLowerInvariant() } | Sort-Object -Unique)
}

function Test-SameList {
    param([string[]]$First, [string[]]$Second)
    if ($First.Count -ne $Second.Count) { return $false }
    return (@(Compare-Object $First $Second).Count -eq 0)
}

New-Item -ItemType Directory -Path $destination -Force | Out-Null
$environmentReports = @()

foreach ($entry in $environments.GetEnumerator()) {
    $name = $entry.Key
    $hostUrl = $entry.Value.TrimEnd('/')
    $environmentDirectory = Join-Path $destination $name
    $rawDirectory = Join-Path $environmentDirectory 'raw'
    $fieldsDirectory = Join-Path $environmentDirectory 'fields'
    $catalogDirectory = Join-Path $environmentDirectory 'referenced-catalogs'
    $projectionDirectory = Join-Path $environmentDirectory 'legacy-projections'
    $datumDirectory = Join-Path $environmentDirectory 'referenced-geodetic-datums'
    foreach ($directory in @($environmentDirectory, $rawDirectory, $fieldsDirectory, $catalogDirectory, $projectionDirectory, $datumDirectory)) {
        New-Item -ItemType Directory -Path $directory -Force | Out-Null
    }

    $errors = [System.Collections.Generic.List[string]]::new()
    $warnings = [System.Collections.Generic.List[string]]::new()
    $swaggerPath = Join-Path $rawDirectory 'field-openapi.json'
    $fieldIdsStartPath = Join-Path $rawDirectory 'field-ids-start.json'
    $fieldsHeavyPath = Join-Path $rawDirectory 'fields-heavy.json'
    $fieldIdsEndPath = Join-Path $rawDirectory 'field-ids-end.json'

    try {
        Download-RawJson "$hostUrl/Field/api/swagger/merged/swagger.json" $swaggerPath
        Download-RawJson "$hostUrl/Field/api/Field" $fieldIdsStartPath
        Download-RawJson "$hostUrl/Field/api/Field/HeavyData" $fieldsHeavyPath
    }
    catch {
        $errors.Add("Core Field export failed: $($_.Exception.Message)")
    }

    $startIds = @()
    $heavyFields = @()
    if (Test-Path -LiteralPath $fieldIdsStartPath) {
        try { $startIds = Normalize-Ids @((Get-Content -Raw -LiteralPath $fieldIdsStartPath | ConvertFrom-Json)) }
        catch { $errors.Add("Initial Field ID list is invalid: $($_.Exception.Message)") }
    }
    if (Test-Path -LiteralPath $fieldsHeavyPath) {
        try { $heavyFields = @((Get-Content -Raw -LiteralPath $fieldsHeavyPath | ConvertFrom-Json)) }
        catch { $errors.Add("Field HeavyData is invalid: $($_.Exception.Message)") }
    }

    $individualFieldIds = [System.Collections.Generic.List[string]]::new()
    $projectionIds = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
    foreach ($id in $startIds) {
        $fieldPath = Join-Path $fieldsDirectory "$id.json"
        try {
            Download-RawJson "$hostUrl/Field/api/Field/$id" $fieldPath
            $field = Get-Content -Raw -LiteralPath $fieldPath | ConvertFrom-Json
            $actualId = Get-RecordId $field
            if ([string]::IsNullOrWhiteSpace([string]$actualId) -or ([Guid]$actualId).ToString('D') -ine $id) {
                $errors.Add("Field $id returned an absent or different MetaInfo.ID.")
                continue
            }
            $individualFieldIds.Add($id)
            $projectionId = Get-PropertyValue $field 'CartographicProjectionID'
            if ($null -ne $projectionId -and -not [string]::IsNullOrWhiteSpace([string]$projectionId)) {
                $normalizedProjectionId = ([Guid]$projectionId).ToString('D').ToLowerInvariant()
                if ($normalizedProjectionId -ne [Guid]::Empty.ToString('D')) { $null = $projectionIds.Add($normalizedProjectionId) }
            }
        }
        catch {
            $errors.Add("Field $id export failed: $($_.Exception.Message)")
        }
    }

    $catalogCounts = [ordered]@{}
    foreach ($catalog in $catalogs) {
        $rawCatalogPath = Join-Path $rawDirectory "$($catalog.ToLowerInvariant())-heavy.json"
        $splitCatalogDirectory = Join-Path $catalogDirectory $catalog
        New-Item -ItemType Directory -Path $splitCatalogDirectory -Force | Out-Null
        try {
            Download-RawJson "$hostUrl/Field/api/$catalog/HeavyData" $rawCatalogPath
            $records = @((Get-Content -Raw -LiteralPath $rawCatalogPath | ConvertFrom-Json))
            $catalogCounts[$catalog] = $records.Count
            foreach ($record in $records) {
                $recordId = Get-RecordId $record
                if ([string]::IsNullOrWhiteSpace([string]$recordId)) {
                    $errors.Add("$catalog contains a record without MetaInfo.ID.")
                    continue
                }
                $normalizedRecordId = ([Guid]$recordId).ToString('D').ToLowerInvariant()
                $record | ConvertTo-Json -Depth 100 | Set-Content -LiteralPath (Join-Path $splitCatalogDirectory "$normalizedRecordId.json") -Encoding UTF8
            }
        }
        catch {
            $catalogCounts[$catalog] = $null
            $errors.Add("$catalog export failed: $($_.Exception.Message)")
        }
    }

    $resolvedProjectionIds = [System.Collections.Generic.List[string]]::new()
    $datumIds = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
    foreach ($projectionId in @($projectionIds | Sort-Object)) {
        $projectionPath = Join-Path $projectionDirectory "$projectionId.json"
        try {
            Download-RawJson "$hostUrl/CartographicProjection/api/CartographicProjection/$projectionId" $projectionPath
            $projection = Get-Content -Raw -LiteralPath $projectionPath | ConvertFrom-Json
            $projectionRecordId = Get-RecordId $projection
            if ([string]::IsNullOrWhiteSpace([string]$projectionRecordId) -or ([Guid]$projectionRecordId).ToString('D') -ine $projectionId) {
                $errors.Add("Legacy projection $projectionId returned an absent or different MetaInfo.ID.")
            }
            else {
                $resolvedProjectionIds.Add($projectionId)
                $datumId = Get-PropertyValue $projection 'GeodeticDatumID'
                if ($null -ne $datumId -and -not [string]::IsNullOrWhiteSpace([string]$datumId)) {
                    $normalizedDatumId = ([Guid]$datumId).ToString('D').ToLowerInvariant()
                    if ($normalizedDatumId -ne [Guid]::Empty.ToString('D')) { $null = $datumIds.Add($normalizedDatumId) }
                }
            }
        }
        catch {
            $errors.Add("Legacy projection $projectionId could not be backed up: $($_.Exception.Message)")
        }
    }

    $datumSources = [ordered]@{}
    foreach ($datumId in @($datumIds | Sort-Object)) {
        $sources = [System.Collections.Generic.List[string]]::new()
        foreach ($datumEnvironment in $environments.GetEnumerator()) {
            $datumHost = $datumEnvironment.Value.TrimEnd('/')
            $datumPath = Join-Path $datumDirectory "$datumId--$($datumEnvironment.Key).json"
            try {
                Download-RawJson "$datumHost/GeodeticDatum/api/GeodeticDatum/$datumId" $datumPath
                $datum = Get-Content -Raw -LiteralPath $datumPath | ConvertFrom-Json
                $datumRecordId = Get-RecordId $datum
                if ([string]::IsNullOrWhiteSpace([string]$datumRecordId) -or ([Guid]$datumRecordId).ToString('D') -ine $datumId) {
                    Remove-Item -LiteralPath $datumPath -Force
                }
                else { $sources.Add($datumEnvironment.Key) }
            }
            catch {
                if (Test-Path -LiteralPath $datumPath) { Remove-Item -LiteralPath $datumPath -Force }
            }
        }
        $datumSources[$datumId] = @($sources)
        if ($sources.Count -eq 0) { $warnings.Add("Referenced legacy geodetic datum $datumId is not exposed by the retired GeodeticDatum routes and could not be snapshotted; its UUID remains preserved in the legacy projection JSON.") }
    }

    try { Download-RawJson "$hostUrl/Field/api/Field" $fieldIdsEndPath }
    catch { $errors.Add("Final Field ID list failed: $($_.Exception.Message)") }
    $endIds = @()
    if (Test-Path -LiteralPath $fieldIdsEndPath) {
        try { $endIds = Normalize-Ids @((Get-Content -Raw -LiteralPath $fieldIdsEndPath | ConvertFrom-Json)) }
        catch { $errors.Add("Final Field ID list is invalid: $($_.Exception.Message)") }
    }

    $heavyIds = @()
    try {
        $heavyIds = Normalize-Ids @($heavyFields | ForEach-Object { Get-RecordId $_ })
    }
    catch { $errors.Add("Field HeavyData contains an absent or malformed MetaInfo.ID: $($_.Exception.Message)") }
    $individualIds = @($individualFieldIds | Sort-Object -Unique)
    $stableSnapshot = (Test-SameList $startIds $endIds)
    $heavyMatches = (Test-SameList $startIds $heavyIds)
    $individualMatches = (Test-SameList $startIds $individualIds)
    $projectionsResolved = ($projectionIds.Count -eq $resolvedProjectionIds.Count)

    if (-not $stableSnapshot) { $errors.Add('Field IDs changed while the export was running.') }
    if (-not $heavyMatches) { $errors.Add('Field HeavyData IDs do not match the initial Field ID list.') }
    if (-not $individualMatches) { $errors.Add('Individually exported Field IDs do not match the initial Field ID list.') }
    if (-not $projectionsResolved) { $errors.Add('Not every referenced legacy projection resolved successfully.') }

    $report = [ordered]@{
        environment = $name
        sourceHost = $hostUrl
        exportedUtc = [DateTime]::UtcNow.ToString('o')
        fieldCount = $startIds.Count
        heavyDataFieldCount = $heavyFields.Count
        individualFieldCount = $individualIds.Count
        referencedProjectionCount = $projectionIds.Count
        resolvedProjectionCount = $resolvedProjectionIds.Count
        referencedGeodeticDatumCount = $datumIds.Count
        resolvedGeodeticDatumCount = @($datumSources.GetEnumerator() | Where-Object { $_.Value.Count -gt 0 }).Count
        geodeticDatumSources = $datumSources
        catalogCounts = $catalogCounts
        stableFieldIdSnapshot = $stableSnapshot
        heavyDataMatchesIdList = $heavyMatches
        individualFilesMatchIdList = $individualMatches
        allReferencedProjectionsResolved = $projectionsResolved
        fieldIds = $startIds
        referencedProjectionIds = @($projectionIds | Sort-Object)
        warnings = @($warnings)
        errors = @($errors)
        verified = ($errors.Count -eq 0)
        externalDependencySnapshotsComplete = (@($datumSources.GetEnumerator() | Where-Object { $_.Value.Count -eq 0 }).Count -eq 0)
    }
    $manifestPath = Join-Path $environmentDirectory 'manifest.json'
    $report | ConvertTo-Json -Depth 20 | Set-Content -LiteralPath $manifestPath -Encoding UTF8

    $checksumLines = Get-ChildItem -LiteralPath $environmentDirectory -Recurse -File |
        Where-Object { $_.Name -ne 'checksums.sha256' } |
        Sort-Object FullName |
        ForEach-Object {
            $relativePath = $_.FullName.Substring($environmentDirectory.Length + 1).Replace('\', '/')
            $hash = (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
            "$hash  $relativePath"
        }
    $checksumLines | Set-Content -LiteralPath (Join-Path $environmentDirectory 'checksums.sha256') -Encoding ASCII
    $environmentReports += [pscustomobject]$report
}

$summary = [ordered]@{
    backupFormatVersion = 1
    createdUtc = [DateTime]::UtcNow.ToString('o')
    backupDirectory = $destination
    excludesCalculationCases = $true
    environments = $environmentReports
    verified = (@($environmentReports | Where-Object { -not $_.verified }).Count -eq 0)
    externalDependencySnapshotsComplete = (@($environmentReports | Where-Object { -not $_.externalDependencySnapshotsComplete }).Count -eq 0)
}
$summary | ConvertTo-Json -Depth 30 | Set-Content -LiteralPath (Join-Path $destination 'backup-report.json') -Encoding UTF8
Write-Output $destination
if (-not $summary.verified) { exit 2 }
