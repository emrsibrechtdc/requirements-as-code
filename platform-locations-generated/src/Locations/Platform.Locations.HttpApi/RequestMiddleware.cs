using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Platform.Shared.MultiProduct;
using Serilog.Context;

namespace Platform.Locations.HttpApi;

public class RequestMiddleware
{
    private readonly RequestDelegate _next;

    public RequestMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task Invoke(HttpContext context,
        IMultiProductRequestContextProvider requestContextProvider,
        ILogger<RequestMiddleware> logger,
        IWebHostEnvironment environment)
    {
        // Set correlationId
        string correlationId = GenerateOrExtractCorrelationId(context);
        
        // Set product context - this feeds into Platform.Shared's multi-product system
        await SetProductContext(context, requestContextProvider, environment);
        
        // Process request with logging context
        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            await _next(context); // Platform.Shared components automatically use the product context
        }
    }

    private string GenerateOrExtractCorrelationId(HttpContext context)
    {
        const string correlationIdHeaderName = "X-Request-Id";
        
        if (context.Request.Headers.TryGetValue(correlationIdHeaderName, out var correlationId))
        {
            return correlationId.ToString();
        }

        var newCorrelationId = Guid.NewGuid().ToString();
        context.Response.Headers[correlationIdHeaderName] = newCorrelationId;
        return newCorrelationId;
    }

    private async Task SetProductContext(
        HttpContext context, 
        IMultiProductRequestContextProvider contextProvider, 
        IWebHostEnvironment environment)
    {
        string? product = null;
        
        if (!environment.IsDevelopment())
        {
            // Production: Extract product from user claims or API key
            if (context.User.Identity!.IsAuthenticated)
            {
                var productClaim = context.User.FindFirst("product")?.Value;
                if (!string.IsNullOrEmpty(productClaim))
                {
                    product = productClaim;
                }
            }
        }
        else
        {
            // Development: Use local-product header for testing
            product = context.Request.Headers["local-product"].FirstOrDefault();
        }
        
        if (!string.IsNullOrEmpty(product))
        {
            // This sets the context that Platform.Shared uses for data segregation
            contextProvider.SetProduct(product);
        }
    }
}