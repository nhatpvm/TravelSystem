# PHÂN TÍCH KỸ LƯỠNG TỪNG MODULE - TICKETBOOKING.V3

---

## **PHẦN 1: MODULE FLIGHT (Hàng không)**

### **1.1 Entities & Relationships**

```
graph:
┌─ Airline (hãng hàng không)
│  ├─ Aircraft (máy bay)
│  │  ├─ AircraftModel (mẫu máy bay)
│  │  └─ CabinSeatMap → CabinSeat (sơ đồ ghế)
│  ├─ Airport → Location (catalog)
│  └─ FareClass → FareRule (hạng vé + rules)
│
├─ Flight (chuyến bay cụ thể)
│  ├─ FromAirport, ToAirport
│  ├─ Airline, Aircraft
│  ├─ DepartureAt, ArrivalAt
│  └─ Status: Draft | Published | Suspended | Cancelled
│
├─ Offer (snapshot giá cho search result)
│  ├─ Flight, Airline, FareClass
│  ├─ BaseFare, TaxesFees → TotalPrice
│  ├─ SeatsAvailable (snapshot tại thời điểm tạo)
│  ├─ RequestedAt → ExpiresAt (TTL ngắn)
│  ├─ Status: Active | Expired | Cancelled
│  ├─ OfferSegment[] (phục vụ multi-leg flights)
│  └─ OfferTaxFeeLine[] (chi tiết breakdown)
│
└─ Ancillary (dịch vụ bổ sung)
   ├─ Type: Baggage | Meal | Seat | Insurance | Lounge | Priority
   └─ Price
```

### **1.2 Core Entities Chi Tiết**

#### **Airline (Hãng hàng không)**
```csharp
public sealed class Airline
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    
    public string Code { get; set; }                // VN (Vietnam Airlines)
    public string Name { get; set; }                // Vietnam Airlines
    public string? IataCode { get; set; }           // VN
    public string? IcaoCode { get; set; }           // HVN
    
    public string? LogoUrl { get; set; }
    public string? WebsiteUrl { get; set; }
    public string? SupportPhone { get; set; }
    public string? SupportEmail { get; set; }
    
    public bool IsActive { get; set; }              // soft switch
    
    // Audit
    public bool IsDeleted { get; set; }             // soft delete
    public DateTimeOffset CreatedAt { get; set; }   // UTC+7
    public Guid? CreatedByUserId { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public Guid? UpdatedByUserId { get; set; }
    public byte[] RowVersion { get; set; }          // concurrency
}
```

#### **Airport (Sân bay)**
```csharp
public sealed class Airport
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    
    public Guid LocationId { get; set; }            // FK → catalog.Locations
    public string Code { get; set; }                // Internal code
    public string? IataCode { get; set; }           // SGN (Tân Sơn Nhất)
    public string? IcaoCode { get; set; }           // VVTS
    
    public string? TimeZone { get; set; }           // Asia/Ho_Chi_Minh
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    
    public bool IsActive { get; set; }
    
    // Audit + Soft delete
    public bool IsDeleted { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public Guid? UpdatedByUserId { get; set; }
    public byte[] RowVersion { get; set; }
}
```

#### **Aircraft & AircraftModel (Máy bay)**
```csharp
public sealed class AircraftModel
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    
    public string Code { get; set; }                // A320, B738, A380...
    public string Manufacturer { get; set; }        // Airbus, Boeing
    public string Model { get; set; }               // A320-200
    public int? TypicalSeatCapacity { get; set; }  // ~180 cho A320
}

public sealed class Aircraft
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    
    public Guid AircraftModelId { get; set; }       // FK
    public Guid AirlineId { get; set; }             // FK
    
    public string Code { get; set; }                // VN-A123 (internal)
    public string? Registration { get; set; }       // VN-A123 (ICAO registration)
    public string? Name { get; set; }               // Thành phố Hồ Chí Minh
}
```

#### **CabinSeatMap & CabinSeat (Sơ đồ ghế)**
```csharp
public sealed class CabinSeatMap
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    
    public Guid AircraftModelId { get; set; }       // FK
    public CabinClass CabinClass { get; set; }      // Economy | PremiumEconomy | Business | First
    
    public string Code { get; set; }                // A320-ECO (unique per tenant)
    public string Name { get; set; }                // A320 Economy Layout
    
    public int TotalRows { get; set; }              // 31 rows
    public int TotalColumns { get; set; }           // 6 columns (A-F)
    public int DeckCount { get; set; }              // 1 (wide-body: 2)
    
    public string? SeatLabelScheme { get; set; }    // A,B,C,D,E,F
    public bool IsActive { get; set; }
}

public sealed class CabinSeat
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    
    public Guid CabinSeatMapId { get; set; }        // FK
    
    public string SeatNumber { get; set; }          // 12A (Row 12, Column A)
    public int RowIndex { get; set; }               // 11 (0-based)
    public int ColumnIndex { get; set; }            // 0 (0-based)
    
    public int DeckIndex { get; set; }              // 1 (default)
    public bool IsAisle { get; set; }               // true nếu cạnh hành lang
    public bool IsWindow { get; set; }              // true nếu sát cửa sổ
    
    public string? SeatType { get; set; }           // Standard | ExitRow | Preferred | ExtraLegroom
    public string? SeatClass { get; set; }          // Có thể ghi đè cabin class
    public decimal? PriceModifier { get; set; }     // +100k cho exit row
}
```

#### **Flight (Chuyến bay cụ thể)**
```csharp
public sealed class Flight
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    
    public Guid AirlineId { get; set; }             // FK
    public Guid AircraftId { get; set; }            // FK
    public Guid FromAirportId { get; set; }         // FK
    public Guid ToAirportId { get; set; }           // FK
    
    public string FlightNumber { get; set; }        // VN123 (IATA flight number)
    
    public DateTimeOffset DepartureAt { get; set; } // Ngờ rằngêu khởi hành tuyệt đối
    public DateTimeOffset ArrivalAt { get; set; }   // Giờ đến tuyệt đối
    
    // Ví dụ:
    // DepartureAt: 2026-04-03 06:00:00 +07:00
    // ArrivalAt:   2026-04-03 09:00:00 +07:00 (3 giờ bay)
    
    public FlightStatus Status { get; set; }        // Published | Draft | Suspended | Cancelled
    public bool IsActive { get; set; }
    public string? Notes { get; set; }
}
```

#### **FareClass & FareRule (Hạng vé)**
```csharp
public sealed class FareClass
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    
    public Guid AirlineId { get; set; }             // FK
    
    public string Code { get; set; }                // Y (Economy), M (Premium Economy), C (Business), F (First)
    public string Name { get; set; }                // Economy, Premium Economy, Business, First
    public CabinClass CabinClass { get; set; }      // Cabin mapping
    
    public bool IsRefundable { get; set; }          // Có hoàn tiền
    public bool IsChangeable { get; set; }          // Có thể đổi
}

public sealed class FareRule
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    
    public Guid FareClassId { get; set; }           // FK
    
    public string RulesJson { get; set; }           // Flexible JSON storage
    // VD:
    // {
    //   "noShowPenalty": "100%",
    //   "baggage": { "checkedQty": 2, "weight": 23 },
    //   "carryon": { "qty": 1, "weight": 7 },
    //   "meal": "included",
    //   "seat_selection": "paid",
    //   "refund_penalty": "non-refundable"
    // }
}
```

#### **Offer (Snapshot giá tìm kiếm)**
```csharp
public sealed class Offer
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    
    public Guid AirlineId { get; set; }             // FK
    public Guid FlightId { get; set; }              // FK
    public Guid FareClassId { get; set; }           // FK
    
    public OfferStatus Status { get; set; }         // Active | Expired | Cancelled
    
    public string CurrencyCode { get; set; }        // VND
    public decimal BaseFare { get; set; }           // 1,500,000
    public decimal TaxesFees { get; set; }          // 300,000
    public decimal TotalPrice { get; set; }         // 1,800,000
    
    public int SeatsAvailable { get; set; }         // 9 (snapshot lúc tạo)
    
    public DateTimeOffset RequestedAt { get; set; } // Lúc tính giá
    public DateTimeOffset ExpiresAt { get; set; }   // +15 phút (TTL)
    
    public string? ConditionsJson { get; set; }     // Snapshot điều kiện
    public string? MetadataJson { get; set; }       // Extra data
    
    // Ví dụ:
    // RequestedAt: 2026-04-03 09:30:00
    // ExpiresAt:   2026-04-03 09:45:00 (15 phút)
}
```

