using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace ImmichFrame.WebApi.Helpers;

/// <summary>
/// Response DTOs use get-only forwarding properties (e.g. <see cref="Models.ClientSettingsDto"/>),
/// which Swashbuckle marks as <c>readOnly</c>. oazapfts then splits every such type into a
/// separate <c>*Read</c> variant and churns the generated SPA client. All DTOs here are
/// response-only, so the flag carries no information — strip it.
/// </summary>
public class NoReadOnlySchemaFilter : ISchemaFilter
{
    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        foreach (var property in schema.Properties.Values)
        {
            property.ReadOnly = false;
        }
    }
}
