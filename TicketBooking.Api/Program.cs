// FILE #014 (UPDATE): TicketBooking.Api/Program.cs
using Asp.Versioning;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi; // keep as your project currently uses this
using Serilog;
using System.IdentityModel.Tokens.Jwt;
using System.Reflection;
using System.Security.Claims;
using System.Text;
using TicketBooking.Api.Auth;
using TicketBooking.Api.Services.Commerce;
using TicketBooking.Api.Services.Cms;
using TicketBooking.Api.Services.Flight;
using TicketBooking.Api.Services.Tenancy;
using TicketBooking.Api.Services.Tours;
using TicketBooking.Api.Swagger;
using TicketBooking.Application.Services.Tours;
using TicketBooking.Infrastructure;
using TicketBooking.Infrastructure.Auth;
using TicketBooking.Infrastructure.Identity;
using TicketBooking.Infrastructure.Persistence;
using TicketBooking.Infrastructure.Seed;

var builder = WebApplication.CreateBuilder(args);

// ===== Serilog =====
builder.Host.UseSerilog((ctx, services, cfg) =>
{
    cfg.ReadFrom.Configuration(ctx.Configuration)
       .ReadFrom.Services(services);
});

// ===== Controllers + ProblemDetails =====
builder.Services.AddControllers();
builder.Services.AddProblemDetails();

// ===== API Versioning (/api/v1/...) =====
builder.Services
    .AddApiVersioning(options =>
    {
        options.DefaultApiVersion = new ApiVersion(1, 0);
        options.AssumeDefaultVersionWhenUnspecified = true;
        options.ReportApiVersions = true;
        options.ApiVersionReader = new UrlSegmentApiVersionReader();
    })
    .AddMvc()
    .AddApiExplorer(options =>
    {
        options.GroupNameFormat = "'v'VVV";
        options.SubstituteApiVersionInUrl = true;
    });
builder.Services.AddMemoryCache();
builder.Services.AddScoped<IPermissionService, PermissionService>();
builder.Services.AddScoped<Microsoft.AspNetCore.Authorization.IAuthorizationHandler, PermissionAuthorizationHandler>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IAuthTokenService, AuthTokenService>();
builder.Services.AddSingleton<PartnerOnboardingStore>();
// ===== Infrastructure DI (DbContext + Identity) =====
builder.Services.AddInfrastructure(builder.Configuration);

// ===== CMS services =====
builder.Services.AddCmsServices();
builder.Services.AddHostedService<TicketBooking.Api.Services.Cms.CmsPublishingBackgroundService>();
// ===== Flight - Public / Query Services =====
builder.Services.AddScoped<IFlightPublicTenantResolver, FlightPublicTenantResolver>();
builder.Services.AddScoped<IFlightPublicQueryService, FlightPublicQueryService>();
builder.Services.AddScoped<IFlightSeatMapPublicQueryService, FlightSeatMapPublicQueryService>();
builder.Services.AddScoped<IFlightOfferAncillaryQueryService, FlightOfferAncillaryQueryService>();

// ===== JWT Auth =====
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "TicketBooking.V3";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "TicketBooking.V3";
var jwtSigningKey = builder.Configuration["Jwt:SigningKey"];

if (string.IsNullOrWhiteSpace(jwtSigningKey))
    throw new InvalidOperationException("Jwt:SigningKey is missing in configuration (appsettings.*.json).");

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false;
        options.SaveToken = true;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,

            ValidateAudience = true,
            ValidAudience = jwtAudience,

            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSigningKey)),

            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30)
        };

        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                var userIdStr = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier)
                                ?? context.Principal?.FindFirstValue(JwtRegisteredClaimNames.Sub);
                if (!Guid.TryParse(userIdStr, out var userId) || userId == Guid.Empty)
                {
                    context.Fail("Invalid token subject.");
                    return;
                }

                var tokenSecurityStamp = context.Principal?.FindFirstValue("security_stamp");
                if (string.IsNullOrWhiteSpace(tokenSecurityStamp))
                {
                    context.Fail("Token is missing security stamp.");
                    return;
                }

                var userManager = context.HttpContext.RequestServices.GetRequiredService<Microsoft.AspNetCore.Identity.UserManager<AppUser>>();
                var user = await userManager.FindByIdAsync(userId.ToString());
                if (user is null || !user.IsActive)
                {
                    context.Fail("User not found or inactive.");
                    return;
                }

                if (user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTimeOffset.UtcNow)
                {
                    context.Fail("User is locked.");
                    return;
                }

                if (!string.Equals(user.SecurityStamp, tokenSecurityStamp, StringComparison.Ordinal))
                {
                    context.Fail("Token has been revoked.");
                    return;
                }

                var sessionId = context.Principal?.FindFirstValue("session_id");
                if (!string.IsNullOrWhiteSpace(sessionId))
                {
                    var authTokenService = context.HttpContext.RequestServices.GetRequiredService<IAuthTokenService>();
                    var isActive = await authTokenService.IsSessionActiveAsync(user.Id, sessionId, context.HttpContext.RequestAborted);
                    if (!isActive)
                    {
                        context.Fail("Session has been revoked.");
                    }
                }
            }
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddSingleton<Microsoft.AspNetCore.Authorization.IAuthorizationPolicyProvider, PermissionPolicyProvider>();

