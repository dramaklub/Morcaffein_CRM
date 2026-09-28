<#
.SYNOPSIS
    Applies pending EF Core migrations to the four CLOUD databases
    (master + three tenants) on MonsterASP.

.DESCRIPTION
    Cloud runs one SQL Server engine per database:
        db66493.public.databaseasp.net  -> master
        db67068.public.databaseasp.net  -> Tenant A
        db67071.public.databaseasp.net  -> Tenant B
        db67072.public.databaseasp.net  -> Tenant C

    This is the cloud counterpart to migrate-all.ps1, which handles the
    four LOCAL LocalDB instances. The two are deliberately separate — no
    shared connection strings, no environment switching, each script
    runs against its own set of targets.

    Run from the solution root (D:\VisualStudio\Projects).

.EXAMPLE
    cd D:\VisualStudio\Projects
    .\migrate-cloud.ps1
#>

$ErrorActionPreference = 'Stop'

$MasterProject  = 'CRM_Morcaffein.infrastructure'
$StartupProject = 'CRM_Morcaffein.api'

$targets = @(
    @{
        Label      = 'Cloud Master'
        Context    = 'MasterCrmDbContext'
        Connection = 'Server=db66493.public.databaseasp.net;Database=db66493;User Id=db66493;Password=R%f9-s7BM+c2;Encrypt=True;TrustServerCertificate=True;MultipleActiveResultSets=True;'
    },
    @{
        Label      = 'Cloud Tenant A'
        Context    = 'TenantCrmDbContext'
        Connection = 'Server=db67068.public.databaseasp.net;Database=db67068;User Id=db67068;Password=iG#9H8g?2d@A;Encrypt=True;TrustServerCertificate=True;MultipleActiveResultSets=True;'
    },
    @{
        Label      = 'Cloud Tenant B'
        Context    = 'TenantCrmDbContext'
        Connection = 'Server=db67071.public.databaseasp.net;Database=db67071;User Id=db67071;Password=m_8XHa%5b9L?;Encrypt=True;TrustServerCertificate=True;MultipleActiveResultSets=True;'
    },
    @{
        Label      = 'Cloud Tenant C'
        Context    = 'TenantCrmDbContext'
        Connection = 'Server=db67072.public.databaseasp.net;Database=db67072;User Id=db67072;Password=8Mz-_w2B3Ga+;Encrypt=True;TrustServerCertificate=True;MultipleActiveResultSets=True;'
    }
)

Write-Host ''
Write-Host 'Applying migrations to CLOUD targets...' -ForegroundColor Cyan
Write-Host ''

$failures = @()

foreach ($t in $targets) {
    Write-Host ("[{0}]" -f $t.Label) -ForegroundColor Yellow
    Write-Host ("  -> {0} / {1}" -f (
        ($t.Connection -split ';')[0],
        ($t.Connection -split ';')[1]))

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

Write-Host 'All CLOUD migrations applied.' -ForegroundColor Green