#### **OfferSegment (Segments cho multi-leg flights)**
```csharp
public sealed class OfferSegment
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    
    public Guid OfferId { get; set; }               // FK
    
    public int SegmentIndex { get; set; }           // 0 (direct) | 0,1 (2-leg)
    
    public Guid? FlightId { get; set; }             // Optional
    public Guid? AirlineId { get; set; }            // Optional
    public Guid? FareClassId { get; set; }          // Optional
    public Guid? CabinSeatMapId { get; set; }       // Optional
    
    public Guid FromAirportId { get; set; }         // SGN
    public Guid ToAirportId { get; set; }           // HAN (leg 1) hoặc HCM → BKK → HAN (leg 1 -> leg 2)
    
    public DateTimeOffset DepartureAt { get; set; } // Segment departure
    public DateTimeOffset ArrivalAt { get; set; }   // Segment arrival
    
    public string? FlightNumber { get; set; }       // VN123
    public CabinClass? CabinClass { get; set; }     // Economy
    
    public string? BaggagePolicyJson { get; set; }  // Snapshot
    public string? FareRulesJson { get; set; }      // Snapshot
}
```

#### **AncillaryDefinition & OfferTaxFeeLine (Dịch vụ bổ sung & Chi tiết tax/fee)**
```csharp
public sealed class AncillaryDefinition
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid AirlineId { get; set; }             // FK
    
    public string Code { get; set; }                // BAGGAGE_23KG | MEAL_PREMIUM | SEAT_XL...
    public string Name { get; set; }               // Extra Baggage 23kg
    public AncillaryType Type { get; set; }        // Baggage | Meal | Seat | Insurance...
    
    public string CurrencyCode { get; set; }        // VND
    public decimal Price { get; set; }              // 200,000
    
    public string? RulesJson { get; set; }          // Conditions
}

public sealed class OfferTaxFeeLine
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    
    public Guid OfferId { get; set; }               // FK
    
    public TaxFeeLineType LineType { get; set; }    // BaseFare | Tax | Fee | Surcharge | Discount
    
    public string Code { get; set; }                // YQ (fuel surcharge) | VAT (VAT)
    public string Name { get; set; }                // Fuel Surcharge | VAT 10%
    public string CurrencyCode { get; set; }        // VND
    public decimal Amount { get; set; }             // 150,000
}
```

### **1.3 Flight Booking Workflow**

#### **Step 1: Search (Tìm kiếm)**
```
Input: 
  - from: SGN (code)
  - to: HAN (code)
  - date: 2026-04-03
  - passengers: 2

Process:
  1. Resolve Airport từ code (IataCode=SGN → AirportId)
  2. Find all Flights với:
     - FromAirportId = SGN airport
     - ToAirportId = HAN airport
     - DepartureAt in [2026-04-03 00:00, 2026-04-04 00:00)
     - Status = Published
     - IsActive = true
     - IsDeleted = false
  
  3. Cho mỗi Flight, find all active, non-expired Offers:
     - FlightId = this Flight
     - Status = Active
     - ExpiresAt > now
     - SeatsAvailable >= passengers
  
  4. Group by (Airline, FareClass) → list best price

Output:
[
  {
    "offerId": "...",
    "flightNumber": "VN123",
    "departure": "2026-04-03 06:00:00 +07:00",
    "arrival": "2026-04-03 09:00:00 +07:00",
    "airline": "Vietnam Airlines",
    "cabin": "Economy",
    "totalPrice": 1800000,
    "seatsAvailable": 9
  },
  ...
]
```

#### **Step 2: Offer Details (Chi tiết giá)**
```
Input: offerId = guid

Process:
  1. Load Offer + OfferSegments[] + OfferTaxFeeLines[]
  2. Join với FareRule để lấy booking conditions
  3. Load AncillaryDefinitions liên quan

Output:
{
  "offer": {
    "id": "...",
    "baseFare": 1500000,
    "taxesFees": 300000,
    "totalPrice": 1800000,
    "seatsAvailable": 9,
    "expiresAt": "2026-04-03 09:45:00"
  },
  "taxBreakdown": [
    { "code": "VAT", "name": "VAT 10%", "amount": 150000 },
    { "code": "FEE", "name": "Admin Fee", "amount": 150000 }
  ],
  "segments": [
    {
      "departure": "06:00 SGN",
      "arrival": "09:00 HAN",
      "flightNumber": "VN123",
      "aircraft": "A320"
    }
  ],
  "conditions": {
    "refundable": true,
    "changeable": true,
    "baggage": "2x 23kg",
    "meal": "included"
  },
  "ancillaries": [
    { "code": "BAGGAGE_EXTRA", "name": "Extra Baggage", "price": 200000 },
    { "code": "SEAT_PREMIUM", "name": "Premium Seat", "price": 500000 }
  ]
}
```

#### **Step 3: Select & Hold (Chọn & Giữ chỗ)**
```
Input:
  - offerId: guid
  - passengers: 2
  - selectedAncillaries: [BAGGAGE_EXTRA, SEAT_PREMIUM]

Process:
  1. Validate Offer:
     - Status = Active
     - ExpiresAt > now
     - SeatsAvailable >= passengers
  
  2. Decrement Offer.SeatsAvailable (optimistic locking với RowVersion):
     UPDATE Offers 
     SET SeatsAvailable = SeatsAvailable - 2, UpdatedAt = now()
     WHERE Id = offerId AND RowVersion = @expectedVersion
  
  3. Nếu fail → show "Nur X ghế còn trống"
  
  4. Create hold record (trong Tours module, nó sẽ là TourPackageReservationItem
     với SourceType=Flight, SourceHoldToken=...)
  
  5. Set expiry: HoldExpiresAt = now() + 15 phút (or config)

Output:
{
  "holdToken": "abc123...",
  "expiresAt": "2026-04-03 09:45:00",
  "totalPrice": 1800000 + (200000 + 500000) * 2 = 4000000
}
```

#### **Step 4: Confirm Booking (Xác nhận đặt vé)**
```
Input:
  - holdToken: "abc123..."
  - passengerDetails: [
      { "name": "Nguyễn Văn A", "dob": "1990-01-01", "email": "..." }
    ]
  - contactInfo: { "email": "...", "phone": "..." }

Process:
  1. Find hold record by holdToken
  2. Validate:
     - Status = Held
     - HoldExpiresAt > now
  
  3. Check payment (integration with payment gateway)
  
  4. Mark hold as Confirmed
  5. Generate e-ticket / booking confirmation
  6. Send email to customer

Output:
{
  "bookingReference": "VNXYZ1ABC",
  "status": "Confirmed",
  "totalPrice": 4000000,
  "eTicket": "...",
  "confirmationEmail": "sent to customer@example.com"
}
```

### **1.4 Flight Inventory & Optimization**

**Challenges:**
- SeatsAvailable là snapshot → cần cập nhật real-time
- Hold timeout (15 phút) → tự động release
- Overbooking strategy (optimize yield)

**Solutions:**
```sql
-- Canonical inventory: (Airline, Flight, FareClass) → SeatsAvailable
-- Tính toán:
SELECT 
  SUM(f.TotalCapacity) as total_capacity,  -- từ aircraft model
  SUM(CASE WHEN o.Status='Active' THEN -o.SeatsAvailable ELSE 0 END) as held_seats,
  SUM(CASE WHEN b.Status='Confirmed' THEN -1 ELSE 0 END) as confirmed_seats,
  total_capacity - held_seats - confirmed_seats as available
FROM Flights f
...
```

---

## **PHẦN 2: MODULE BUS (Xe khách)**

### **2.1 Entities & Relationships**

```
┌─ Provider (Nhà cung cấp xe)
│  │ (catalog.Providers, Type=Bus)
│  │
│  ├─ BusRoute (Tuyến A→B)
│  │  ├─ FromStopPoint, ToStopPoint (→ Locations)
│  │  ├─ RouteStop[] (ordered stops)
│  │  │  └─ StopPoint i @ index 0,1,2...
│  │  │
│  │  └─ Trip (Chuyến cụ thể, ngày cụ thể)
│  │     ├─ TripStopTime[] (giờ đến/đi mỗi stop)
│  │     ├─ TripStopPickupPoint[] (pickup variants)
│  │     ├─ TripStopDropoffPoint[] (dropoff variants)
│  │     ├─ TripSegmentPrice[] (giá segment i→j)
│  │     ├─ Vehicle (FK → fleet.Vehicles)
│  │     └─ TripSeatHold[] (giữ ghế + expiry)
│  │
│  └─ StopPoint (Ga xe: Hà Nội, TP.HCM, Hải Phòng...)
│     └─ LocationId (FK → catalog.Locations)
│
└─ Vehicle (fleet.Vehicles)
   ├─ SeatMap (sơ đồ ghế)
   └─ VehicleDetail (chi tiết xe buýt)
```

### **2.2 Core Entities Chi Tiết**

