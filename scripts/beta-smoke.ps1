#Requires -Version 7.3

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$previousPSNativeCommandUseErrorActionPreference = $PSNativeCommandUseErrorActionPreference
$PSNativeCommandUseErrorActionPreference = $true
$script:webSession = New-Object Microsoft.PowerShell.Commands.WebRequestSession

function Fail([string]$Message) {
    throw $Message
}

function Write-Step([string]$Message) {
    Write-Host "[beta-smoke] $Message" -ForegroundColor Cyan
}

function Invoke-Api {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [ValidateSet('GET', 'POST')][string]$Method = 'GET',
        [object]$Body = $null,
        [Microsoft.PowerShell.Commands.WebRequestSession]$Session = $script:webSession
    )

    $uri = "http://127.0.0.1:5178$Path"
    $headers = @{ 'Content-Type' = 'application/json' }
    $parameters = @{
        Uri = $uri
        Method = $Method
        Headers = $headers
        NoProxy = $true
        WebSession = $Session
    }

    if ($null -ne $Body) {
        $parameters.Body = ($Body | ConvertTo-Json -Depth 10 -Compress)
    }

    try {
        return Invoke-RestMethod @parameters
    }
    catch {
        $message = $_.Exception.Message
        if ($_.ErrorDetails.Message) {
            $message = $_.ErrorDetails.Message
        }

        Fail("API call failed: $Method $Path -> $message")
    }
}

function Wait-ForCondition {
    param(
        [Parameter(Mandatory = $true)][scriptblock]$Condition,
        [Parameter(Mandatory = $true)][string]$Description,
        [int]$TimeoutSeconds = 180,
        [int]$IntervalSeconds = 2
    )

    $startedAt = Get-Date
    $deadline = $startedAt.AddSeconds($TimeoutSeconds)
    $nextProgressAt = $startedAt.AddSeconds(10)
    $lastError = $null

    while ((Get-Date) -lt $deadline) {
        try {
            if (& $Condition) {
                return
            }
        }
        catch {
            $lastError = $_.Exception.Message
        }

        if ((Get-Date) -ge $nextProgressAt) {
            $elapsedSeconds = [int]((Get-Date) - $startedAt).TotalSeconds
            Write-Step "Still waiting for $Description ($elapsedSeconds s)"
            $nextProgressAt = (Get-Date).AddSeconds(10)
        }

        Start-Sleep -Seconds $IntervalSeconds
    }

    $details = if ($lastError) {
        " Last error: $lastError"
    }
    else {
        ''
    }

    Fail("Timed out after $TimeoutSeconds seconds waiting for $Description.$details")
}

