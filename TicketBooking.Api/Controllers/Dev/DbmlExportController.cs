// FILE #245: TicketBooking.Api/Controllers/Dev/DbmlExportController.cs
using System.Text;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using TicketBooking.Infrastructure.Persistence;

namespace TicketBooking.Api.Controllers.Dev;

[ApiController]
[ApiVersion("1.0")]
[Authorize(Policy = "perm:tenants.manage")]
[Route("api/v{version:apiVersion}/dev/dbml-export")]
public sealed class DbmlExportController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<DbmlExportController> _logger;

    public DbmlExportController(
        AppDbContext db,
        IWebHostEnvironment env,
        ILogger<DbmlExportController> logger)
    {
        _db = db;
        _env = env;
        _logger = logger;
    }

    [HttpGet("generate")]
    public async Task<ActionResult<DbmlExportResponse>> Generate(
        [FromQuery] bool includeIdentity = true,
        [FromQuery] string? schemas = null,
        CancellationToken ct = default)
    {
        EnsureDevelopmentOnly();

        await _db.Database.CanConnectAsync(ct);

        var schemaFilter = ParseSchemaFilter(schemas);
        var dbml = BuildDbml(_db.Model, includeIdentity, schemaFilter);

        var exportDir = Path.Combine(_env.ContentRootPath, "exports");
        Directory.CreateDirectory(exportDir);

        var fileName = $"TicketBookingV3_{DateTime.Now:yyyyMMdd_HHmmss}.dbml";
        var filePath = Path.Combine(exportDir, fileName);

        await System.IO.File.WriteAllTextAsync(
            filePath,
            dbml,
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
            ct);

        _logger.LogInformation(
            "DBML exported to {FilePath}. includeIdentity={IncludeIdentity}, schemas={Schemas}",
            filePath,
            includeIdentity,
            schemas ?? "(all)");

        return Ok(new DbmlExportResponse
        {
            Ok = true,
            FileName = fileName,
            FilePath = filePath,
            RelativeFilePath = Path.Combine("exports", fileName),
            DownloadUrl = Url.ActionLink(
                action: nameof(Download),
                controller: "DbmlExport",
                values: new { version = "1.0", fileName }),
            PreviewUrl = Url.ActionLink(
                action: nameof(Preview),
                controller: "DbmlExport",
                values: new { version = "1.0", includeIdentity, schemas }),
            TableCount = CountTables(_db.Model, includeIdentity, schemaFilter),
            RefCount = CountRefs(_db.Model, includeIdentity, schemaFilter),
            Notes = new List<string>
            {
                "File .dbml này dành để import vào dbdiagram.io.",
                "Nếu chỉ muốn các schema nghiệp vụ, gọi lại với includeIdentity=false.",
                "Có thể lọc schema bằng query ?schemas=tenants,bus,train,flight,hotels,tours,booking,payments,ticketing,ops,common,auth"
            }
        });
    }

    [HttpGet("preview")]
    public async Task<IActionResult> Preview(
        [FromQuery] bool includeIdentity = true,
        [FromQuery] string? schemas = null,
        CancellationToken ct = default)
    {
        EnsureDevelopmentOnly();

        await _db.Database.CanConnectAsync(ct);

        var schemaFilter = ParseSchemaFilter(schemas);
        var dbml = BuildDbml(_db.Model, includeIdentity, schemaFilter);

        return Content(dbml, "text/plain", Encoding.UTF8);
    }

    [HttpGet("download")]
    public async Task<IActionResult> Download([FromQuery] string fileName, CancellationToken ct = default)
    {
        EnsureDevelopmentOnly();

        if (string.IsNullOrWhiteSpace(fileName))
            return BadRequest(new { message = "fileName is required." });

        if (fileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            return BadRequest(new { message = "Invalid file name." });

        var exportDir = Path.Combine(_env.ContentRootPath, "exports");
        var filePath = Path.Combine(exportDir, fileName);

        if (!System.IO.File.Exists(filePath))
            return NotFound(new { message = "Export file not found." });

        var bytes = await System.IO.File.ReadAllBytesAsync(filePath, ct);
        return File(bytes, "text/plain", fileName);
    }

    private void EnsureDevelopmentOnly()
    {
        if (!_env.IsDevelopment())
            throw new InvalidOperationException("DBML export endpoint is only available in Development environment.");
    }

    private static HashSet<string>? ParseSchemaFilter(string? schemas)
    {
        if (string.IsNullOrWhiteSpace(schemas))
            return null;

        var set = schemas
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return set.Count == 0 ? null : set;
    }

    private static int CountTables(IModel model, bool includeIdentity, HashSet<string>? schemaFilter)
    {
        return GetTableGroups(model, includeIdentity, schemaFilter).Count;
    }

    private static int CountRefs(IModel model, bool includeIdentity, HashSet<string>? schemaFilter)
    {
        return BuildRefs(model, includeIdentity, schemaFilter).Count;
    }

    private static string BuildDbml(IModel model, bool includeIdentity, HashSet<string>? schemaFilter)
    {
        var sb = new StringBuilder();

        sb.AppendLine("// Auto-generated from EF Core model");
        sb.AppendLine("// Upload this .dbml file to dbdiagram.io");
        sb.AppendLine();

        var groups = GetTableGroups(model, includeIdentity, schemaFilter)
            .OrderBy(x => x.Key.Schema, StringComparer.OrdinalIgnoreCase)
            .ThenBy(x => x.Key.Table, StringComparer.OrdinalIgnoreCase)
            .ToList();

        foreach (var group in groups)
        {
            var schema = group.Key.Schema;
            var table = group.Key.Table;

            sb.AppendLine($"Table {schema}.{table} {{");

            var storeObject = StoreObjectIdentifier.Table(table, schema);
            var columns = BuildColumns(group.Value, storeObject);

            foreach (var column in columns.OrderBy(x => x.Order ?? int.MaxValue).ThenBy(x => x.Name, StringComparer.OrdinalIgnoreCase))
            {
                sb.Append("  ");
                sb.Append(column.Name);
                sb.Append(' ');
                sb.Append(column.Type);

                var settings = new List<string>();
                if (column.IsPrimaryKey) settings.Add("pk");
                if (column.IsIncrement) settings.Add("increment");
                if (column.IsNotNull) settings.Add("not null");

                if (settings.Count > 0)
                {
                    sb.Append(" [");
                    sb.Append(string.Join(", ", settings));
                    sb.Append(']');
                }

                sb.AppendLine();
            }

            sb.AppendLine("}");
            sb.AppendLine();
        }

        var refs = BuildRefs(model, includeIdentity, schemaFilter)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToList();

        foreach (var reference in refs)
        {
            sb.AppendLine(reference);
        }

        return sb.ToString().Trim() + Environment.NewLine;
    }

    private static Dictionary<TableKey, List<IEntityType>> GetTableGroups(
        IModel model,
        bool includeIdentity,
        HashSet<string>? schemaFilter)
    {
        var groups = new Dictionary<TableKey, List<IEntityType>>();

        foreach (var entityType in model.GetEntityTypes())
        {
            if (entityType.IsOwned())
                continue;

            var tableName = entityType.GetTableName();
            if (string.IsNullOrWhiteSpace(tableName))
                continue;

            var schema = entityType.GetSchema() ?? "dbo";

            if (!includeIdentity && IsIdentityLikeTable(schema, tableName))
                continue;

            if (schemaFilter is not null && !schemaFilter.Contains(schema))
                continue;

            var key = new TableKey(schema, tableName);

            if (!groups.TryGetValue(key, out var list))
            {
                list = new List<IEntityType>();
                groups[key] = list;
            }

            list.Add(entityType);
        }

        return groups;
    }

    private static List<DbmlColumn> BuildColumns(IEnumerable<IEntityType> entityTypes, StoreObjectIdentifier storeObject)
    {
        var result = new Dictionary<string, DbmlColumn>(StringComparer.OrdinalIgnoreCase);

        foreach (var entityType in entityTypes)
        {
            var primaryKey = entityType.FindPrimaryKey();

            foreach (var property in entityType.GetProperties())
            {
                var columnName = property.GetColumnName(storeObject);
                if (string.IsNullOrWhiteSpace(columnName))
                    continue;

                var storeType = property.GetColumnType(storeObject);
                var type = NormalizeDbmlType(storeType, property.ClrType);
                var isPk = primaryKey?.Properties.Contains(property) == true;
                var isNotNull = !property.IsColumnNullable(storeObject);
                var isIncrement = property.ValueGenerated == ValueGenerated.OnAdd && IsIncrementType(property.ClrType);
                int? order = null;
                if (!result.TryGetValue(columnName, out var existing))
                {
                    result[columnName] = new DbmlColumn
                    {
                        Name = columnName,
                        Type = type,
                        IsPrimaryKey = isPk,
                        IsNotNull = isNotNull,
                        IsIncrement = isIncrement,
                        Order = order
                    };
                }
                else
                {
                    existing.IsPrimaryKey |= isPk;
                    existing.IsNotNull |= isNotNull;
                    existing.IsIncrement |= isIncrement;

                    if (existing.Order is null && order is not null)
                        existing.Order = order;

                    if (string.IsNullOrWhiteSpace(existing.Type) && !string.IsNullOrWhiteSpace(type))
                        existing.Type = type;
                }
            }
        }

        return result.Values.ToList();
    }

    private static HashSet<string> BuildRefs(IModel model, bool includeIdentity, HashSet<string>? schemaFilter)
    {
        var refs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var entityType in model.GetEntityTypes())
        {
            if (entityType.IsOwned())
                continue;

            var dependentTable = entityType.GetTableName();
            if (string.IsNullOrWhiteSpace(dependentTable))
                continue;

            var dependentSchema = entityType.GetSchema() ?? "dbo";

            if (!includeIdentity && IsIdentityLikeTable(dependentSchema, dependentTable))
                continue;

            if (schemaFilter is not null && !schemaFilter.Contains(dependentSchema))
                continue;

            var dependentStore = StoreObjectIdentifier.Table(dependentTable, dependentSchema);

            foreach (var fk in entityType.GetForeignKeys())
            {
                if (fk.IsOwnership)
                    continue;

                var principalEntity = fk.PrincipalEntityType;
                var principalTable = principalEntity.GetTableName();
                if (string.IsNullOrWhiteSpace(principalTable))
                    continue;

                var principalSchema = principalEntity.GetSchema() ?? "dbo";

                if (!includeIdentity && IsIdentityLikeTable(principalSchema, principalTable))
                    continue;

                if (schemaFilter is not null && !schemaFilter.Contains(principalSchema))
                    continue;

                var principalStore = StoreObjectIdentifier.Table(principalTable, principalSchema);

                var dependentColumns = fk.Properties
                    .Select(x => x.GetColumnName(dependentStore))
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .ToList();

                var principalColumns = fk.PrincipalKey.Properties
                    .Select(x => x.GetColumnName(principalStore))
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .ToList();

                if (dependentColumns.Count == 0 || principalColumns.Count == 0 || dependentColumns.Count != principalColumns.Count)
                    continue;

                string reference;
                if (dependentColumns.Count == 1)
                {
                    reference =
                        $"Ref: {dependentSchema}.{dependentTable}.{dependentColumns[0]} > {principalSchema}.{principalTable}.{principalColumns[0]}";
                }
                else
                {
                    reference =
                        $"Ref: {dependentSchema}.{dependentTable}.({string.Join(", ", dependentColumns)}) > {principalSchema}.{principalTable}.({string.Join(", ", principalColumns)})";
                }

                refs.Add(reference);
            }
        }

        return refs;
    }

    private static bool IsIdentityLikeTable(string schema, string table)
    {
        if (table.StartsWith("AspNet", StringComparison.OrdinalIgnoreCase))
            return true;

        if (schema.Equals("dbo", StringComparison.OrdinalIgnoreCase) &&
            table.Equals("__EFMigrationsHistory", StringComparison.OrdinalIgnoreCase))
            return true;

        return false;
    }

    private static bool IsIncrementType(Type clrType)
    {
        var t = Nullable.GetUnderlyingType(clrType) ?? clrType;

        return t == typeof(byte) ||
               t == typeof(short) ||
               t == typeof(int) ||
               t == typeof(long);
    }

    private static string NormalizeDbmlType(string? storeType, Type clrType)
    {
        if (!string.IsNullOrWhiteSpace(storeType))
        {
            var normalized = storeType.Trim();

            if (normalized.Equals("rowversion", StringComparison.OrdinalIgnoreCase))
                return "varbinary(8)";

            return normalized;
        }

        var t = Nullable.GetUnderlyingType(clrType) ?? clrType;

        if (t == typeof(Guid)) return "uniqueidentifier";
        if (t == typeof(string)) return "nvarchar(max)";
        if (t == typeof(bool)) return "bit";
        if (t == typeof(byte)) return "tinyint";
        if (t == typeof(short)) return "smallint";
        if (t == typeof(int)) return "int";
        if (t == typeof(long)) return "bigint";
        if (t == typeof(float)) return "real";
        if (t == typeof(double)) return "float";
        if (t == typeof(decimal)) return "decimal(18,2)";
        if (t == typeof(DateTime)) return "datetime2";
        if (t == typeof(DateTimeOffset)) return "datetimeoffset";
        if (t == typeof(TimeSpan)) return "time";
        if (t == typeof(DateOnly)) return "date";
        if (t == typeof(TimeOnly)) return "time";
        if (t == typeof(byte[])) return "varbinary(max)";

        if (t.IsEnum)
            return "int";

        return "nvarchar(max)";
    }

    private readonly record struct TableKey(string Schema, string Table);

    private sealed class DbmlColumn
    {
        public string Name { get; set; } = "";
        public string Type { get; set; } = "";
        public bool IsPrimaryKey { get; set; }
        public bool IsNotNull { get; set; }
        public bool IsIncrement { get; set; }
        public int? Order { get; set; }
    }
}

public sealed class DbmlExportResponse
{
    public bool Ok { get; set; }
    public string FileName { get; set; } = "";
    public string FilePath { get; set; } = "";
    public string RelativeFilePath { get; set; } = "";
    public string? DownloadUrl { get; set; }
    public string? PreviewUrl { get; set; }
    public int TableCount { get; set; }
    public int RefCount { get; set; }
    public List<string> Notes { get; set; } = new();
}
