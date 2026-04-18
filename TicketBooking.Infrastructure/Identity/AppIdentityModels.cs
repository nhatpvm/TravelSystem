// FILE #004: TicketBooking.Infrastructure/Identity/AppIdentityModels.cs
using Microsoft.AspNetCore.Identity;
using System;

namespace TicketBooking.Infrastructure.Identity
{
    /// <summary>
    /// ASP.NET Core Identity user with GUID key.
    /// Keep it minimal at Phase 2; we can extend later (multi-tenant claims/permissions, profile fields...).
    /// </summary>
    public class AppUser : IdentityUser<Guid>
    {
        public string? FullName { get; set; }
        public bool IsActive { get; set; } = true;

        // Optional profile fields (safe to keep; can be unused in UI)
        public string? AvatarUrl { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset? UpdatedAt { get; set; }

    }

    /// <summary>
    /// ASP.NET Core Identity role with GUID key.
    /// </summary>
    public class AppRole : IdentityRole<Guid>
    {
        public AppRole() : base() { }
        public AppRole(string roleName) : base(roleName) { }
    }

    /// <summary>
    /// System roles (PascalCase first letter as you requested).
    /// </summary>
    public static class RoleNames
    {
        public const string Customer = "Customer";
        public const string Admin = "Admin";
        public const string QLNX = "QLNX";
        public const string QLVT = "QLVT";
        public const string QLVMM = "QLVMM";
        public const string QLKS = "QLKS";
        public const string QLTour = "QLTour";

        public static readonly string[] All =
        {
            Customer, Admin, QLNX, QLVT, QLVMM, QLKS, QLTour
        };
    }
}