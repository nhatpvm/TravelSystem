# VẬN HÀNH HỆ THỐNG TICKETBOOKING.V3

---

## **1. KHỞI ĐỘNG HỆ THỐNG (Startup Phase)**

### **1.1 Flow Khởi Động (Program.cs)**

```
[1] Đọc configuration (appsettings.*.json)
    ├─ ConnectionStrings (SQL Server)
    ├─ Jwt (Issuer, Audience, SigningKey)
    ├─ Tenancy (Header names)
    ├─ SePay (API credentials)
    └─ Serilog (Logging config)

[2] Cấu hình Logging (Serilog)
    ├─ Console output (dev)
    ├─ File output (Logs/log-YYYY-MM-DD.txt, rotate daily, keep 14 days)
    ├─ Enrichment: Environment, MachineName, ProcessId, ThreadId
    └─ min level: Information

[3] Đăng ký Dependencies (DI Container)
    ├─ Controllers + API Versioning (v1.0)
    ├─ DbContext + Interceptor (AuditTenantSoftDeleteInterceptor)
    ├─ Identity (AppUser, AppRole, GUID-based)
    ├─ JWT Authentication
    ├─ Authorization (Permission-based)
    ├─ Swagger/OpenAPI (with JWT authorize button)
    ├─ Health checks (SQL Server connectivity)
    ├─ Tenancy context (ITenantContext)
    ├─ Services:
    │  ├─ Flight services (public queries)
    │  ├─ Bus/Train services
    │  ├─ Hotel services
    │  ├─ Tour services (complex)
    │  │  ├─ Booking, Cancellation, Reschedule
    │  │  ├─ 4 Quote Adapters (Flight, Hotel, Bus, Train)
    │  │  ├─ 4 Reservation Adapters
    │  │  ├─ 4 Booking Adapters
    │  │  └─ 4 Cancellation Adapters
    │  └─ CMS services + background publishing service
    └─ Hosted services (background jobs)

[4] Automatic Migrations (nếu Dev + config cho phép)
    └─ Chạy EF Core migrations lên SQL Server

[5] Data Seeding (Sequential, in order)
    ├─ IdentitySeed: Roles (Admin, Manager, Customer) + default users
    ├─ TenantsSeed: Tenants (VMM001, NX001, VT001, KS001, TOUR001)
    ├─ PermissionsSeed: Permissions (create_booking, view_reports, etc.)
    ├─ BusDemoSeed: Bus demo data (NX001 tenant)
    ├─ TrainDemoSeed: Train demo data (VT001 tenant)
    ├─ FlightDemoSeed: Flight demo data (VMM001 tenant)
    ├─ CmsDemoSeed: CMS demo data (NX001 tenant)
    └─ HotelDemoSeed: Hotel demo data (KS001 tenant)
    └─ TourDemoSeed: Tour demo data (commented out, can enable)

[6] Middleware Pipeline
    ├─ Exception Handler (global error handling → ProblemDetails or JSON error)
    ├─ Swagger UI (dev only)
    ├─ Serilog request logging
    ├─ HTTPS Redirect
    ├─ Authentication (JWT validation)
    ├─ TenantContextMiddleware (resolve tenant from header)
    ├─ Authorization (permission checks)
    ├─ Route handlers
    └─ Health check endpoints

[7] Start listening
    └─ https://localhost:5001 (or configured port)
```

### **1.2 Seeding Details**

Seeding là quá trình đưa sample data vào database khi app khởi động.

#### **IdentitySeed**
```csharp
// Roles:
- Admin: toàn quyền hệ thống
- Manager: quản lý dữ liệu trong tenant
- Customer: người dùng thường, chỉ booking
- QL_Bus, QL_Flight, QL_Hotel, QL_Tour: managers cho từng module

// Default Users:
- admin@example.com (password: Admin@123)
- customer@example.com (password: Customer@123)
```

#### **TenantsSeed**
```sql
INSERT INTO Tenants (Id, Code, Name, Type, Status)
VALUES
  (guid1, 'VMM001', 'Vietnam Airlines Master', Flight, Active),
  (guid2, 'NX001', 'Sao Vàng Bus Company', Bus, Active),
  (guid3, 'VT001', 'Vietnam Railways', Train, Active),
  (guid4, 'KS001', 'Hotel Brands', Hotel, Active),
  (guid5, 'TOUR001', 'Tour Operators', Tour, Active)

-- Assign users to tenants
INSERT INTO TenantUsers (UserId, TenantId, Role)
VALUES
  (admin_id, guid1, Admin),
  (admin_id, guid2, Admin),
  (admin_id, guid3, Admin),
  (admin_id, guid4, Admin),
  (admin_id, guid5, Admin),
  (customer_id, guid2, Customer),
  (customer_id, guid3, Customer)
```