// ===== Swagger + Authorize button (ONLY ONE AddSwaggerGen) =====
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "TicketBooking.Api",
        Version = "v1"
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "Input: Bearer {your JWT token}"
    });

    options.AddSecurityRequirement(doc => new OpenApiSecurityRequirement
    {
        { new OpenApiSecuritySchemeReference("Bearer", doc), new List<string>() }
    });

    options.OperationFilter<TenantHeaderOperationFilter>();
    options.CustomSchemaIds(t => (t.FullName ?? t.Name).Replace("+", "."));
});

builder.Services.AddHttpClient();
builder.Services.Configure<CommerceOptions>(builder.Configuration.GetSection("Commerce"));
builder.Services.Configure<SePayGatewayOptions>(builder.Configuration.GetSection("SePayGateway"));
builder.Services.AddScoped<SePayGatewayService>();
builder.Services.AddScoped<CustomerNotificationService>();
builder.Services.AddScoped<CustomerOrderService>();

// ===== Tour services =====
builder.Services.AddScoped<TourBookabilityService>();
builder.Services.AddScoped<TourAvailabilityService>();
builder.Services.AddScoped<TourPriceCalculator>();
builder.Services.AddScoped<TourQuoteBuilder>();
builder.Services.AddScoped<TourPackageQuoteBuilder>();
builder.Services.AddScoped<TourPackageSourceQuoteResolver>();
builder.Services.AddScoped<TourPackageAuditService>();
builder.Services.AddScoped<TourPackageReservationService>();
builder.Services.AddScoped<TourPackageBookingService>();
builder.Services.AddScoped<TourPackageBookingOpsService>();
builder.Services.AddScoped<TourPackageCancellationService>();
builder.Services.AddScoped<TourPackageRescheduleService>();
builder.Services.AddScoped<TourPackageDocumentService>();
builder.Services.AddScoped<ITourPackageSourceQuoteAdapter, FlightTourPackageSourceQuoteAdapter>();
builder.Services.AddScoped<ITourPackageSourceQuoteAdapter, HotelTourPackageSourceQuoteAdapter>();
builder.Services.AddScoped<ITourPackageSourceQuoteAdapter, BusTourPackageSourceQuoteAdapter>();
builder.Services.AddScoped<ITourPackageSourceQuoteAdapter, TrainTourPackageSourceQuoteAdapter>();
builder.Services.AddScoped<ITourPackageSourceReservationAdapter, FlightTourPackageReservationAdapter>();
builder.Services.AddScoped<ITourPackageSourceReservationAdapter, HotelTourPackageReservationAdapter>();
builder.Services.AddScoped<ITourPackageSourceReservationAdapter, BusTourPackageReservationAdapter>();
builder.Services.AddScoped<ITourPackageSourceReservationAdapter, TrainTourPackageReservationAdapter>();
builder.Services.AddScoped<ITourPackageSourceBookingAdapter, FlightTourPackageBookingAdapter>();
builder.Services.AddScoped<ITourPackageSourceBookingAdapter, HotelTourPackageBookingAdapter>();
builder.Services.AddScoped<ITourPackageSourceBookingAdapter, BusTourPackageBookingAdapter>();
builder.Services.AddScoped<ITourPackageSourceBookingAdapter, TrainTourPackageBookingAdapter>();
builder.Services.AddScoped<ITourPackageSourceCancellationAdapter, FlightTourPackageCancellationAdapter>();
builder.Services.AddScoped<ITourPackageSourceCancellationAdapter, HotelTourPackageCancellationAdapter>();
builder.Services.AddScoped<ITourPackageSourceCancellationAdapter, BusTourPackageCancellationAdapter>();
builder.Services.AddScoped<ITourPackageSourceCancellationAdapter, TrainTourPackageCancellationAdapter>();
builder.Services.AddScoped<TourLocalTimeService>();


