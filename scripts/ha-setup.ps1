$ErrorActionPreference = "Stop"
$HA_URL = "http://localhost:8123"

Write-Host "🚧 Checking Home Assistant API..." -ForegroundColor Cyan

# Wait for HA to boot
$maxRetries = 30
$retryCount = 0
$haReady = $false

while (-not $haReady -and $retryCount -lt $maxRetries) {
    try {
        $response = Invoke-RestMethod -Uri "$HA_URL/api/onboarding" -Method Get -ErrorAction Stop
        $haReady = $true
    }
    catch {
        $retryCount++
        Write-Host "Waiting for Home Assistant to start (attempt $retryCount/$maxRetries)..."
        Start-Sleep -Seconds 2
    }
}

if (-not $haReady) {
    Write-Host "❌ Home Assistant failed to start." -ForegroundColor Red
    exit 1
}

Write-Host "✅ Home Assistant is running!" -ForegroundColor Green

# Check onboarding status
$onboarding = Invoke-RestMethod -Uri "$HA_URL/api/onboarding" -Method Get
$userCreated = ($onboarding | Where-Object { $_.step -eq "user" }).done

if ($userCreated) {
    Write-Host "✅ Admin user already exists. Skipping creation." -ForegroundColor Yellow
}
else {
    Write-Host "👤 Creating 'admin' user..." -ForegroundColor Cyan
    $payload = @{
        client_id = "$HA_URL/"
        name      = "Administrator"
        username  = "admin"
        password  = "farmOS_password123!"
        language  = "en"
    } | ConvertTo-Json

    try {
        Invoke-RestMethod -Uri "$HA_URL/api/onboarding/users" -Method Post -Body $payload -ContentType "application/json" | Out-Null
        Write-Host "✅ 'admin' user created successfully! (Password: farmOS_password123!)" -ForegroundColor Green
    }
    catch {
        Write-Host "❌ Failed to create user: $($_.Exception.Message)" -ForegroundColor Red
    }
}

Write-Host ""
Write-Host "🎉 Home Assistant Basics Started!" -ForegroundColor Magenta
Write-Host "--------------------------------------------------------"
Write-Host "1. I have seeded HA with 7 mock sensors (Temp, Humidity, etc.)"
Write-Host "2. Go to $HA_URL and log in with admin / farmOS_password123!"
Write-Host "3. Complete the rest of the location/analytics wizard"
Write-Host "4. Go to Profile (bottom left) -> Security -> Long-Lived Access Tokens"
Write-Host "5. Create a token and put it in your docker-compose.yml as HA_TOKEN"
Write-Host "--------------------------------------------------------"