#### **BusDemoSeed (NX001)**
```
Routes: HN → Sài Đồng, HN → Hải Phòng
Stops: Hà Nội, Ninh Bình, Hà Tĩnh, Sài Đồng, Hải Phòng
Vehicles: 2 buses (29 chỗ each)
Trips: HN→Sài Đồng 06:00, 12:00, 18:00
Prices: 150k, 200k, 250k (segment-based)
Holds: Configurable via code
```

---

## **2. REQUEST LIFECYCLE (Xử lý một request)**

### **2.1 Request đến hệ thống**

```
Client sends: GET /api/v1/search/flights?from=SGN&to=HAN&date=2026-04-03
             Headers: Authorization: Bearer <JWT_TOKEN>
                      X-TenantId: <TENANT_ID_OPTIONAL>

[1] AUTHENTICATION (JWT)
    ├─ Validate token signature (using Jwt:SigningKey)
    ├─ Check issuer = Jwt:Issuer
    ├─ Check audience = Jwt:Audience
    ├─ Validate expiry (+ 30s clock skew)
    └─ Extract claims (sub=userId, email, roles, etc.)

[2] TENANTCONTEXTMIDDLEWARE
    ├─ Check if route is global bypass (auth/*, admin/geo, etc.)
    ├─ Load authenticated user (AppUser by userId)
    ├─ Load user's tenant memberships (TenantUsers)
    ├─ Apply Tenancy rules:
    │  ├─ Admin:
    │  │  - Read: can omit X-TenantId (cross-tenant read)
    │  │  - Write: MUST provide X-TenantId
    │  └─ Non-admin:
    │     - 1 tenant: auto-select if no header
    │     - many tenants: header REQUIRED
    │     - validate user is member of header tenant
    └─ Set ITenantContext.SetTenant(tenantId)

[3] AUTHORIZATION (Permission check)
    ├─ Route requires [Authorize] attribute?
    ├─ Check Permission Policy
    │  ├─ User-level permissions (UserPermissions table)
    │  ├─ Role-level permissions (RolePermissions table)
    │  └─ Tenant-role permissions (TenantRolePermissions table)
    └─ Deny wins: if any Deny found → 403 Forbidden

[4] CONTROLLER ACTION
    ├─ Model binding & validation (FluentValidation)
    ├─ Business logic execution
    │  ├─ DbContext automatically filters by TenantId (via global filter)
    │  ├─ If write: interceptor adds CreatedAt, UpdatedAt, TenantId
    └─ Return response (200 OK / 400 Bad / 404 Not Found / etc.)

[5] GLOBAL EXCEPTION HANDLER
    ├─ Catch any unhandled exceptions
    ├─ Log to Serilog
    ├─ Transform to ProblemDetails or JSON error
    └─ Return error response

[6] RESPONSE
    └─ Send JSON back to client with status code
```

### **2.2 Contoh: Flight Search Request**

```http
GET /api/v1/search/flights?from=SGN&to=HAN&date=2026-04-03
Authorization: Bearer eyJhbGc...
X-TenantId: <nullable, controller infers>
```

**Server Processing:**
```csharp
// 1. FlightSearchController.Search()
public async Task<IActionResult> Search(
    FlightSearchRequest request,  // from=SGN, to=HAN, date=2026-04-03
    CancellationToken ct = default)
{
    // 2. Validate input
    if (string.IsNullOrWhiteSpace(request.From))
        return BadRequest("from is required");
    
    // 3. Call service
    var response = await _flightPublicQueryService.SearchAsync(request, ct);
    
    // 4. Return response
    return Ok(response);
}

// Service logic:
public async Task<FlightSearchResponse> SearchAsync(FlightSearchRequest request, CancellationToken ct)
{
    // Query flights
    var flights = await _db.FlightFlights
        .Where(x => 
            x.FromAirport.Code == request.From &&
            x.ToAirport.Code == request.To &&
            x.DepartureAt.Date == request.Date &&
            x.Status == FlightStatus.Published &&
            !x.IsDeleted)
        .ToListAsync(ct);
    
    // Build offers for each flight
    var offers = new List<FlightSearchResponseItem>();
    
    foreach (var flight in flights)
    {
        var activeOffers = await _db.FlightOffers
            .Where(x => 
                x.FlightId == flight.Id &&
                x.Status == OfferStatus.Active &&
                x.ExpiresAt > DateTime.Now &&
                !x.IsDeleted)
            .ToListAsync(ct);
        
        offers.AddRange(activeOffers.Select(o => new FlightSearchResponseItem
        {
            OfferId = o.Id,
            FlightNumber = flight.FlightNumber,
            Departure = flight.DepartureAt,
            Arrival = flight.ArrivalAt,
            TotalPrice = o.TotalPrice,
            SeatsAvailable = o.SeatsAvailable
        }));
    }
    
    // Return response
    return new FlightSearchResponse
    {
        Count = offers.Count,
        Items = offers
    };
}
```