#### **StopPoint (Ga xe khách)**
```csharp
public sealed class StopPoint
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    
    public Guid LocationId { get; set; }            // FK → catalog.Locations
    public StopPointType Type { get; set; }         // Terminal | Pickup | Dropoff | RestStop
    
    public string Name { get; set; }                // "Hà Nội - Mỹ Đình"
    public string? AddressLine { get; set; }        // "Cầu Mỹ Đình, Hà Nội"
    
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    
    public int SortOrder { get; set; }
    public bool IsActive { get; set; }
}

public enum StopPointType
{
    Terminal = 1,    // Bến xe chính
    Pickup = 2,      // Điểm đón thêm
    Dropoff = 3,     // Điểm trả thêm
    RestStop = 4,    // Dừng nghỉ
    Other = 99
}
```

#### **BusRoute (Tuyến xe)**
```csharp
public sealed class BusRoute
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    
    public Guid ProviderId { get; set; }            // FK → catalog.Providers
    
    public string Code { get; set; }                // HN-SD (Hà Nội - Sài Đồng)
    public string Name { get; set; }                // Hà Nội → Sài Đồng
    
    public Guid FromStopPointId { get; set; }       // FK
    public Guid ToStopPointId { get; set; }         // FK
    
    public int EstimatedMinutes { get; set; }       // 360 (6 giờ)
    public int DistanceKm { get; set; }             // 300
}
```

#### **RouteStop (Các dừng trên tuyến)**
```csharp
public sealed class RouteStop
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    
    public Guid RouteId { get; set; }               // FK
    public Guid StopPointId { get; set; }           // FK
    
    public int StopIndex { get; set; }              // 0: Hà Nội Mỹ Đình
                                                     // 1: Ninh Bình
                                                     // 2: Hà Tĩnh
                                                     // 3: Sài Đồng (cuối)
    
    public int? DistanceFromStartKm { get; set; }   // 0, 100, 200, 300
    public int? MinutesFromStart { get; set; }      // 0, 120, 240, 360
}
```

#### **Trip (Chuyến xe cụ thể)**
```csharp
public sealed class Trip
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    
    public Guid ProviderId { get; set; }            // FK
    public Guid RouteId { get; set; }               // FK
    public Guid VehicleId { get; set; }             // FK → fleet.Vehicles
    
    public string Code { get; set; }                // NX001-HN-SD-20260403-06 (unique per tenant)
    public string Name { get; set; }                // "Hà Nội 06:00 → Sài Đồng"
    
    public TripStatus Status { get; set; }          // Published | Draft | Suspended | Cancelled
    
    public DateTimeOffset DepartureAt { get; set; } // 2026-04-03 06:00:00 +07:00
    public DateTimeOffset ArrivalAt { get; set; }   // 2026-04-03 12:00:00 +07:00
    
    // Policies stored as JSON (flexible)
    public string? FareRulesJson { get; set; }
    public string? BaggagePolicyJson { get; set; }
    public string? BoardingPolicyJson { get; set; }
    
    public bool IsActive { get; set; }
}

public enum TripStatus
{
    Draft = 1,
    Published = 2,
    Suspended = 3,
    Cancelled = 4
}
```

#### **TripStopTime (Giờ đến/đi mỗi dừng)**
```csharp
public sealed class TripStopTime
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    
    public Guid TripId { get; set; }                // FK
    public Guid StopPointId { get; set; }           // FK
    
    public int StopIndex { get; set; }              // 0,1,2,3
    
    public DateTimeOffset? ArriveAt { get; set; }   // Khi đến stop (nullable cho stop đầu)
    public DateTimeOffset? DepartAt { get; set; }   // Khi rời stop (nullable cho stop cuối)
    
    public int? MinutesFromStart { get; set; }      // Cache: 0, 120, 240, 360
}

// Ví dụ: Trip HN→Sài Đồng
// TripStopTime[0]: HN Mỹ Đình    - ArriveAt=null,  DepartAt=06:00
// TripStopTime[1]: Ninh Bình     - ArriveAt=08:00, DepartAt=08:10
// TripStopTime[2]: Hà Tĩnh       - ArriveAt=10:00, DepartAt=10:10
// TripStopTime[3]: Sài Đồng      - ArriveAt=12:00, DepartAt=null
```

#### **TripStopPickupPoint & TripStopDropoffPoint (Pickup/Dropoff Options)**
```csharp
public sealed class TripStopPickupPoint
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    
    public Guid TripStopTimeId { get; set; }        // FK → TripStopTime
    
    public string Name { get; set; }                // "Mỹ Đình Terminal", "Đại học Bách Khoa"
    public string? AddressLine { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    
    public bool IsDefault { get; set; }             // highlight rồi
    public int SortOrder { get; set; }              // UI ordering
}

// Tương tự TripStopDropoffPoint
```

#### **TripSegmentPrice (Giá theo segment)**
```csharp
public sealed class TripSegmentPrice
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    
    public Guid TripId { get; set; }                // FK
    
    public Guid FromTripStopTimeId { get; set; }    // FK
    public Guid ToTripStopTimeId { get; set; }      // FK
    
    public int FromStopIndex { get; set; }          // 0
    public int ToStopIndex { get; set; }            // 2 (Hà Nội → Hà Tĩnh)
    
    public string CurrencyCode { get; set; }        // VND
    public decimal BaseFare { get; set; }           // 150,000
    public decimal? TaxesFees { get; set; }         // 15,000
    public decimal TotalPrice { get; set; }         // 165,000
    
    // Business rule:
    // Segment price cho từ stop i → stop j
    // Ví dụ:
    //   HN(0) → Ninh Bình(1): 50,000
    //   HN(0) → Hà Tĩnh(2): 100,000
    //   HN(0) → Sài Đồng(3): 150,000
    //   Ninh Bình(1) → Hà Tĩnh(2): 60,000
}
```

#### **TripSeatHold (Giữ ghế)**
```csharp
public sealed class TripSeatHold
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    
    public Guid TripId { get; set; }                // FK
    public Guid SeatId { get; set; }                // FK → fleet.Seats
    
    public Guid FromTripStopTimeId { get; set; }    // FK
    public Guid ToTripStopTimeId { get; set; }      // FK
    
    public int FromStopIndex { get; set; }          // 0
    public int ToStopIndex { get; set; }            // 2
    
    public SeatHoldStatus Status { get; set; }      // Held | Confirmed | Cancelled | Expired
    public string HoldToken { get; set; }           // unique ID
    
    public DateTimeOffset HoldExpiresAt { get; set; } // +15 phút
    
    public DateTimeOffset CreatedAt { get; set; }
}

public enum SeatHoldStatus
{
    Held = 1,       // Đang giữ
    Confirmed = 2,  // Đã xác nhận thanh toán
    Cancelled = 3,  // Hủy
    Expired = 4     // Hết hạn hold
}
```

### **2.3 Bus Booking Workflow**

#### **Step 1: Search Trips (Tìm chuyến)**
```
Input:
  - fromLocationId: Hà Nội
  - toLocationId: Sài Đồng
  - departDate: 2026-04-03
  - passengers: 2

Process:
  1. Find RouteStops từ:
     - RouteStop.StopPointId.LocationId = fromLocationId (stop index A)
     - RouteStop.StopPointId.LocationId = toLocationId (stop index B)
     - Ensure A < B
  
  2. Find Trips với:
     - Route này
     - DepartureAt in [2026-04-03 00:00, 2026-04-04 00:00)
     - Status = Published
     - IsActive = true
  
  3. Cho mỗi Trip, đếm available seats:
     = Total seats - held seats - confirmed seats
     (within segment A→B)
  
  4. Filter: AvailableSeats >= passengers

Output:
[
  {
    "tripId": "...",
    "code": "NX001-HN-SD-20260403-06",
    "name": "Hà Nội 06:00 → Sài Đồng 12:00",
    "fromLocation": "Hà Nội",
    "toLocation": "Sài Đồng",
    "pickupOptions": ["Mỹ Đình", "Đại học Bách Khoa"],
    "dropoffOptions": ["Sài Đồng Terminal", "Tân Phú"],
    "departureAt": "2026-04-03 06:00:00",
    "arrivalAt": "2026-04-03 12:00:00",
    "duration": "6 giờ",
    "price": 150000,  // per person, segment HN→Sài Đồng
    "availableSeats": 8,
    "vehicle": "Hyundai County 29 chỗ"
  },
  ...
]
```

#### **Step 2: Select Seats & Hold**
```
Input:
  - tripId: guid
  - fromStopIndex: 0 (Hà Nội)
  - toStopIndex: 3 (Sài Đồng)
  - selectedSeats: ["01A", "01B"]
  - pickupPoint: "Mỹ Đình Terminal"
  - dropoffPoint: "Sài Đồng Terminal"

Process:
  1. Load Trip + Vehicle + SeatMap
  2. Validate:
     - Each seat status = Available (not Held, not Confirmed)
  
  3. Create TripSeatHold[] for each seat:
     - Status = Held
     - HoldExpiresAt = now() + 15 phút
  
  4. Query TripSegmentPrice[0..3] để lấy giá
  
  5. Return totalPrice = sum(segment prices) * qty

Output:
{
  "holds": [
    { "seatNumber": "01A", "holdToken": "abc123", "expiresAt": "..." },
    { "seatNumber": "01B", "holdToken": "def456", "expiresAt": "..." }
  ],
  "totalPrice": 300000,  // 150k * 2
  "expiresAt": "2026-04-03 09:45:00"
}
```

