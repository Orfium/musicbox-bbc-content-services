using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using MusicManager.Application;
using MusicManager.Core.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace MusicManager.Infrastructure.MiddleWares
{
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionMiddleware> _logger;
       


        public ExceptionMiddleware(RequestDelegate next,
            ILogger<ExceptionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
           
        }

        public async Task Invoke(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch(Exception ex)
            {
                context.Response.StatusCode = 400;
                var method = ex.TargetSite.ReflectedType.FullName;
                _logger.LogError(ex, string.Format("{0} -> {1}", ex.Message, method));
            }
        }
    }
}
