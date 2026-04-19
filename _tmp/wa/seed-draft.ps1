$base = 'http://127.0.0.1:5183/api/v1'
$loginBody = @{ userNameOrEmail = 'customer@ticketbooking.local'; password = 'Customer@12345'; rememberMe = $true } | ConvertTo-Json
$login = Invoke-RestMethod -Method Post -Uri "$base/auth/login" -ContentType 'application/json' -Body $loginBody
$headers = @{ Authorization = "Bearer $($login.accessToken)" }
$payload = @{
  productType = 'hotel'
  checkoutKey = 'product=hotel&seatCount=1&seatPriceModifier=0&hotelId=demo&roomCount=1&adultCount=1&childCount=0&totalPrice=0'
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
Invoke-RestMethod -Method Put -Uri "$base/customer/account/checkout-drafts" -Headers $headers -ContentType 'application/json' -Body $payload | ConvertTo-Json -Depth 6
