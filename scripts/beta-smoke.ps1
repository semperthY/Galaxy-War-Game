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
    Write-Host "[v0.2-smoke] $Message" -ForegroundColor Cyan
}

function Invoke-Api {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [ValidateSet('GET', 'POST', 'PUT')][string]$Method = 'GET',
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

    Write-Step "Verifying direct game page routing"
    $researchPage = Invoke-WebRequest `
        -Uri 'http://127.0.0.1:5178/game/research' `
        -Method GET `
        -NoProxy
    if ($researchPage.StatusCode -ne 200 -or
        $researchPage.Content -notmatch 'data-game-page="research"') {
        Fail('Direct research page route did not return the game interface')
    }
    $operationsPage = Invoke-WebRequest `
        -Uri 'http://127.0.0.1:5178/game/operations' `
        -Method GET `
        -NoProxy
    if ($operationsPage.StatusCode -ne 200 -or
        $operationsPage.Content -notmatch 'data-game-page="operations"' -or
        $operationsPage.Content -notmatch 'data-page="operations"') {
        Fail('Living Galaxy routes or mobile navigation are missing')
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
    if ($currentGame.materials -lt 1200 -or $currentGame.deuterium -lt 400) {
        Fail('Starting economy did not apply the Beta 2 resource balance')
    }

    Write-Step "Validating the complete Beta 2 component catalog"
    $components = Invoke-Api -Path '/api/game/components' -Method GET
    if ($components.Count -ne 40) {
        Fail("Expected 40 v0.2 catalog components, got $($components.Count)")
    }
    $uniqueComponents = @($components | Where-Object { $null -ne $_.race })
    if ($uniqueComponents.Count -ne 8) {
        Fail("Expected 8 unique race components, got $($uniqueComponents.Count)")
    }
    if (@($components | Where-Object {
        [string]::IsNullOrWhiteSpace($_.shortDescription) -or
        [string]::IsNullOrWhiteSpace($_.bestFor) -or
        [string]::IsNullOrWhiteSpace($_.tradeoff)
    }).Count -ne 0) {
        Fail('Component guidance is incomplete')
    }

    $hulls = @($components | Where-Object { $_.type -eq 'Hull' })
    if ($hulls.Count -ne 6 -or
        -not ($hulls.code -contains 'HUL-06')) {
        Fail('The catalog does not expose all six ship hulls')
    }
    $quantumDamper = @($components | Where-Object { $_.code -eq 'QDM-01' })[0]
    if ($null -eq $quantumDamper -or
        -not $quantumDamper.futureContent -or
        $quantumDamper.canInstall -or
        $quantumDamper.canManufacture) {
        Fail('Quantum damper is not visible as locked future archaeology content')
    }

    $controlRatings = @{
        'CTL-01' = 120; 'CTL-02' = 240; 'CTL-03' = 440
        'CTL-04' = 880; 'CTL-H01' = 540
    }
    foreach ($controlCode in $controlRatings.Keys) {
        $control = @($components | Where-Object { $_.code -eq $controlCode })[0]
        if ($null -eq $control -or
            $control.commandRating -ne $controlRatings[$controlCode]) {
            Fail("Unexpected command rating for $controlCode")
        }
    }

    Write-Step "Validating the large-hull test supply"
    $materialsBeforeSupply = $currentGame.materials
    $deuteriumBeforeSupply = $currentGame.deuterium
    $supply = Invoke-Api `
        -Path "/api/dev/supply?planetId=$homeworldId" `
        -Method POST
    if ($supply.componentTypes -ne 39 -or
        $supply.materialsGranted -ne 100000 -or
        $supply.deuteriumGranted -ne 50000 -or
        $supply.componentQuantityGranted -ne 100 -or
        $supply.materials -ne ($materialsBeforeSupply + 100000) -or
        $supply.deuterium -ne ($deuteriumBeforeSupply + 50000)) {
        Fail('Development supply did not grant the complete component catalog')
    }

    $secondSupply = Invoke-Api `
        -Path "/api/dev/supply?planetId=$homeworldId" `
        -Method POST
    if ($secondSupply.materials -ne ($supply.materials + 100000) -or
        $secondSupply.deuterium -ne ($supply.deuterium + 50000)) {
        Fail('Repeated development supply replaced resources instead of adding them')
    }

    $productionStatus = Invoke-Api `
        -Path "/api/game/production/?planetId=$homeworldId" `
        -Method GET
    $stockedHulls = @($productionStatus.inventory | Where-Object {
        $_.componentCode -like 'HUL-*' -and $_.quantity -eq 200
    })
    if ($stockedHulls.Count -ne 6) {
        Fail("Expected six stocked hulls, got $($stockedHulls.Count)")
    }

    Write-Step "Creating a full-stat Beta 2 ship blueprint"
    $blueprint = Invoke-Api -Path '/api/game/blueprints/' -Method POST -Body @{
        name = 'Beta Smoke Escort'
        hullCode = 'HUL-01'
        modules = @(
            @{ componentCode = 'ENG-01'; quantity = 1 },
            @{ componentCode = 'RCT-02'; quantity = 1 },
            @{ componentCode = 'CTL-01'; quantity = 1 },
            @{ componentCode = 'ARM-01'; quantity = 1 },
            @{ componentCode = 'SHD-01'; quantity = 1 },
            @{ componentCode = 'SNS-01'; quantity = 2 },
            @{ componentCode = 'LAS-01'; quantity = 1 },
            @{ componentCode = 'MSL-01'; quantity = 1 }
        )
    }
    if ($blueprint.design.structuralIntegrity -ne 160 -or
        $blueprint.design.shieldCapacity -ne 50 -or
        $blueprint.design.shieldDamage -ne 13 -or
        $blueprint.design.hullDamage -ne 14 -or
        $blueprint.design.scanRange -ne 45) {
        Fail('Blueprint API returned incorrect Beta 2 ship statistics')
    }

    Write-Step "Validating the Beta 2 technology catalog"
    $research = Invoke-Api -Path "/api/game/research/?planetId=$homeworldId" -Method GET
    if ($research.technologies.Count -ne 15) {
        Fail("Expected 15 technology branches, got $($research.technologies.Count)")
    }
    if ($research.availableStreams -ne 0 -or $research.activeResearch.Count -ne 0) {
        Fail('A starting planet without a research center exposed an active stream')
    }

    Write-Step "Verifying that a research order is inserted"
    $enableResearchSql = "UPDATE `"Planets`" SET `"ResearchLaboratoryLevel`" = 1 WHERE `"Id`" = '$homeworldId';"
    docker compose exec -T postgres psql -U galaxy -d $tempDbName -v ON_ERROR_STOP=1 -c $enableResearchSql | Out-Null

    $startedResearch = Invoke-Api `
        -Path "/api/game/research/MaterialsScience/start?planetId=$homeworldId" `
        -Method POST
    if ($startedResearch.activeResearch.Count -ne 1 -or
        $startedResearch.activeResearch[0].technology -ne 'MaterialsScience') {
        Fail('Starting MaterialsScience did not create an active research order')
    }

    Write-Step "Verifying active research survives logout and login"
    Invoke-Api -Path '/api/auth/logout' -Method POST | Out-Null
    $loginAfterStart = Invoke-Api -Path '/api/auth/login' -Method POST -Body @{
        email = 'beta-smoke@example.test'
        password = 'BetaSmoke2026'
    }
    if (-not $loginAfterStart.authenticated) {
        Fail('Login after starting research failed')
    }
    $researchAfterLogin = Invoke-Api `
        -Path "/api/game/research/?planetId=$homeworldId" `
        -Method GET
    if ($researchAfterLogin.activeResearch.Count -ne 1 -or
        $researchAfterLogin.activeResearch[0].technology -ne 'MaterialsScience') {
        Fail('Active research was lost after logout and login')
    }

    Write-Step "Completing research while the commander is offline"
    Invoke-Api -Path '/api/auth/logout' -Method POST | Out-Null

    $finishResearchSql = "UPDATE `"ResearchOrders`" SET `"CompletesAt`" = NOW() - INTERVAL '1 second' WHERE `"PlayerId`" = '$($game.playerId)';"
    docker compose exec -T postgres psql -U galaxy -d $tempDbName -v ON_ERROR_STOP=1 -c $finishResearchSql | Out-Null

    $loginAfterCompletion = Invoke-Api -Path '/api/auth/login' -Method POST -Body @{
        email = 'beta-smoke@example.test'
        password = 'BetaSmoke2026'
    }
    if (-not $loginAfterCompletion.authenticated) {
        Fail('Login after offline research completion failed')
    }

    Write-Step "Applying offline completion under concurrent dashboard refreshes"
    $researchUri = "http://127.0.0.1:5178/api/game/research/?planetId=$homeworldId"
    $researchCookies = $script:webSession.Cookies.GetCookies(
        [Uri]'http://127.0.0.1:5178')
    $cookieHeader = ($researchCookies |
        ForEach-Object { "$($_.Name)=$($_.Value)" }) -join '; '
    $concurrentResearch = 1..4 | ForEach-Object -Parallel {
        Invoke-WebRequest `
            -Uri $using:researchUri `
            -Headers @{ Cookie = $using:cookieHeader } `
            -NoProxy `
            -SkipHttpErrorCheck
    }
    $failedResearchRequests = @($concurrentResearch | Where-Object {
        $_.StatusCode -ne 200
    })
    if ($failedResearchRequests.Count -ne 0) {
        Fail("$($failedResearchRequests.Count) concurrent research requests failed")
    }

    $completedResearch = Invoke-Api `
        -Path "/api/game/research/?planetId=$homeworldId" `
        -Method GET
    $materialsScience = @($completedResearch.technologies | Where-Object {
        $_.technology -eq 'MaterialsScience'
    })[0]
    if ($materialsScience.currentLevel -ne 1 -or
        $completedResearch.activeResearch.Count -ne 0) {
        Fail('Concurrent completion did not apply the research level exactly once')
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

    $secondCommanderPlanets = Invoke-Api -Path '/api/game/planets' -Method GET -Session $secondCommanderSession
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

    $firstCommanderPlanets = Invoke-Api -Path '/api/game/planets' -Method GET -Session $firstCommanderSession
    if ($firstCommanderPlanets.Count -ne 1) {
        Fail("First commander saw $($firstCommanderPlanets.Count) planets instead of exactly one")
    }
    if ($firstCommanderPlanets[0].id -ne $homeworldId) {
        Fail('First commander session returned another commander''s planet')
    }

    Write-Step "Creating a combat ship and a physical fleet"
    $combatShipId = [Guid]::NewGuid().ToString()
    $insertCombatShipSql = "INSERT INTO `"Ships`" (`"Id`", `"PlayerId`", `"PlanetId`", `"ShipBlueprintId`", `"Name`", `"CreatedAt`") VALUES ('$combatShipId', '$($game.playerId)', '$homeworldId', '$($blueprint.id)', 'Smoke Vanguard', NOW());"
    docker compose exec -T postgres psql -U galaxy -d $tempDbName -v ON_ERROR_STOP=1 -c $insertCombatShipSql | Out-Null
    $combatFleet = Invoke-Api -Path '/api/game/living-galaxy/fleets' -Method POST -Body @{
        planetId = $homeworldId
        name = 'Smoke Vanguard'
        shipIds = @($combatShipId)
    }
    if ($combatFleet.status -ne 'Landed' -or $combatFleet.ships.Count -ne 1) {
        Fail('A player fleet was not formed from the reserve')
    }

    Write-Step "Refueling the landed fleet from its home planet"
    $fuelBefore = [decimal]$combatFleet.fuelReserve
    $refuel = Invoke-Api `
        -Path "/api/game/living-galaxy/fleets/$($combatFleet.id)/refuel" `
        -Method POST `
        -Body @{ amount = 1000 }
    if ([decimal]$refuel.amount -ne 1000 -or
        [decimal]$refuel.fuelReserve -ne ($fuelBefore + 1000)) {
        Fail('Refueling did not transfer deuterium into the fleet fuel reserve')
    }

    Write-Step "Validating permanent fields and physical pirate contacts"
    $systemView = Invoke-Api -Path "/api/game/living-galaxy/system?galaxy=$($game.galaxy)&system=$($game.system)" -Method GET
    if ($systemView.fields.Count -lt 4 -or $systemView.fields.Count -gt 6) {
        Fail("Expected 4-6 permanent resource fields, got $($systemView.fields.Count)")
    }
    $pirate = @($systemView.fleets | Where-Object { $_.isPirate })[0]
    if ($null -eq $pirate -or [string]::IsNullOrWhiteSpace($pirate.id)) {
        Fail('The system did not contain a physical pirate fleet')
    }

    Write-Step "Launching attack, resolving a simultaneous round and creating debris"
    $combatPlan = @{
        commands = @(
            @{
                type = 'Attack'; speedMode = 'Cruise'
                targetGalaxy = $game.galaxy; targetSystem = $game.system; targetPosition = $pirate.position
                targetFleetId = $pirate.id; targetObjectId = $null; durationMinutes = 0
                manifestMaterials = 0; manifestDeuterium = 0
            },
            @{
                type = 'Patrol'; speedMode = 'Economy'
                targetGalaxy = $null; targetSystem = $null; targetPosition = $null
                targetFleetId = $null; targetObjectId = $null; durationMinutes = 0
                manifestMaterials = 0; manifestDeuterium = 0
            }
        )
    }
    Invoke-Api -Path "/api/game/living-galaxy/fleets/$($combatFleet.id)/plan" -Method PUT -Body $combatPlan | Out-Null
    Invoke-Api -Path "/api/game/living-galaxy/fleets/$($combatFleet.id)/launch" -Method POST | Out-Null
    $forceAttackSql = "UPDATE `"FlightCommands`" SET `"CompletesAt`" = NOW() - INTERVAL '1 second' WHERE `"FleetId`" = '$($combatFleet.id)' AND `"Status`" = 2; UPDATE `"FleetShips`" SET `"Shield`" = 0, `"Hull`" = 1 WHERE `"FleetId`" = '$($pirate.id)';"
    docker compose exec -T postgres psql -U galaxy -d $tempDbName -v ON_ERROR_STOP=1 -c $forceAttackSql | Out-Null
    Invoke-Api -Path '/api/game/living-galaxy/fleets' -Method GET | Out-Null
    $battles = Invoke-Api -Path '/api/game/living-galaxy/battles' -Method GET
    $battle = @($battles | Where-Object { $_.status -ne 'Completed' })[0]
    if ($null -eq $battle) { Fail('Attack arrival did not create a battle') }
    $resolveBattleSql = "UPDATE `"Battles`" SET `"ResolveAt`" = NOW() - INTERVAL '1 second' WHERE `"Id`" = '$($battle.id)';"
    docker compose exec -T postgres psql -U galaxy -d $tempDbName -v ON_ERROR_STOP=1 -c $resolveBattleSql | Out-Null
    $resolvedBattles = Invoke-Api -Path '/api/game/living-galaxy/battles' -Method GET
    $resolvedBattle = @($resolvedBattles | Where-Object { $_.id -eq $battle.id })[0]
    if ($resolvedBattle.status -ne 'Completed' -or $resolvedBattle.report.Count -lt 1) {
        Fail('The deterministic battle round did not complete with a report')
    }
    $systemAfterBattle = Invoke-Api -Path "/api/game/living-galaxy/system?galaxy=$($game.galaxy)&system=$($game.system)" -Method GET
    if ($systemAfterBattle.debris.Count -lt 1) { Fail('Destroyed pirate ship did not create a debris field') }

    Write-Step "Changing only the next patrol command, returning and landing"
    $returnCommand = @{
        command = @{
            type = 'Return'; speedMode = 'Economy'
            targetGalaxy = $null; targetSystem = $null; targetPosition = $null
            targetFleetId = $null; targetObjectId = $null; durationMinutes = 0
            manifestMaterials = 0; manifestDeuterium = 0
        }
    }
    Invoke-Api -Path "/api/game/living-galaxy/fleets/$($combatFleet.id)/next-command" -Method PUT -Body $returnCommand | Out-Null
    $forceReturnSql = "UPDATE `"FlightCommands`" SET `"CompletesAt`" = NOW() - INTERVAL '1 second' WHERE `"FleetId`" = '$($combatFleet.id)' AND `"Status`" = 2;"
    docker compose exec -T postgres psql -U galaxy -d $tempDbName -v ON_ERROR_STOP=1 -c $forceReturnSql | Out-Null
    $fleetsAfterReturn = Invoke-Api -Path '/api/game/living-galaxy/fleets' -Method GET
    $returnedFleet = @($fleetsAfterReturn | Where-Object { $_.id -eq $combatFleet.id })[0]
    if ($returnedFleet.status -ne 'Orbiting') { Fail('Return did not leave the fleet vulnerable in orbit') }
    $landedFleet = Invoke-Api -Path "/api/game/living-galaxy/fleets/$($combatFleet.id)/land" -Method POST
    if ($landedFleet.status -ne 'Landed') { Fail('Explicit landing did not protect the fleet') }
    $damagedShip = @($landedFleet.ships | Where-Object { $_.shield -lt $_.maxShield })[0]
    if ($null -eq $damagedShip) { Fail('Simultaneous fire did not damage the winning fleet') }
    Invoke-Api -Path "/api/game/living-galaxy/ships/$($damagedShip.id)/service" -Method POST -Body @{ type = 'ShieldRecharge' } | Out-Null

    Write-Step "Mining a permanent field and unloading cargo contextually"
    $minerBlueprint = Invoke-Api -Path '/api/game/blueprints/' -Method POST -Body @{
        name = 'Beta Smoke Miner'
        hullCode = 'HUL-01'
        modules = @(
            @{ componentCode = 'ENG-01'; quantity = 1 },
            @{ componentCode = 'RCT-02'; quantity = 1 },
            @{ componentCode = 'CTL-01'; quantity = 1 },
            @{ componentCode = 'IND-01'; quantity = 1 },
            @{ componentCode = 'IND-05'; quantity = 1 }
        )
    }
    $minerShipId = [Guid]::NewGuid().ToString()
    $insertMinerSql = "INSERT INTO `"Ships`" (`"Id`", `"PlayerId`", `"PlanetId`", `"ShipBlueprintId`", `"Name`", `"CreatedAt`") VALUES ('$minerShipId', '$($game.playerId)', '$homeworldId', '$($minerBlueprint.id)', 'Smoke Miner', NOW());"
    docker compose exec -T postgres psql -U galaxy -d $tempDbName -v ON_ERROR_STOP=1 -c $insertMinerSql | Out-Null
    $minerFleet = Invoke-Api -Path '/api/game/living-galaxy/fleets' -Method POST -Body @{ planetId = $homeworldId; name = 'Smoke Miners'; shipIds = @($minerShipId) }
    $field = $systemView.fields[0]
    $minePlan = @{ commands = @(
        @{ type = 'Mine'; speedMode = 'Economy'; targetGalaxy = $game.galaxy; targetSystem = $game.system; targetPosition = $field.position; targetFleetId = $null; targetObjectId = $field.id; durationMinutes = 1; manifestMaterials = 0; manifestDeuterium = 0 },
        @{ type = 'Return'; speedMode = 'Economy'; targetGalaxy = $null; targetSystem = $null; targetPosition = $null; targetFleetId = $null; targetObjectId = $null; durationMinutes = 0; manifestMaterials = 0; manifestDeuterium = 0 }
    ) }
    Invoke-Api -Path "/api/game/living-galaxy/fleets/$($minerFleet.id)/plan" -Method PUT -Body $minePlan | Out-Null
    Invoke-Api -Path "/api/game/living-galaxy/fleets/$($minerFleet.id)/launch" -Method POST | Out-Null
    $forceMineSql = "UPDATE `"FlightCommands`" SET `"CompletesAt`" = NOW() - INTERVAL '1 second' WHERE `"FleetId`" = '$($minerFleet.id)' AND `"Status`" = 2;"
    docker compose exec -T postgres psql -U galaxy -d $tempDbName -v ON_ERROR_STOP=1 -c $forceMineSql | Out-Null
    $miningResult = Invoke-Api -Path '/api/game/living-galaxy/fleets' -Method GET
    $loadedMiner = @($miningResult | Where-Object { $_.id -eq $minerFleet.id })[0]
    if (($loadedMiner.materialsCargo + $loadedMiner.deuteriumCargo) -le 0) { Fail('Mining did not put resources into the cargo hold') }
    docker compose exec -T postgres psql -U galaxy -d $tempDbName -v ON_ERROR_STOP=1 -c $forceMineSql | Out-Null
    Invoke-Api -Path '/api/game/living-galaxy/fleets' -Method GET | Out-Null
    Invoke-Api -Path "/api/game/living-galaxy/fleets/$($minerFleet.id)/land" -Method POST | Out-Null
    $unloadPlan = @{ commands = @(@{ type = 'LoadUnload'; speedMode = 'Economy'; targetGalaxy = $null; targetSystem = $null; targetPosition = $null; targetFleetId = $null; targetObjectId = $null; durationMinutes = 0; manifestMaterials = 0; manifestDeuterium = 0 }) }
    Invoke-Api -Path "/api/game/living-galaxy/fleets/$($minerFleet.id)/plan" -Method PUT -Body $unloadPlan | Out-Null
    Invoke-Api -Path "/api/game/living-galaxy/fleets/$($minerFleet.id)/launch" -Method POST | Out-Null
    $afterUnload = Invoke-Api -Path '/api/game/living-galaxy/fleets' -Method GET
    $emptyMiner = @($afterUnload | Where-Object { $_.id -eq $minerFleet.id })[0]
    if (($emptyMiner.materialsCargo + $emptyMiner.deuteriumCargo) -ne 0) { Fail('Context-aware unload did not empty the cargo hold') }

    Write-Host ''
    Write-Host 'GALAXY WAR GAME V0.2 SMOKE TEST PASSED' -ForegroundColor Green
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
