// FILE #039 (REWRITE v3): TicketBooking.Infrastructure/Seed/TenantsSeed.cs
using System.Reflection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TicketBooking.Domain.Tenants;
using TicketBooking.Infrastructure.Identity;
using TicketBooking.Infrastructure.Persistence;

namespace TicketBooking.Infrastructure.Seed
{
    /// <summary>
    /// Phase 3 seed: Tenants + TenantUsers (multi-tenant safe, idempotent)
    ///
    /// Why this rewrite:
    /// - Your SaveChanges interceptor enforces: tenant-owned entity TenantUser MUST have TenantId in TenantContext.
    /// - Seeding TenantUsers across multiple tenants must therefore:
    ///   (1) Create tenants (no tenant context needed) + SaveChanges
    ///   (2) For EACH tenant: set TenantContext -> upsert TenantUsers for that tenant -> SaveChanges -> clear TenantContext
    ///
    /// Program.cs call (required):
    /// - Resolve ITenantContext as object and pass into SeedAsync(..., tenantCtxObj)
    ///   var tenantCtxObj = scope.ServiceProvider.GetRequiredService(typeof(TicketBooking.Infrastructure.Tenancy.ITenantContext));
    ///   await TenantsSeed.SeedAsync(db, userManager, logger, tenantCtxObj);
    /// </summary>
    public static class TenantsSeed
    {
        public static async Task SeedAsync(
            AppDbContext db,
            UserManager<AppUser> userManager,
            ILogger logger,
            object tenantCtxObj,
            CancellationToken ct = default)
        {
            var now = DateTimeOffset.Now;

            // 1) Create tenants (global) - use IgnoreQueryFilters for idempotency.
            var nx = await EnsureTenantAsync(db, "NX001", "Nhà xe NX001", TenantType.Bus, 5, now, logger, ct);
            var vt = await EnsureTenantAsync(db, "VT001", "Vé tàu VT001", TenantType.Train, 5, now, logger, ct);
            var vmm = await EnsureTenantAsync(db, "VMM001", "Vé máy bay VMM001", TenantType.Flight, 5, now, logger, ct);
            var ks = await EnsureTenantAsync(db, "KS001", "Khách sạn KS001", TenantType.Hotel, 5, now, logger, ct);
            var tour = await EnsureTenantAsync(db, "TOUR001", "Tour TOUR001", TenantType.Tour, 5, now, logger, ct);

            // Persist tenants first (avoid FK/merge conflicts).
            await db.SaveChangesAsync(ct);

            // 2) Link role users to their tenant (Owner = true)
            // 3) Link admin to ALL tenants for testing
            await WithTenantContextAsync(tenantCtxObj, nx.Id, "NX001", async () =>
            {
                await EnsureTenantUserAsync(db, userManager, nx.Id, "qlnx", RoleNames.QLNX, isOwner: true, now, logger, ct);
                await EnsureTenantUserAsync(db, userManager, nx.Id, "admin", RoleNames.Admin, isOwner: false, now, logger, ct);
                await db.SaveChangesAsync(ct);
            });

            await WithTenantContextAsync(tenantCtxObj, vt.Id, "VT001", async () =>
            {
                await EnsureTenantUserAsync(db, userManager, vt.Id, "qlvt", RoleNames.QLVT, isOwner: true, now, logger, ct);
                await EnsureTenantUserAsync(db, userManager, vt.Id, "admin", RoleNames.Admin, isOwner: false, now, logger, ct);
                await db.SaveChangesAsync(ct);
            });

            await WithTenantContextAsync(tenantCtxObj, vmm.Id, "VMM001", async () =>
            {
                await EnsureTenantUserAsync(db, userManager, vmm.Id, "qlvmm", RoleNames.QLVMM, isOwner: true, now, logger, ct);
                await EnsureTenantUserAsync(db, userManager, vmm.Id, "admin", RoleNames.Admin, isOwner: false, now, logger, ct);
                await db.SaveChangesAsync(ct);
            });

            await WithTenantContextAsync(tenantCtxObj, ks.Id, "KS001", async () =>
            {
                await EnsureTenantUserAsync(db, userManager, ks.Id, "qlks", RoleNames.QLKS, isOwner: true, now, logger, ct);
                await EnsureTenantUserAsync(db, userManager, ks.Id, "admin", RoleNames.Admin, isOwner: false, now, logger, ct);
                await db.SaveChangesAsync(ct);
            });

            await WithTenantContextAsync(tenantCtxObj, tour.Id, "TOUR001", async () =>
            {
                await EnsureTenantUserAsync(db, userManager, tour.Id, "qltour", RoleNames.QLTour, isOwner: true, now, logger, ct);
                await EnsureTenantUserAsync(db, userManager, tour.Id, "admin", RoleNames.Admin, isOwner: false, now, logger, ct);
                await db.SaveChangesAsync(ct);
            });

            logger.LogInformation("Tenants seed completed (NX001/VT001/VMM001/KS001/TOUR001).");
        }

