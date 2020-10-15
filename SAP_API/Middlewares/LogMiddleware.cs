using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Internal;

namespace SAP_API.Middlewares {

    // You may need to install the Microsoft.AspNetCore.Http.Abstractions package into your project
    public class LogMiddleware {
        private readonly RequestDelegate _next;

        public LogMiddleware(RequestDelegate next) {
            _next = next;
        }

        public async Task Invoke(HttpContext context) {

            var timeRequest = DateTime.Now;

            using (MemoryStream requestBodyStream = new MemoryStream()) {
                using (MemoryStream responseBodyStream = new MemoryStream()) {
                    Stream originalRequestBody = context.Request.Body;
                    context.Request.EnableRewind();
                    Stream originalResponseBody = context.Response.Body;

                    try {
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

                        Console.WriteLine(
                            "-------------------------------------------------------------------------------------------------\n" +
                            $"To: {context.Request.Host.Host}\n" +
                            $"Route: {context.Request.Path}\n" +
                            $"From: {context.Connection.RemoteIpAddress.MapToIPv4().ToString()}\n" +
                            $"Request Body: {requestBodyText}\n" +
                            $"Status Code: {context.Response.StatusCode}\n" +
                            $"Response Body: {responseBody}\n" +
                            $"Date Begin: {timeRequest}\n" +
                            $"Date Finish: {DateTime.Now}\n" +
                            $"time: {watch.Elapsed.TotalMilliseconds} ms");

                        responseBodyStream.Seek(0, SeekOrigin.Begin);

                        await responseBodyStream.CopyToAsync(originalResponseBody);
                    }
                    catch (Exception ex) {
                        Console.WriteLine($"ERROR: {ex.Message}\n{ex.StackTrace}");
                        byte[] data = System.Text.Encoding.UTF8.GetBytes("Unhandled Error occured, the error has been logged and the persons concerned are notified!! Please, try again in a while.");
                        originalResponseBody.Write(data, 0, data.Length);
                    }
                    finally {
                        context.Request.Body = originalRequestBody;
                        context.Response.Body = originalResponseBody;
                    }
                }
            }
            
        }
    }

    // Extension method used to add the middleware to the HTTP request pipeline.
    public static class LogMiddlewareExtensions {
        public static IApplicationBuilder UseLogMiddleware(this IApplicationBuilder builder) {
            return builder.UseMiddleware<LogMiddleware>();
        }
    }
}
