// FILE #264: TicketBooking.Api/Controllers/Tours/TourQuoteController.cs
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TicketBooking.Application.Services.Tours;
using TicketBooking.Api.Services.Tours;
using TicketBooking.Domain.Tours;
using TicketBooking.Infrastructure.Persistence;

namespace TicketBooking.Api.Controllers.Tours;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/tours/{tourId:guid}/quote")]
[AllowAnonymous]
public sealed class TourQuoteController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly TourBookabilityService _bookabilityService;
    private readonly TourQuoteBuilder _quoteBuilder;
    private readonly TourPackageQuoteBuilder _packageQuoteBuilder;
    private readonly TourPackageSourceQuoteResolver _packageSourceQuoteResolver;
    private readonly TourLocalTimeService _tourLocalTimeService;

    public TourQuoteController(
        AppDbContext db,
        TourBookabilityService bookabilityService,
        TourQuoteBuilder quoteBuilder,
        TourPackageQuoteBuilder packageQuoteBuilder,
        TourPackageSourceQuoteResolver packageSourceQuoteResolver,
        TourLocalTimeService tourLocalTimeService)
    {
        _db = db;
        _bookabilityService = bookabilityService;
        _quoteBuilder = quoteBuilder;
        _packageQuoteBuilder = packageQuoteBuilder;
        _packageSourceQuoteResolver = packageSourceQuoteResolver;
        _tourLocalTimeService = tourLocalTimeService;
    }

    [HttpPost]
    public async Task<ActionResult<TourQuoteResponse>> Quote(
        Guid tourId,
        [FromBody] TourQuoteRequest req,
        CancellationToken ct = default)
    {
        if (req is null)
            return BadRequest(new { message = "Request body is required." });

        if (req.ScheduleId == Guid.Empty)
            return BadRequest(new { message = "ScheduleId is required." });

        if (req.PaxGroups is null || req.PaxGroups.Count == 0)
            return BadRequest(new { message = "At least one PaxGroup is required." });

        if (req.PaxGroups.Any(x => x.Quantity <= 0))
            return BadRequest(new { message = "Each PaxGroup.Quantity must be greater than 0." });

        if (req.SelectedAddons is not null && req.SelectedAddons.Any(x => x.Quantity <= 0))
            return BadRequest(new { message = "Each SelectedAddon.Quantity must be greater than 0." });

        if (req.SelectedPackageOptions is not null &&
            req.SelectedPackageOptions.Any(x => x.OptionId == Guid.Empty))
        {
            return BadRequest(new { message = "Each SelectedPackageOption.OptionId is required." });
        }

        if (req.SelectedPackageOptions is not null &&
            req.SelectedPackageOptions.Any(x => x.Quantity.HasValue && x.Quantity.Value <= 0))
        {
            return BadRequest(new { message = "Each SelectedPackageOption.Quantity must be greater than 0 when provided." });
        }

        if (HasDuplicatePaxTypes(req.PaxGroups))
            return BadRequest(new { message = "Duplicate PriceType values are not allowed in PaxGroups." });

        if (req.SelectedAddons is not null && HasDuplicateAddonIds(req.SelectedAddons))
            return BadRequest(new { message = "Duplicate AddonId values are not allowed in SelectedAddons." });

        if (req.SelectedPackageOptions is not null && HasDuplicatePackageOptionIds(req.SelectedPackageOptions))
            return BadRequest(new { message = "Duplicate OptionId values are not allowed in SelectedPackageOptions." });

        var tour = await _db.Tours
            .AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.Id == tourId &&
                x.IsActive &&
                !x.IsDeleted &&
                x.Status == TourStatus.Active, ct);

        if (tour is null)
            return NotFound(new { message = "Tour not found." });

        var schedule = await _db.TourSchedules
            .AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.Id == req.ScheduleId &&
                x.TourId == tourId &&
                x.IsActive &&
                !x.IsDeleted, ct);

        if (schedule is null)
            return NotFound(new { message = "Tour schedule not found." });

        var totalPax = req.PaxGroups.Sum(x => x.Quantity);
        if (totalPax <= 0)
            return BadRequest(new { message = "Total pax must be greater than 0." });

        var capacity = await _db.TourScheduleCapacities
            .AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.TourScheduleId == req.ScheduleId &&
                x.IsActive &&
                !x.IsDeleted, ct);

        var currentTime = await _tourLocalTimeService.ResolveCurrentTimeAsync(tour.PrimaryLocationId, ct);

        var bookability = _bookabilityService.EvaluateSchedule(new TourBookabilityRequest
        {
            Schedule = schedule,
            Tour = tour,
            Capacity = capacity,
            RequestedPax = totalPax,
            Now = currentTime
        });

        if (!bookability.CanBook)
            return BadRequest(new { message = bookability.Reason });

        var requestedTypes = req.PaxGroups
            .Select(x => x.PriceType)
            .Distinct()
            .ToList();

        var prices = await _db.TourSchedulePrices
            .AsNoTracking()
            .Where(x =>
                x.TourScheduleId == req.ScheduleId &&
                x.IsActive &&
                !x.IsDeleted &&
                requestedTypes.Contains(x.PriceType))
            .ToListAsync(ct);

        var resolvedPassengerPrices = req.PaxGroups
            .Select(group => new
            {
                Group = group,
                Price = TourPricingResolver.ResolvePriceForQuantity(prices, group.PriceType, group.Quantity)
            })
            .ToList();

        var missingTypes = resolvedPassengerPrices
            .Where(x => x.Price is null)
            .Select(x => x.Group.PriceType)
            .Distinct()
            .ToList();

        if (missingTypes.Count > 0)
        {
            return BadRequest(new
            {
                message = $"Missing matching prices for passenger types: {string.Join(", ", missingTypes)}."
            });
        }

        var passengerInputs = resolvedPassengerPrices
            .Select(x =>
            {
                var price = x.Price!;
                var group = x.Group;

                return new TourQuoteBuildPassengerInput
                {
                    PriceType = group.PriceType,
                    DisplayName = group.PriceType.ToString(),
                    Quantity = group.Quantity,
                    CurrencyCode = price.CurrencyCode,
                    UnitPrice = price.Price,
                    UnitOriginalPrice = price.OriginalPrice,
                    UnitTaxes = price.Taxes,
                    UnitFees = price.Fees,
                    IsIncludedTax = price.IsIncludedTax,
                    IsIncludedFee = price.IsIncludedFee,
                    Label = price.Label
                };
            })
            .ToList();

        var resolvedCurrency = passengerInputs.FirstOrDefault()?.CurrencyCode ?? tour.CurrencyCode;

        var addonResolve = await ResolveAddonInputsAsync(
            tourId,
            req.ScheduleId,
            req,
            totalPax,
            resolvedCurrency,
            ct);

        if (!addonResolve.Ok)
            return BadRequest(new { message = addonResolve.ErrorMessage });

        var packageResolve = await ResolvePackageQuoteAsync(
            tour,
            schedule,
            req,
            totalPax,
            resolvedCurrency,
            ct);

        if (!packageResolve.Ok)
            return BadRequest(new { message = packageResolve.ErrorMessage });

        TourQuoteBuildResult buildResult;
        try
        {
            buildResult = _quoteBuilder.Build(new TourQuoteBuildRequest
            {
                TourId = tour.Id,
                TourName = tour.Name,
                TourCurrencyCode = tour.CurrencyCode,
                IsWaitlist = bookability.IsWaitlist,
                Schedule = new TourQuoteBuildScheduleRequest
                {
                    ScheduleId = schedule.Id,
                    Code = schedule.Code,
                    Name = schedule.Name,
                    DepartureDate = schedule.DepartureDate,
                    ReturnDate = schedule.ReturnDate,
                    DepartureTime = schedule.DepartureTime,
                    ReturnTime = schedule.ReturnTime,
                    BookingOpenAt = schedule.BookingOpenAt,
                    BookingCutoffAt = schedule.BookingCutoffAt,
                    IsGuaranteedDeparture = schedule.IsGuaranteedDeparture,
                    IsInstantConfirm = schedule.IsInstantConfirm,
                    AvailableSlots = capacity?.AvailableSlots,
                    AllowWaitlist = capacity?.AllowWaitlist,
                    CurrencyCode = resolvedCurrency
                },
                Passengers = passengerInputs,
                Addons = addonResolve.Items,
                Package = packageResolve.Package is null
                    ? null
                    : new TourQuoteBuildPackageRequest
                    {
                        PackageId = packageResolve.Package.PackageId,
                        Code = packageResolve.Package.PackageCode,
                        Name = packageResolve.Package.PackageName,
                        Mode = packageResolve.Package.Mode
                    },
                PackageLines = packageResolve.Package?.Lines ?? new List<TourQuoteBuildPackageLineInput>(),
                AdditionalNotes = packageResolve.Package?.Notes ?? new List<string>()
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }

        return Ok(MapResponse(buildResult));
    }

    private async Task<ResolveAddonsResult> ResolveAddonInputsAsync(
        Guid tourId,
        Guid scheduleId,
        TourQuoteRequest req,
        int totalPax,
        string expectedCurrency,
        CancellationToken ct)
    {
        var selectedAddonQty = (req.SelectedAddons ?? new List<TourQuoteSelectedAddonRequest>())
            .ToDictionary(x => x.AddonId, x => x.Quantity);

        var addonIdsToLoad = selectedAddonQty.Keys.ToHashSet();
        var scheduleAddonOverrides = await _db.TourScheduleAddonPrices
            .AsNoTracking()
            .Where(x =>
                x.TourScheduleId == scheduleId &&
                x.IsActive &&
                !x.IsDeleted)
            .ToListAsync(ct);

        var scheduleAddonByAddonId = scheduleAddonOverrides
            .GroupBy(x => x.TourAddonId)
            .ToDictionary(g => g.Key, g => g.First());

        var defaultAddonsEnabled = req.IncludeDefaultAddons ?? true;
        var baseAutoAddons = await _db.TourAddons
            .AsNoTracking()
            .Where(x =>
                x.TourId == tourId &&
                x.IsActive &&
                !x.IsDeleted &&
                (x.IsRequired || (defaultAddonsEnabled && x.IsDefaultSelected)))
            .Select(x => new { x.Id, x.IsRequired, x.IsDefaultSelected })
            .ToListAsync(ct);

        foreach (var addon in baseAutoAddons)
            addonIdsToLoad.Add(addon.Id);

        foreach (var scheduleAddon in scheduleAddonOverrides)
        {
            if (scheduleAddon.IsRequired || (defaultAddonsEnabled && scheduleAddon.IsDefaultSelected))
                addonIdsToLoad.Add(scheduleAddon.TourAddonId);
        }

        if (addonIdsToLoad.Count == 0)
        {
            return new ResolveAddonsResult
            {
                Ok = true,
                Items = new List<TourQuoteBuildAddonInput>()
            };
        }

        var addons = await _db.TourAddons
            .AsNoTracking()
            .Where(x =>
                x.TourId == tourId &&
                x.IsActive &&
                !x.IsDeleted &&
                addonIdsToLoad.Contains(x.Id))
            .ToListAsync(ct);

        var missingSelectedAddonIds = selectedAddonQty.Keys
            .Where(id => addons.All(a => a.Id != id))
            .ToList();

        if (missingSelectedAddonIds.Count > 0)
        {
            return new ResolveAddonsResult
            {
                Ok = false,
                ErrorMessage = $"One or more selected addons are invalid: {string.Join(", ", missingSelectedAddonIds)}."
            };
        }

        var result = new List<TourQuoteBuildAddonInput>();

        var resolvedAddons = addons
            .Select(addon =>
            {
                scheduleAddonByAddonId.TryGetValue(addon.Id, out var scheduleAddon);

                return ResolveAddon(addon, scheduleAddon, totalPax);
            })
            .OrderByDescending(x => x.IsRequired)
            .ThenByDescending(x => x.IsDefaultSelected)
            .ThenBy(x => x.SortOrder)
            .ThenBy(x => x.Name)
            .ToList();

        foreach (var addon in resolvedAddons)
        {
            var explicitlySelected = selectedAddonQty.TryGetValue(addon.Id, out var explicitQty);

            if (!explicitlySelected && !addon.IsRequired && !(defaultAddonsEnabled && addon.IsDefaultSelected))
                continue;

            var quantity = explicitlySelected ? explicitQty : addon.DefaultQuantity;

            if (!addon.AllowQuantitySelection && explicitlySelected && quantity != addon.DefaultQuantity)
            {
                return new ResolveAddonsResult
                {
                    Ok = false,
                    ErrorMessage = $"Addon '{addon.Name}' does not allow quantity selection."
                };
            }

            if (addon.MinQuantity.HasValue && quantity < addon.MinQuantity.Value)
            {
                return new ResolveAddonsResult
                {
                    Ok = false,
                    ErrorMessage = $"Addon '{addon.Name}' requires at least {addon.MinQuantity.Value}."
                };
            }

            if (addon.MaxQuantity.HasValue && quantity > addon.MaxQuantity.Value)
            {
                return new ResolveAddonsResult
                {
                    Ok = false,
                    ErrorMessage = $"Addon '{addon.Name}' exceeds MaxQuantity ({addon.MaxQuantity.Value})."
                };
            }

            if (!string.Equals(expectedCurrency, addon.CurrencyCode, StringComparison.OrdinalIgnoreCase))
            {
                return new ResolveAddonsResult
                {
                    Ok = false,
                    ErrorMessage = $"Mixed currencies are not supported in quote. Schedule uses '{expectedCurrency}', addon '{addon.Name}' uses '{addon.CurrencyCode}'."
                };
            }

            result.Add(new TourQuoteBuildAddonInput
            {
                AddonId = addon.Id,
                Code = addon.Code,
                Name = addon.Name,
                Quantity = quantity,
                CurrencyCode = addon.CurrencyCode,
                UnitPrice = addon.UnitPrice,
                UnitOriginalPrice = addon.OriginalPrice,
                IsRequired = addon.IsRequired,
                IsDefaultSelected = addon.IsDefaultSelected
            });
        }

        return new ResolveAddonsResult
        {
            Ok = true,
            Items = result
        };
    }

    private async Task<ResolvePackageResult> ResolvePackageQuoteAsync(
        Tour tour,
        TourSchedule schedule,
        TourQuoteRequest req,
        int totalPax,
        string expectedCurrency,
        CancellationToken ct)
    {
        var requestedPackageId = req.PackageId.HasValue && req.PackageId.Value != Guid.Empty
            ? req.PackageId
            : null;

        IQueryable<TourPackage> query = _db.TourPackages
            .AsNoTracking()
            .Where(x =>
                x.TourId == tour.Id &&
                x.IsActive &&
                !x.IsDeleted &&
                x.Status == TourPackageStatus.Active);

        query = requestedPackageId.HasValue
            ? query.Where(x => x.Id == requestedPackageId.Value)
            : query.Where(x => x.IsDefault);

        var package = await query
            .Include(x => x.Components)
            .ThenInclude(x => x.Options)
            .ThenInclude(x => x.ScheduleOverrides)
            .FirstOrDefaultAsync(ct);

        if (package is null)
        {
            if (requestedPackageId.HasValue)
            {
                return new ResolvePackageResult
                {
                    Ok = false,
                    ErrorMessage = "Tour package not found or not available for quote."
                };
            }

            if (req.SelectedPackageOptions is { Count: > 0 })
            {
                return new ResolvePackageResult
                {
                    Ok = false,
                    ErrorMessage = "SelectedPackageOptions require an active default package."
                };
            }

            return new ResolvePackageResult
            {
                Ok = true,
                Package = null
            };
        }

        var totalNights = schedule.ReturnDate.DayNumber - schedule.DepartureDate.DayNumber;
        if (totalNights < 0)
            totalNights = 0;

        if (totalNights == 0 && tour.DurationNights > 0)
            totalNights = tour.DurationNights;

        try
        {
            var selectedPackageOptions = (req.SelectedPackageOptions ?? new List<TourQuoteSelectedPackageOptionRequest>())
                .Select(x => new TourPackageQuoteSelectedOptionInput
                {
                    OptionId = x.OptionId,
                    Quantity = x.Quantity
                })
                .ToList();

            var sourceResolve = await _packageSourceQuoteResolver.ResolveAsync(new TourPackageSourceQuoteResolverRequest
            {
                Tour = tour,
                Schedule = schedule,
                Package = package,
                TotalPax = totalPax,
                TotalNights = totalNights,
                SelectedOptions = selectedPackageOptions
            }, ct);

            var buildResult = _packageQuoteBuilder.Build(new TourPackageQuoteBuildRequest
            {
                TourId = tour.Id,
                ScheduleId = schedule.Id,
                TotalPax = totalPax,
                TotalNights = totalNights,
                ExpectedCurrency = expectedCurrency,
                IncludeDefaultOptions = req.IncludeDefaultPackageOptions ?? true,
                Package = package,
                SelectedOptions = selectedPackageOptions,
                SourceQuotes = sourceResolve.SourceQuotes
            });

            foreach (var note in sourceResolve.Notes.Where(x => !string.IsNullOrWhiteSpace(x)))
            {
                var normalized = note.Trim();
                if (!buildResult!.Notes.Contains(normalized, StringComparer.OrdinalIgnoreCase))
                    buildResult.Notes.Add(normalized);
            }

            return new ResolvePackageResult
            {
                Ok = true,
                Package = buildResult
            };
        }
        catch (ArgumentException ex)
        {
            return new ResolvePackageResult
            {
                Ok = false,
                ErrorMessage = ex.Message
            };
        }
    }

    private static ResolvedAddonQuoteItem ResolveAddon(
        TourAddon addon,
        TourScheduleAddonPrice? scheduleAddon,
        int totalPax)
    {
        var isPerPerson = scheduleAddon?.IsPerPerson ?? addon.IsPerPerson;

        return new ResolvedAddonQuoteItem
        {
            Id = addon.Id,
            Code = addon.Code,
            Name = addon.Name,
            CurrencyCode = (scheduleAddon?.CurrencyCode ?? addon.CurrencyCode).Trim().ToUpperInvariant(),
            UnitPrice = scheduleAddon?.Price ?? addon.BasePrice,
            OriginalPrice = scheduleAddon?.OriginalPrice ?? addon.OriginalPrice,
            IsPerPerson = isPerPerson,
            IsRequired = scheduleAddon?.IsRequired ?? addon.IsRequired,
            IsDefaultSelected = scheduleAddon?.IsDefaultSelected ?? addon.IsDefaultSelected,
            AllowQuantitySelection = scheduleAddon?.AllowQuantitySelection ?? addon.AllowQuantitySelection,
            MinQuantity = scheduleAddon?.MinQuantity ?? addon.MinQuantity,
            MaxQuantity = scheduleAddon?.MaxQuantity ?? addon.MaxQuantity,
            SortOrder = scheduleAddon?.SortOrder ?? addon.SortOrder,
            DefaultQuantity = isPerPerson ? totalPax : 1
        };
    }

    private static TourQuoteResponse MapResponse(TourQuoteBuildResult buildResult)
    {
        return new TourQuoteResponse
        {
            TourId = buildResult.TourId,
            TourName = buildResult.TourName,
            CurrencyCode = buildResult.CurrencyCode,
            TotalPax = buildResult.TotalPax,
            Package = buildResult.Package is null
                ? null
                : new TourQuotePackageDto
                {
                    PackageId = buildResult.Package.PackageId,
                    Code = buildResult.Package.Code,
                    Name = buildResult.Package.Name,
                    Mode = buildResult.Package.Mode
                },
            Schedule = new TourQuoteScheduleDto
            {
                ScheduleId = buildResult.Schedule.ScheduleId,
                Code = buildResult.Schedule.Code,
                Name = buildResult.Schedule.Name,
                DepartureDate = buildResult.Schedule.DepartureDate,
                ReturnDate = buildResult.Schedule.ReturnDate,
                DepartureTime = buildResult.Schedule.DepartureTime,
                ReturnTime = buildResult.Schedule.ReturnTime,
                BookingOpenAt = buildResult.Schedule.BookingOpenAt,
                BookingCutoffAt = buildResult.Schedule.BookingCutoffAt,
                IsGuaranteedDeparture = buildResult.Schedule.IsGuaranteedDeparture,
                IsInstantConfirm = buildResult.Schedule.IsInstantConfirm,
                AvailableSlots = buildResult.Schedule.AvailableSlots,
                AllowWaitlist = buildResult.Schedule.AllowWaitlist
            },
            PassengerLines = buildResult.PassengerLines.Select(x => new TourQuoteLineDto
            {
                Kind = x.Kind,
                ReferenceId = x.ReferenceId,
                Code = x.Code,
                Name = x.Name,
                Quantity = x.Quantity,
                UnitPrice = x.UnitPrice,
                UnitOriginalPrice = x.UnitOriginalPrice,
                LineBaseAmount = x.LineBaseAmount,
                LineOriginalAmount = x.LineOriginalAmount,
                LineTaxAmount = x.LineTaxAmount,
                LineFeeAmount = x.LineFeeAmount,
                CurrencyCode = x.CurrencyCode,
                Notes = x.Notes
            }).ToList(),
            AddonLines = buildResult.AddonLines.Select(x => new TourQuoteLineDto
            {
                Kind = x.Kind,
                ReferenceId = x.ReferenceId,
                Code = x.Code,
                Name = x.Name,
                Quantity = x.Quantity,
                UnitPrice = x.UnitPrice,
                UnitOriginalPrice = x.UnitOriginalPrice,
                LineBaseAmount = x.LineBaseAmount,
                LineOriginalAmount = x.LineOriginalAmount,
                LineTaxAmount = x.LineTaxAmount,
                LineFeeAmount = x.LineFeeAmount,
                CurrencyCode = x.CurrencyCode,
                Notes = x.Notes
            }).ToList(),
            PackageLines = buildResult.PackageLines.Select(x => new TourQuotePackageLineDto
            {
                Kind = x.Kind,
                ReferenceId = x.ReferenceId,
                ComponentId = x.ComponentId,
                ComponentCode = x.ComponentCode,
                ComponentName = x.ComponentName,
                ComponentType = x.ComponentType,
                OptionId = x.OptionId,
                BoundSourceEntityId = x.BoundSourceEntityId,
                Code = x.Code,
                Name = x.Name,
                Quantity = x.Quantity,
                UnitPrice = x.UnitPrice,
                UnitOriginalPrice = x.UnitOriginalPrice,
                LineBaseAmount = x.LineBaseAmount,
                LineOriginalAmount = x.LineOriginalAmount,
                LineTaxAmount = x.LineTaxAmount,
                LineFeeAmount = x.LineFeeAmount,
                CurrencyCode = x.CurrencyCode,
                PricingMode = x.PricingMode,
                IsRequired = x.IsRequired,
                IsDefaultSelected = x.IsDefaultSelected,
                SelectedByOptimization = x.SelectedByOptimization,
                OptimizationStrategy = x.OptimizationStrategy,
                OptimizationScore = x.OptimizationScore,
                SelectionReason = x.SelectionReason,
                Notes = x.Notes
            }).ToList(),
            SubtotalAmount = buildResult.SubtotalAmount,
            TaxAmount = buildResult.TaxAmount,
            FeeAmount = buildResult.FeeAmount,
            TotalAmount = buildResult.TotalAmount,
            OriginalTotalAmount = buildResult.OriginalTotalAmount,
            DiscountAmount = buildResult.DiscountAmount,
            IsWaitlist = buildResult.IsWaitlist,
            Notes = buildResult.Notes
        };
    }

    private static bool HasDuplicatePaxTypes(List<TourQuotePaxGroupRequest> groups)
        => groups.GroupBy(x => x.PriceType).Any(g => g.Count() > 1);

    private static bool HasDuplicateAddonIds(List<TourQuoteSelectedAddonRequest> groups)
        => groups.GroupBy(x => x.AddonId).Any(g => g.Count() > 1);

    private static bool HasDuplicatePackageOptionIds(List<TourQuoteSelectedPackageOptionRequest> groups)
        => groups.GroupBy(x => x.OptionId).Any(g => g.Count() > 1);

    private sealed class ResolveAddonsResult
    {
        public bool Ok { get; set; }
        public string? ErrorMessage { get; set; }
        public List<TourQuoteBuildAddonInput> Items { get; set; } = new();
    }

    private sealed class ResolvePackageResult
    {
        public bool Ok { get; set; }
        public string? ErrorMessage { get; set; }
        public TourPackageQuoteBuildResult? Package { get; set; }
    }

    private sealed class ResolvedAddonQuoteItem
    {
        public Guid Id { get; set; }
        public string Code { get; set; } = "";
        public string Name { get; set; } = "";
        public string CurrencyCode { get; set; } = "";
        public decimal UnitPrice { get; set; }
        public decimal? OriginalPrice { get; set; }
        public bool IsPerPerson { get; set; }
        public bool IsRequired { get; set; }
        public bool IsDefaultSelected { get; set; }
        public bool AllowQuantitySelection { get; set; }
        public int? MinQuantity { get; set; }
        public int? MaxQuantity { get; set; }
        public int SortOrder { get; set; }
        public int DefaultQuantity { get; set; }
    }
}

