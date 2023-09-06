using Microsoft.OpenApi.Models;

using Pullaroo.Contracts.StronglyTypedIds;

using Swashbuckle.AspNetCore.SwaggerGen;

namespace Pullaroo.Server;

public class StronglyTypedIdSchemaFilter : ISchemaFilter
{
    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        if (StronglyTypedIdHelper.IsStronglyTypedId(context.Type))
        {
            schema.Type = "string";
            schema.Format = null; // You can optionally specify format if it's something like "uuid"
        }
    }
}
