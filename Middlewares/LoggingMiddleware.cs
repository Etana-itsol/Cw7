using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cw5.Middlewares
{
    public class LoggingMiddleware
    {
        private readonly RequestDelegate _next;

        public LoggingMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext httpContext)
        {
            httpContext.Request.EnableBuffering();

            if (httpContext.Request != null)
            {
                string sciezka = httpContext.Request.Path; //"weatherforecast/c os"
                string metoda = httpContext.Request.Method.ToString();//GET,POST...
                string querystring = httpContext.Request?.QueryString.ToString();
                string bodyStr = "";

                using (StreamReader reader
                 = new StreamReader(httpContext.Request.Body, Encoding.UTF8, true, 1024, true))
                {
                    bodyStr = await reader.ReadToEndAsync();
                    httpContext.Request.Body.Position = 0;
                }

                var writer = new FileStream("requestLog.txt", FileMode.Create);

                using (var logwriter = new StreamWriter(writer))
                {
                    string text = $"Path: {sciezka} \nQueryString:{querystring} \nMethod: {metoda} \nBody Parameters: {bodyStr}";
                    logwriter.WriteLine(text);
                }
            }

            await _next(httpContext);
        }
    }
}
