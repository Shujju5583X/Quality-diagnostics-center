using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace LabSystem.Api.Middleware
{
    public class ApiKeyAuthMiddleware
    {
        private const string API_KEY_HEADER = "X-Api-Key";
        private readonly RequestDelegate _next;
        private readonly string _validApiKey;

        public ApiKeyAuthMiddleware(RequestDelegate next, IConfiguration configuration)
        {
            _next = next;
            _validApiKey = configuration["Api:ApiKey"] ?? "default-dev-key";
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (!context.Request.Headers.TryGetValue(API_KEY_HEADER, out var extractedApiKey))
            {
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync("API Key missing.");
                return;
            }

            if (!_validApiKey.Equals(extractedApiKey))
            {
                context.Response.StatusCode = 403;
                await context.Response.WriteAsync("Invalid API Key.");
                return;
            }

            await _next(context);
        }
    }
}