function Wait-ForApi {
    param(
        [Parameter(Mandatory = $true)]
        [System.Diagnostics.Process]$Process,
        [Parameter(Mandatory = $true)][string]$LogPath,
        [Parameter(Mandatory = $true)][string]$ErrorLogPath,
        [int]$TimeoutSeconds = 120
    )

    $startedAt = Get-Date
    $deadline = $startedAt.AddSeconds($TimeoutSeconds)
    $nextProgressAt = $startedAt.AddSeconds(10)

    while ((Get-Date) -lt $deadline) {
        if ($Process.HasExited) {
            $log = if (Test-Path $LogPath) {
                (Get-Content $LogPath -Tail 30) -join [Environment]::NewLine
            }
            else {
                ''
            }

            $errorLog = if (Test-Path $ErrorLogPath) {
                (Get-Content $ErrorLogPath -Tail 30) -join [Environment]::NewLine
            }
            else {
                ''
            }

            Fail("Galaxy.Api exited with code $($Process.ExitCode).`n$log`n$errorLog")
        }

        try {
            $health = Invoke-RestMethod `
                -Uri 'http://127.0.0.1:5178/health' `
                -Method GET `
                -NoProxy

            if ($health.status -eq 'Healthy') {
                return
            }
        }
        catch {
            # The API can refuse connections while it is starting.
        }

        if ((Get-Date) -ge $nextProgressAt) {
            $elapsedSeconds = [int]((Get-Date) - $startedAt).TotalSeconds
            Write-Step "Still waiting for Galaxy.Api readiness ($elapsedSeconds s)"
            $nextProgressAt = (Get-Date).AddSeconds(10)
        }

        Start-Sleep -Seconds 1
    }

    Fail("Timed out after $TimeoutSeconds seconds waiting for Galaxy.Api readiness.")
}

$previousLocation = Get-Location
$repoRoot = Resolve-Path (Join-Path $PSScriptRoot '..')
Set-Location $repoRoot

$artifactRoot = Join-Path `
    ([System.IO.Path]::GetTempPath()) `
    'Galaxy-War-Game/beta-smoke'
New-Item -ItemType Directory -Path $artifactRoot -Force | Out-Null

$timestamp = Get-Date -Format 'yyyyMMddHHmmss'
$tempDbName = "galaxy_smoke_$timestamp"
$connectionString = "Host=127.0.0.1;Port=5432;Database=$tempDbName;Username=galaxy;Password=galaxy"

$apiProcess = $null
$databaseCreated = $false
$previousAspNetCoreEnvironment = $env:ASPNETCORE_ENVIRONMENT
$previousConnectionString = $env:ConnectionStrings__Database
$previousAspNetCoreUrls = $env:ASPNETCORE_URLS

try {
    Write-Step "Starting PostgreSQL container if needed"
    docker compose up -d postgres | Out-Null

    Write-Step "Waiting for PostgreSQL readiness"
    Wait-ForCondition -Description 'PostgreSQL readiness' -Condition {
        docker compose exec -T postgres pg_isready -U galaxy -d postgres 2>$null | Out-Null
        $LASTEXITCODE -eq 0
    } -TimeoutSeconds 60 -IntervalSeconds 1

    Write-Step "Creating temporary database $tempDbName"
    $createSql = "CREATE DATABASE `"$tempDbName`";"
    docker compose exec -T postgres psql -U galaxy -d postgres -v ON_ERROR_STOP=1 -c $createSql | Out-Null
    $databaseCreated = $true

    Write-Step "Building solution"
    dotnet build backend/Galaxy.sln --verbosity minimal
    Write-Step "Running tests"
    dotnet test backend/Galaxy.sln --no-build --verbosity minimal
    Write-Step "Applying EF Core migrations to $tempDbName"
    $env:ASPNETCORE_ENVIRONMENT = 'Development'
    $env:ConnectionStrings__Database = $connectionString
    dotnet ef database update --project backend/src/Galaxy.Infrastructure --startup-project backend/src/Galaxy.Api

    Write-Step "Starting Galaxy.Api on http://127.0.0.1:5178"
    $dllPath = Join-Path $repoRoot 'backend/src/Galaxy.Api/bin/Debug/net10.0/Galaxy.Api.dll'
    $env:ASPNETCORE_ENVIRONMENT = 'Development'
    $env:ConnectionStrings__Database = $connectionString
    $env:ASPNETCORE_URLS = 'http://127.0.0.1:5178'

    $apiLogPath = Join-Path $artifactRoot 'beta-smoke-api.log'
    $apiErrorLogPath = Join-Path $artifactRoot 'beta-smoke-api.err'
    Remove-Item $apiLogPath, $apiErrorLogPath -Force -ErrorAction SilentlyContinue

    $apiProcess = Start-Process `
        -FilePath 'dotnet' `
        -ArgumentList @($dllPath) `
        -WorkingDirectory (Join-Path $repoRoot 'backend/src/Galaxy.Api') `
        -PassThru `
        -RedirectStandardOutput $apiLogPath `
        -RedirectStandardError $apiErrorLogPath

    Write-Step "Waiting for API readiness"
    Wait-ForApi `
        -Process $apiProcess `
        -LogPath $apiLogPath `
        -ErrorLogPath $apiErrorLogPath `
        -TimeoutSeconds 120

    Write-Step "Verifying that game endpoints require authentication"
    $unauthorized = Invoke-WebRequest `
        -Uri 'http://127.0.0.1:5178/api/game/current' `
        -Method GET `
        -NoProxy `
        -SkipHttpErrorCheck
    if ($unauthorized.StatusCode -ne 401) {
        Fail("Unauthenticated game request returned $($unauthorized.StatusCode) instead of 401")
    }

    Write-Step "Registering a Beta 2 account"
    $account = Invoke-Api -Path '/api/auth/register' -Method POST -Body @{
        commanderName = 'BetaSmoke'
        email = 'beta-smoke@example.test'
        password = 'BetaSmoke2026'
        confirmPassword = 'BetaSmoke2026'
    }
    if (-not $account.requiresRaceSelection) {
        Fail('New account did not require race selection')
    }

    Write-Step "Selecting Humans race and creating the starting world"
    $session = Invoke-Api -Path '/api/auth/race' -Method POST -Body @{ race = 'Humans' }
    if ($session.race -ne 'Humans' -or $session.requiresRaceSelection) {
        Fail('Race selection did not activate the Humans game')
    }

    $game = Invoke-Api -Path '/api/game/current' -Method GET
    $homeworldId = $game.planetId
    if ([string]::IsNullOrWhiteSpace($homeworldId)) {
        Fail('Created game did not return a planet id')
    }

    Write-Step "Validating the starting planet"
    $currentGame = Invoke-Api -Path '/api/game/current' -Method GET
    if ($currentGame.planetName -ne 'Homeworld') {
        Fail('The initial planet was not named Homeworld')
    }

    Write-Step "Verifying account isolation with a second commander"
    $firstCommanderSession = $script:webSession
    $secondCommanderSession = New-Object Microsoft.PowerShell.Commands.WebRequestSession
    Invoke-Api -Path '/api/auth/register' -Method POST -Session $secondCommanderSession -Body @{
        commanderName = 'BetaSmokeTwo'
        email = 'beta-smoke-two@example.test'
        password = 'BetaSmokeTwo2026'
        confirmPassword = 'BetaSmokeTwo2026'
    } | Out-Null
    Invoke-Api -Path '/api/auth/race' -Method POST -Session $secondCommanderSession -Body @{ race = 'Synthetics' } | Out-Null

    $secondCommanderIdentity = Invoke-Api -Path '/api/auth/me' -Session $secondCommanderSession
    if ($secondCommanderIdentity.commanderName -ne 'BetaSmokeTwo') {
        Fail("Second session belongs to '$($secondCommanderIdentity.commanderName)' instead of BetaSmokeTwo")
    }

    $secondCommanderPlanets = @(Invoke-Api -Path '/api/game/planets' -Method GET -Session $secondCommanderSession)
    if ($secondCommanderPlanets.Count -ne 1) {
        Fail("Second commander saw $($secondCommanderPlanets.Count) planets instead of exactly one")
    }
    $secondHomeworldId = $secondCommanderPlanets[0].id
    if ([string]::IsNullOrWhiteSpace($secondHomeworldId) -or
        $secondHomeworldId -eq $homeworldId) {
        Fail('Second commander did not receive a distinct starting planet')
    }

    $firstCommanderIdentity = Invoke-Api -Path '/api/auth/me' -Session $firstCommanderSession
    if ($firstCommanderIdentity.commanderName -ne 'BetaSmoke') {
        Fail("First session belongs to '$($firstCommanderIdentity.commanderName)' instead of BetaSmoke")
    }

    $firstCommanderPlanets = @(Invoke-Api -Path '/api/game/planets' -Method GET -Session $firstCommanderSession)
    if ($firstCommanderPlanets.Count -ne 1) {
        Fail("First commander saw $($firstCommanderPlanets.Count) planets instead of exactly one")
    }
    if ($firstCommanderPlanets[0].id -ne $homeworldId) {
        Fail('First commander session returned another commander''s planet')
    }

    Write-Host ''
    Write-Host 'BETA V2 FOUNDATION SMOKE TEST PASSED' -ForegroundColor Green
}
catch {
    Write-Host ''
    Write-Host "SMOKE TEST FAILED: $($_.Exception.Message)" -ForegroundColor Red
    throw
}
finally {
    try {
        if ($null -ne $apiProcess -and -not $apiProcess.HasExited) {
            Write-Step "Stopping Galaxy.Api"
            Stop-Process -Id $apiProcess.Id -Force
            Wait-Process -Id $apiProcess.Id -Timeout 10 -ErrorAction SilentlyContinue
        }
    }
    catch {
        Write-Warning "Could not stop Galaxy.Api cleanly: $($_.Exception.Message)"
    }

    try {
        if ($databaseCreated) {
            if ($tempDbName -notmatch '^galaxy_smoke_[0-9]{14}$') {
                throw "Refusing to drop unexpected database name '$tempDbName'."
            }

            Write-Step "Dropping temporary database $tempDbName"
            $terminateSql = "SELECT pg_terminate_backend(pid) FROM pg_stat_activity WHERE datname = '$tempDbName' AND pid <> pg_backend_pid();"
            docker compose exec -T postgres psql -U galaxy -d postgres -v ON_ERROR_STOP=1 -c $terminateSql | Out-Null

            $dropSql = 'DROP DATABASE IF EXISTS "' + $tempDbName + '";'
            docker compose exec -T postgres psql -U galaxy -d postgres -v ON_ERROR_STOP=1 -c $dropSql | Out-Null
        }
    }
    catch {
        Write-Warning "Could not remove temporary database '$tempDbName': $($_.Exception.Message)"
    }

    $env:ASPNETCORE_ENVIRONMENT = $previousAspNetCoreEnvironment
    $env:ConnectionStrings__Database = $previousConnectionString
    $env:ASPNETCORE_URLS = $previousAspNetCoreUrls
    $PSNativeCommandUseErrorActionPreference = $previousPSNativeCommandUseErrorActionPreference
    Set-Location $previousLocation
}