public sealed class TourQuoteRequest
{
    public Guid ScheduleId { get; set; }
    public Guid? PackageId { get; set; }
    public bool? IncludeDefaultAddons { get; set; } = true;
    public bool? IncludeDefaultPackageOptions { get; set; } = true;
    public List<TourQuotePaxGroupRequest> PaxGroups { get; set; } = new();
    public List<TourQuoteSelectedAddonRequest>? SelectedAddons { get; set; }
    public List<TourQuoteSelectedPackageOptionRequest>? SelectedPackageOptions { get; set; }
}

public sealed class TourQuotePaxGroupRequest
{
    public TourPriceType PriceType { get; set; }
    public int Quantity { get; set; }
}

public sealed class TourQuoteSelectedAddonRequest
{
    public Guid AddonId { get; set; }
    public int Quantity { get; set; } = 1;
}

public sealed class TourQuoteSelectedPackageOptionRequest
{
    public Guid OptionId { get; set; }
    public int? Quantity { get; set; }
}

public sealed class TourQuoteResponse
{
    public Guid TourId { get; set; }
    public string TourName { get; set; } = "";
    public string CurrencyCode { get; set; } = "";
    public int TotalPax { get; set; }
    public TourQuotePackageDto? Package { get; set; }
    public TourQuoteScheduleDto Schedule { get; set; } = new();
    public List<TourQuoteLineDto> PassengerLines { get; set; } = new();
    public List<TourQuoteLineDto> AddonLines { get; set; } = new();
    public List<TourQuotePackageLineDto> PackageLines { get; set; } = new();
    public decimal SubtotalAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal FeeAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal OriginalTotalAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public bool IsWaitlist { get; set; }
    public List<string> Notes { get; set; } = new();
}

