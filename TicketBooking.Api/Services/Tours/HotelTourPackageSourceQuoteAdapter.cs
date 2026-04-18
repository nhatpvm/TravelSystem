using Microsoft.EntityFrameworkCore;
using TicketBooking.Domain.Hotels;
using TicketBooking.Domain.Tours;
using TicketBooking.Infrastructure.Persistence;

namespace TicketBooking.Api.Services.Tours;

public sealed class HotelTourPackageSourceQuoteAdapter : ITourPackageSourceQuoteAdapter
{
    private readonly AppDbContext _db;

    public HotelTourPackageSourceQuoteAdapter(AppDbContext db)
    {
        _db = db;
    }

    public bool CanHandle(TourPackageSourceType sourceType)
        => sourceType == TourPackageSourceType.Hotel;

    public async Task<TourPackageSourceQuoteResult> ResolveAsync(
        TourPackageSourceQuoteAdapterRequest request,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        return request.Option.BindingMode switch
        {
            TourPackageBindingMode.StaticReference => await ResolveStaticAsync(request, ct),
            TourPackageBindingMode.SearchTemplate => await ResolveFromSearchTemplateAsync(request, ct),
            _ => TourPackageSourceQuoteResultFactory.Unavailable(request, "Hotel source does not support this binding mode.")
        };
    }

    private async Task<TourPackageSourceQuoteResult> ResolveStaticAsync(
        TourPackageSourceQuoteAdapterRequest request,
        CancellationToken ct)
    {
        if (request.TotalNights <= 0)
            return TourPackageSourceQuoteResultFactory.Unavailable(request, "Hotel source requires at least 1 night.");

        if (request.RequestedQuantity <= 0)
            return TourPackageSourceQuoteResultFactory.Unavailable(request, "Hotel source requires a quantity greater than 0.");

        var mapping = await _db.RatePlanRoomTypes
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.Id == request.SourceEntityId &&
                x.IsActive &&
                !x.IsDeleted, ct);

        if (mapping is null)
            return TourPackageSourceQuoteResultFactory.Unavailable(request, "Hotel rate source was not found.");

