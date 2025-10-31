using Microsoft.AspNetCore.Builder;

namespace R2.ShopNet.Catalog.API.Extensions;

using R2.ShopNet.Catalog.API.Endpoints;

public static class EndpointsExtension
{
    public static void RegisterCatalogApiEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.RegisterCategoryEndpoints();
        endpoints.RegisterProductEndpoints();
    }
}