#### **Step 3: Confirm Booking**
```
Input:
  - holdTokens: ["abc123", "def456"]
  - passengerDetails: [
      { "name": "Người 1", "idNumber": "...", "phone": "..." },
      { "name": "Người 2", "idNumber": "...", "phone": "..." }
    ]
  - paymentInfo: { ... }

Process:
  1. Validate all holds exist + not expired
  2. Process payment
  3. Update TripSeatHold.Status = Confirmed (for all holds)
  4. Send ticket to customer email
  5. Update TripSeatHold.FromStopIndex/ToStopIndex/SeatId

Output:
{
  "bookingReference": "NXBUS001ABC",
  "status": "Confirmed",
  "seats": ["01A", "01B"],
  "totalPrice": 300000,
  "tickets": [
    { "ticketNumber": "...", "seatNumber": "01A", "passenger": "Người 1" },
    { "ticketNumber": "...", "seatNumber": "01B", "passenger": "Người 2" }
  ]
}
```

### **2.4 Bus Seat Availability Calculation**

```sql
-- Occupancy for segment i→j in trip T
SELECT
  '01A' as seat_number,
  CASE
    WHEN EXISTS (
      SELECT 1 FROM TripSeatHolds h
      WHERE h.TripId = @tripId
        AND h.SeatId = S.Id
        AND h.FromStopIndex <= @fromStopIndex
        AND h.ToStopIndex > @fromStopIndex  -- overlaps
        AND h.Status IN (Held, Confirmed)
        AND (h.Status = Confirmed OR h.HoldExpiresAt > @now)
    )
    THEN 'HOLD'
    ELSE 'AVAILABLE'
  END as status
FROM Seats S
WHERE S.SeatMapId IN (SELECT SeatMapId FROM Vehicles WHERE Id = @vehicleId)
```

---

## **PHẦN 3: MODULE TRAIN (Tàu hỏa)**

### **3.1 Entities & Relationships**

```
┌─ Provider (VT001, Type=Train)
│  │
│  ├─ TrainRoute (Tuyến)
│  │  ├─ FromStopPoint, ToStopPoint (→ Locations)
│  │  ├─ TrainRouteStop[] (ordered stations)
│  │  │
│  │  └─ TrainTrip (Chuyến tàu cụ thể)
│  │     ├─ TrainTripStopTime[] (giờ đến/đi mỗi ga)
│  │     ├─ TrainTripSegmentPrice[] (giá segment i→j)
│  │     ├─ TrainCar[] (các toa: Toa 1-K1, Toa 2-K2...)
│  │     │  └─ TrainCarSeat[] (ghế/giường trong toa)
│  │     └─ TrainTripSeatHold[] (giữ chỗ + expiry)
│  │
│  └─ TrainStopPoint (Ga tàu: Hà Nội, TP.HCM...)
│     └─ LocationId (FK → catalog.Locations)
│
└─ (không dùng fleet.Vehicles, tàu quản lý riêng)
```

### **3.2 Core Entities Chi Tiết**

#### **TrainStopPoint (Ga tàu)**
```csharp
public sealed class TrainStopPoint
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    
    public Guid LocationId { get; set; }            // FK → catalog.Locations
    public TrainStopPointType Type { get; set; }    // Station | Other
    
    public string Name { get; set; }                // Hà Nội, Hải Phòng, TP.HCM
    public string? AddressLine { get; set; }
    
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    
    public int SortOrder { get; set; }
    public bool IsActive { get; set; }
}
```

#### **TrainRoute (Tuyến tàu)**
```csharp
public sealed class TrainRoute
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    
    public Guid ProviderId { get; set; }            // FK → catalog.Providers (Type=Train)
    
    public string Code { get; set; }                // SE1 (Hà Nội-TP.HCM)
    public string Name { get; set; }                // Hà Nội Express
    
    public Guid FromStopPointId { get; set; }       // FK
    public Guid ToStopPointId { get; set; }         // FK
    
    public int EstimatedMinutes { get; set; }       // 1200 (20 giờ)
    public int DistanceKm { get; set; }             // 1500
}
```

#### **TrainTrip (Chuyến tàu cụ thể)**
```csharp
public sealed class TrainTrip
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    
    public Guid ProviderId { get; set; }            // FK
    public Guid RouteId { get; set; }               // FK
    
    public string TrainNumber { get; set; }         // SE1, SE2, SE3...
    public string Code { get; set; }                // VT001-SE1-20260403 (unique)
    public string Name { get; set; }                // "SE1 Hà Nội 10:00"
    
    public TrainTripStatus Status { get; set; }     // Published | Draft | Suspended | Cancelled
    
    public DateTimeOffset DepartureAt { get; set; } // 2026-04-03 10:00:00
    public DateTimeOffset ArrivalAt { get; set; }   // 2026-04-04 06:00:00 (20 giờ)
    
    public string? FareRulesJson { get; set; }
    public string? BaggagePolicyJson { get; set; }
    public string? BoardingPolicyJson { get; set; }
    
    public bool IsActive { get; set; }
}
```

#### **TrainTripStopTime (Giờ đến/đi mỗi ga)**
```csharp
public sealed class TrainTripStopTime
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    
    public Guid TripId { get; set; }                // FK
    public Guid StopPointId { get; set; }           // FK
    
    public int StopIndex { get; set; }              // 0: Hà Nội, 1: Hải Phòng, 2: TP.HCM
    
    public DateTimeOffset? ArriveAt { get; set; }   // Khi đến (null cho stop đầu)
    public DateTimeOffset? DepartAt { get; set; }  // Khi rời (null cho stop cuối)
    
    public int? MinutesFromStart { get; set; }      // Cache: 0, 600, 1200
}

// Ví dụ:
// Stop 0 (Hà Nội):    ArriveAt=null,       DepartAt=10:00
// Stop 1 (Hải Phòng): ArriveAt=13:00,      DepartAt=13:30
// Stop 2 (TP.HCM):    ArriveAt=06:00+1day, DepartAt=null
```

#### **TrainCar (Toa tàu)**
```csharp
public sealed class TrainCar
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    
    public Guid TripId { get; set; }                // FK
    
    public string CarNumber { get; set; }           // "01", "02", "K1" (Khoán 1), "K6" (Khoán 6)
    public TrainCarType CarType { get; set; }       // SeatCoach | Sleeper | Business | Other
    
    public string? CabinClass { get; set; }         // "Economy", "Business", "SoftSleeper"
    public int SortOrder { get; set; }
    
    public bool IsActive { get; set; }

    // Ví dụ SeatCoach:
    //   CarNumber = "01"
    //   CarType = SeatCoach
    //   Chứa 64 ghế ngồi (rows 16x4)
    //
    // Ví dụ Sleeper:
    //   CarNumber = "K4" (khoán 4)
    //   CarType = Sleeper
    //   Chứa compartments 01-12, mỗi compartment 4 giường (2 cứng, 2 mềm)
}

public enum TrainCarType
{
    SeatCoach = 1,   // Ghế ngồi
    Sleeper = 2,     // Giường nằm (compartments)
    Business = 3,    // Ghế business
    Other = 99
}
```

#### **TrainCarSeat (Ghế/Giường trong toa)**
```csharp
public sealed class TrainCarSeat
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    
    public Guid CarId { get; set; }                 // FK
    
    public string SeatNumber { get; set; }          // "01A", "16D" (ghế) hoặc "K4-01" (giường compartment 1)
    public TrainSeatType SeatType { get; set; }     // Seat | UpperBerth | LowerBerth
    
    public string? CompartmentCode { get; set; }    // "K4" (khoán 4), "K6" (khoán 6)
    public int? CompartmentIndex { get; set; }      // 1..12 (compartment số)
    
    public int RowIndex { get; set; }               // Giúp UI hiển thị
    public int ColumnIndex { get; set; }            // Giúp UI hiển thị
    
    public bool IsWindow { get; set; }              // Sát cửa sổ
    public bool IsAisle { get; set; }               // Cạnh hành lang
    
    public string? SeatClass { get; set; }          // "HardSeat", "SoftSeat", VIP
    public decimal? PriceModifier { get; set; }     // +100k cho VIP
}

public enum TrainSeatType
{
    Seat = 1,
    UpperBerth = 2,   // Giường trên
    LowerBerth = 3    // Giường dưới
}

// Ví dụ SeatCoach:
//   RowIndex=0..15, ColumnIndex=0..3
//   SeatNumber = "01A" (row 0, col A), "16D"
//
// Ví dụ Sleeper Compartment K4:
//   CompartmentCode="K4", CompartmentIndex=1..12
//   SeatNumber="K4-01", "K4-02", "K4-03", "K4-04"
//   Gồm: 2 UpperBerth + 2 LowerBerth
```