---

## **3. CONFIGURATION & ENVIRONMENT**

### **3.1 appsettings.json (Base)**
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```

### **3.2 appsettings.Development.json**
```json
{
  "ConnectionStrings": {
    "Default": "Server=LAPTOP-0ANAMQF1\\MINHNHAT;Database=TicketBookingV3;Trusted_Connection=True;TrustServerCertificate=True;"
  },

  "Serilog": {
    "MinimumLevel": { "Default": "Information" },
    "WriteTo": [
      { "Name": "Console" },
      {
        "Name": "File",
        "Args": {
          "path": "Logs/log-.txt",
          "rollingInterval": "Day",
          "retainedFileCountLimit": 14
        }
      }
    ]
  },

  "Jwt": {
    "Issuer": "TicketBooking.V3",
    "Audience": "TicketBooking.V3",
    "SigningKey": "DEV_ONLY_CHANGE_ME_TO_A_LONG_RANDOM_SECRET_KEY_32+",
    "AccessTokenMinutes": 60
  },

  "Tenancy": {
    "AdminWriteRequiresTenantSwitch": true,
    "HeaderTenantId": "X-TenantId",
    "HeaderTenantCode": "X-TenantCode"
  },

  "SePay": {
    "ApiBaseUrl": "https://api.sepay.vn",
    "ApiKey": "DEV_SEPAY_API_KEY",
    "WebhookSecret": "DEV_SEPAY_WEBHOOK_SECRET"
  },

  "Database": {
    "AutoMigrate": true  // Auto-run EF migrations on startup
  }
}
```

### **3.3 Environment-specific configs**
```
appsettings.Development.json  → Local dev (localhost, trusted connection)
appsettings.Staging.json      → Staging server (encrypted connection string, real JWT key)
appsettings.Production.json   → Prod server (Azure Key Vault, real SePay credentials)
```

---

## **4. LOGGING & MONITORING**

### **4.1 Serilog Configuration**

**Outputs:**
- **Console** (dev only): Real-time logs in terminal
- **File** (all envs): Daily rolling files, 14-day retention
  - Path: `Logs/log-2026-04-03.txt`
  - Example: `2026-04-03 09:30:15.123 [INF] User 'admin@example.com' logged in`

**Enrichers:**
- Environment (Development, Staging, Production)
- MachineName (server hostname)
- ProcessId (PID)
- ThreadId (thread ID)

**Min Levels:**
```
Default:                          Information
Microsoft:                        Warning (suppress EF Core verbose logs)
Microsoft.Hosting.Lifetime:       Information (startup messages)
```

### **4.2 Log Examples**

```
[INF] TicketBooking.Api.Controllers.FlightSearchController
      GET /api/v1/search/flights?from=SGN&to=HAN&date=2026-04-03 by user admin@example.com
      Duration: 245ms

[ERR] DbUpdateConcurrencyException
      Message: Database operation expected to affect 1 row(s) but actually affected 0 row(s).
      Data was changed by another user.
      Stack trace: ...

[WRN] TenantContextMiddleware
      User 'customer@example.com' belongs to multiple tenants but no X-TenantId header provided.
      Response: 400 Bad Request

[DBG] AuditTenantSoftDeleteInterceptor
      Inserting TourPackageBooking 'TOURXYZ001ABC'
      Setting TenantId = guid, CreatedAt = 2026-04-03T09:30:15Z, CreatedBy = user_id