// ===== Health checks =====
var health = builder.Services.AddHealthChecks();
var cs = builder.Configuration.GetConnectionString("Default");
if (!string.IsNullOrWhiteSpace(cs))
{
    health.AddCheck("sqlserver", new SqlServerConnectionHealthCheck(cs));
}

builder.Services.AddScoped<TicketBooking.Api.Middlewares.TenantContextMiddleware>();

// ✅ Build AFTER all services are registered
var app = builder.Build();

// ===== Optional: auto migrate in dev (Off by default) =====
if (app.Environment.IsDevelopment() && builder.Configuration.GetValue("Database:AutoMigrate", false))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
}

// ===== Seed Identity (roles/users) =====
{
    using var scope = app.Services.CreateScope();
    var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("IdentitySeed");
    var roleManager = scope.ServiceProvider.GetRequiredService<Microsoft.AspNetCore.Identity.RoleManager<TicketBooking.Infrastructure.Identity.AppRole>>();
    var userManager = scope.ServiceProvider.GetRequiredService<Microsoft.AspNetCore.Identity.UserManager<TicketBooking.Infrastructure.Identity.AppUser>>();
    await IdentitySeed.SeedAsync(roleManager, userManager, logger);
}

// ===== Seed Tenants + TenantUsers =====
{
    using var scope = app.Services.CreateScope();
    var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("TenantsSeed");
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var userManager = scope.ServiceProvider.GetRequiredService<Microsoft.AspNetCore.Identity.UserManager<TicketBooking.Infrastructure.Identity.AppUser>>();

    var tenantCtxObj = scope.ServiceProvider.GetRequiredService(typeof(TicketBooking.Infrastructure.Tenancy.ITenantContext));

    await TenantsSeed.SeedAsync(db, userManager, logger, tenantCtxObj);
}


// ===== Seed Permissions (auth.*) =====
{
    using var scope = app.Services.CreateScope();
    var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("PermissionsSeed");
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var roleManager = scope.ServiceProvider.GetRequiredService<Microsoft.AspNetCore.Identity.RoleManager<TicketBooking.Infrastructure.Identity.AppRole>>();
    var userManager = scope.ServiceProvider.GetRequiredService<Microsoft.AspNetCore.Identity.UserManager<TicketBooking.Infrastructure.Identity.AppUser>>();
    await PermissionsSeed.SeedAsync(db, roleManager, logger);
    await TenantSystemRolesSeed.SeedAsync(db, userManager, logger);
    await TenantRolesSeed.SeedForNx001Async(db, userManager, logger);
}

// ===== Seed BUS Demo (Phase 8) =====
{
    using var scope = app.Services.CreateScope();
    var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("BusDemoSeed");
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    var nxTenantId = await db.Tenants.IgnoreQueryFilters()
        .Where(x => x.Code == "NX001" && !x.IsDeleted)
        .Select(x => x.Id)
        .FirstOrDefaultAsync();

    if (nxTenantId == Guid.Empty)
    {
        logger.LogWarning("BusDemoSeed skipped: tenant NX001 not found.");
    }
    else
    {
        var tenantCtxObj = scope.ServiceProvider.GetRequiredService(typeof(TicketBooking.Infrastructure.Tenancy.ITenantContext));
        SetTenantContextForSeed(tenantCtxObj, nxTenantId, "NX001");

        try
        {
            await BusDemoSeed.SeedAsync(db, logger);
        }
        finally
        {
            ClearTenantContextForSeed(tenantCtxObj);
        }
    }
}