#### **TrainTripSegmentPrice (Giá theo segment)**
```csharp
public sealed class TrainTripSegmentPrice
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    
    public Guid TripId { get; set; }                // FK
    
    public Guid FromTripStopTimeId { get; set; }    // FK
    public Guid ToTripStopTimeId { get; set; }      // FK
    
    public int FromStopIndex { get; set; }          // 0 (Hà Nội)
    public int ToStopIndex { get; set; }            // 2 (TP.HCM)
    
    public string CurrencyCode { get; set; }        // VND
    public decimal BaseFare { get; set; }           // 300,000
    public decimal? TaxesFees { get; set; }         // 30,000
    public decimal TotalPrice { get; set; }         // 330,000
    
    // Business rule:
    //   SeatCoach khác giá với Sleeper
    //   UpperBerth khác giá với LowerBerth
    //   Nhưng TrainTripSegmentPrice chỉ lưu base
    //   Thêm modifier từ TrainCarSeat.PriceModifier
}
```

#### **TrainTripSeatHold (Giữ chỗ)**
```csharp
public sealed class TrainTripSeatHold
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    
    public Guid TripId { get; set; }                // FK
    public Guid TrainCarSeatId { get; set; }        // FK
    
    public Guid FromTripStopTimeId { get; set; }    // FK
    public Guid ToTripStopTimeId { get; set; }      // FK
    
    public int FromStopIndex { get; set; }          // 0
    public int ToStopIndex { get; set; }            // 2
    
    public TrainSeatHoldStatus Status { get; set; } // Held | Confirmed | Cancelled | Expired
    public string HoldToken { get; set; }
    
    public DateTimeOffset HoldExpiresAt { get; set; }
    
    public DateTimeOffset CreatedAt { get; set; }
}

public enum TrainSeatHoldStatus
{
    Held = 1,
    Confirmed = 2,
    Cancelled = 3,
    Expired = 4
}
```

### **3.3 Train Booking Workflow**

Tương tự Bus, nhưng khác:
- Ghế/Giường có types: Seat, UpperBerth, LowerBerth
- Compartment support cho sleeper cars
- Giá theo CarType + SeatType

```
Search →
  Select Berth Type (SeatCoach hoặc Sleeper k4-01...) →
  Select Seat/Berth (01A hoặc K4-01) →
  Hold →
  Confirm
```

---

## **PHẦN 4: MODULE HOTEL (Khách sạn)**

### **4.1 Entities & Relationships**

```
┌─ Hotel (Khách sạn cụ thể)
│  ├─ HotelImage[] (hình ảnh)
│  ├─ HotelAmenity[] (tiện nghi: WiFi, AC, Pool)
│  ├─ HotelAmenityLink[] (link tiện nghi → Hotel)
│  │
│  ├─ RoomType (Phòng Deluxe, Suite...)
│  │  ├─ RoomTypeImage[]
│  │  ├─ RoomAmenity[]
│  │  ├─ RoomAmenityLink[]
│  │  ├─ BedType[] + RoomTypeBed[] (giường)
│  │  ├─ RoomTypeOccupancyRule[] (max guests)
│  │  │
│  │  └─ RoomInventory (số phòng khả dụng)
│  │
│  ├─ MealPlan (BB, HB, FB)
│  ├─ CancellationPolicy (rules hoàn hủy)
│  ├─ ExtraService (extra bed, breakfast)
│  │
│  └─ RoomHold (giữ phòng)
│
└─ BedType (Single, Double, Twin...)
```

### **4.2 Core Entities Chi Tiết**

#### **Hotel (Khách sạn)**
```csharp
public sealed class Hotel
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    
    public string Code { get; set; }                // HOTEL001
    public string Name { get; set; }                // Hilton Hanoi Opera
    public string? Slug { get; set; }               // hilton-hanoi-opera (SEO)
    
    // Location
    public Guid? LocationId { get; set; }           // FK → catalog.Locations
    public string? AddressLine { get; set; }        // 1 Tràng Tiền, Hoàn Kiếm, Hà Nội
    public string? City { get; set; }               // Hà Nội
    public string? Province { get; set; }           // Hà Nội
    public string? CountryCode { get; set; }        // VN
    
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public string? TimeZone { get; set; }           // Asia/Ho_Chi_Minh
    
    // Content
    public string? ShortDescription { get; set; }
    public string? DescriptionMarkdown { get; set; }
    public string? DescriptionHtml { get; set; }
    
    public int StarRating { get; set; }             // 5
    public HotelStatus Status { get; set; }         // Active | Draft | Inactive | Suspended
    
    // Check-in/out
    public TimeOnly? DefaultCheckInTime { get; set; }   // 14:00
    public TimeOnly? DefaultCheckOutTime { get; set; }  // 12:00
    
    // Contact
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? WebsiteUrl { get; set; }
    
    // SEO
    public string? SeoTitle { get; set; }
    public string? SeoDescription { get; set; }
    public string? SeoKeywords { get; set; }
    public string? CanonicalUrl { get; set; }
    public string? Robots { get; set; }
    public string? OgImageUrl { get; set; }
    public string? SchemaJsonLd { get; set; }
    
    // Media
    public Guid? CoverMediaAssetId { get; set; }    // FK → cms.MediaAssets
    public string? CoverImageUrl { get; set; }
    
    // Policies
    public string? PoliciesJson { get; set; }       // property rules, child policy, pet policy...
    public string? MetadataJson { get; set; }
}
```

#### **RoomType (Loại phòng)**
```csharp
public sealed class RoomType
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    
    public Guid HotelId { get; set; }               // FK
    
    public string Code { get; set; }                // DELUXE, SUITE, FAMILY...
    public string Name { get; set; }                // Deluxe Room
    
    public string? DescriptionMarkdown { get; set; }
    public string? DescriptionHtml { get; set; }
    
    public int SortOrder { get; set; }
    
    // Physical attributes
    public int? AreaSquareMeters { get; set; }      // 30
    public bool? HasBalcony { get; set; }
    public bool? HasWindow { get; set; }
    public bool? SmokingAllowed { get; set; }
    
    // Capacity
    public int DefaultAdults { get; set; }          // 2
    public int DefaultChildren { get; set; }        // 0
    public int MaxAdults { get; set; }              // 2
    public int MaxChildren { get; set; }            // 1
    public int MaxGuests { get; set; }              // 3
    
    // Inventory
    public int TotalUnits { get; set; }             // 50 phòng loại này
    
    // Media
    public Guid? CoverMediaAssetId { get; set; }
    public string? CoverImageUrl { get; set; }
    
    public RoomTypeStatus Status { get; set; }      // Active | Draft | Inactive
    public bool IsActive { get; set; }
}
```

#### **BedType & RoomTypeBed (Loại giường)**
```csharp
public sealed class BedType
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    
    public string Code { get; set; }                // SINGLE, QUEEN, TWIN, KING, BUNK...
    public string Name { get; set; }                // Single Bed, Queen Bed, Twin Beds
    public int Width { get; set; }                  // cm (90 cho single)
    public int Length { get; set; }                 // cm (200)
    
    public int SortOrder { get; set; }
    public bool IsActive { get; set; }
}

public sealed class RoomTypeBed
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    
    public Guid RoomTypeId { get; set; }            // FK
    public Guid BedTypeId { get; set; }             // FK
    
    public int Quantity { get; set; }               // Deluxe: 1x QUEEN bed
    public int SortOrder { get; set; }
}

// Ví dụ:
// RoomType DELUXE: 1x QUEEN + 0x SOFA
// RoomType TWIN: 2x SINGLE
// RoomType FAMILY: 1x QUEEN + 2x SINGLE
```

#### **RoomTypeOccupancyRule (Rule khách)**
```csharp
public sealed class RoomTypeOccupancyRule
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    
    public Guid RoomTypeId { get; set; }            // FK
    
    public int Adults { get; set; }                 // 2
    public int Children { get; set; }               // 0
    public int Infants { get; set; }                // 0
    
    public bool IsDefault { get; set; }             // default configuration
    public decimal Price { get; set; }              // price for this configuration
    
    // Ví dụ:
    // Rule 1 (default): 2 adults + 0 children → 800,000 VND (default)
    // Rule 2: 1 adult + 1 child → 800,000 VND (tính child như adult)
    // Rule 3: 1 adult + 2 children → 900,000 VND (extra child surcharge)
}
```

