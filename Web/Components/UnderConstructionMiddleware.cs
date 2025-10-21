using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;

namespace Web.Components
{

    public class UnderConstructionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IConfiguration _configuration;

        public UnderConstructionMiddleware(RequestDelegate next, IConfiguration configuration)
        {
            _next = next;
            _configuration = configuration;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var path = context.Request.Path.Value?.ToLower() ?? "";

            // SKIP API ROUTES
            if (path.StartsWith("/api/"))
            {
                await _next(context);
                return;
            }

            var isUnderConstruction = _configuration.GetValue<bool>("UnderConstruction");
            if (isUnderConstruction && !context.Request.Path.StartsWithSegments("/admin"))
            {
                context.Response.ContentType = "text/html";
                await context.Response.WriteAsync(@"
         <html>
         <head><title>Under Construction</title></head>
         <body style='text-align:center; padding-top:100px; font-family:Arial;'>
             <h1> Under Construction</h1>
             <p>We're working on something awesome. Please check back soon!</p>
         </body>
         </html>");
                return;
            }
            await _next(context);
        }
    }
}