        var roomType = await _db.RoomTypes
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.Id == mapping.RoomTypeId &&
                x.IsActive &&
                !x.IsDeleted &&
                x.Status == RoomTypeStatus.Active, ct);

        if (roomType is null)
            return TourPackageSourceQuoteResultFactory.Unavailable(request, "Hotel room type source is not available.");

        var ratePlan = await _db.RatePlans
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.Id == mapping.RatePlanId &&
                x.IsActive &&
                !x.IsDeleted &&
                x.Status == RatePlanStatus.Active, ct);

        if (ratePlan is null)
            return TourPackageSourceQuoteResultFactory.Unavailable(request, "Hotel rate plan source is not available.");

        var hotel = await _db.Hotels
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.Id == roomType.HotelId &&
                x.IsActive &&
                !x.IsDeleted &&
                x.Status == HotelStatus.Active, ct);

        if (hotel is null)
            return TourPackageSourceQuoteResultFactory.Unavailable(request, "Hotel property source is not available.");

        var checkInDate = request.Schedule.DepartureDate;
        var checkOutDate = checkInDate.AddDays(request.TotalNights);
        var stayDates = EachNight(checkInDate, checkOutDate).ToList();
        var nightCount = stayDates.Count;

        var hotelLocalDate = GetHotelLocalDate(hotel.TimeZone);
        if (checkInDate < hotelLocalDate)
            return TourPackageSourceQuoteResultFactory.Unavailable(request, "Hotel source cannot be quoted for a past check-in date.");

        var advanceDays = checkInDate.DayNumber - hotelLocalDate.DayNumber;
        if (ratePlan.MinNights.HasValue && nightCount < ratePlan.MinNights.Value)
        {
            return TourPackageSourceQuoteResultFactory.Unavailable(
                request,
                $"Hotel source requires at least {ratePlan.MinNights.Value} night(s).");
        }

        if (ratePlan.MaxNights.HasValue && nightCount > ratePlan.MaxNights.Value)
        {
            return TourPackageSourceQuoteResultFactory.Unavailable(
                request,
                $"Hotel source allows at most {ratePlan.MaxNights.Value} night(s).");
        }

        if (ratePlan.MinAdvanceDays.HasValue && advanceDays < ratePlan.MinAdvanceDays.Value)
        {
            return TourPackageSourceQuoteResultFactory.Unavailable(
                request,
                $"Hotel source requires booking at least {ratePlan.MinAdvanceDays.Value} day(s) in advance.");
        }

        if (ratePlan.MaxAdvanceDays.HasValue && advanceDays > ratePlan.MaxAdvanceDays.Value)
        {
            return TourPackageSourceQuoteResultFactory.Unavailable(
                request,
                $"Hotel source can only be booked within {ratePlan.MaxAdvanceDays.Value} day(s) of arrival.");
        }

        var inventories = await _db.RoomTypeInventories
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(x =>
                x.RoomTypeId == roomType.Id &&
                !x.IsDeleted &&
                x.Status == InventoryStatus.Open &&
                stayDates.Contains(x.Date))
            .ToListAsync(ct);

        if (inventories.Count != nightCount)
            return TourPackageSourceQuoteResultFactory.Unavailable(request, "Hotel source does not have inventory for the full stay.");

        if (inventories.Any(x => x.MinNights.HasValue && nightCount < x.MinNights.Value))
            return TourPackageSourceQuoteResultFactory.Unavailable(request, "Hotel source does not allow such a short stay.");

        if (inventories.Any(x => x.MaxNights.HasValue && nightCount > x.MaxNights.Value))
            return TourPackageSourceQuoteResultFactory.Unavailable(request, "Hotel source does not allow such a long stay.");

        if (inventories.Any(x => (x.TotalUnits - x.SoldUnits - x.HeldUnits) < request.RequestedQuantity))
        {
            return TourPackageSourceQuoteResultFactory.Unavailable(
                request,
                $"Hotel source only has limited availability for {request.RequestedQuantity} unit(s).");
        }

        var dailyRates = await _db.DailyRates
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(x =>
                x.RatePlanRoomTypeId == mapping.Id &&
                !x.IsDeleted &&
                x.IsActive &&
                stayDates.Contains(x.Date))
            .ToListAsync(ct);

        decimal totalPrice = 0m;
        decimal totalBasePrice = 0m;
        decimal totalTaxes = 0m;
        decimal totalFees = 0m;
        string? currencyCode = null;

        foreach (var date in stayDates)
        {
            var daily = dailyRates.FirstOrDefault(x => x.Date == date);
            var nightlyPrice = daily?.Price ?? mapping.BasePrice;
            if (!nightlyPrice.HasValue)
            {
                return TourPackageSourceQuoteResultFactory.Unavailable(
                    request,
                    $"Hotel source does not have rate data for {date:yyyy-MM-dd}.");
            }

            var nightlyBasePrice = daily?.BasePrice ?? mapping.BasePrice ?? nightlyPrice.Value;
            var nightlyTaxes = daily?.Taxes ?? 0m;
            var nightlyFees = daily?.Fees ?? 0m;
            var nightlyCurrency = TourPackageQuoteSupport.NormalizeCurrency(
                daily?.CurrencyCode,
                mapping.CurrencyCode,
                request.Option.CurrencyCode,
                request.Package.CurrencyCode,
                request.Tour.CurrencyCode);

            if (currencyCode is not null &&
                !string.Equals(currencyCode, nightlyCurrency, StringComparison.OrdinalIgnoreCase))
            {
                return TourPackageSourceQuoteResultFactory.Unavailable(request, "Hotel source returned mixed currencies across the stay.");
            }

            currencyCode = nightlyCurrency;
            totalPrice += nightlyPrice.Value;
            totalBasePrice += nightlyBasePrice;
            totalTaxes += nightlyTaxes;
            totalFees += nightlyFees;
        }

        return new TourPackageSourceQuoteResult
        {
            OptionId = request.Option.Id,
            SourceType = request.Option.SourceType,
            WasResolved = true,
            IsAvailable = true,
            BoundSourceEntityId = mapping.Id,
            CurrencyCode = TourPackageQuoteSupport.NormalizeCurrency(
                request.ScheduleOverride?.CurrencyCode,
                currencyCode,
                request.Option.CurrencyCode,
                request.Package.CurrencyCode,
                request.Tour.CurrencyCode),
            UnitBasePrice = totalBasePrice,
            UnitTaxes = totalTaxes,
            UnitFees = totalFees,
            UnitTotalPrice = totalPrice,
            UnitCost = totalPrice,
            SourceCode = $"{hotel.Code}:{ratePlan.Code}:{roomType.Code}",
            SourceName = $"{hotel.Name} / {roomType.Name} / {ratePlan.Name}",
            Note = $"Live hotel rate for {nightCount} night(s).",
            SelectionReason = "Pinned/static hotel source.",
            ServiceStartAt = BuildStayDateTime(checkInDate),
            ServiceEndAt = BuildStayDateTime(checkOutDate),
            StopCount = 0,
            IsRefundable = ratePlan.Refundable,
            IncludesBreakfast = ratePlan.BreakfastIncluded
        };
    }

    private async Task<TourPackageSourceQuoteResult> ResolveFromSearchTemplateAsync(
        TourPackageSourceQuoteAdapterRequest request,
        CancellationToken ct)
    {
        TourPackageSearchTemplate template;
        try
        {
            template = TourPackageSearchTemplateSupport.ParseRequired(request.Option.SearchTemplateJson);
        }
        catch (ArgumentException ex)
        {
            return TourPackageSourceQuoteResultFactory.Unavailable(request, ex.Message);
        }

        var roomCount = TourPackageSearchTemplateSupport.ResolveRoomCount(request, template);
        if (roomCount <= 0)
            return TourPackageSourceQuoteResultFactory.Unavailable(request, "Hotel search template requires at least 1 room.");

        var nightCount = TourPackageSearchTemplateSupport.ResolveNightCount(request, template);
        if (nightCount <= 0)
            return TourPackageSourceQuoteResultFactory.Unavailable(request, "Hotel search template requires at least 1 night.");

        var checkInDate = TourPackageSearchTemplateSupport.ResolveServiceDate(request.Schedule, request.Component, template);
        var checkOutDate = checkInDate.AddDays(nightCount);
        var stayDates = EachNight(checkInDate, checkOutDate).ToList();

        var mappings = await _db.RatePlanRoomTypes
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(x =>
                !x.IsDeleted &&
                x.IsActive &&
                (!template.TenantId.HasValue || x.TenantId == template.TenantId.Value) &&
                (!template.RatePlanRoomTypeId.HasValue || x.Id == template.RatePlanRoomTypeId.Value) &&
                (!template.RoomTypeId.HasValue || x.RoomTypeId == template.RoomTypeId.Value) &&
                (!template.RatePlanId.HasValue || x.RatePlanId == template.RatePlanId.Value))
            .ToListAsync(ct);

        if (mappings.Count == 0)
            return TourPackageSourceQuoteResultFactory.Unavailable(request, "Hotel search template did not find any active rate mapping.");

        var roomTypeIds = mappings.Select(x => x.RoomTypeId).Distinct().ToList();
        var ratePlanIds = mappings.Select(x => x.RatePlanId).Distinct().ToList();

        var roomTypes = await _db.RoomTypes
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(x =>
                roomTypeIds.Contains(x.Id) &&
                !x.IsDeleted &&
                x.IsActive &&
                x.Status == RoomTypeStatus.Active &&
                (!template.TenantId.HasValue || x.TenantId == template.TenantId.Value) &&
                (!template.HotelId.HasValue || x.HotelId == template.HotelId.Value))
            .ToListAsync(ct);

        if (roomTypes.Count == 0)
            return TourPackageSourceQuoteResultFactory.Unavailable(request, "Hotel search template did not find any active room type.");

        var roomTypesById = roomTypes.ToDictionary(x => x.Id);
        mappings = mappings.Where(x => roomTypesById.ContainsKey(x.RoomTypeId)).ToList();

        var ratePlans = await _db.RatePlans
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(x =>
                ratePlanIds.Contains(x.Id) &&
                !x.IsDeleted &&
                x.IsActive &&
                x.Status == RatePlanStatus.Active &&
                (!template.TenantId.HasValue || x.TenantId == template.TenantId.Value) &&
                (!template.HotelId.HasValue || x.HotelId == template.HotelId.Value))
            .ToListAsync(ct);

        if (template.RefundableOnly == true)
            ratePlans = ratePlans.Where(x => x.Refundable).ToList();

        if (template.BreakfastIncluded == true)
            ratePlans = ratePlans.Where(x => x.BreakfastIncluded).ToList();

        if (ratePlans.Count == 0)
            return TourPackageSourceQuoteResultFactory.Unavailable(request, "Hotel search template did not find any active rate plan.");

        var ratePlansById = ratePlans.ToDictionary(x => x.Id);
        mappings = mappings.Where(x => ratePlansById.ContainsKey(x.RatePlanId)).ToList();

        if (mappings.Count == 0)
            return TourPackageSourceQuoteResultFactory.Unavailable(request, "Hotel search template did not find any matching room/rate combination.");

        var hotelIds = roomTypes.Select(x => x.HotelId).Distinct().ToList();
        var hotels = await _db.Hotels
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(x =>
                hotelIds.Contains(x.Id) &&
                !x.IsDeleted &&
                x.IsActive &&
                x.Status == HotelStatus.Active &&
                (!template.TenantId.HasValue || x.TenantId == template.TenantId.Value))
            .ToListAsync(ct);

        var hotelsById = hotels.ToDictionary(x => x.Id);
        mappings = mappings.Where(x => hotelsById.ContainsKey(roomTypesById[x.RoomTypeId].HotelId)).ToList();

        if (mappings.Count == 0)
            return TourPackageSourceQuoteResultFactory.Unavailable(request, "Hotel search template did not find any active hotel property.");

        var inventories = await _db.RoomTypeInventories
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(x =>
                roomTypeIds.Contains(x.RoomTypeId) &&
                !x.IsDeleted &&
                x.Status == InventoryStatus.Open &&
                stayDates.Contains(x.Date) &&
                (!template.TenantId.HasValue || x.TenantId == template.TenantId.Value))
            .ToListAsync(ct);

        var dailyRates = await _db.DailyRates
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(x =>
                mappings.Select(m => m.Id).Contains(x.RatePlanRoomTypeId) &&
                !x.IsDeleted &&
                x.IsActive &&
                stayDates.Contains(x.Date) &&
                (!template.TenantId.HasValue || x.TenantId == template.TenantId.Value))
            .ToListAsync(ct);

        var candidates = new List<HotelSearchCandidate>();

        foreach (var mapping in mappings)
        {
            var roomType = roomTypesById[mapping.RoomTypeId];
            var ratePlan = ratePlansById[mapping.RatePlanId];
            var hotel = hotelsById[roomType.HotelId];

            var hotelLocalDate = GetHotelLocalDate(hotel.TimeZone);
            if (checkInDate < hotelLocalDate)
                continue;

            var advanceDays = checkInDate.DayNumber - hotelLocalDate.DayNumber;
            if (ratePlan.MinNights.HasValue && nightCount < ratePlan.MinNights.Value)
                continue;
            if (ratePlan.MaxNights.HasValue && nightCount > ratePlan.MaxNights.Value)
                continue;
            if (ratePlan.MinAdvanceDays.HasValue && advanceDays < ratePlan.MinAdvanceDays.Value)
                continue;
            if (ratePlan.MaxAdvanceDays.HasValue && advanceDays > ratePlan.MaxAdvanceDays.Value)
                continue;

            var roomInventories = inventories
                .Where(x => x.RoomTypeId == roomType.Id)
                .ToList();

            if (roomInventories.Count != nightCount)
                continue;

            if (roomInventories.Any(x => x.MinNights.HasValue && nightCount < x.MinNights.Value))
                continue;

            if (roomInventories.Any(x => x.MaxNights.HasValue && nightCount > x.MaxNights.Value))
                continue;

            if (roomInventories.Any(x => (x.TotalUnits - x.SoldUnits - x.HeldUnits) < roomCount))
                continue;

            decimal totalPrice = 0m;
            decimal totalBasePrice = 0m;
            decimal totalTaxes = 0m;
            decimal totalFees = 0m;
            string? currencyCode = null;
            var invalid = false;

            foreach (var date in stayDates)
            {
                var daily = dailyRates.FirstOrDefault(x =>
                    x.RatePlanRoomTypeId == mapping.Id &&
                    x.Date == date);

                var nightlyPrice = daily?.Price ?? mapping.BasePrice;
                if (!nightlyPrice.HasValue)
                {
                    invalid = true;
                    break;
                }

                var nightlyBasePrice = daily?.BasePrice ?? mapping.BasePrice ?? nightlyPrice.Value;
                var nightlyTaxes = daily?.Taxes ?? 0m;
                var nightlyFees = daily?.Fees ?? 0m;
                var nightlyCurrency = TourPackageQuoteSupport.NormalizeCurrency(
                    daily?.CurrencyCode,
                    mapping.CurrencyCode,
                    request.Option.CurrencyCode,
                    request.Package.CurrencyCode,
                    request.Tour.CurrencyCode);

                if (currencyCode is not null &&
                    !string.Equals(currencyCode, nightlyCurrency, StringComparison.OrdinalIgnoreCase))
                {
                    invalid = true;
                    break;
                }

                currencyCode = nightlyCurrency;
                totalPrice += nightlyPrice.Value;
                totalBasePrice += nightlyBasePrice;
                totalTaxes += nightlyTaxes;
                totalFees += nightlyFees;
            }

            if (invalid || currencyCode is null)
                continue;

            candidates.Add(new HotelSearchCandidate
            {
                MappingId = mapping.Id,
                HotelName = hotel.Name,
                RoomTypeName = roomType.Name,
                RatePlanName = ratePlan.Name,
                CurrencyCode = currencyCode,
                TotalBasePrice = totalBasePrice,
                TotalTaxes = totalTaxes,
                TotalFees = totalFees,
                TotalPrice = totalPrice,
                CheckInDate = checkInDate,
                CheckOutDate = checkOutDate,
                IsRefundable = ratePlan.Refundable,
                IncludesBreakfast = ratePlan.BreakfastIncluded
            });
        }

        if (candidates.Count == 0)
            return TourPackageSourceQuoteResultFactory.Unavailable(request, "Hotel search template did not find any available room/rate combination.");

        var decision = TourPackageOptimizationSupport.SelectBestCandidate(
            candidates,
            template,
            candidate => new TourPackageOptimizationCandidateContext
            {
                Label = $"{candidate.HotelName} / {candidate.RoomTypeName} / {candidate.RatePlanName}",
                UnitPrice = candidate.TotalPrice,
                ServiceStartAt = BuildStayDateTime(candidate.CheckInDate),
                ServiceEndAt = BuildStayDateTime(candidate.CheckOutDate),
                StopCount = 0,
                IsRefundable = candidate.IsRefundable,
                IncludesBreakfast = candidate.IncludesBreakfast
            });

        var candidate = decision.Candidate;

        return new TourPackageSourceQuoteResult
        {
            OptionId = request.Option.Id,
            SourceType = request.Option.SourceType,
            WasResolved = true,
            IsAvailable = true,
            BoundSourceEntityId = candidate.MappingId,
            CurrencyCode = TourPackageQuoteSupport.NormalizeCurrency(
                request.ScheduleOverride?.CurrencyCode,
                candidate.CurrencyCode,
                request.Option.CurrencyCode,
                request.Package.CurrencyCode,
                request.Tour.CurrencyCode),
            UnitBasePrice = candidate.TotalBasePrice,
            UnitTaxes = candidate.TotalTaxes,
            UnitFees = candidate.TotalFees,
            UnitTotalPrice = candidate.TotalPrice,
            UnitCost = candidate.TotalPrice,
            SourceCode = $"HOTEL-{candidate.MappingId:N}",
            SourceName = $"{candidate.HotelName} / {candidate.RoomTypeName} / {candidate.RatePlanName}",
            Note = $"Resolved from hotel search template for {nightCount} night(s).",
            WasOptimizedSelection = true,
            OptimizationStrategy = decision.StrategyLabel,
            SelectionReason = decision.Reason,
            OptimizationScore = decision.Score,
            CandidateCount = decision.CandidateCount,
            ServiceStartAt = BuildStayDateTime(candidate.CheckInDate),
            ServiceEndAt = BuildStayDateTime(candidate.CheckOutDate),
            StopCount = 0,
            IsRefundable = candidate.IsRefundable,
            IncludesBreakfast = candidate.IncludesBreakfast
        };
    }

    private static IEnumerable<DateOnly> EachNight(DateOnly checkInDate, DateOnly checkOutDate)
    {
        for (var d = checkInDate; d < checkOutDate; d = d.AddDays(1))
            yield return d;
    }

    private static DateOnly GetHotelLocalDate(string? timeZoneId)
    {
        var timeZone = TourTimeZoneHelper.TryResolveTimeZone(timeZoneId);
        if (timeZone is null)
            return DateOnly.FromDateTime(DateTime.UtcNow);

        return DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(DateTime.UtcNow, timeZone));
    }

    private static DateTimeOffset BuildStayDateTime(DateOnly date)
        => new(date.Year, date.Month, date.Day, 12, 0, 0, TimeSpan.FromHours(7));

    private sealed class HotelSearchCandidate
    {
        public Guid MappingId { get; set; }
        public string HotelName { get; set; } = "";
        public string RoomTypeName { get; set; } = "";
        public string RatePlanName { get; set; } = "";
        public string CurrencyCode { get; set; } = "";
        public decimal TotalBasePrice { get; set; }
        public decimal TotalTaxes { get; set; }
        public decimal TotalFees { get; set; }
        public decimal TotalPrice { get; set; }
        public DateOnly CheckInDate { get; set; }
        public DateOnly CheckOutDate { get; set; }
        public bool IsRefundable { get; set; }
        public bool IncludesBreakfast { get; set; }
    }
}