#### **MealPlan & ExtraService (Bao gồm dịch vụ)**
```csharp
public sealed class MealPlan
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    
    public Guid HotelId { get; set; }               // FK
    
    public string Code { get; set; }                // BB, HB, FB, AI (All-inclusive)
    public string Name { get; set; }                // Bed & Breakfast, Half Board, Full Board
    
    public bool IncludesBreakfast { get; set; }     // true
    public bool IncludesLunch { get; set; }         // false (HB)
    public bool IncludesDinner { get; set; }        // false (HB)
    public bool IncludesSnacks { get; set; }
    
    public decimal Price { get; set; }              // 200,000 (per night)
    public PricingUnit PricingUnit { get; set; }    // PerNight | PerStay | PerGuest...
}

public sealed class ExtraService
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    
    public Guid HotelId { get; set; }               // FK
    
    public string Code { get; set; }                // EXTRA_BED, LATE_CHECKOUT, EARLY_CHECKIN...
    public string Name { get; set; }                // Extra Bed, Late Checkout
    public ExtraServiceType Type { get; set; }
    
    public decimal Price { get; set; }              // 300,000
    public PricingUnit PricingUnit { get; set; }    // PerNight | PerStay
}

public enum PricingUnit
{
    PerNight = 1,
    PerStay = 2,
    PerGuest = 3,
    PerRoom = 4,
    PerUnit = 99
}
```

#### **CancellationPolicy & PenaltyRule (Chính sách hủy)**
```csharp
public sealed class CancellationPolicy
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    
    public Guid HotelId { get; set; }               // FK
    public Guid? RatePlanId { get; set; }           // Optional: specific rate plan
    
    public string Code { get; set; }                // STRICT, MODERATE, FLEXIBLE
    public string Name { get; set; }                // Strict, Moderate, Flexible
    public CancellationPolicyType Type { get; set; }  // FreeCancellation | NonRefundable | Custom
    
    public string? Description { get; set; }
    
    public bool IsActive { get; set; }
    public int SortOrder { get; set; }
}

public sealed class CancellationPolicyPenaltyRule
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    
    public Guid CancellationPolicyId { get; set; }  // FK
    
    public int DaysBeforeArrival { get; set; }      // 7: cancel 7 days before arrival
    public string CurrencyCode { get; set; }        // VND
    
    public PenaltyChargeType ChargeType { get; set; } // PercentOfNight | PercentOfTotal | FixedAmount | NightCount
    public decimal ChargeValue { get; set; }        // Percent or Amount
    
    // Ví dụ:
    // Rule 1: Cancel 7+ days → 0% (free)
    // Rule 2: Cancel 3-7 days → 50% of 1 night
    // Rule 3: Cancel <3 days → 100% of total stay
}

public enum CancellationPolicyType
{
    FreeCancellation = 1,   // Hoàn 100%
    NonRefundable = 2,      // Không hoàn
    Custom = 3              // Custom rules
}

public enum PenaltyChargeType
{
    PercentOfNight = 1,     // % của 1 night
    PercentOfTotal = 2,     // % của total
    FixedAmount = 3,        // Fixed amount
    NightCount = 4          // N nights charge
}
```

#### **RoomInventory & RoomHold (Quản lý phòng)**
```csharp
// Không có entity riêng; tính toán từ RoomHold status

public sealed class RoomHold
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    
    public Guid RoomTypeId { get; set; }            // FK
    
    public DateOnly CheckInDate { get; set; }       // 2026-04-03
    public DateOnly CheckOutDate { get; set; }      // 2026-04-05 (2 nights)
    
    public int RoomCount { get; set; }              // giữ bao nhiêu phòng
    
    public HoldStatus Status { get; set; }          // Held | Confirmed | Cancelled | Expired
    public string HoldToken { get; set; }
    
    public DateTimeOffset HoldExpiresAt { get; set; }
    
    public DateTimeOffset CreatedAt { get; set; }
}

public enum HoldStatus
{
    Held = 1,
    Confirmed = 2,
    Cancelled = 3,
    Expired = 4
}

// Calculation:
// Available = TotalUnits - ConfirmedCount - ActiveHeldCount
// ConfirmedCount = sum(RoomHold where Status=Confirmed AND overlap)
// ActiveHeldCount = sum(RoomHold where Status=Held AND HoldExpiresAt > now AND overlap)
```

### **4.3 Hotel Booking Workflow**

```
Search Hotels →
  View Details (Room Types, Amenities, Cancellation Policy) →
  Select Room Type (Deluxe, Suite) →
  Select Check-in/out dates & Guests →
  Select Meal Plan, Extra Services →
  Calculate Price (RoomPrice + MealPlan + ExtraService) →
  Hold Rooms →
  Confirm Booking
```

---

## **PHẦN 5: MODULE TOUR (Du lịch)**

### **5.1 Tour Entity Hierarchy**

```
┌─ Tour (Hạ Long 3D2N, Sapa Trek...)
│  ├─ TourType: Domestic | International | Daily | Combo | Charter
│  ├─ TourStatus: Draft | Active | Inactive | Hidden | Archived
│  ├─ DurationDays, DurationNights
│  ├─ MinGuests, MaxGuests
│  ├─ CurrencyCode
│  ├─ IsInstantConfirm, IsPrivateTourSupported
│  │
│  ├─ TourImage[], TourContact[], TourPolicy[], TourFaq[]
│  ├─ TourReview[]
│  │
│  ├─ TourItineraryDay[] (Day 1, Day 2, ...)
│  │  └─ TourItineraryItem[] (activities, meals, transport)
│  │
│  ├─ TourSchedule[] (các ngày khởi hành)
│  │  ├─ DepartureDate, ReturnDate
│  │  ├─ BookingOpenAt, BookingCutoffAt
│  │  ├─ TourScheduleStatus: Draft | Open | Closed | Full | OnRequest
│  │  ├─ IsGuaranteedDeparture, IsInstantConfirm, IsFeatured
│  │  ├─ MinGuestsToOperate, MaxGuests
│  │  │
│  │  ├─ TourSchedulePrice[] (giá theo adult/child/infant)
│  │  ├─ TourScheduleCapacity[] (tracking capacity available)
│  │  ├─ TourScheduleAddonPrice[] (addon services)
│  │  │
│  │  └─ TourPackageScheduleOptionOverride[] (override cho schedule này)
│  │
│  ├─ TourPackage[] (VIP, Standard, Budget packages)
│  │  ├─ TourPackageMode: Fixed | Configurable | Dynamic
│  │  ├─ IsDefault, AutoRepriceBeforeConfirm
│  │  ├─ HoldStrategy: AllOrNothing | BestEffort | None
│  │  │
│  │  └─ TourPackageComponent[] (Flight + Hotel + Activity)
│  │     ├─ ComponentType: OutboundTransport | ReturnTransport | Accommodation | Transfer | Activity | Meal | Guide...
│  │     ├─ SelectionMode: RequiredSingle | RequiredMulti | OptionalSingle | OptionalMulti
│  │     │
│  │     └─ TourPackageComponentOption[] (Option 1: VN123 Flight, Option 2: VN456 Flight)
│  │        ├─ SourceType: Flight | Bus | Train | Hotel | Activity | ...
│  │        ├─ SourceId: guid (Flight offer ID, Hotel room type ID, ...)
│  │        ├─ Status: Active | Inactive | Expired
│  │        ├─ Price (override từ source)
│  │
│  ├─ TourPackageReservation (Giữ chỗ - Step 1)
│  │  ├─ Status: Pending | Held | PartiallyHeld | Released | Expired | Failed | Confirmed
│  │  ├─ HoldToken
│  │  ├─ HoldExpiresAt
│  │  ├─ HoldStrategy
│  │  │
│  │  └─ TourPackageReservationItem[] (mỗi component option)
│  │
│  ├─ TourPackageBooking (Xác nhận đặt - Step 2)
│  │  ├─ Status: Pending | Confirmed | PartiallyConfirmed | Cancelled | Failed | PartiallyCancelled
│  │  ├─ SourceReservationId (ref)
│  │  ├─ ConfirmedAt
│  │  │
│  │  ├─ TourPackageBookingItem[] (mỗi component option)
│  │  │  └─ SourceConfirmationId, SourceStatus
│  │  │
│  │  ├─ TourPackageReschedule[] (tạo từ booking này)
│  │  ├─ TourPackageCancellation[] (hủy từ booking này)
│  │  └─ TourPackageRefund[] (hoàn tiền)
│  │
│  ├─ TourPackageReschedule (Đổi lịch)
│  │  ├─ Status: Requested | Held | Confirming | Completed | Released | Failed | AttentionRequired
│  │  ├─ SourceBooking, TargetSchedule/Package
│  │  ├─ PriceDifferenceAmount (nếu target đắt hơn)
│  │  ├─ HoldExpiresAt, ConfirmedAt
│  │
│  ├─ TourPackageCancellation (Hủy đặt)
│  │  ├─ Status: Requested | Approved | Rejected | Completed
│  │  ├─ RequestedByUserId
│  │  ├─ RefundAmount (tính từ policy)
│  │  ├─ PenaltyAmount
│  │  │
│  │  └─ TourPackageCancellationItem[] (hủy từng component)
│  │
│  └─ TourPackageAuditEvent[] (audit trail)
│     ├─ EventType: ReservationCreated, ReservationExpired, BookingConfirmed, BookingPartiallyConfirmed, CancellationApproved...
│     ├─ Title, Description
│     ├─ Actor (user)
│     └─ Timestamp
```

