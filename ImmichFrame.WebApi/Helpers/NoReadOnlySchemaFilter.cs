using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace ImmichFrame.WebApi.Helpers;

/// <summary>
/// <see cref="Models.ClientSettingsDto"/> uses get-only forwarding properties, which
/// Swashbuckle marks as <c>readOnly</c>. oazapfts then splits the type into a separate
/// <c>*Read</c> variant and churns the generated SPA client. The DTO is response-only,
/// so the flag carries no information — strip it for that type.
/// </summary>
public class NoReadOnlySchemaFilter : ISchemaFilter
{
    public void Apply(IOpenApiSchema schema, SchemaFilterContext context)
    {
        if (context.Type != typeof(Models.ClientSettingsDto))
            return;

        if (schema is not OpenApiSchema openApiSchema || openApiSchema.Properties is null)
            return;

        foreach (var property in openApiSchema.Properties.Values)
        {
            if (property is OpenApiSchema propertySchema)
                propertySchema.ReadOnly = false;
        }
    }
}