public sealed class TourQuotePackageDto
{
    public Guid PackageId { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public TourPackageMode Mode { get; set; }
}

public sealed class TourQuoteScheduleDto
{
    public Guid ScheduleId { get; set; }
    public string Code { get; set; } = "";
    public string? Name { get; set; }
    public DateOnly DepartureDate { get; set; }
    public DateOnly ReturnDate { get; set; }
    public TimeOnly? DepartureTime { get; set; }
    public TimeOnly? ReturnTime { get; set; }
    public DateTimeOffset? BookingOpenAt { get; set; }
    public DateTimeOffset? BookingCutoffAt { get; set; }
    public bool IsGuaranteedDeparture { get; set; }
    public bool IsInstantConfirm { get; set; }
    public int? AvailableSlots { get; set; }
    public bool? AllowWaitlist { get; set; }
}

public sealed class TourQuoteLineDto
{
    public string Kind { get; set; } = "";
    public Guid? ReferenceId { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal? UnitOriginalPrice { get; set; }
    public decimal LineBaseAmount { get; set; }
    public decimal LineOriginalAmount { get; set; }
    public decimal LineTaxAmount { get; set; }
    public decimal LineFeeAmount { get; set; }
    public string CurrencyCode { get; set; } = "";
    public string? Notes { get; set; }
}

public sealed class TourQuotePackageLineDto
{
    public string Kind { get; set; } = "";
    public Guid? ReferenceId { get; set; }
    public Guid ComponentId { get; set; }
    public string ComponentCode { get; set; } = "";
    public string ComponentName { get; set; } = "";
    public TourPackageComponentType ComponentType { get; set; }
    public Guid OptionId { get; set; }
    public Guid? BoundSourceEntityId { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal? UnitOriginalPrice { get; set; }
    public decimal LineBaseAmount { get; set; }
    public decimal LineOriginalAmount { get; set; }
    public decimal LineTaxAmount { get; set; }
    public decimal LineFeeAmount { get; set; }
    public string CurrencyCode { get; set; } = "";
    public TourPackagePricingMode PricingMode { get; set; }
    public bool IsRequired { get; set; }
    public bool IsDefaultSelected { get; set; }
    public bool SelectedByOptimization { get; set; }
    public string? OptimizationStrategy { get; set; }
    public decimal? OptimizationScore { get; set; }
    public string? SelectionReason { get; set; }
    public string? Notes { get; set; }
}