```

### **4.3 Health Checks**

**Endpoints:**
- `/health` → General health check (HTTP 200)
- `/health/db` → Database connectivity check

**SQL Server Health Check:**
```csharp
// Queries: SELECT 1
// If connection fails → returns 503 Service Unavailable
```

---

## **5. MIDDLEWARE PIPELINE**

### **5.1 Order Matters**

```
┌─────────────────────────────────────────────────────┐
│  Request comes in                                   │
└────────────────────────────┬────────────────────────┘
                             ↓
┌─────────────────────────────────────────────────────┐
│ [1] ExceptionHandler (catches all exceptions)       │
└────────────────────────────┬────────────────────────┘
                             ↓
┌─────────────────────────────────────────────────────┐
│ [2] Swagger UI (if IsDevelopment)                   │
│     GET /swagger → serves UI                        │
│     GET /swagger/v1/swagger.json → serves spec      │
└────────────────────────────┬────────────────────────┘
                             ↓
┌─────────────────────────────────────────────────────┐
│ [3] Serilog Request Logging (logs all requests)     │
│     Logs: method, path, status, duration            │
└────────────────────────────┬────────────────────────┘
                             ↓
┌─────────────────────────────────────────────────────┐
│ [4] HTTPS Redirect                                  │
│     HTTP → HTTPS (if configured)                    │
└────────────────────────────┬────────────────────────┘
                             ↓
┌─────────────────────────────────────────────────────┐
│ [5] Authentication (JWT Bearer token validation)    │
│     Extracts claims, validates expiry               │
└────────────────────────────┬────────────────────────┘
                             ↓
┌─────────────────────────────────────────────────────┐
│ [6] TenantContextMiddleware (tenant resolution)     │
│     Sets ITenantContext based on user + header      │
│     Validates tenant membership                     │
└────────────────────────────┬────────────────────────┘
                             ↓
┌─────────────────────────────────────────────────────┐
│ [7] Authorization (permission-based)                │
│     Checks [Authorize] policy + custom permissions  │
└────────────────────────────┬────────────────────────┘
                             ↓
┌─────────────────────────────────────────────────────┐
│ [8] Route Matching & Controller Action              │
│     Executes business logic                         │
└────────────────────────────┬────────────────────────┘
                             ↓
┌─────────────────────────────────────────────────────┐
│ Response sent back through middleware stack         │
│ (in reverse order)                                  │
└────────────────────────────┬────────────────────────┘
                             ↓