// ===== Seed TRAIN Demo (Phase 9) =====
{
    using var scope = app.Services.CreateScope();
    var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("TrainDemoSeed");
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    var vtTenantId = await db.Tenants.IgnoreQueryFilters()
        .Where(x => x.Code == "VT001" && !x.IsDeleted)
        .Select(x => x.Id)
        .FirstOrDefaultAsync();

    if (vtTenantId == Guid.Empty)
    {
        logger.LogWarning("TrainDemoSeed skipped: tenant VT001 not found.");
    }
    else
    {
        var tenantCtxObj = scope.ServiceProvider.GetRequiredService(typeof(TicketBooking.Infrastructure.Tenancy.ITenantContext));
        SetTenantContextForSeed(tenantCtxObj, vtTenantId, "VT001");

        try
        {
            await TrainDemoSeed.SeedAsync(db, logger);
        }
        finally
        {
            ClearTenantContextForSeed(tenantCtxObj);
        }
    }
}

// ===== Seed FLIGHT Demo (Phase 10) =====
{
    using var scope = app.Services.CreateScope();
    var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("FlightDemoSeed");
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    var vmmTenantId = await db.Tenants.IgnoreQueryFilters()
        .Where(x => x.Code == "VMM001" && !x.IsDeleted)
        .Select(x => x.Id)
        .FirstOrDefaultAsync();

    if (vmmTenantId == Guid.Empty)
    {
        logger.LogWarning("FlightDemoSeed skipped: tenant VMM001 not found.");
    }
    else
    {
        var tenantCtxObj = scope.ServiceProvider.GetRequiredService(typeof(TicketBooking.Infrastructure.Tenancy.ITenantContext));
        SetTenantContextForSeed(tenantCtxObj, vmmTenantId, "VMM001");

        try
        {
            await FlightDemoSeed.SeedAsync(db, logger);
        }
        finally
        {
            ClearTenantContextForSeed(tenantCtxObj);
        }
    }
}

// ===== Seed CMS Demo (Phase 7) =====
{
    using var scope = app.Services.CreateScope();
    var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("CmsDemoSeed");
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var userManager = scope.ServiceProvider.GetRequiredService<Microsoft.AspNetCore.Identity.UserManager<TicketBooking.Infrastructure.Identity.AppUser>>();

    var nxTenantId = await db.Tenants.IgnoreQueryFilters()
        .Where(x => x.Code == "NX001" && !x.IsDeleted)
        .Select(x => x.Id)
        .FirstOrDefaultAsync();

    if (nxTenantId == Guid.Empty)
    {
        logger.LogWarning("CmsDemoSeed skipped: tenant NX001 not found.");
    }
    else
    {
        var tenantCtxObj = scope.ServiceProvider.GetRequiredService(typeof(TicketBooking.Infrastructure.Tenancy.ITenantContext));
        SetTenantContextForSeed(tenantCtxObj, nxTenantId, "NX001");

        try
        {
            await TicketBooking.Infrastructure.Seed.CmsDemoSeed.SeedAsync(db, userManager, logger);
        }
        finally
        {
            ClearTenantContextForSeed(tenantCtxObj);
        }
    }
}

// ===== Seed HOTEL Demo (Phase 11) =====
{
    using var scope = app.Services.CreateScope();
    var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("HotelDemoSeed");
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    var ksTenantId = await db.Tenants.IgnoreQueryFilters()
        .Where(x => x.Code == "KS001" && !x.IsDeleted)
        .Select(x => x.Id)
        .FirstOrDefaultAsync();

    if (ksTenantId == Guid.Empty)
    {
        logger.LogWarning("HotelDemoSeed skipped: tenant KS001 not found.");
    }
    else
    {
        var tenantCtxObj = scope.ServiceProvider.GetRequiredService(typeof(TicketBooking.Infrastructure.Tenancy.ITenantContext));
        SetTenantContextForSeed(tenantCtxObj, ksTenantId, "KS001");

        try
        {
            await HotelDemoSeed.SeedAsync(scope.ServiceProvider);
        }
        finally
        {
            ClearTenantContextForSeed(tenantCtxObj);
        }
    }
}

// ===== Seed TOUR Demo (Phase Tour) =====
//{
//    using var scope = app.Services.CreateScope();
//    await TourDemoSeed.SeedAsync(scope.ServiceProvider);
//}

// ===== Global exception handling (ProblemDetails) =====
app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;
        var response = BuildErrorResponse(exception, context.TraceIdentifier, context.Request.Path, app.Environment.IsDevelopment());

        context.Response.StatusCode = response.StatusCode;
        context.Response.ContentType = response.ContentType;
        await context.Response.WriteAsJsonAsync(response.Body);
    });
});

// ===== Swagger UI (dev) =====
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "TicketBooking.Api v1");
        c.DisplayRequestDuration();
    });
}

