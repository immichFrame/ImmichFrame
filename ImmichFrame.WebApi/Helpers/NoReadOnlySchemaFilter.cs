using Microsoft.OpenApi.Models;
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
    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        if (context.Type != typeof(Models.ClientSettingsDto))
            return;

        foreach (var property in schema.Properties.Values)
        {
            property.ReadOnly = false;
        }
    }
}