        private static async Task<Tenant> EnsureTenantAsync(
            AppDbContext db,
            string code,
            string name,
            TenantType type,
            int holdMinutes,
            DateTimeOffset now,
            ILogger logger,
            CancellationToken ct)
        {
            var tenant = await db.Tenants.IgnoreQueryFilters()
                .FirstOrDefaultAsync(x => x.Code == code, ct);

            if (tenant is null)
            {
                tenant = new Tenant
                {
                    Id = Guid.NewGuid(),
                    Code = code,
                    Name = name,
                    Type = type,
                    Status = TenantStatus.Active,
                    HoldMinutes = holdMinutes,
                    IsDeleted = false,
                    CreatedAt = now
                };

                db.Tenants.Add(tenant);
                logger.LogInformation("Created tenant {Code} ({Type}).", code, type);
                return tenant;
            }

            var changed = false;
            if (tenant.Name != name) { tenant.Name = name; changed = true; }
            if (tenant.Type != type) { tenant.Type = type; changed = true; }
            if (tenant.Status != TenantStatus.Active) { tenant.Status = TenantStatus.Active; changed = true; }
            if (tenant.HoldMinutes != holdMinutes) { tenant.HoldMinutes = holdMinutes; changed = true; }
            if (tenant.IsDeleted) { tenant.IsDeleted = false; changed = true; }

            if (changed)
            {
                tenant.UpdatedAt = now;
                logger.LogInformation("Updated tenant {Code}.", code);
            }
            else
            {
                logger.LogInformation("Tenant {Code} already exists.", code);
            }

            return tenant;
        }

        private static async Task EnsureTenantUserAsync(
            AppDbContext db,
            UserManager<AppUser> userManager,
            Guid tenantId,
            string userName,
            string roleName,
            bool isOwner,
            DateTimeOffset now,
            ILogger logger,
            CancellationToken ct)
        {
            var user = await userManager.FindByNameAsync(userName);
            if (user is null)
            {
                logger.LogWarning("Cannot link tenant user: username '{UserName}' not found.", userName);
                return;
            }

            var link = await db.TenantUsers.IgnoreQueryFilters()
                .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.UserId == user.Id, ct);

            if (link is null)
            {
                link = new TenantUser
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    UserId = user.Id,
                    RoleName = roleName,
                    IsOwner = isOwner,
                    IsDeleted = false,
                    CreatedAt = now
                };

                db.TenantUsers.Add(link);

                logger.LogInformation(
                    "Linked user '{UserName}' -> tenant {TenantId} as {RoleName} (Owner={Owner}).",
                    userName, tenantId, roleName, isOwner);

                return;
            }

            var changed = false;
            if (link.RoleName != roleName) { link.RoleName = roleName; changed = true; }
            if (link.IsOwner != isOwner) { link.IsOwner = isOwner; changed = true; }
            if (link.IsDeleted) { link.IsDeleted = false; changed = true; }