app.UseSerilogRequestLogging();

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseMiddleware<TicketBooking.Api.Middlewares.TenantContextMiddleware>();
app.UseAuthorization();

app.MapControllers();

// ===== Health endpoints =====
app.MapHealthChecks("/health");
app.MapHealthChecks("/health/db");

// ===== Minimal ping endpoint =====
app.MapGet("/api/v1/ping", () => Results.Ok(new { ok = true }))
   .WithName("Ping");

app.Run();

static (int StatusCode, string ContentType, object Body) BuildErrorResponse(
    Exception? exception,
    string traceId,
    string path,
    bool isDevelopment)
{
    return exception switch
    {
        ArgumentException ex => BuildJsonError(
            StatusCodes.Status400BadRequest,
            ex.Message,
            traceId),
        BadHttpRequestException ex => BuildJsonError(
            ex.StatusCode > 0 ? ex.StatusCode : StatusCodes.Status400BadRequest,
            ex.Message,
            traceId),
        KeyNotFoundException ex => BuildJsonError(
            StatusCodes.Status404NotFound,
            ex.Message,
            traceId),
        DbUpdateConcurrencyException => BuildJsonError(
            StatusCodes.Status409Conflict,
            "Data was changed by another user. Please reload and try again.",
            traceId),
        InvalidOperationException ex when IsNotFoundMessage(ex.Message) => BuildJsonError(
            StatusCodes.Status404NotFound,
            ex.Message,
            traceId),
        InvalidOperationException ex when IsClientErrorMessage(ex.Message) => BuildJsonError(
            StatusCodes.Status400BadRequest,
            ex.Message,
            traceId),
        _ => BuildProblemError(
            StatusCodes.Status500InternalServerError,
            "An unexpected error occurred.",
            path,
            traceId,
            isDevelopment ? exception?.ToString() : null)
    };
}

static bool IsNotFoundMessage(string? message)
{
    if (string.IsNullOrWhiteSpace(message))
        return false;

    var normalized = message.Trim();
    return normalized.Contains("not found", StringComparison.OrdinalIgnoreCase)
           || normalized.Contains("không tìm thấy", StringComparison.OrdinalIgnoreCase);
}

static bool IsClientErrorMessage(string? message)
{
    if (string.IsNullOrWhiteSpace(message))
        return false;

    var normalized = message.Trim();
    return normalized.Contains("required", StringComparison.OrdinalIgnoreCase)
           || normalized.Contains("max length", StringComparison.OrdinalIgnoreCase)
           || normalized.Contains("must be", StringComparison.OrdinalIgnoreCase)
           || normalized.Contains("cannot", StringComparison.OrdinalIgnoreCase)
           || normalized.Contains("invalid", StringComparison.OrdinalIgnoreCase)
           || normalized.Contains("already exists", StringComparison.OrdinalIgnoreCase)
           || normalized.Contains("không hợp lệ", StringComparison.OrdinalIgnoreCase)
           || normalized.Contains("đã hết hạn", StringComparison.OrdinalIgnoreCase)
           || normalized.Contains("không còn hợp lệ", StringComparison.OrdinalIgnoreCase)
           || normalized.Contains("không còn số dư", StringComparison.OrdinalIgnoreCase)
           || normalized.Contains("chỉ có thể", StringComparison.OrdinalIgnoreCase)
           || normalized.Contains("thiếu", StringComparison.OrdinalIgnoreCase)
           || normalized.Contains("require tenant context", StringComparison.OrdinalIgnoreCase)
           || normalized.Contains("requires tenant context", StringComparison.OrdinalIgnoreCase)
           || normalized.Contains("requires switched tenant context", StringComparison.OrdinalIgnoreCase)
           || normalized.Contains("X-TenantId is required", StringComparison.OrdinalIgnoreCase)
           || normalized.Contains("only available in Development environment", StringComparison.OrdinalIgnoreCase);
}

static (int StatusCode, string ContentType, object Body) BuildJsonError(int statusCode, string message, string traceId)
    => (statusCode, "application/json", new { message, traceId });

static (int StatusCode, string ContentType, object Body) BuildProblemError(
    int statusCode,
    string title,
    string path,
    string traceId,
    string? detail)
{
    var problem = new ProblemDetails
    {
        Type = $"https://httpstatuses.com/{statusCode}",
        Title = title,
        Status = statusCode,
        Detail = detail,
        Instance = path
    };

    problem.Extensions["traceId"] = traceId;
    return (statusCode, "application/problem+json", problem);
}

