#Requires -Version 7.3

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$previousPSNativeCommandUseErrorActionPreference = $PSNativeCommandUseErrorActionPreference
$PSNativeCommandUseErrorActionPreference = $true

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
        [object]$Body = $null
    )

    $uri = "http://127.0.0.1:5178$Path"
    $headers = @{ 'Content-Type' = 'application/json' }
    $parameters = @{
        Uri = $uri
        Method = $Method
        Headers = $headers
        NoProxy = $true
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

    Write-Step "Creating new game for Humans"
    $game = Invoke-Api -Path '/api/game/new' -Method POST -Body @{ username = 'BetaSmoke'; race = 'Humans' }
    if ($game.race -ne 'Humans') {
        Fail('Created game did not return Humans race')
    }

    $homeworldId = $game.planetId
    if ([string]::IsNullOrWhiteSpace($homeworldId)) {
        Fail('Created game did not return a planet id')
    }

    Write-Step "Validating the starting planet"
    $currentGame = Invoke-Api -Path '/api/game/current' -Method GET
    if ($currentGame.planetName -ne 'Homeworld') {
        Fail('The initial planet was not named Homeworld')
    }

    Write-Step "Applying development supply"
    $supply = Invoke-Api -Path "/api/dev/supply?planetId=$homeworldId" -Method POST
    if ($supply.planetId -ne $homeworldId) {
        Fail('Development supply response did not reference the expected planet')
    }

    Write-Step "Building research lab, production complex and assembly complex"
    $buildings = @('ResearchLaboratory', 'ProductionComplex', 'AssemblyComplex')
    foreach ($building in $buildings) {
        Invoke-Api -Path "/api/game/buildings/$building/start?planetId=$homeworldId" -Method POST | Out-Null
        Wait-ForCondition -Description "$building completion" -Condition {
            $status = Invoke-Api -Path "/api/game/buildings/?planetId=$homeworldId" -Method GET
            $status.queuedBuilding -eq $null -and (($status.buildings | Where-Object { $_.building -eq $building }).currentLevel -ge 1)
        }
    }

    Write-Step "Researching required technologies"
    $technologies = @('MaterialsScience', 'EnergySystems', 'ControlSystems', 'Propulsion', 'ComponentEngineering')
    foreach ($technology in $technologies) {
        Invoke-Api -Path "/api/game/research/$technology/start?planetId=$homeworldId" -Method POST | Out-Null
        Wait-ForCondition -Description "$technology completion" -Condition {
            $status = Invoke-Api -Path "/api/game/research/?planetId=$homeworldId" -Method GET
            $status.queuedTechnology -eq $null -and (($status.technologies | Where-Object { $_.technology -eq $technology }).currentLevel -ge 1)
        }
    }

    Write-Step "Producing one colony module and waiting for inventory growth"
    $producedComponentCode = 'humans-colony-1'
    $productionBefore = Invoke-Api -Path "/api/game/production/?planetId=$homeworldId" -Method GET
    $inventoryBefore = $productionBefore.inventory |
        Where-Object { $_.componentCode -eq $producedComponentCode } |
        Select-Object -First 1
    $quantityBefore = if ($null -eq $inventoryBefore) {
        0
    }
    else {
        [int]$inventoryBefore.quantity
    }

    Invoke-Api `
        -Path "/api/game/production/lines/1/orders?planetId=$homeworldId" `
        -Method POST `
        -Body @{ componentCode = $producedComponentCode; quantity = 1 } |
        Out-Null

    Wait-ForCondition -Description 'colony module production' -Condition {
        $productionStatus = Invoke-Api -Path "/api/game/production/?planetId=$homeworldId" -Method GET
        $inventoryItem = $productionStatus.inventory |
            Where-Object { $_.componentCode -eq $producedComponentCode } |
            Select-Object -First 1

        $null -ne $inventoryItem -and
            [int]$inventoryItem.quantity -ge ($quantityBefore + 1)
    }

    Write-Step "Creating a valid blueprint with a colony module"
    $blueprintName = "Beta Colony B1"
    $blueprint = Invoke-Api -Path '/api/game/blueprints/' -Method POST -Body @{
        name = $blueprintName
        hullCode = 'humans-hull-1'
        modules = @(
            @{ componentCode = 'humans-engine-1'; quantity = 1 },
            @{ componentCode = 'humans-reactor-1'; quantity = 1 },
            @{ componentCode = 'humans-control-1'; quantity = 1 },
            @{ componentCode = 'humans-colony-1'; quantity = 1 }
        )
    }

    if (-not $blueprint.id) {
        Fail('Blueprint creation did not return a blueprint id')
    }

    Write-Step "Assembling one ship"
    $assemblyStatus = Invoke-Api -Path "/api/game/assembly/orders?planetId=$homeworldId" -Method POST -Body @{ blueprintId = $blueprint.id; quantity = 1 }
    if (-not $assemblyStatus.orders) {
        Fail('Assembly endpoint did not return an order list')
    }

    Write-Step "Waiting for the ship to appear in reserve"
    Wait-ForCondition -Description 'ship assembly reserve' -Condition {
        $status = Invoke-Api -Path "/api/game/assembly/?planetId=$homeworldId" -Method GET
        $status.reserve.Count -ge 1
    }

    $assemblyStatusFinal = Invoke-Api -Path "/api/game/assembly/?planetId=$homeworldId" -Method GET
    $reserveShip = $assemblyStatusFinal.reserve | Select-Object -First 1
    if (-not $reserveShip) {
        Fail('No ship appeared in reserve after assembly')
    }

    Write-Step "Finding a neutral planet in the same system"
    $galaxy = Invoke-Api -Path '/api/galaxy' -Method GET
    $homeSystem = $galaxy | Where-Object { $_.planets | Where-Object { $_.id -eq $homeworldId } } | Select-Object -First 1
    if (-not $homeSystem) {
        Fail('Could not locate the home system from the galaxy endpoint')
    }

    $targetPlanet = $homeSystem.planets | Where-Object { $_.id -ne $homeworldId -and $_.playerId -eq $null } | Select-Object -First 1
    if (-not $targetPlanet) {
        Fail('No neutral planet was found in the same system for colonization')
    }

    Write-Step "Starting timed colonization deployment"
    $colonization = Invoke-Api -Path "/api/game/colonization/$($targetPlanet.id)" -Method POST -Body @{ shipId = $reserveShip.id }
    if ($colonization.consumedShipId -ne $reserveShip.id) {
        Fail('Colonization response did not reference the consumed ship')
    }

    $planetsBeforeCompletion = Invoke-Api -Path '/api/game/planets' -Method GET
    if (@($planetsBeforeCompletion).Count -ne 1) {
        Fail('Colonization claimed the target planet before deployment completed')
    }

    $activeColonization = Invoke-Api -Path '/api/game/colonization/' -Method GET
    if (-not ($activeColonization | Where-Object { $_.id -eq $colonization.id })) {
        Fail('Colonization operation did not persist')
    }

    Write-Step "Completing colonization through protected development tools"
    Invoke-Api -Path "/api/dev/colonization/$($colonization.id)/complete" -Method POST | Out-Null

    Write-Step "Verifying that the player now has two planets"
    $planets = Invoke-Api -Path '/api/game/planets' -Method GET
    $ownedPlanetCount = @($planets | Where-Object { $_.id -ne $null }).Count
    if ($ownedPlanetCount -lt 2) {
        Fail("Expected at least two owned planets after colonization, got $ownedPlanetCount")
    }

    $newColonyBuildings = Invoke-Api -Path "/api/game/buildings/?planetId=$($targetPlanet.id)" -Method GET
    $newMaterialsExtractor = $newColonyBuildings.buildings | Where-Object { $_.building -eq 'MaterialsExtractor' } | Select-Object -First 1
    $newResearchLaboratory = $newColonyBuildings.buildings | Where-Object { $_.building -eq 'ResearchLaboratory' } | Select-Object -First 1
    if ($newMaterialsExtractor.currentLevel -ne 1 -or $newResearchLaboratory.currentLevel -ne 0) {
        Fail('New colony did not receive an independent starting building state')
    }

    Write-Step "Verifying independent colony construction state"
    Invoke-Api -Path "/api/dev/supply?planetId=$($targetPlanet.id)" -Method POST | Out-Null
    Invoke-Api -Path "/api/game/buildings/MaterialsExtractor/start?planetId=$($targetPlanet.id)" -Method POST | Out-Null
    $homeworldBuildingsAfter = Invoke-Api -Path "/api/game/buildings/?planetId=$homeworldId" -Method GET
    $newColonyBuildingsAfter = Invoke-Api -Path "/api/game/buildings/?planetId=$($targetPlanet.id)" -Method GET
    if ($homeworldBuildingsAfter.queuedBuilding -ne $null) {
        Fail('New colony construction leaked into the homeworld queue')
    }
    if ($newColonyBuildingsAfter.queuedBuilding -ne 'MaterialsExtractor') {
        Fail('New colony did not keep its own construction queue')
    }

    Write-Host ''
    Write-Host 'BETA V1 SMOKE TEST PASSED' -ForegroundColor Green
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
