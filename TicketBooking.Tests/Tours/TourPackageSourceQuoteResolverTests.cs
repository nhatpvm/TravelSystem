using TicketBooking.Api.Services.Tours;
using TicketBooking.Domain.Tours;

namespace TicketBooking.Tests.Tours;

public sealed class TourPackageSourceQuoteResolverTests
{
    [Fact]
    public async Task ResolveAsync_UsesSchedulePinnedSourceAndExplicitQuantity()
    {
        var adapter = new FakeSourceQuoteAdapter(TourPackageSourceType.Bus);
        var resolver = new TourPackageSourceQuoteResolver(new[] { adapter });

        var tour = new Tour
        {
            Id = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            Code = "TOUR-001",
            Name = "Combo",
            Slug = "combo",
            CurrencyCode = "VND"
        };

        var schedule = new TourSchedule
        {
            Id = Guid.NewGuid(),
            TenantId = tour.TenantId,
            TourId = tour.Id,
            Code = "SCH-001",
            DepartureDate = new DateOnly(2026, 4, 10),
            ReturnDate = new DateOnly(2026, 4, 12),
            IsActive = true
        };

        var package = new TourPackage
        {
            Id = Guid.NewGuid(),
            TenantId = tour.TenantId,
            TourId = tour.Id,
            Code = "PKG-001",
            Name = "Standard",
            CurrencyCode = "VND",
            Status = TourPackageStatus.Active,
            IsActive = true
        };

        var component = new TourPackageComponent
        {
            Id = Guid.NewGuid(),
            TenantId = tour.TenantId,
            TourPackageId = package.Id,
            Code = "BUS",
            Name = "Bus",
            ComponentType = TourPackageComponentType.OutboundTransport,
            SelectionMode = TourPackageSelectionMode.RequiredSingle,
            IsActive = true
        };

        var baseSourceId = Guid.NewGuid();
        var pinnedSourceId = Guid.NewGuid();

        var option = new TourPackageComponentOption
        {
            Id = Guid.NewGuid(),
            TenantId = tour.TenantId,
            TourPackageComponentId = component.Id,
            Code = "BUS-VIP",
            Name = "Bus VIP",
            SourceType = TourPackageSourceType.Bus,
            BindingMode = TourPackageBindingMode.StaticReference,
            SourceEntityId = baseSourceId,
            PricingMode = TourPackagePricingMode.PassThrough,
            CurrencyCode = "VND",
            QuantityMode = TourPackageQuantityMode.Custom,
            DefaultQuantity = 1,
            IsActive = true
        };

        option.ScheduleOverrides.Add(new TourPackageScheduleOptionOverride
        {
            Id = Guid.NewGuid(),
            TenantId = tour.TenantId,
            TourScheduleId = schedule.Id,
            TourPackageComponentOptionId = option.Id,
            Status = TourPackageScheduleOverrideStatus.Pinned,
            BoundSourceEntityId = pinnedSourceId,
            IsActive = true
        });

        component.Options.Add(option);
        package.Components.Add(component);

        var result = await resolver.ResolveAsync(new TourPackageSourceQuoteResolverRequest
        {
            Tour = tour,
            Schedule = schedule,
            Package = package,
            TotalPax = 2,
            TotalNights = 2,
            SelectedOptions = new List<TourPackageQuoteSelectedOptionInput>
            {
                new()
                {
                    OptionId = option.Id,
                    Quantity = 3
                }
            }
        });

        Assert.True(result.SourceQuotes.ContainsKey(option.Id));
        Assert.Single(adapter.Requests);
        Assert.Equal(pinnedSourceId, adapter.Requests[0].SourceEntityId);
        Assert.Equal(3, adapter.Requests[0].RequestedQuantity);
    }

    [Fact]
    public async Task ResolveAsync_ResolvesSearchTemplateOption()
    {
        var adapter = new FakeSourceQuoteAdapter(TourPackageSourceType.Train);
        var resolver = new TourPackageSourceQuoteResolver(new[] { adapter });

        var tour = new Tour
        {
            Id = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            Code = "TOUR-002",
            Name = "Combo",
            Slug = "combo-2",
            CurrencyCode = "VND"
        };

        var schedule = new TourSchedule
        {
            Id = Guid.NewGuid(),
            TenantId = tour.TenantId,
            TourId = tour.Id,
            Code = "SCH-002",
            DepartureDate = new DateOnly(2026, 4, 10),
            ReturnDate = new DateOnly(2026, 4, 12),
            IsActive = true
        };

        var package = new TourPackage
        {
            Id = Guid.NewGuid(),
            TenantId = tour.TenantId,
            TourId = tour.Id,
            Code = "PKG-002",
            Name = "Dynamic",
            CurrencyCode = "VND",
            Status = TourPackageStatus.Active,
            IsActive = true
        };

        var component = new TourPackageComponent
        {
            Id = Guid.NewGuid(),
            TenantId = tour.TenantId,
            TourPackageId = package.Id,
            Code = "TRAIN",
            Name = "Train",
            ComponentType = TourPackageComponentType.OutboundTransport,
            SelectionMode = TourPackageSelectionMode.RequiredSingle,
            IsActive = true
        };

        var option = new TourPackageComponentOption
        {
            Id = Guid.NewGuid(),
            TenantId = tour.TenantId,
            TourPackageComponentId = component.Id,
            Code = "TRAIN-DYN",
            Name = "Train dynamic",
            SourceType = TourPackageSourceType.Train,
            BindingMode = TourPackageBindingMode.SearchTemplate,
            SearchTemplateJson = "{\"fromLocationId\":\"11111111-1111-1111-1111-111111111111\",\"toLocationId\":\"22222222-2222-2222-2222-222222222222\"}",
            PricingMode = TourPackagePricingMode.PassThrough,
            CurrencyCode = "VND",
            QuantityMode = TourPackageQuantityMode.PerPax,
            DefaultQuantity = 1,
            IsActive = true
        };

        component.Options.Add(option);
        package.Components.Add(component);

        var result = await resolver.ResolveAsync(new TourPackageSourceQuoteResolverRequest
        {
            Tour = tour,
            Schedule = schedule,
            Package = package,
            TotalPax = 2,
            TotalNights = 2
        });

        Assert.True(result.SourceQuotes.ContainsKey(option.Id));
        Assert.Single(adapter.Requests);
        Assert.Equal(Guid.Empty, adapter.Requests[0].SourceEntityId);
        Assert.Equal(option.Id, adapter.Requests[0].Option.Id);
    }