static void SetTenantContextForSeed(object tenantContext, Guid tenantId, string tenantCode)
{
    var type = tenantContext.GetType();

    var methodsToTry = new[]
    {
        "SwitchTenant", "SetTenant", "SetCurrentTenant", "UseTenant", "Set",
        "SetTenantContext", "SetTenantId"
    };

    foreach (var name in methodsToTry)
    {
        var m = type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .FirstOrDefault(x => x.Name == name);

        if (m is null) continue;

        var ps = m.GetParameters();

        try
        {
            if (ps.Length == 1 && ps[0].ParameterType == typeof(Guid))
            {
                m.Invoke(tenantContext, new object[] { tenantId });
                return;
            }

            if (ps.Length == 2 && ps[0].ParameterType == typeof(Guid) && ps[1].ParameterType == typeof(string))
            {
                m.Invoke(tenantContext, new object[] { tenantId, tenantCode });
                return;
            }

            if (ps.Length == 3 && ps[0].ParameterType == typeof(Guid) && ps[1].ParameterType == typeof(string) && ps[2].ParameterType == typeof(bool))
            {
                m.Invoke(tenantContext, new object[] { tenantId, tenantCode, true });
                return;
            }
        }
        catch
        {
            // ignore and fallback to property set
        }
    }

    var tenantIdProp = type.GetProperty("TenantId", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
    if (tenantIdProp is not null && tenantIdProp.CanWrite)
    {
        if (tenantIdProp.PropertyType == typeof(Guid?))
            tenantIdProp.SetValue(tenantContext, (Guid?)tenantId);
        else if (tenantIdProp.PropertyType == typeof(Guid))
            tenantIdProp.SetValue(tenantContext, tenantId);
    }

    var tenantCodeProp = type.GetProperty("TenantCode", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
    if (tenantCodeProp is not null && tenantCodeProp.CanWrite && tenantCodeProp.PropertyType == typeof(string))
        tenantCodeProp.SetValue(tenantContext, tenantCode);

    var adminSwitchProp = type.GetProperty("IsAdminWriteSwitched", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
        ?? type.GetProperty("IsAdminSwitched", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

    if (adminSwitchProp is not null && adminSwitchProp.CanWrite && adminSwitchProp.PropertyType == typeof(bool))
        adminSwitchProp.SetValue(tenantContext, true);
}

static void ClearTenantContextForSeed(object tenantContext)
{
    var type = tenantContext.GetType();

    var clearMethods = new[] { "Clear", "Reset", "ClearTenant", "ClearCurrentTenant" };
    foreach (var name in clearMethods)
    {
        var m = type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .FirstOrDefault(x => x.Name == name && x.GetParameters().Length == 0);

        if (m is null) continue;

        try
        {
            m.Invoke(tenantContext, Array.Empty<object>());
            return;
        }
        catch
        {
        }
    }

    var tenantIdProp = type.GetProperty("TenantId", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
    if (tenantIdProp is not null && tenantIdProp.CanWrite)
    {
        if (tenantIdProp.PropertyType == typeof(Guid?))
            tenantIdProp.SetValue(tenantContext, null);
        else if (tenantIdProp.PropertyType == typeof(Guid))
            tenantIdProp.SetValue(tenantContext, Guid.Empty);
    }

    var tenantCodeProp = type.GetProperty("TenantCode", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
    if (tenantCodeProp is not null && tenantCodeProp.CanWrite && tenantCodeProp.PropertyType == typeof(string))
        tenantCodeProp.SetValue(tenantContext, null);

    var adminSwitchProp = type.GetProperty("IsAdminWriteSwitched", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
        ?? type.GetProperty("IsAdminSwitched", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

    if (adminSwitchProp is not null && adminSwitchProp.CanWrite && adminSwitchProp.PropertyType == typeof(bool))
        adminSwitchProp.SetValue(tenantContext, false);
}

sealed class SqlServerConnectionHealthCheck : IHealthCheck
{
    private readonly string _connectionString;

    public SqlServerConnectionHealthCheck(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync(cancellationToken);
            return HealthCheckResult.Healthy("SQL connection OK.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("SQL connection FAILED.", ex);
        }
    }
}