### **5.2 Tour Workflow - Detailed Steps**

#### **Step 1: Browse Tours & Select Schedule**

```
GET /api/v1/tours?type=Domestic&city=Hà%20Nội&durationDays=3
  Filter, Sort, Paginate

GET /api/v1/tours/{tourId}
  Show detail: itinerary (Day 1, 2, 3), amenities, policy
  Show schedules: [
    {
      "scheduleId": "...",
      "departureDate": "2026-04-10",
      "returnDate": "2026-04-13",
      "status": "Open",
      "availableSlots": 15,
      "minimumGuests": 2,
      "pricePerAdult": 1500000,
      "pricePerChild": 900000
    },
    ...
  ]

User selects: Schedule (2026-04-10 departure)
```

#### **Step 2: Select Package & Components**

```
GET /api/v1/tours/{tourId}/schedules/{scheduleId}/packages
  VIP Package:
    - Component 1 (OutboundTransport):
      Option A: VN123 Flight SGN→HAN (05:00-08:00) - 1,500,000
      Option B: VN456 Flight SGN→HAN (06:00-09:00) - 1,200,000
    
    - Component 2 (Accommodation, 3 nights):
      Option A: 5-star Hotel Hilton - 600,000/night
      Option B: 4-star Hotel Sofitel - 400,000/night
    
    - Component 3 (Activities):
      Option A: Premium Guide + Transport - 300,000
      Option B: Standard Guide - 200,000
    
    - Component 4 (ReturnTransport):
      Option A: VN789 Flight HAN→SGN - 1,200,000
      ...

User selects:
  - Package: VIP
  - Component 1: Option A (VN123)
  - Component 2: Option B (Sofitel) 
  - Component 3: Option A (Premium)
  - Component 4: Option A (VN789)

Total pre-calculated price:
  = 1,500,000 + (400,000 * 3) + 300,000 + 1,200,000 = 4,200,000
```

#### **Step 3: Enter Passenger Details**

```
Input:
  - Passengers: 2
    Passenger 1: Nguyễn Văn A (Adult, DOB: 1990-01-01)
    Passenger 2: Nguyễn Thị B (Child, DOB: 2015-01-01, 9 years old)
  
  - Contact: +84-xxx-xxx-xxx, email@...

Validate: 
  - RequestedPax (2) >= Tour.MinGuests
  - ApplyChildPrice where applicable
```

#### **Step 4: Review & Confirm Terms**

```
Display:
  - Selected options + prices
  - Cancellation policy (e.g., "Cancel 7+ days: Free, <7 days: 50% penalty")
  - Terms & conditions
  
User: Confirm booking
```

#### **Step 5: Call TourPackageBookingService.ConfirmAsync()**

```csharp
// File: TourPackageBookingService.cs

public async Task<TourPackageBookingConfirmServiceResult> ConfirmAsync(
    Guid tourId,
    TourPackageBookingConfirmRequest request,  // contains reservationId
    Guid? userId,
    bool isAdmin,
    CancellationToken ct = default)
{
    // 1. Load Reservation (Held state)
    var reservation = await LoadReservationAsync(...);
    
    if (!IsReservationConfirmable(reservation))
        throw new InvalidOperationException("Cannot confirm");
    
    // 2. Validate expiry
    if (reservation.HoldExpiresAt <= now)
    {
        await ExpireReservationAsync(reservation, ...);
        throw new InvalidOperationException("Hold expired");
    }
    
    // 3. Load Tour, Schedule, Package
    var tour = await _db.Tours.FirstOrDefaultAsync(...);
    var schedule = await _db.TourSchedules.FirstOrDefaultAsync(...);
    var package = await _db.TourPackages
        .Include(x => x.Components)
        .ThenInclude(x => x.Options)
        .FirstOrDefaultAsync(...);
    
    // 4. Create Booking entity
    var booking = CreateBookingEntity(reservation, request, userId, currentTime);
    
    // 5. Update each ReservationItem → BookingItem + call adapters
    using var tx = await _db.Database.BeginTransactionAsync();
    
    _db.TourPackageBookings.Add(booking);
    await _db.SaveChangesAsync();
    
    var outcomes = new List<TourPackageSourceBookingConfirmResult>();
    
    foreach (var reservationItem in reservation.Items)
    {
        var bookingItem = booking.Items[...]  // matching item
        var component = package.Components[...]
        var option = component.Options[...]
        
        // 6. Call adapter (FlightAdapter, HotelAdapter, etc.)
        var outcome = await ConfirmBookingItemAsync(
            tour, schedule, package,
            reservation, reservationItem,
            booking, bookingItem,
            component, option,
            userId, isAdmin, ct);
        
        // 7. Apply outcome (success/fail)
        ApplyOutcomeToBookingItem(bookingItem, outcome);
        outcomes.Add(outcome);
    }
    
    // 8. Calculate final status
    booking.Status = TourPackageBookingSupport
        .CalculateBookingStatus(outcomes, package.HoldStrategy);
    
    await _db.SaveChangesAsync();
    await tx.CommitAsync();
    
    // 9. Return result
    return new TourPackageBookingConfirmServiceResult
    {
        Booking = MapBooking(booking),
        Status = booking.Status,  // Confirmed | PartiallyConfirmed | Failed
        OutcomeDetails = outcomes
    };
}

// ConfirmBookingItemAsync details:
private async Task<TourPackageSourceBookingConfirmResult> ConfirmBookingItemAsync(...)
{
    // Based on component.SourceType (Flight, Hotel, ...)
    // Call corresponding adapter
    
    if (component.SourceType == TourPackageSourceType.Flight)
    {
        var adapter = _bookingAdapters.OfType<FlightTourPackageSourceBookingAdapter>().First();
        return await adapter.ConfirmAsync(request, userId, isAdmin, ct);
    }
    else if (component.SourceType == TourPackageSourceType.Hotel)
    {
        var adapter = _bookingAdapters.OfType<HotelTourPackageSourceBookingAdapter>().First();
        return await adapter.ConfirmAsync(request, userId, isAdmin, ct);
    }
    else if (component.SourceType == TourPackageSourceType.Bus)
    {
        var adapter = _bookingAdapters.OfType<BusTourPackageSourceBookingAdapter>().First();
        return await adapter.ConfirmAsync(request, userId, isAdmin, ct);
    }
    // ...
}
```

#### **Step 6: Payment Processing**

```
Adapter confirms in third-party system:
  - Flight: Hold seat + generate ticket
  - Hotel: Hold room + send confirmation email
  - Bus: Hold seat + send details
  - Activity: Register participant

If AllOrNothing strategy:
  - All succeed → Booking.Status = Confirmed
  - Any fail → Rollback all → Status = Failed

If BestEffort strategy:
  - Some succeed → Status = PartiallyConfirmed
  - Rest are refunded
```

#### **Step 7: Booking Confirmation Email**

```
Subject: Tour Booking Confirmed - TOURXYZ001ABC

Dear Customer,

Your tour has been successfully booked!

Tour: Hạ Long 3D2N
Dates: 2026-04-10 to 2026-04-13
Passengers: Nguyễn Văn A, Nguyễn Thị B
Booking Reference: TOURXYZ001ABC

Included:
- Outbound Flight: VN123 (05:00-08:00)
- Hotel: Sofitel (3 nights)
- Guide & Activities
- Return Flight: VN789 (14:00-17:00)

Total: 4,200,000 VND

Cancellation Policy: Free cancellation 7+ days before departure

...
```

### **5.3 Tour Reschedule Workflow**

```
User wants to reschedule from 2026-04-10 → 2026-04-24

Input:
  POST /api/v1/tours/{tourId}/bookings/{bookingId}/reschedule
  {
    "targetScheduleId": "...",  // new schedule
    "targetPackageId": "...",   // same package
    "reasonCode": "EARLIER_DEPARTURE",
    "reasonText": "Want to leave earlier"
  }

Process:
  1. Load source Booking (Status=Confirmed)
  2. Load target Schedule & Package (must exist)
  3. Create TourPackageReschedule entity:
     - Status: Requested
     - SourceBooking* = this booking
     - TargetSchedule* = new schedule
  
  4. Calculate price difference:
     oldPrice = SourceBooking.PackageSubtotalAmount
     newPrice = CalculatePackagePrice(TargetSchedule, TargetPackage)
     diff = newPrice - oldPrice
     → if diff > 0: Customer must pay
     → if diff < 0: Refund to customer
  
  5. If PaymentRequired:
     - Show payment dialog
     - Process payment
  
  6. Upon payment:
     - Cancel source holds (Flight, Hotel, Bus...)
     - Create new holds for target schedule
     - Mark TourPackageReschedule.Status = Confirming
     - Call confirm adapters
     - Status = Completed
```

