// FILE #042: TicketBooking.Infrastructure/Persistence/DbModelConventions.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace TicketBooking.Infrastructure.Persistence
{
    /// <summary>
    /// Phase 5 conventions applied to all entities:
    /// - Guid Id => default NEWSEQUENTIALID()
    /// - CreatedAt => default Vietnam time (datetimeoffset +07)
    /// - IsDeleted => default 0
    /// Notes:
    /// - Does NOT touch Identity tables.
    /// - Does NOT override properties that already have defaults configured.
    /// </summary>
    public static class DbModelConventions
    {
        private const string VietnamNowSql = "SWITCHOFFSET(SYSDATETIMEOFFSET(), '+07:00')";

        public static void Apply(ModelBuilder builder)
        {
            foreach (var entityType in builder.Model.GetEntityTypes())
            {
                // Skip identity tables
                if (entityType.ClrType.Namespace != null &&
                    entityType.ClrType.Namespace.StartsWith("Microsoft.AspNetCore.Identity", StringComparison.Ordinal))
                    continue;

                ApplySequentialGuidId(entityType);
                ApplyCreatedAtDefault(entityType);
                ApplyIsDeletedDefault(entityType);
            }
        }

        private static void ApplySequentialGuidId(IMutableEntityType entityType)
        {
            var idProp = entityType.FindProperty("Id");
            if (idProp is null) return;

            // Only apply to Guid Id
            if (idProp.ClrType != typeof(Guid)) return;

            // If already configured by entity config, don't override
            if (idProp.GetDefaultValueSql() is not null) return;
            if (idProp.GetDefaultValue() is not null) return;

            idProp.SetDefaultValueSql("NEWSEQUENTIALID()");
            idProp.ValueGenerated = ValueGenerated.OnAdd;
        }

        private static void ApplyCreatedAtDefault(IMutableEntityType entityType)
        {
            var createdAtProp = entityType.FindProperty("CreatedAt");
            if (createdAtProp is null) return;

            // Only apply to DateTimeOffset
            if (createdAtProp.ClrType != typeof(DateTimeOffset)) return;

            if (createdAtProp.GetDefaultValueSql() is not null) return;
            if (createdAtProp.GetDefaultValue() is not null) return;

            createdAtProp.SetDefaultValueSql(VietnamNowSql);
            createdAtProp.ValueGenerated = ValueGenerated.OnAdd;
        }

        private static void ApplyIsDeletedDefault(IMutableEntityType entityType)
        {
            var isDeletedProp = entityType.FindProperty("IsDeleted");
            if (isDeletedProp is null) return;

            if (isDeletedProp.ClrType != typeof(bool)) return;

            if (isDeletedProp.GetDefaultValueSql() is not null) return;
            if (isDeletedProp.GetDefaultValue() is not null) return;

            isDeletedProp.SetDefaultValue(false);
        }
    }
}