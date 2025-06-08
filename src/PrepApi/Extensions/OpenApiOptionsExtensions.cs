using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi.Models;

namespace PrepApi.Extensions;

public static class OpenApiOptionsExtensions
{
    public static OpenApiOptions AddBearerTokenAuthentication(this OpenApiOptions options)
    {
        const string schemeName = "Bearer";

        var scheme = new OpenApiSecurityScheme()
        {
            Type = SecuritySchemeType.Http,
            Name = schemeName,
            Scheme = schemeName,
            Reference = new()
            {
                Type = ReferenceType.SecurityScheme,
                Id = schemeName
            }
        };
        
        options.AddDocumentTransformer((document, _, _) =>
        {
            document.Components ??= new();
            document.Components.SecuritySchemes.Add(schemeName, scheme);
            return Task.CompletedTask;
        });
        
        options.AddOperationTransformer((operation, context, _) =>
        {
            if (context.Description.ActionDescriptor.EndpointMetadata.OfType<IAuthorizeData>().Any())
            {
                operation.Security = [new() { [scheme] = [] }];
            }

            return Task.CompletedTask;
        });
        
        return options;
    }
}