using Microsoft.ApplicationInsights;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace MailDispatcher.Core.Insights
{
    /// <summary>
    /// 
    /// </summary>
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public class CustomErrorHandler : IExceptionFilter
    {

        public void OnException(ExceptionContext context)
        {
            if (!context.ExceptionHandled)
            {
                TelemetryClient client = context.HttpContext.RequestServices.GetRequiredService<TelemetryClient>();
                client.TrackException(context.Exception);
            }

            context.Result = new ContentResult
            {
                Content = context.Exception.ToString(),
                ContentType = "text/plain",
                StatusCode = 500,
            };
        }
    }
}
