using System.Net;
using System.Text.Json;

namespace CompanyMng.Middleware
{
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionMiddleware> _logger;


        public ExceptionMiddleware(
            RequestDelegate next,
            ILogger<ExceptionMiddleware> logger)
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

            catch(Exception ex)
            {
                _logger.LogError(ex, "Beklenmeyen bir hata oluştu.");

                var problemDetailsService = context.RequestServices.GetRequiredService<IProblemDetailsService>();


                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                  

                await problemDetailsService.WriteAsync(
                    new ProblemDetailsContext
                    {
                        HttpContext = context,
                        ProblemDetails =
                    {
                        Status = StatusCodes.Status500InternalServerError,
                        Title = "Sunucu hatası",
                         Detail = "Beklenmeyen bir hata oluştu."
                    }
                    });

            }




        }


    }
}
