$base = 'http://127.0.0.1:5183/api/v1'
$loginBody = @{ userNameOrEmail = 'customer@ticketbooking.local'; password = 'Customer@12345'; rememberMe = $true } | ConvertTo-Json
$login = Invoke-RestMethod -Method Post -Uri "$base/auth/login" -ContentType 'application/json' -Body $loginBody
$headers = @{ Authorization = "Bearer $($login.accessToken)" }

$draftPayload = @{
  productType = 'hotel'
  checkoutKey = 'hotel:test-wa:2026-04-20'
  title = 'Khach san demo WA'
  subtitle = 'Resume checkout smoke'
  resumeUrl = '/checkout?product=hotel&hotelId=demo'
  snapshotJson = (@{
    useVAT = $true
    contact = @{
      fullName = 'Customer Demo'
      phone = '0900000009'
      email = 'customer@ticketbooking.local'
      note = 'WA smoke'
      companyName = '2TMNY'
      taxCode = '0123456789'
      companyAddress = 'HCMC'
      invoiceEmail = 'customer@ticketbooking.local'
    }
    passengers = @(@{
      fullName = 'Customer Demo'
      passengerType = 'adult'
      idNumber = '079123456789'
      email = 'customer@ticketbooking.local'
      phoneNumber = '0900000009'
    })
  } | ConvertTo-Json -Depth 5)
} | ConvertTo-Json -Depth 8
$draft = Invoke-RestMethod -Method Put -Uri "$base/customer/account/checkout-drafts" -Headers $headers -ContentType 'application/json' -Body $draftPayload
$draftResume = Invoke-RestMethod -Method Post -Uri "$base/customer/account/checkout-drafts/$($draft.id)/resume" -Headers $headers -ContentType 'application/json' -Body '{}'
$draftList = @(Invoke-RestMethod -Method Get -Uri "$base/customer/account/checkout-drafts?limit=5" -Headers $headers)

$searchPayload = @{
  productType = 'flight'
  searchKey = 'flight:SGN:HAN:2026-05-01:1'
  queryText = 'Sai Gon - Ha Noi'
  summaryText = 'SGN -> HAN'
  searchUrl = '/flight/results?from=SGN&to=HAN&date=2026-05-01&passengers=1'
  criteriaJson = (@{ from='SGN'; to='HAN'; date='2026-05-01'; passengers='1' } | ConvertTo-Json)
} | ConvertTo-Json -Depth 6
$recentSearch = Invoke-RestMethod -Method Post -Uri "$base/customer/account/recent-searches" -Headers $headers -ContentType 'application/json' -Body $searchPayload
$recentSearches = @(Invoke-RestMethod -Method Get -Uri "$base/customer/account/recent-searches?limit=5" -Headers $headers)

$hotels = Invoke-RestMethod -Method Get -Uri "$base/hotels?page=1&pageSize=1" -ContentType 'application/json'
$hotelItem = $hotels.items[0]
$viewPayload = @{
  productType = 'hotel'
  targetId = $hotelItem.id
  title = $hotelItem.name
  subtitle = $hotelItem.shortDescription
  locationText = @($hotelItem.city, $hotelItem.province) -join ', '
  targetUrl = "/hotel/$($hotelItem.id)"
  imageUrl = $hotelItem.coverImageUrl
} | ConvertTo-Json -Depth 6
$recentView = Invoke-RestMethod -Method Post -Uri "$base/customer/account/recent-views" -Headers $headers -ContentType 'application/json' -Body $viewPayload
$recentViews = @(Invoke-RestMethod -Method Get -Uri "$base/customer/account/recent-views?limit=5" -Headers $headers)

$result = [ordered]@{
  loginUser = $login.user.email
  draft = @{ id = $draft.id; resumeCount = $draftResume.resumeCount; title = $draft.title }
  draftListCount = $draftList.Count
  recentSearch = @{ id = $recentSearch.id; searchCount = $recentSearch.searchCount; summaryText = $recentSearch.summaryText }
  recentSearchCount = $recentSearches.Count
  recentView = @{ id = $recentView.id; viewCount = $recentView.viewCount; title = $recentView.title }
  recentViewCount = $recentViews.Count
}
$result | ConvertTo-Json -Depth 6 | Set-Content -Path 'D:\FPT\TicketBooking.V3\_tmp\wa\wa-api-smoke.json' -Encoding UTF8
$result | ConvertTo-Json -Depth 6
