using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Internal;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace SAP_API.Middlewares
{

    // You may need to install the Microsoft.AspNetCore.Http.Abstractions package into your project
    public class LogMiddleware
    {
        private readonly RequestDelegate _next;

        public LogMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {

            var timeRequest = DateTime.Now;

            using (MemoryStream requestBodyStream = new MemoryStream())
            {
                using (MemoryStream responseBodyStream = new MemoryStream())
                {
                    Stream originalRequestBody = context.Request.Body;
                    context.Request.EnableRewind();
                    Stream originalResponseBody = context.Response.Body;

                    try
                    {
                        await context.Request.Body.CopyToAsync(requestBodyStream);
                        requestBodyStream.Seek(0, SeekOrigin.Begin);

                        string requestBodyText = new StreamReader(requestBodyStream).ReadToEnd();

                        requestBodyStream.Seek(0, SeekOrigin.Begin);
                        context.Request.Body = requestBodyStream;

                        string responseBody = "";

                        context.Response.Body = responseBodyStream;

                        Stopwatch watch = Stopwatch.StartNew();
                        await _next(context);
                        watch.Stop();

                        responseBodyStream.Seek(0, SeekOrigin.Begin);
                        responseBody = new StreamReader(responseBodyStream).ReadToEnd();

                        string Consola = "\r\n" +
                            "-------------------------------------------------------------------------------------------------\r\n" +
                            $"To: {context.Request.Host.Host}\r\n" +
                            $"Route: {context.Request.Path}\r\n" +
                            $"From: {context.Connection.RemoteIpAddress.MapToIPv4().ToString()}\r\n" +
                            $"Request Body: {requestBodyText}\r\n" +
                            $"Status Code: {context.Response.StatusCode}\r\n" +
                            $"Response Body: {responseBody}\r\n" +
                            $"Date Begin: {timeRequest}\r\n" +
                            $"Date Finish: {DateTime.Now}\r\n" +
                            $"Time: {watch.Elapsed.TotalMilliseconds} ms\r\n";
                        string Sobrepasa = watch.Elapsed.TotalMilliseconds > 300000 ? "Sobrepasa: True" : "Sobrepasa: False";
                        Consola += Sobrepasa;
                        Console.WriteLine(Consola);

                        responseBodyStream.Seek(0, SeekOrigin.Begin);

                        await responseBodyStream.CopyToAsync(originalResponseBody);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"ERROR: {ex.Message}\n{ex.StackTrace}");
                        byte[] data = System.Text.Encoding.UTF8.GetBytes("Unhandled Error occured, the error has been logged and the persons concerned are notified!! Please, try again in a while.");
                        originalResponseBody.Write(data, 0, data.Length);
                    }
                    finally
                    {
                        context.Request.Body = originalRequestBody;
                        context.Response.Body = originalResponseBody;
                    }
                }
            }

        }
    }

    // Extension method used to add the middleware to the HTTP request pipeline.
    public static class LogMiddlewareExtensions
    {
        public static IApplicationBuilder UseLogMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<LogMiddleware>();
        }
    }
}
