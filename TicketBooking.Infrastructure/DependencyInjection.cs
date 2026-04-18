// FILE #036: TicketBooking.Infrastructure/DependencyInjection.cs  (UPDATE)
// Purpose (Phase 5):
// - Register AuditTenantSoftDeleteInterceptor (scoped)
// - Attach interceptor to DbContext
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TicketBooking.Infrastructure.Identity;
using TicketBooking.Infrastructure.Persistence;
using TicketBooking.Infrastructure.Persistence.Interceptors;
using TicketBooking.Infrastructure.Tenancy;

namespace TicketBooking.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
        {
            var cs = config.GetConnectionString("Default");
            if (string.IsNullOrWhiteSpace(cs))
                throw new InvalidOperationException("ConnectionStrings:Default is missing.");

            // Tenancy
            services.AddScoped<ITenantContext, TenantContext>();

            // Phase 5: Interceptor (scoped per request)
            services.AddScoped<AuditTenantSoftDeleteInterceptor>();

            // DbContext + interceptor
            services.AddDbContext<AppDbContext>((sp, opt) =>
            {
                opt.UseSqlServer(cs, sql =>
                {
                    sql.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName);
                });

                // Attach interceptor
                opt.AddInterceptors(sp.GetRequiredService<AuditTenantSoftDeleteInterceptor>());
            });

            // Identity (GUID)
            services
                .AddIdentityCore<AppUser>(options =>
                {
                    options.User.RequireUniqueEmail = true;

                    options.Password.RequiredLength = 8;
                    options.Password.RequireDigit = true;
                    options.Password.RequireLowercase = true;
                    options.Password.RequireUppercase = true;
                    options.Password.RequireNonAlphanumeric = false;

                    options.Lockout.AllowedForNewUsers = true;
                    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
                    options.Lockout.MaxFailedAccessAttempts = 5;
                })
                .AddRoles<AppRole>()
                .AddEntityFrameworkStores<AppDbContext>()
                .AddSignInManager<SignInManager<AppUser>>()
                .AddDefaultTokenProviders();

            return services;
        }
    }
}