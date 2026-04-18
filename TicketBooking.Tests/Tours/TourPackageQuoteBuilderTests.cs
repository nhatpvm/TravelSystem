using TicketBooking.Api.Services.Tours;
using TicketBooking.Domain.Tours;

namespace TicketBooking.Tests.Tours;

public sealed class TourPackageQuoteBuilderTests
{
    private readonly TourPackageQuoteBuilder _builder = new();

    [Fact]
    public void Build_UsesDefaultRequiredOption_AndResolvesIncludedPerPaxQuantity()
    {
        var package = CreatePackage();
        var component = new TourPackageComponent
        {
            Id = Guid.NewGuid(),
            TourPackageId = package.Id,
            Code = "OUTBOUND",
            Name = "Outbound Transport",
            ComponentType = TourPackageComponentType.OutboundTransport,
            SelectionMode = TourPackageSelectionMode.RequiredSingle,
            IsActive = true
        };

        component.Options.Add(new TourPackageComponentOption
        {
            Id = Guid.NewGuid(),
            TourPackageComponentId = component.Id,
            Code = "BUS-STD",
            Name = "Bus Standard",
            BindingMode = TourPackageBindingMode.StaticReference,
            SourceType = TourPackageSourceType.Bus,
            SourceEntityId = Guid.NewGuid(),
            PricingMode = TourPackagePricingMode.Included,
            CurrencyCode = "VND",
            QuantityMode = TourPackageQuantityMode.PerPax,
            DefaultQuantity = 1,
            IsDefaultSelected = true,
            IsActive = true
        });

        package.Components.Add(component);

        var result = _builder.Build(new TourPackageQuoteBuildRequest
        {
            TourId = package.TourId,
            ScheduleId = Guid.NewGuid(),
            TotalPax = 3,
            TotalNights = 2,
            ExpectedCurrency = "VND",
            IncludeDefaultOptions = true,
            Package = package
        });

        Assert.NotNull(result);
        Assert.Equal(package.Id, result!.PackageId);
        Assert.Single(result.Lines);
        Assert.Equal(3, result.Lines[0].Quantity);
        Assert.Equal(0m, result.Lines[0].UnitPrice);
        Assert.Equal(TourPackagePricingMode.Included, result.Lines[0].PricingMode);
    }