    [Fact]
    public async Task ResolveAsync_AddsOptimizationNote_WhenSourceQuoteIncludesScore()
    {
        var adapter = new FakeSourceQuoteAdapter(TourPackageSourceType.Flight)
        {
            OptimizationScore = 91m,
            SelectionReason = "Selected by recommended optimization: best overall trade-off."
        };
        var resolver = new TourPackageSourceQuoteResolver(new[] { adapter });

        var tour = new Tour
        {
            Id = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            Code = "TOUR-003",
            Name = "Combo",
            Slug = "combo-3",
            CurrencyCode = "VND"
        };

        var schedule = new TourSchedule
        {
            Id = Guid.NewGuid(),
            TenantId = tour.TenantId,
            TourId = tour.Id,
            Code = "SCH-003",
            DepartureDate = new DateOnly(2026, 4, 10),
            ReturnDate = new DateOnly(2026, 4, 12),
            IsActive = true
        };

        var package = new TourPackage
        {
            Id = Guid.NewGuid(),
            TenantId = tour.TenantId,
            TourId = tour.Id,
            Code = "PKG-003",
            Name = "Dynamic",
            CurrencyCode = "VND",
            Status = TourPackageStatus.Active,
            IsActive = true
        };

        var component = new TourPackageComponent
        {
            Id = Guid.NewGuid(),
            TenantId = tour.TenantId,
            TourPackageId = package.Id,
            Code = "AIR",
            Name = "Air",
            ComponentType = TourPackageComponentType.OutboundTransport,
            SelectionMode = TourPackageSelectionMode.RequiredSingle,
            IsActive = true
        };

        var option = new TourPackageComponentOption
        {
            Id = Guid.NewGuid(),
            TenantId = tour.TenantId,
            TourPackageComponentId = component.Id,
            Code = "AIR-DYN",
            Name = "Air dynamic",
            SourceType = TourPackageSourceType.Flight,
            BindingMode = TourPackageBindingMode.SearchTemplate,
            SearchTemplateJson = "{\"fromAirportCode\":\"SGN\",\"toAirportCode\":\"DAD\"}",
            PricingMode = TourPackagePricingMode.PassThrough,
            CurrencyCode = "VND",
            QuantityMode = TourPackageQuantityMode.PerPax,
            DefaultQuantity = 1,
            IsActive = true
        };

        component.Options.Add(option);
        package.Components.Add(component);

        var result = await resolver.ResolveAsync(new TourPackageSourceQuoteResolverRequest
        {
            Tour = tour,
            Schedule = schedule,
            Package = package,
            TotalPax = 2,
            TotalNights = 2
        });

        Assert.Contains(result.Notes, x => x.Contains("Dynamic package optimization", StringComparison.OrdinalIgnoreCase));
        Assert.Equal(91m, result.SourceQuotes[option.Id].OptimizationScore);
    }

    private sealed class FakeSourceQuoteAdapter : ITourPackageSourceQuoteAdapter
    {
        private readonly TourPackageSourceType _sourceType;

        public FakeSourceQuoteAdapter(TourPackageSourceType sourceType)
        {
            _sourceType = sourceType;
        }

        public List<TourPackageSourceQuoteAdapterRequest> Requests { get; } = new();
        public decimal? OptimizationScore { get; set; }
        public string? SelectionReason { get; set; }

        public bool CanHandle(TourPackageSourceType sourceType)
            => sourceType == _sourceType;

        public Task<TourPackageSourceQuoteResult> ResolveAsync(
            TourPackageSourceQuoteAdapterRequest request,
            CancellationToken ct = default)
        {
            Requests.Add(request);

            return Task.FromResult(new TourPackageSourceQuoteResult
            {
                OptionId = request.Option.Id,
                SourceType = request.Option.SourceType,
                WasResolved = true,
                IsAvailable = true,
                BoundSourceEntityId = request.SourceEntityId,
                CurrencyCode = "VND",
                UnitTotalPrice = 100m,
                UnitCost = 100m,
                WasOptimizedSelection = OptimizationScore.HasValue,
                OptimizationScore = OptimizationScore,
                SelectionReason = SelectionReason
            });
        }
    }
}
