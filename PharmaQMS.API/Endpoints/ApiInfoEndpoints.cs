namespace PharmaQMS.API.Endpoints;

public static class ApiInfoEndpoints
{
    public static IEndpointRouteBuilder MapApiInfoEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1").WithTags("API Info");

        group.MapGet("/status", () => Results.Ok(new
        {
            service = "PharmaQMS.API",
            status = "Running",
            availableEndpoints = new[]
            {
                "POST /api/v1/auth/login",
                "POST /api/v1/auth/refresh",
                "POST /api/v1/auth/revoke",
                "GET /api/v1/status"
            }
        }))
        .WithName("GetApiStatus");

        group.MapGet("/swagger", () => Results.Content("""
<!doctype html>
<html lang="en">
<head>
  <meta charset="utf-8" />
  <meta name="viewport" content="width=device-width, initial-scale=1" />
  <title>PharmaQMS API Swagger</title>
  <link rel="stylesheet" href="https://unpkg.com/swagger-ui-dist@5/swagger-ui.css" />
</head>
<body>
  <div id="swagger-ui"></div>
  <script src="https://unpkg.com/swagger-ui-dist@5/swagger-ui-bundle.js"></script>
  <script src="https://unpkg.com/swagger-ui-dist@5/swagger-ui-standalone-preset.js"></script>
  <script>
    window.onload = () => {
      window.ui = SwaggerUIBundle({
        url: '/openapi/v1.json',
        dom_id: '#swagger-ui',
        deepLinking: true,
        presets: [SwaggerUIBundle.presets.apis, SwaggerUIStandalonePreset],
        layout: 'BaseLayout'
      });
    };
  </script>
</body>
</html>
""", "text/html"))
        .WithName("GetSwaggerUi")
        .ExcludeFromDescription();

        return app;
    }
}
