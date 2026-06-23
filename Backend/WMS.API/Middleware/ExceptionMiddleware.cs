namespace WMS.API.Middleware;

public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;

    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception occurred. Inner exception: {InnerException}", ex.InnerException?.Message);
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            var environment = context.RequestServices.GetRequiredService<IWebHostEnvironment>();
            var errors = environment.IsDevelopment()
                ? new[] { ex.Message, ex.InnerException?.Message }.Where(message => !string.IsNullOrWhiteSpace(message)).ToArray()
                : new[] { "An unexpected server error occurred." };

            await context.Response.WriteAsJsonAsync(new
            {
                success = false,
                message = "An unexpected error occurred.",
                errors
            });
        }
    }
}
