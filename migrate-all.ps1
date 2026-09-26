<#
.SYNOPSIS
    Applies pending EF Core migrations to the master database and all three
    tenant databases, each on its own LocalDB instance.

.DESCRIPTION
    Under the multi-instance layout, every database lives on a dedicated
    LocalDB instance:
        (localdb)\CRM_Master    -> CRM_Morcaffein_MasterCrm
        (localdb)\CRM_TenantA   -> CRM_Morcaffein_TenantA
        (localdb)\CRM_TenantB   -> CRM_Morcaffein_TenantB
        (localdb)\CRM_TenantC   -> CRM_Morcaffein_TenantC

    EF's design-time factories point at one instance as a reference; this
    script overrides the connection string per target so migrations land on
    the right instance every time.

    Run from the solution root (where the .sln lives).

.EXAMPLE
    .\migrate-all.ps1
#>

$ErrorActionPreference = 'Stop'

# ---- Config ----
$MasterProject  = 'CRM_Morcaffein.infrastructure'
$StartupProject = 'CRM_Morcaffein.api'

$targets = @(
    @{
        Label          = 'Master'
        Context        = 'MasterCrmDbContext'
        Connection     = 'Server=(localdb)\CRM_Master;Database=CRM_Morcaffein_MasterCrm;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True;'
    },
    @{
        Label          = 'Tenant A'
        Context        = 'TenantCrmDbContext'
        Connection     = 'Server=(localdb)\CRM_TenantA;Database=CRM_Morcaffein_TenantA;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True;'
    },
    @{
        Label          = 'Tenant B'
        Context        = 'TenantCrmDbContext'
        Connection     = 'Server=(localdb)\CRM_TenantB;Database=CRM_Morcaffein_TenantB;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True;'
    },
    @{
        Label          = 'Tenant C'
        Context        = 'TenantCrmDbContext'
        Connection     = 'Server=(localdb)\CRM_TenantC;Database=CRM_Morcaffein_TenantC;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True;'
    }
)

# ---- Run ----
Write-Host ''
Write-Host 'Applying migrations to all targets...' -ForegroundColor Cyan
Write-Host ''

$failures = @()

foreach ($t in $targets) {
    Write-Host ("[{0}]" -f $t.Label) -ForegroundColor Yellow
    Write-Host ("  -> {0}" -f $t.Connection)

    dotnet ef database update `
        --project $MasterProject `
        --startup-project $StartupProject `
        --context $t.Context `
        --connection $t.Connection

    if ($LASTEXITCODE -ne 0) {
        Write-Host ("[{0}] FAILED with exit code {1}" -f $t.Label, $LASTEXITCODE) -ForegroundColor Red
        $failures += $t.Label
    }
    else {
        Write-Host ("[{0}] OK" -f $t.Label) -ForegroundColor Green
    }

    Write-Host ''
}

if ($failures.Count -gt 0) {
    Write-Host ('Migration finished with failures: {0}' -f ($failures -join ', ')) -ForegroundColor Red
    exit 1
}

Write-Host 'All migrations applied.' -ForegroundColor Green