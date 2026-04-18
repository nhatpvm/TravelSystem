using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace TicketBooking.Api.Swagger;

public sealed class TenantHeaderOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (HasAllowAnonymous(context))
            return;

        if (IsPublicTenantInferredRoute(context.ApiDescription.RelativePath))
            return;

        if (!ShouldShowTenantHeader(context.ApiDescription.RelativePath))
            return;

        if (operation.Parameters is null)
            operation.Parameters = new List<IOpenApiParameter>();

        operation.Parameters = operation.Parameters
            .Where(p => !string.Equals(p.Name, "X-TenantId", StringComparison.OrdinalIgnoreCase))
            .Where(p => !string.Equals(p.Name, "X-TenantCode", StringComparison.OrdinalIgnoreCase))
            .ToList();

        operation.Parameters.Add(new OpenApiParameter
        {
            Name = "X-TenantId",
            In = ParameterLocation.Header,
            Required = IsTenantIdRequired(context.ApiDescription.RelativePath, context.ApiDescription.HttpMethod),
            Description = BuildTenantIdDescription(context.ApiDescription.RelativePath, context.ApiDescription.HttpMethod),
            Schema = new OpenApiSchema
            {
                Type = JsonSchemaType.String,
                Format = "uuid"
            }
        });
    }

    private static bool HasAllowAnonymous(OperationFilterContext context)
    {
        var actionAttrs = context.MethodInfo.GetCustomAttributes(true);
        if (actionAttrs.OfType<AllowAnonymousAttribute>().Any())
            return true;

        var controllerAttrs = context.MethodInfo.DeclaringType?.GetCustomAttributes(true);
        return controllerAttrs?.OfType<AllowAnonymousAttribute>().Any() == true;
    }

    private static bool ShouldShowTenantHeader(string? relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
            return false;

        var path = relativePath.Split('?', 2)[0].ToLowerInvariant();

        return path.Contains("/admin/")
               || path.Contains("/qlnx/")
               || path.Contains("/qlvt/")
               || path.Contains("/qlvmm/")
               || path.Contains("/qltour/")
               || path.Contains("/qlks/");
    }

    private static bool IsTenantIdRequired(string? relativePath, string? httpMethod)
    {
        return IsAdminRoute(relativePath) && IsWriteMethod(httpMethod);
    }

    private static string BuildTenantIdDescription(string? relativePath, string? httpMethod)
    {
        if (IsAdminRoute(relativePath))
        {
            if (IsWriteMethod(httpMethod))
                return "Tenant context header (GUID). Required for admin write requests (POST, PUT, PATCH, DELETE).";

            return "Tenant context header (GUID). Optional for admin read requests. Supply it to scope results to one tenant.";
        }

        if (IsManagerRoute(relativePath))
        {
            return "Tenant context header (GUID). Optional when the signed-in manager belongs to exactly one tenant; required if the account belongs to multiple tenants.";
        }

        return "Tenant context header (GUID).";
    }

    private static bool IsAdminRoute(string? relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
            return false;

        return relativePath.Split('?', 2)[0].Contains("/admin/", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsManagerRoute(string? relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
            return false;

        var path = relativePath.Split('?', 2)[0].ToLowerInvariant();

        return path.Contains("/qlnx/")
               || path.Contains("/qlvt/")
               || path.Contains("/qlvmm/")
               || path.Contains("/qltour/")
               || path.Contains("/qlks/");
    }

    private static bool IsWriteMethod(string? httpMethod)
    {
        return string.Equals(httpMethod, "POST", StringComparison.OrdinalIgnoreCase)
               || string.Equals(httpMethod, "PUT", StringComparison.OrdinalIgnoreCase)
               || string.Equals(httpMethod, "PATCH", StringComparison.OrdinalIgnoreCase)
               || string.Equals(httpMethod, "DELETE", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsPublicTenantInferredRoute(string? relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
            return false;

        var path = relativePath.Split('?', 2)[0];

        var segments = path
            .Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (segments.Length < 4)
            return false;

        if (!string.Equals(segments[0], "api", StringComparison.OrdinalIgnoreCase))
            return false;

        if (!segments[1].StartsWith("v", StringComparison.OrdinalIgnoreCase))
            return false;

        var module = segments[2];

        if (string.Equals(module, "bus", StringComparison.OrdinalIgnoreCase))
        {
            if (string.Equals(segments[3], "search", StringComparison.OrdinalIgnoreCase))
                return true;

            if (string.Equals(segments[3], "seat-holds", StringComparison.OrdinalIgnoreCase))
                return true;

            if (segments.Length >= 5 &&
                string.Equals(segments[3], "trips", StringComparison.OrdinalIgnoreCase) &&
                string.Equals(segments[^1], "seats", StringComparison.OrdinalIgnoreCase))
                return true;
        }

        if (string.Equals(module, "train", StringComparison.OrdinalIgnoreCase))
        {
            if (string.Equals(segments[3], "search", StringComparison.OrdinalIgnoreCase))
                return true;

            if (string.Equals(segments[3], "seat-holds", StringComparison.OrdinalIgnoreCase))
                return true;

            if (segments.Length >= 5 &&
                string.Equals(segments[3], "trips", StringComparison.OrdinalIgnoreCase) &&
                string.Equals(segments[^1], "seats", StringComparison.OrdinalIgnoreCase))
                return true;
        }

        if (string.Equals(module, "flight", StringComparison.OrdinalIgnoreCase))
        {
            if (string.Equals(segments[3], "search", StringComparison.OrdinalIgnoreCase))
                return true;

            if (string.Equals(segments[3], "offers", StringComparison.OrdinalIgnoreCase))
                return true;

            if (string.Equals(segments[3], "cabin-seat-maps", StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }
}
