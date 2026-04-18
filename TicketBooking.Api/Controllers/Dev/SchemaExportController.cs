// FILE #245: TicketBooking.Api/Controllers/Dev/SchemaExportController.cs
using System.Text;
using System.Text.RegularExpressions;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TicketBooking.Infrastructure.Persistence;

namespace TicketBooking.Api.Controllers.Dev;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/dev/schema-export")]
public sealed class SchemaExportController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<SchemaExportController> _logger;

    public SchemaExportController(
        AppDbContext db,
        IWebHostEnvironment env,
        ILogger<SchemaExportController> logger)
    {
        _db = db;
        _env = env;
        _logger = logger;
    }

    [HttpGet("drawdb-sql/generate")]
    public async Task<ActionResult<DrawDbSchemaExportResponse>> GenerateDrawDbSql(CancellationToken ct = default)
    {
        EnsureDevelopmentOnly();

        await _db.Database.CanConnectAsync(ct);

        var rawSql = _db.Database.GenerateCreateScript();
        var cleanedSql = SanitizeForDrawDb(rawSql);

        var exportDir = Path.Combine(_env.ContentRootPath, "exports");
        Directory.CreateDirectory(exportDir);

        var fileName = $"TicketBookingV3_DrawDb_{DateTime.Now:yyyyMMdd_HHmmss}.sql";
        var filePath = Path.Combine(exportDir, fileName);

        await System.IO.File.WriteAllTextAsync(filePath, cleanedSql, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false), ct);

        _logger.LogInformation("DrawDB schema exported to {FilePath}", filePath);

        return Ok(new DrawDbSchemaExportResponse
        {
            Ok = true,
            FileName = fileName,
            FilePath = filePath,
            RelativeFilePath = Path.Combine("exports", fileName),
            DownloadUrl = Url.ActionLink(
                action: nameof(DownloadDrawDbSql),
                controller: "SchemaExport",
                values: new { version = "1.0", fileName }),
            LineCount = cleanedSql.Split('\n').Length,
            CharacterCount = cleanedSql.Length,
            Notes = new List<string>
            {
                "File này đã được làm sạch cho drawdb.app, không phải file SQL để chạy production.",
                "Đã loại các phần dễ gây lỗi parse như GO, USE, SET ANSI_NULLS, CHECK CONSTRAINT, filegroup.",
                "Đã đổi một số kiểu SQL Server sang kiểu generic hơn như uniqueidentifier -> uuid, bit -> boolean, rowversion -> varbinary(8)."
            }
        });
    }

    [HttpGet("drawdb-sql/download")]
    public async Task<IActionResult> DownloadDrawDbSql([FromQuery] string fileName, CancellationToken ct = default)
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
        return File(bytes, "application/sql", fileName);
    }

    [HttpGet("drawdb-sql/preview")]
    public async Task<IActionResult> PreviewDrawDbSql(CancellationToken ct = default)
    {
        EnsureDevelopmentOnly();

        await _db.Database.CanConnectAsync(ct);

        var rawSql = _db.Database.GenerateCreateScript();
        var cleanedSql = SanitizeForDrawDb(rawSql);

        return Content(cleanedSql, "text/plain", Encoding.UTF8);
    }

    private void EnsureDevelopmentOnly()
    {
        if (!_env.IsDevelopment())
            throw new InvalidOperationException("Schema export endpoint is only available in Development environment.");
    }

    private static string SanitizeForDrawDb(string sql)
    {
        if (string.IsNullOrWhiteSpace(sql))
            return string.Empty;

        var text = sql.Replace("\r\n", "\n").Replace('\r', '\n');

        // Remove SQL Server headers / commands that drawdb often cannot parse
        var lines = text.Split('\n');
        var kept = new List<string>(lines.Length);

        foreach (var raw in lines)
        {
            var line = raw.Trim();

            if (string.IsNullOrWhiteSpace(line))
            {
                kept.Add(string.Empty);
                continue;
            }

            if (line.Equals("GO", StringComparison.OrdinalIgnoreCase)) continue;
            if (line.StartsWith("USE ", StringComparison.OrdinalIgnoreCase)) continue;
            if (line.StartsWith("SET ANSI_NULLS", StringComparison.OrdinalIgnoreCase)) continue;
            if (line.StartsWith("SET QUOTED_IDENTIFIER", StringComparison.OrdinalIgnoreCase)) continue;
            if (line.StartsWith("SET ANSI_PADDING", StringComparison.OrdinalIgnoreCase)) continue;
            if (line.StartsWith("SET NOCOUNT", StringComparison.OrdinalIgnoreCase)) continue;
            if (line.StartsWith("ALTER DATABASE ", StringComparison.OrdinalIgnoreCase)) continue;
            if (line.StartsWith("PRINT ", StringComparison.OrdinalIgnoreCase)) continue;
            if (line.StartsWith("EXEC ", StringComparison.OrdinalIgnoreCase)) continue;
            if (line.Contains("TEXTIMAGE_ON", StringComparison.OrdinalIgnoreCase)) continue;
            if (line.StartsWith("ON [PRIMARY]", StringComparison.OrdinalIgnoreCase)) continue;
            if (line.StartsWith("ON PRIMARY", StringComparison.OrdinalIgnoreCase)) continue;
            if (line.StartsWith("FILESTREAM_ON", StringComparison.OrdinalIgnoreCase)) continue;
            if (line.StartsWith("ALTER TABLE", StringComparison.OrdinalIgnoreCase) &&
                line.Contains("CHECK CONSTRAINT", StringComparison.OrdinalIgnoreCase)) continue;
            if (line.StartsWith("CREATE INDEX", StringComparison.OrdinalIgnoreCase)) continue;
            if (line.StartsWith("CREATE UNIQUE INDEX", StringComparison.OrdinalIgnoreCase)) continue;
            if (line.StartsWith("CREATE NONCLUSTERED INDEX", StringComparison.OrdinalIgnoreCase)) continue;
            if (line.StartsWith("CREATE CLUSTERED INDEX", StringComparison.OrdinalIgnoreCase)) continue;
            if (line.Contains("__EFMigrationsHistory", StringComparison.OrdinalIgnoreCase)) continue;
            if (line.StartsWith("IF SCHEMA_ID", StringComparison.OrdinalIgnoreCase)) continue;
            if (line.StartsWith("IF OBJECT_ID", StringComparison.OrdinalIgnoreCase)) continue;
            if (line.StartsWith("IF EXISTS", StringComparison.OrdinalIgnoreCase)) continue;
            if (line.StartsWith("IF NOT EXISTS", StringComparison.OrdinalIgnoreCase)) continue;
            if (line.StartsWith("BEGIN", StringComparison.OrdinalIgnoreCase)) continue;
            if (line.StartsWith("END", StringComparison.OrdinalIgnoreCase)) continue;


            kept.Add(raw);
        }

        text = string.Join("\n", kept);

        // Remove comments blocks and SQL Server noise
        text = Regex.Replace(text, @"/\*.*?\*/", string.Empty, RegexOptions.Singleline);
        text = Regex.Replace(text, @"--.*?$", string.Empty, RegexOptions.Multiline);

        // Remove SQL Server filegroup tails after CREATE TABLE
        text = Regex.Replace(text, @"\)\s*ON\s+PRIMARY", ")", RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"\)\s*ON\s+\[PRIMARY\]", ")", RegexOptions.IgnoreCase);

        // Remove clustered/nonclustered keywords from PK/unique definitions
        text = Regex.Replace(text, @"\bCLUSTERED\b", string.Empty, RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"\bNONCLUSTERED\b", string.Empty, RegexOptions.IgnoreCase);

        // Simplify SQL Server specific constraint syntax
        text = Regex.Replace(text, @"WITH\s+CHECK\s+ADD\s+CONSTRAINT", "ADD CONSTRAINT", RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"WITH\s+NOCHECK\s+ADD\s+CONSTRAINT", "ADD CONSTRAINT", RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"\bIDENTITY\s*\(\s*\d+\s*,\s*\d+\s*\)", string.Empty, RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"\s+COLLATE\s+\w+", string.Empty, RegexOptions.IgnoreCase);

        // Convert SQL Server identifiers [schema].[table] -> schema.table
        text = text.Replace("[", string.Empty).Replace("]", string.Empty);

        // Convert common SQL Server data types to generic types drawdb understands more easily
        text = Regex.Replace(text, @"\buniqueidentifier\b", "uuid", RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"\bbit\b", "boolean", RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"\browversion\b", "varbinary(8)", RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"\bdatetimeoffset\b", "datetime", RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"\bdatetime2\b", "datetime", RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"\bnvarchar\s*\(\s*max\s*\)", "text", RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"\bvarchar\s*\(\s*max\s*\)", "text", RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"\bvarbinary\s*\(\s*max\s*\)", "blob", RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"\bnvarchar\s*\(", "varchar(", RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"\bnchar\s*\(", "char(", RegexOptions.IgnoreCase);

        // Remove stray "ASC"/"DESC" inside PK/unique key column lists
        text = Regex.Replace(text, @"\s+ASC\b", string.Empty, RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"\s+DESC\b", string.Empty, RegexOptions.IgnoreCase);

        // Remove empty commas caused by cleanup
        text = Regex.Replace(text, @",\s*,", ",");
        text = Regex.Replace(text, @",\s*\)", "\n)");
        text = Regex.Replace(text, @"\(\s*,", "(");
        text = Regex.Replace(text, @"IF\s+SCHEMA_ID\s*\([^\)]*\)\s+IS\s+NULL\s+EXEC\s*\([^\)]*\)\s*;?", string.Empty, RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"IF\s+OBJECT_ID\s*\([^\)]*\)\s+IS\s+NULL\s*;?", string.Empty, RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"^\s*IF\s+.*$", string.Empty, RegexOptions.Multiline | RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"^\s*BEGIN\s*$", string.Empty, RegexOptions.Multiline | RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"^\s*END\s*$", string.Empty, RegexOptions.Multiline | RegexOptions.IgnoreCase);


        // Compact excessive blank lines
        text = Regex.Replace(text, @"\n{3,}", "\n\n");

        return text.Trim() + Environment.NewLine;
    }
}

public sealed class DrawDbSchemaExportResponse
{
    public bool Ok { get; set; }
    public string FileName { get; set; } = "";
    public string FilePath { get; set; } = "";
    public string RelativeFilePath { get; set; } = "";
    public string? DownloadUrl { get; set; }
    public int LineCount { get; set; }
    public int CharacterCount { get; set; }
    public List<string> Notes { get; set; } = new();
}