┌─────────────────────────────────────────────────────┐
│ Response to client (JSON, status code, headers)     │
└─────────────────────────────────────────────────────┘
```

### **5.2 TenantContextMiddleware Rules**

| User Type | Read | Write |
|-----------|------|-------|
| Admin | X-TenantId optional | X-TenantId required |
| Non-admin (1 tenant) | Auto-select | Auto-select or header |
| Non-admin (N tenants) | Header required | Header required |
| Unauthenticated | N/A | 401 Unauthorized |

**Global Bypass Routes (no tenant required):**
- `/api/v1/auth/*` (login, register, forgot-password)
- `/api/v1/admin/users` (user management)
- `/api/v1/admin/roles` (role management)
- `/api/v1/admin/tenants` (tenant management)
- `/api/v1/admin/geo` (geographic data)

---

## **6. ERROR HANDLING & RESPONSES**

### **6.1 Exception → Response Mapping**

```csharp
Exception Type                          → HTTP Status
─────────────────────────────────────────────────────
ArgumentException                       → 400 Bad Request
BadHttpRequestException                 → 400 Bad Request (or custom)
KeyNotFoundException                    → 404 Not Found
DbUpdateConcurrencyException            → 409 Conflict
InvalidOperationException (not found)   → 404 Not Found
InvalidOperationException (client error)→ 400 Bad Request
Unhandled Exception                     → 500 Internal Server Error
```

### **6.2 Response Formats**

**Success:**
```json
HTTP/1.1 200 OK
Content-Type: application/json

{
  "count": 5,
  "items": [...]
}
```

**Error (4xx/5xx):**

Option 1 - Simple JSON:
```json
HTTP/1.1 400 Bad Request
Content-Type: application/json

{
  "message": "Query 'from' and 'to' are required.",
  "traceId": "0HN4P4MRN0ELF:00000001"
}
```

Option 2 - ProblemDetails (RFC 7231):
```json
HTTP/1.1 500 Internal Server Error
Content-Type: application/problem+json

{
  "type": "https://httpstatuses.com/500",
  "title": "An unexpected error occurred.",
  "status": 500,
  "instance": "/api/v1/search/flights",
  "traceId": "0HN4P4MRN0ELF:00000001",
  "detail": "[DEV ONLY] Stack trace..."
}
```

### **6.3 Concurrency Exception Handling**

```csharp
// When two users modify same row simultaneously
DbUpdateConcurrencyException
  → 409 Conflict
  → Message: "Data was changed by another user. Please reload and try again."
  
// Example: Two admins updating same hotel config
Thread1: GET Hotel → RowVersion v1
Thread2: GET Hotel → RowVersion v1 (same)
Thread1: PUT Hotel with RowVersion v1 → SUCCESS, RowVersion now v2
Thread2: PUT Hotel with RowVersion v1 → FAILS, 409 Conflict
  → User2 must reload and retry
```

---

## **7. DATABASE & PERSISTENCE**

### **7.1 SQL Server Connection**

```
ConnectionString:
Server=LAPTOP-0ANAMQF1\MINHNHAT;
Database=TicketBookingV3;
Trusted_Connection=True;
TrustServerCertificate=True;
MultipleActiveResultSets=True;
```

**Options:**
- `Trusted_Connection=True`: Windows authentication (dev)
- `User Id=sa; Password=...`: SQL auth (prod, connection string from Key Vault)
- `MultipleActiveResultSets=True`: Allow multiple concurrent queries in same connection

### **7.2 EF Core Interceptor (AuditTenantSoftDeleteInterceptor)**

**Runs on every SaveChanges():**

```csharp
// On INSERT
entity.TenantId = TenantContext.TenantId  // auto-assign
entity.CreatedAt = DateTimeOffset.Now     // in UTC+7
entity.CreatedByUserId = CurrentUserId    // who made change

// On UPDATE
entity.UpdatedAt = DateTimeOffset.Now
entity.UpdatedByUserId = CurrentUserId

// On DELETE (logical, not physical)
entity.IsDeleted = true
entity.UpdatedAt = DateTimeOffset.Now
```

**Global Query Filter (automatic):**
```csharp
// Every SELECT query automatically adds:
WHERE IsDeleted = false
  AND TenantId = @currentTenantId
```

### **7.3 Database Migrations**

```powershell
# Create new migration
Add-Migration AddHotelAmenities

# Apply to database (manual)
Update-Database

# Or automatic (if config Database:AutoMigrate = true)
# Runs at startup

# Rollback to previous
Update-Database -Migration PreviousMigrationName
```

### **7.4 Transactions & Concurrency**

**Optimistic Locking (Flight Offers):**
```csharp
// RowVersion is checked on update
try
{
    var offer = await _db.FlightOffers.FindAsync(offerId);
    offer.SeatsAvailable -= 2;
    await _db.SaveChangesAsync();  // If RowVersion changed, throws
}
catch (DbUpdateConcurrencyException)
{
    // Reload and retry
    await _db.Entry(offer).ReloadAsync();
    // Try again...
}
```

**Explicit Transactions (Tour Booking):**
```csharp
using var tx = await _db.Database.BeginTransactionAsync();

try
{
    // Confirm flight
    await flightAdapter.ConfirmAsync(...);
    // Confirm hotel
    await hotelAdapter.ConfirmAsync(...);
    // If any fails, exception thrown
    
    await _db.SaveChangesAsync();
    await tx.CommitAsync();
}
catch
{
    await tx.RollbackAsync();
    // Release all holds
}
```

---

## **8. BACKGROUND JOBS & ASYNC PROCESSING**

### **8.1 Hangfire Integration**

**Installed in Infrastructure:**
```csharp
// NuGet: Hangfire.AspNetCore, Hangfire.SqlServer

builder.Services.AddHangfire(config =>
    config.UseSqlServerStorage(connectionString));

app.UseHangfireServer();
app.UseHangfireDashboard("/hangfire");  // Monitor jobs
```

**Job Examples:**
```csharp
// Enqueue immediate
BackgroundJob.Enqueue(() => FlightInventorySync());

// Schedule for later
BackgroundJob.Schedule(() => SendConfirmationEmail(...), TimeSpan.FromMinutes(5));

// Recurring job
RecurringJob.AddOrUpdate(
    "flight-sync",
    () => FlightInventorySync(),
    Cron.Daily(2, 0));  // 2 AM every day
```

### **8.2 Hosted Service (CMS Publishing)**

```csharp
// Runs continuously in background

public class CmsPublishingBackgroundService : BackgroundService
{
    protected async override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            // Every 5 minutes
            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            
            // Find posts scheduled for publishing
            var scheduled = await _db.CmsNewsPosts
                .Where(x => 
                    x.Status == PostStatus.Scheduled &&
                    x.PublishedAt <= DateTime.Now)
                .ToListAsync(stoppingToken);
            
            // Update status to Published
            foreach (var post in scheduled)
            {
                post.Status = PostStatus.Published;
            }
            
            await _db.SaveChangesAsync(stoppingToken);
        }
    }
}
```

---

## **9. IDENTITY & PERMISSIONS**

### **9.1 Authentication Flow**

```
[1] User POST /api/v1/auth/login
    {
      "email": "admin@example.com",
      "password": "Admin@123"
    }

[2] Server validates password using AppUser.PasswordHash (bcrypt)

[3] If valid, generate JWT token:
    {
      "sub": "user_id",
      "email": "admin@example.com",
      "name": "Admin User",
      "role": "Admin",
      "iss": "TicketBooking.V3",
      "aud": "TicketBooking.V3",
      "iat": 1680000000,
      "exp": 1680003600  (1 hour)
    }
    Signed with Jwt:SigningKey

[4] Return token to client
    {
      "accessToken": "eyJhbGc...",
      "expiresIn": 3600
    }

[5] Client stores token, includes in future requests:
    Authorization: Bearer eyJhbGc...
```

### **9.2 Permission Resolution Order**

```
When checking permission "create_booking":

[1] Check UserPermissions (TenantId-specific)
    IF found:
      - Deny? → Return false immediately
      - Allow? → Continue to [2]
    IF not found → Go to [2]

[2] Check TenantRolePermissions (via TenantUserRoles)
    IF found:
      - Deny? → Return false
      - Allow? → Continue to [3]
    IF not found → Go to [3]

[3] Check RolePermissions (global, via Identity roles)
    IF found:
      - Deny? → Return false
      - Allow? → Return true
    IF not found → Default: Return false

Permission Model:
  Role "Admin" has permissions: [create_booking, update_booking, delete_booking, view_reports]
  Role "Manager" has permissions: [create_booking, update_booking, view_reports]
  Role "Customer" has permissions: [create_booking, view_own_bookings]
```

---

## **10. PERFORMANCE & SCALABILITY**

### **10.1 Query Optimization**

**Problem: N+1 Queries**
```csharp
// ❌ BAD: 101 queries (1 hotel + 100 rooms)
var hotels = await _db.Hotels.ToListAsync();
foreach (var hotel in hotels)
{
    var rooms = await _db.RoomTypes.Where(x => x.HotelId == hotel.Id).ToListAsync();
}

// ✅ GOOD: 1 query (eager loading)
var hotels = await _db.Hotels
    .Include(x => x.RoomTypes)
    .ToListAsync();
```

**Indexes:**
```sql
-- Example indexes (EF Core fluent config)
CREATE INDEX IX_Flights_FromAirportId_ToAirportId_DepartureAt
    ON flight.Flights (FromAirportId, ToAirportId, DepartureAt);

CREATE INDEX IX_Offers_FlightId_Status_ExpiresAt
    ON flight.Offers (FlightId, Status, ExpiresAt);

-- Helps: Search flights by date efficiently
```

### **10.2 Caching Strategy**

**Entity-level caching (not implemented yet, but recommended):**
```csharp
// Cache locations (read-only, changes rarely)
var locations = await _cache.GetOrCreateAsync("locations", async entry =>
{
    entry.SlidingExpiration = TimeSpan.FromHours(1);
    return await _db.Locations.ToListAsync();
});

// Cache hotel amenities per hotel
var amenities = await _cache.GetOrCreateAsync($"hotel-{hotelId}-amenities", async entry =>
{
    entry.AbsoluteExpiration = DateTime.Now.AddHours(24);
    return await _db.HotelAmenities.Where(...).ToListAsync();
});
```

### **10.3 API Rate Limiting**

**Not implemented yet, but recommended for production:**
```csharp
// Per IP: 100 requests/minute
// Per user: 1000 requests/hour
// Per API key: custom limits

// Using AspNetCore.RateLimit package
builder.Services.AddMemoryCache();
builder.Services.Configure<IpRateLimitOptions>(options =>
{
    options.GeneralRules = new List<RateLimitRule>
    {
        new RateLimitRule
        {
            Endpoint = "*",
            Limit = 100,
            Period = "1m"
        }
    };
});
```

---

## **11. DEPLOYMENT & ENVIRONMENT MANAGEMENT**

### **11.1 Development**
- Machine: Local laptop/desktop
- Database: LocalDB or SQL Server Express
- JWT Key: Dev key (hardcoded, not secure)
- SePay: Dev sandbox (fake credentials)
- Logging: Console + File
- Swagger: Enabled
- Auto-migrations: Yes

### **11.2 Staging**
- Machine: Azure VM or on-prem server
- Database: SQL Server (prod-like)
- JWT Key: Real secret key (from environment variable)
- SePay: Staging sandbox (real staging credentials)
- Logging: File only (no Console)
- Swagger: Disabled
- Auto-migrations: No (manual migrations)
- TLS: HTTPS required

### **11.3 Production**
- Machine: Kubernetes cluster or load-balanced VMs
- Database: SQL Server with replication/failover
- JWT Key: Azure Key Vault (rotated regularly)
- SePay: Production credentials
- Logging: Application Insights / centralized logging
- Swagger: Disabled
- Auto-migrations: No
- TLS: HTTPS required, HSTS
- Rate limiting: Enabled
- DDoS protection: CloudFlare or similar

### **11.4 Configuration Management**

**Environment Variables (override appsettings):**
```powershell
# On server
$env:ConnectionStrings__Default = "Server=prod-db;Database=TicketBooking;User Id=sa;Password=..."
$env:Jwt__SigningKey = "prod-key-from-keyvault"
$env:SePay__ApiKey = "prod-sepay-key"

# Docker
docker run -e "ConnectionStrings__Default=..." `
           -e "Jwt__SigningKey=..." `
           ticketbooking-api:latest
```

---

## **12. MONITORING & DIAGNOSTICS**

### **12.1 Application Insights (Production)**

```csharp
// Add to DI:
builder.Services.AddApplicationInsightsTelemetry();

// Tracks:
- Request/Response times (latency)
- Exceptions (stack traces)
- Database command times
- Custom metrics (bookings/hour, cancellations/day)
- Availability (uptime)
```

**Alerts:**
- Error rate > 5%
- Response time > 5s
- Database connection failures
- Booking failures spike

### **12.2 Health Check Endpoints**

```bash
# General health
$ curl https://api.ticketbooking.com/health
HTTP/1.1 200 OK
{ "ok": true }

# Database connectivity
$ curl https://api.ticketbooking.com/health/db
HTTP/1.1 200 OK
{ "db": "up" }

# On failure
$ curl https://api.ticketbooking.com/health/db
HTTP/1.1 503 Service Unavailable
{ "error": "Database unreachable" }
```

### **12.3 Performance Metrics (Monitoring)**

**Key Metrics:**
```
Flight Search:
  - P50: 150ms
  - P95: 500ms
  - P99: 1000ms
  - Error rate: <1%

Booking Confirmation:
  - P50: 2s (multi-adapter calls)
  - P95: 5s
  - P99: 10s
  - Error rate: <2% (likely external API timeouts)

Database:
  - Connection pool utilization: <80%
  - Query times: <100ms (p95)
  - Locks: <10ms (p95)

System:
  - CPU: <70%
  - Memory: <80%
  - Disk I/O: <70%
```

---

## **KẾT LUẬN: VẬN HÀNH**

Hệ thống vận hành theo flow:

1. **Startup Phase**: Load config → DI setup → Auto-migrate → Seed demo data
2. **Request Lifecycle**: Auth → Tenant resolution → Permission check → Business logic → Error handling
3. **Database**: EF Core with interceptor (auto audit) + soft delete + multi-tenant filtering
4. **Logging**: Serilog (console + file, daily rotation)
5. **Async**: Hangfire for background jobs, hosted services for long-running tasks
6. **Error Handling**: Global exception handler → ProblemDetails or JSON error
7. **Monitoring**: Health checks, logs, Application Insights
8. **Deployment**: Dev/Staging/Prod with different configs

**Key Features:**
- ✅ Multi-tenant with automatic tenant filtering
- ✅ JWT authentication + permission-based authorization
- ✅ Optimistic concurrency handling
- ✅ Full audit trail (who/when/what)
- ✅ Structured logging (Serilog)
- ✅ API versioning (v1.0)
- ✅ Swagger documentation