            if (changed)
            {
                link.UpdatedAt = now;
                logger.LogInformation("Updated tenant link '{UserName}' -> tenant {TenantId}.", userName, tenantId);
            }
        }

        // =========================================================
        // TenantContext helpers (reflection-based, no hard dependency)
        // =========================================================

        private static async Task WithTenantContextAsync(
            object tenantCtxObj,
            Guid tenantId,
            string tenantCode,
            Func<Task> action)
        {
            SetTenantContextForSeed(tenantCtxObj, tenantId, tenantCode);
            try
            {
                await action();
            }
            finally
            {
                ClearTenantContextForSeed(tenantCtxObj);
            }
        }

        private static void SetTenantContextForSeed(object tenantCtxObj, Guid tenantId, string tenantCode)
        {
            var t = tenantCtxObj.GetType();

            // Try common methods first
            if (TryInvokeMethod(t, tenantCtxObj, new[] { "SetTenant", "SwitchTenant", "UseTenant", "SetCurrentTenant" },
                    new object?[] { tenantId, tenantCode }))
                return;

            if (TryInvokeMethod(t, tenantCtxObj, new[] { "SetTenant", "SwitchTenant", "UseTenant", "SetCurrentTenant" },
                    new object?[] { tenantId }))
            {
                // fallthrough to set code via property
            }

            // Then set properties (common patterns)
            TrySetProperty(t, tenantCtxObj, "TenantId", tenantId);
            TrySetProperty(t, tenantCtxObj, "TenantCode", tenantCode);
            TrySetProperty(t, tenantCtxObj, "HasTenant", true);

            // Best-effort validation: if after setting, TenantId is still null/empty -> throw helpful message.
            var tenantIdVal = TryGetGuidProperty(t, tenantCtxObj, "TenantId");
            if (!tenantIdVal.HasValue || tenantIdVal.Value == Guid.Empty)
            {
                throw new InvalidOperationException(
                    "Cannot set TenantContext for seeding. " +
                    "Please ensure ITenantContext exposes (TenantId/TenantCode/HasTenant) properties or a method like SetTenant(Guid, string).");
            }
        }

        private static void ClearTenantContextForSeed(object tenantCtxObj)
        {
            var t = tenantCtxObj.GetType();

            // Try methods first
            if (TryInvokeMethod(t, tenantCtxObj, new[] { "Clear", "ClearTenant", "Reset", "ResetTenant" }, Array.Empty<object?>()))
                return;

            // Then properties
            TrySetProperty(t, tenantCtxObj, "HasTenant", false);
            TrySetProperty(t, tenantCtxObj, "TenantCode", null);

            // TenantId might be Guid? or Guid
            var pi = t.GetProperty("TenantId", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (pi is not null)
            {
                if (pi.PropertyType == typeof(Guid?))
                    pi.SetValue(tenantCtxObj, null);
                else if (pi.PropertyType == typeof(Guid))
                    pi.SetValue(tenantCtxObj, Guid.Empty);
            }
        }

        private static bool TryInvokeMethod(Type t, object target, string[] methodNames, object?[] args)
        {
            var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
            foreach (var name in methodNames)
            {
                var methods = t.GetMethods(flags)
                    .Where(m => string.Equals(m.Name, name, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                foreach (var m in methods)
                {
                    var ps = m.GetParameters();
                    if (ps.Length != args.Length) continue;

                    // basic compatibility check
                    var ok = true;
                    for (var i = 0; i < ps.Length; i++)
                    {
                        if (args[i] is null) continue;
                        var argType = args[i]!.GetType();
                        var pType = ps[i].ParameterType;

                        if (pType == argType) continue;
                        if (pType == typeof(Guid?) && argType == typeof(Guid)) continue;
                        if (pType.IsAssignableFrom(argType)) continue;

                        ok = false;
                        break;
                    }

                    if (!ok) continue;

                    m.Invoke(target, args);
                    return true;
                }
            }

            return false;
        }

        private static void TrySetProperty(Type t, object target, string propertyName, object? value)
        {
            var pi = t.GetProperty(propertyName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (pi is null || !pi.CanWrite) return;

            try
            {
                if (value is null)
                {
                    pi.SetValue(target, null);
                    return;
                }

                var pType = pi.PropertyType;

                if (pType == typeof(Guid) && value is Guid g1)
                {
                    pi.SetValue(target, g1);
                    return;
                }

                if (pType == typeof(Guid?) && value is Guid g2)
                {
                    pi.SetValue(target, (Guid?)g2);
                    return;
                }

                if (pType == typeof(string) && value is string s)
                {
                    pi.SetValue(target, s);
                    return;
                }

                if (pType == typeof(bool) && value is bool b)
                {
                    pi.SetValue(target, b);
                    return;
                }

                // last resort: Convert.ChangeType for primitives
                pi.SetValue(target, Convert.ChangeType(value, Nullable.GetUnderlyingType(pType) ?? pType));
            }
            catch
            {
                // ignore best-effort
            }
        }

        private static Guid? TryGetGuidProperty(Type t, object target, string propertyName)
        {
            var pi = t.GetProperty(propertyName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (pi is null || !pi.CanRead) return null;

            try
            {
                var val = pi.GetValue(target);
                if (val is null) return null;
                if (val is Guid g) return g;
                if (val is Guid gn) return gn;
                return null;
            }
            catch
            {
                return null;
            }
        }
    }
}

