// FILE #043: TicketBooking.Infrastructure/Persistence/AppDbContext.cs  (UPDATE - Phase 5 Conventions hook)
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq.Expressions;
using TicketBooking.Infrastructure.Identity;
using TicketBooking.Infrastructure.Tenancy;

namespace TicketBooking.Infrastructure.Persistence
{
    public sealed partial class AppDbContext : IdentityDbContext<AppUser, AppRole, Guid>
    {
        private readonly ITenantContext _tenantContext;

        public AppDbContext(DbContextOptions<AppDbContext> options, ITenantContext tenantContext) : base(options)
        {
            _tenantContext = tenantContext;
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<AppUser>(b =>
            {
                b.Property(x => x.FullName).HasMaxLength(200);
                b.Property(x => x.AvatarUrl).HasMaxLength(500);
                b.Property(x => x.IsActive).HasDefaultValue(true);
                b.HasIndex(x => x.IsActive);
            });

            // Module models
            AppDbContextTenantsModel.ApplyTenantsModel(builder);
            AppDbContextAuthModel.ApplyAuthModel(builder);
            AppDbContextGeoModel.ApplyGeoModel(builder);
            AppDbContextCatalogModel.ApplyCatalogModel(builder);
            AppDbContextFleetModel.ApplyFleetModel(builder);
            AppDbContextBusModel.ApplyBusModel(builder);
            AppDbContextTrainModel.ApplyTrainModel(builder);
            AppDbContextFlightModel.ApplyFlightModel(builder);
            AppDbContextCmsModel.ApplyCmsModel(builder);
            AppDbContextHotelsModel.ApplyHotelsModel(builder);
            AppDbContextTourModel.ApplyTourModel(builder);
            AppDbContextCommerceModel.ApplyCommerceModel(builder);


            // Global filters
            ApplyGlobalQueryFilters(builder);

            // Phase 5: global conventions (sequential GUID, CreatedAt +07, IsDeleted default)
            DbModelConventions.Apply(builder);
        }

        private void ApplyGlobalQueryFilters(ModelBuilder builder)
        {
            foreach (var entityType in builder.Model.GetEntityTypes())
            {
                if (entityType.ClrType.Namespace != null &&
                    entityType.ClrType.Namespace.StartsWith("Microsoft.AspNetCore.Identity", StringComparison.Ordinal))
                    continue;

                var clrType = entityType.ClrType;

                var isDeletedProp = entityType.FindProperty("IsDeleted");
                var tenantIdProp = entityType.FindProperty("TenantId");

                var hasIsDeleted = isDeletedProp is not null;
                var hasTenantId = tenantIdProp is not null;

                if (!hasIsDeleted && !hasTenantId)
                    continue;

                var param = Expression.Parameter(clrType, "e");
                Expression? body = null;

                if (hasIsDeleted)
                {
                    var eIsDeleted = Expression.Property(param, "IsDeleted");
                    var notDeleted = Expression.Equal(eIsDeleted, Expression.Constant(false));
                    body = notDeleted;
                }

                if (hasTenantId)
                {
                    var hasTenantExpr = Expression.Property(Expression.Constant(_tenantContext), nameof(ITenantContext.HasTenant));
                    var noTenant = Expression.Equal(hasTenantExpr, Expression.Constant(false));

                    var ctxTenantIdNullable = Expression.Property(Expression.Constant(_tenantContext), nameof(ITenantContext.TenantId)); // Guid?

                    var eTenantIdExpr = Expression.Property(param, "TenantId"); // Guid or Guid?

                    Expression eTenantIdNullable = eTenantIdExpr.Type == typeof(Guid)
                        ? Expression.Convert(eTenantIdExpr, typeof(Guid?))
                        : eTenantIdExpr;

                    var tenantMatches = Expression.Equal(eTenantIdNullable, ctxTenantIdNullable);

                    var tenantPredicate = Expression.OrElse(noTenant, tenantMatches);

                    body = body is null ? tenantPredicate : Expression.AndAlso(body, tenantPredicate);
                }

                if (body is null)
                    continue;

                var lambda = Expression.Lambda(body, param);
                entityType.SetQueryFilter(lambda);
            }
        }
    }
}