### **5.4 Tour Cancellation & Refund Workflow**

```
User cancels booking

Input:
  POST /api/v1/tours/{tourId}/bookings/{bookingId}/cancel
  {
    "reasonCode": "SCHEDULE_CONFLICT",
    "reasonText": "Personal conflict arose"
  }

Process:
  1. Load Booking (Status=Confirmed or PartiallyConfirmed)
  
  2. Load CancellationPolicy:
     - Get applicable policy for this booking
  
  3. Evaluate refund:
     - Days before departure = departureDate - today
     - Apply policy rules:
       if days >= 7: refund 100%
       if 3 <= days < 7: refund 50% (penalty 50%)
       if days < 3: refund 0% (full penalty)
  
  4. Create TourPackageCancellation:
     - Status: Approved (if auto-approval) or Requested (if manual)
     - RefundAmount: 2,100,000 (50% of 4,200,000)
     - PenaltyAmount: 2,100,000
  
  5. Create TourPackageCancellationItem[] (per component):
     - Cancel Flight hold
     - Cancel Hotel hold
     - Cancel Bus hold
     - Etc.
  
  6. Call CancellationAdapter for each:
     - FlightCancellationAdapter.CancelAsync()
     - HotelCancellationAdapter.CancelAsync()
     - BusCancellationAdapter.CancelAsync()
  
  7. Create TourPackageRefund:
     - Amount: 2,100,000
     - RefundProvider: SePay (default)
     - Status: Requested
  
  8. Initiate refund:
     - Call SePay API: RefundAsync(amount, ...)
     - Track refund via TourPackageRefundAttempt
  
  9. Upon completion:
     - TourPackageRefund.Status = Completed
     - Booking.Status = Cancelled
     - Send email to customer
```

### **5.5 Tour Audit & Event Tracking**

```csharp
// TourPackageAuditEvent entity tracks all changes

public sealed class TourPackageAuditEvent
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    
    public Guid TourId { get; set; }
    public Guid? TourScheduleId { get; set; }
    public Guid? TourPackageId { get; set; }
    public Guid? TourPackageReservationId { get; set; }
    public Guid? TourPackageBookingId { get; set; }
    public Guid? TourPackageBookingItemId { get; set; }
    public Guid? TourPackageCancellationId { get; set; }
    public Guid? TourPackageRefundId { get; set; }
    public Guid? TourPackageRescheduleId { get; set; }
    
    public Guid? ActorUserId { get; set; }
    
    public string SourceType { get; set; }           // API, Admin, System, Webhook
    public string EventType { get; set; }            // ReservationCreated, BookingConfirmed, CancellationApproved, etc.
    public string Title { get; set; }                // Display title
    public string? Description { get; set; }         // Details
    
    public string? MetadataJson { get; set; }        // Extra data
    
    public DateTimeOffset OccurredAt { get; set; }
}

// Example events:
// 1. ReservationCreated: "User created tour package reservation for tour HXL001"
// 2. ReservationExpired: "Reservation expired (hold timeout)"
// 3. BookingConfirmed: "Booking confirmed, flight offer #XYZ confirmed"
// 4. BookingPartiallyConfirmed: "Hotel booking failed, flight confirmed"
// 5. BookingFailed: "Booking failed, all components failed"
// 6. CancellationApproved: "Cancellation approved, refund 2,100,000 VND"
// 7. RefundInitiated: "Refund initiated via SePay"
// 8. RefundCompleted: "Refund completed, amount received"
// 9. RescheduleRequested: "Customer requested rescheduling to new date"
// 10. RescheduleCompleted: "Rescheduling completed successfully"
```

---

## **PHẦN 6: CROSS-MODULE INTERACTIONS**

### **6.1 Adapter Pattern (khôi phục)**

```
Tours module không đơn giản: cần tích hợp Flight, Bus, Train, Hotel...

Solution: Adapter pattern

ITourPackageSourceReservationAdapter
├── FlightTourPackageReservationAdapter
├── HotelTourPackageReservationAdapter
├── BusTourPackageReservationAdapter
├── TrainTourPackageReservationAdapter
└── ActivityTourPackageReservationAdapter

ITourPackageSourceBookingAdapter (tương tự)
ITourPackageSourceCancellationAdapter (tương tự)

Benefit:
- Tours service không phải biết Flight logic
- Each adapter self-contained
- Easy to add new source (e.g., Activity partner)
```

### **6.2 Multi-tenant Orchestration**

```
Booking flow:
1. Customer creates reservation (Tenant=TOUR001)
2. Adapter calls Flight service (Tenant=VMM001)
   - But customer sees from TOUR001 perspective
   - Adapter resolves tenant context switching

TenantContext switching:
  originalTenant = ITenantContext.TenantId  // TOUR001
  try {
    ITenantContext.SetTenant(flightTenantId)  // VMM001
    // call flight service
    var result = await flightAdapter.HoldAsync(...)
  } finally {
    ITenantContext.SetTenant(originalTenant)  // back to TOUR001
  }
```

### **6.3 Soft Delete & Audit Pattern**

```
All entities have:
- IsDeleted (logical delete)
- CreatedAt, UpdatedAt (timestamps)
- CreatedByUserId, UpdatedByUserId (who made changes)
- RowVersion (optimistic concurrency)

Interceptor (AuditTenantSoftDeleteInterceptor):
- Automatically sets TenantId on insert
- Automatically sets CreatedAt, UpdatedAt
- Marks IsDeleted=true on delete (not physical delete)
- Global query filters exclude soft-deleted items

Benefit: Full audit trail, easy restores, multi-tenant safety
```

---

## **PHẦN 7: TRANSACTION & CONCURRENCY**

### **7.1 Hold Inventory Transactions**

```sql
-- Flight Offer seat hold (optimistic locking)
UPDATE FlightOffers 
SET SeatsAvailable = SeatsAvailable - @qty,
    UpdatedAt = @now,
    RowVersion = CONCAT(RowVersion, '')  -- EF updates RowVersion
WHERE Id = @offerId
  AND RowVersion = @expectedVersion

If @@ROWCOUNT = 0, throw ConcurrencyException

-- Bus/Train seat hold (explicit transaction)
BEGIN TRANSACTION
  SELECT SUM(case when Status IN (Held, Confirmed) then 1 else 0 end)
    FROM TripSeatHolds
   WHERE TripId = @tripId AND FromStopIndex <= @fromIdx AND ToStopIndex > @fromIdx
    FOR UPDATE  -- pessimistic lock

  INSERT INTO TripSeatHolds (Id, Status, HoldExpiresAt, ...)
  VALUES (...)

COMMIT
```

### **7.2 Booking Confirmation (All-or-Nothing)**

```csharp
// Tour booking with HoldStrategy = AllOrNothing

using var tx = await _db.Database.BeginTransactionAsync();

try
{
    // Confirm Flight
    var flightOutcome = await flightAdapter.ConfirmAsync(...);
    if (flightOutcome.Status == Failed)
        throw new Exception("Flight confirmation failed");
    
    // Confirm Hotel
    var hotelOutcome = await hotelAdapter.ConfirmAsync(...);
    if (hotelOutcome.Status == Failed)
        throw new Exception("Hotel confirmation failed");
    
    // Confirm Activity
    var activityOutcome = await activityAdapter.ConfirmAsync(...);
    if (activityOutcome.Status == Failed)
        throw new Exception("Activity confirmation failed");
    
    // All succeeded, mark booking as Confirmed
    booking.Status = TourPackageBookingStatus.Confirmed;
    await _db.SaveChangesAsync();
    
    await tx.CommitAsync();
}
catch
{
    // Any failure → rollback all
    // Release holds
    await flightAdapter.ReleaseAsync(...);
    await hotelAdapter.ReleaseAsync(...);
    await activityAdapter.ReleaseAsync(...);
    
    await tx.RollbackAsync();
    
    booking.Status = TourPackageBookingStatus.Failed;
    await _db.SaveChangesAsync();
    
    throw;
}
```

---

## **KẾT LUẬN**

Đây là hệ thống **Omnichannel Travel Booking** với:

1. **5 Transport/Accommodation Modules** (Flight, Bus, Train, Hotel, Tour)
2. **Unified Booking Orchestra** (Tours coordinating all sources)
3. **Multi-tenant SaaS** (Isolated data, shared infrastructure)
4. **Flexible Inventory** (Soft holds, expiry, capacity management)
5. **Full Audit Trail** (Every action logged, refund tracking)
6. **Adapter Pattern** (Pluggable sources, easy extensibility)

Complexity increases: Flight (simple) → Bus/Train (segments) → Hotel (occupancy) → Tour (composite, multi-source).