    [Fact]
    public void Build_UsesScheduleOverridePrice_AndKeepsPinnedSourceReference()
    {
        var package = CreatePackage();
        var scheduleId = Guid.NewGuid();
        var pinnedSourceId = Guid.NewGuid();

        var component = new TourPackageComponent
        {
            Id = Guid.NewGuid(),
            TourPackageId = package.Id,
            Code = "ACTIVITY",
            Name = "Activity",
            ComponentType = TourPackageComponentType.Activity,
            SelectionMode = TourPackageSelectionMode.OptionalMulti,
            IsActive = true
        };

        var option = new TourPackageComponentOption
        {
            Id = Guid.NewGuid(),
            TourPackageComponentId = component.Id,
            Code = "BANA",
            Name = "Ba Na Hills",
            BindingMode = TourPackageBindingMode.ManualFulfillment,
            SourceType = TourPackageSourceType.Activity,
            PricingMode = TourPackagePricingMode.Override,
            CurrencyCode = "VND",
            PriceOverride = 100m,
            QuantityMode = TourPackageQuantityMode.Custom,
            DefaultQuantity = 1,
            IsActive = true
        };

        option.ScheduleOverrides.Add(new TourPackageScheduleOptionOverride
        {
            Id = Guid.NewGuid(),
            TourScheduleId = scheduleId,
            TourPackageComponentOptionId = option.Id,
            Status = TourPackageScheduleOverrideStatus.Pinned,
            BoundSourceEntityId = pinnedSourceId,
            PriceOverride = 120m,
            IsActive = true
        });

        component.Options.Add(option);
        package.Components.Add(component);

        var result = _builder.Build(new TourPackageQuoteBuildRequest
        {
            TourId = package.TourId,
            ScheduleId = scheduleId,
            TotalPax = 2,
            TotalNights = 2,
            ExpectedCurrency = "VND",
            IncludeDefaultOptions = true,
            Package = package,
            SelectedOptions = new List<TourPackageQuoteSelectedOptionInput>
            {
                new()
                {
                    OptionId = option.Id,
                    Quantity = 2
                }
            }
        });

        Assert.NotNull(result);
        Assert.Single(result!.Lines);
        Assert.Equal(120m, result.Lines[0].UnitPrice);
        Assert.Equal(2, result.Lines[0].Quantity);
        Assert.Equal(pinnedSourceId, result.Lines[0].BoundSourceEntityId);
        Assert.Contains("Pinned", result.Lines[0].Note, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Build_ThrowsWhenRequiredSingleComponentHasMultipleChoicesButNoSelection()
    {
        var package = CreatePackage();
        var component = new TourPackageComponent
        {
            Id = Guid.NewGuid(),
            TourPackageId = package.Id,
            Code = "HOTEL",
            Name = "Hotel",
            ComponentType = TourPackageComponentType.Accommodation,
            SelectionMode = TourPackageSelectionMode.RequiredSingle,
            IsActive = true
        };

        component.Options.Add(CreateOption(component.Id, "STD", "Standard"));
        component.Options.Add(CreateOption(component.Id, "DLX", "Deluxe"));
        package.Components.Add(component);

        var ex = Assert.Throws<ArgumentException>(() => _builder.Build(new TourPackageQuoteBuildRequest
        {
            TourId = package.TourId,
            ScheduleId = Guid.NewGuid(),
            TotalPax = 2,
            TotalNights = 2,
            ExpectedCurrency = "VND",
            Package = package
        }));

        Assert.Contains("requires one option", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Build_UsesLiveSourcePriceForPassThroughOption()
    {
        var package = CreatePackage();
        var component = new TourPackageComponent
        {
            Id = Guid.NewGuid(),
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
            TourPackageComponentId = component.Id,
            Code = "TRAIN-SEAT",
            Name = "Soft seat",
            BindingMode = TourPackageBindingMode.StaticReference,
            SourceType = TourPackageSourceType.Train,
            SourceEntityId = Guid.NewGuid(),
            PricingMode = TourPackagePricingMode.PassThrough,
            CurrencyCode = "VND",
            QuantityMode = TourPackageQuantityMode.PerPax,
            DefaultQuantity = 1,
            IsDefaultSelected = true,
            IsActive = true
        };

        component.Options.Add(option);
        package.Components.Add(component);

        var result = _builder.Build(new TourPackageQuoteBuildRequest
        {
            TourId = package.TourId,
            ScheduleId = Guid.NewGuid(),
            TotalPax = 2,
            TotalNights = 1,
            ExpectedCurrency = "VND",
            Package = package,
            SourceQuotes = new Dictionary<Guid, TourPackageSourceQuoteResult>
            {
                [option.Id] = new()
                {
                    OptionId = option.Id,
                    SourceType = TourPackageSourceType.Train,
                    WasResolved = true,
                    IsAvailable = true,
                    BoundSourceEntityId = option.SourceEntityId,
                    CurrencyCode = "VND",
                    UnitTotalPrice = 150m,
                    UnitCost = 150m,
                    SourceName = "SE1",
                    Note = "Live train segment price."
                }
            }
        });

        Assert.NotNull(result);
        Assert.Single(result!.Lines);
        Assert.Equal(150m, result.Lines[0].UnitPrice);
        Assert.Equal(2, result.Lines[0].Quantity);
        Assert.Contains("SE1", result.Lines[0].Note, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Build_SkipsUnavailableDefaultSource_AndFallsBackToAvailableOption()
    {
        var package = CreatePackage();
        var component = new TourPackageComponent
        {
            Id = Guid.NewGuid(),
            TourPackageId = package.Id,
            Code = "OUTBOUND",
            Name = "Outbound",
            ComponentType = TourPackageComponentType.OutboundTransport,
            SelectionMode = TourPackageSelectionMode.RequiredSingle,
            IsActive = true
        };

        var unavailableDefault = new TourPackageComponentOption
        {
            Id = Guid.NewGuid(),
            TourPackageComponentId = component.Id,
            Code = "FLIGHT-A",
            Name = "Flight A",
            BindingMode = TourPackageBindingMode.StaticReference,
            SourceType = TourPackageSourceType.Flight,
            SourceEntityId = Guid.NewGuid(),
            PricingMode = TourPackagePricingMode.PassThrough,
            CurrencyCode = "VND",
            QuantityMode = TourPackageQuantityMode.PerPax,
            DefaultQuantity = 1,
            IsDefaultSelected = true,
            SortOrder = 1,
            IsActive = true
        };

        var fallback = new TourPackageComponentOption
        {
            Id = Guid.NewGuid(),
            TourPackageComponentId = component.Id,
            Code = "BUS-B",
            Name = "Bus B",
            BindingMode = TourPackageBindingMode.ManualFulfillment,
            SourceType = TourPackageSourceType.Bus,
            PricingMode = TourPackagePricingMode.Override,
            CurrencyCode = "VND",
            PriceOverride = 80m,
            QuantityMode = TourPackageQuantityMode.PerPax,
            DefaultQuantity = 1,
            SortOrder = 2,
            IsActive = true
        };

        component.Options.Add(unavailableDefault);
        component.Options.Add(fallback);
        package.Components.Add(component);

        var result = _builder.Build(new TourPackageQuoteBuildRequest
        {
            TourId = package.TourId,
            ScheduleId = Guid.NewGuid(),
            TotalPax = 2,
            TotalNights = 1,
            ExpectedCurrency = "VND",
            Package = package,
            SourceQuotes = new Dictionary<Guid, TourPackageSourceQuoteResult>
            {
                [unavailableDefault.Id] = new()
                {
                    OptionId = unavailableDefault.Id,
                    SourceType = TourPackageSourceType.Flight,
                    WasResolved = true,
                    IsAvailable = false,
                    BoundSourceEntityId = unavailableDefault.SourceEntityId,
                    CurrencyCode = "VND",
                    ErrorMessage = "Flight offer source has expired."
                }
            }
        });

        Assert.NotNull(result);
        Assert.Single(result!.Lines);
        Assert.Equal(fallback.Id, result.Lines[0].OptionId);
        Assert.Equal(80m, result.Lines[0].UnitPrice);
    }

    [Fact]
    public void Build_AutoSelectsBestDynamicCandidate_ForRequiredSingleComponent()
    {
        var package = CreatePackage();
        var component = new TourPackageComponent
        {
            Id = Guid.NewGuid(),
            TourPackageId = package.Id,
            Code = "TRANSPORT",
            Name = "Transport",
            ComponentType = TourPackageComponentType.OutboundTransport,
            SelectionMode = TourPackageSelectionMode.RequiredSingle,
            IsActive = true
        };

        var recommended = new TourPackageComponentOption
        {
            Id = Guid.NewGuid(),
            TourPackageComponentId = component.Id,
            Code = "FLIGHT-REC",
            Name = "Flight recommended",
            BindingMode = TourPackageBindingMode.SearchTemplate,
            SearchTemplateJson = "{}",
            SourceType = TourPackageSourceType.Flight,
            PricingMode = TourPackagePricingMode.PassThrough,
            CurrencyCode = "VND",
            QuantityMode = TourPackageQuantityMode.PerPax,
            DefaultQuantity = 1,
            IsDynamicCandidate = true,
            SortOrder = 2,
            IsActive = true
        };

        var fallback = new TourPackageComponentOption
        {
            Id = Guid.NewGuid(),
            TourPackageComponentId = component.Id,
            Code = "BUS-FALLBACK",
            Name = "Bus fallback",
            BindingMode = TourPackageBindingMode.SearchTemplate,
            SearchTemplateJson = "{}",
            SourceType = TourPackageSourceType.Bus,
            PricingMode = TourPackagePricingMode.PassThrough,
            CurrencyCode = "VND",
            QuantityMode = TourPackageQuantityMode.PerPax,
            DefaultQuantity = 1,
            IsDynamicCandidate = true,
            IsFallback = true,
            SortOrder = 1,
            IsActive = true
        };

        component.Options.Add(recommended);
        component.Options.Add(fallback);
        package.Components.Add(component);

        var result = _builder.Build(new TourPackageQuoteBuildRequest
        {
            TourId = package.TourId,
            ScheduleId = Guid.NewGuid(),
            TotalPax = 2,
            TotalNights = 1,
            ExpectedCurrency = "VND",
            Package = package,
            SourceQuotes = new Dictionary<Guid, TourPackageSourceQuoteResult>
            {
                [recommended.Id] = new()
                {
                    OptionId = recommended.Id,
                    SourceType = TourPackageSourceType.Flight,
                    WasResolved = true,
                    IsAvailable = true,
                    BoundSourceEntityId = Guid.NewGuid(),
                    CurrencyCode = "VND",
                    UnitTotalPrice = 140m,
                    UnitCost = 140m,
                    OptimizationStrategy = "recommended",
                    OptimizationScore = 88m,
                    SelectionReason = "Selected by recommended optimization: best overall trade-off.",
                    SourceName = "Flight VN123"
                },
                [fallback.Id] = new()
                {
                    OptionId = fallback.Id,
                    SourceType = TourPackageSourceType.Bus,
                    WasResolved = true,
                    IsAvailable = true,
                    BoundSourceEntityId = Guid.NewGuid(),
                    CurrencyCode = "VND",
                    UnitTotalPrice = 100m,
                    UnitCost = 100m,
                    OptimizationStrategy = "cheapest",
                    OptimizationScore = 95m,
                    SelectionReason = "Selected by cheapest optimization: lowest live price.",
                    SourceName = "Bus sleeper"
                }
            }
        });

        Assert.NotNull(result);
        Assert.Single(result!.Lines);
        Assert.Equal(recommended.Id, result.Lines[0].OptionId);
        Assert.True(result.Lines[0].SelectedByOptimization);
        Assert.Equal("recommended", result.Lines[0].OptimizationStrategy);
        Assert.Equal(88m, result.Lines[0].OptimizationScore);
        Assert.Contains("Auto-selected", result.Lines[0].SelectionReason, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Flight VN123", result.Lines[0].Note, StringComparison.OrdinalIgnoreCase);
    }

    private static TourPackage CreatePackage()
    {
        return new TourPackage
        {
            Id = Guid.NewGuid(),
            TourId = Guid.NewGuid(),
            Code = "PKG-STD",
            Name = "Standard Package",
            Mode = TourPackageMode.Configurable,
            Status = TourPackageStatus.Active,
            CurrencyCode = "VND",
            IsActive = true
        };
    }

    private static TourPackageComponentOption CreateOption(Guid componentId, string code, string name)
    {
        return new TourPackageComponentOption
        {
            Id = Guid.NewGuid(),
            TourPackageComponentId = componentId,
            Code = code,
            Name = name,
            BindingMode = TourPackageBindingMode.ManualFulfillment,
            SourceType = TourPackageSourceType.Hotel,
            PricingMode = TourPackagePricingMode.Override,
            CurrencyCode = "VND",
            PriceOverride = 100m,
            QuantityMode = TourPackageQuantityMode.PerBooking,
            DefaultQuantity = 1,
            IsActive = true
        };
    }
}
