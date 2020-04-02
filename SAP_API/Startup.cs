using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using SAP_API.Models;

namespace SAP_API
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddCors(options =>
            {
                options.AddPolicy("CORS",
                builder =>
                {
                    builder.WithOrigins("*")
                                        .AllowAnyHeader()
                                        .AllowAnyMethod();
                });
            });
            services.AddMvc()
                .AddJsonOptions(options => {options.SerializerSettings.ContractResolver = new DefaultContractResolver();})
                .SetCompatibilityVersion(CompatibilityVersion.Version_2_1);
            services.Add(new ServiceDescriptor(typeof(SAPContext), new SAPContext()));

        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment()) {
                app.UseDeveloperExceptionPage();
            } else {
                app.UseHsts();
            }
            app.UseCors("CORS");
            app.UseHttpsRedirection();

            SAPContext SAPContext = app.ApplicationServices.GetService(typeof(SAPContext)) as SAPContext;
            app.UseWhen(context => !context.Request.Path.Value.Contains("value"), action => {
                action.Use(async (context, next) => {
                    if (!SAPContext.oCompany.Connected) {
                        int code = SAPContext.oCompany.Connect();
                        if (code != 0) {
                            string error = SAPContext.oCompany.GetLastErrorDescription();
                            var result = JsonConvert.SerializeObject(new { error });
                            context.Response.ContentType = "application/json";
                            context.Response.StatusCode = 400;
                            await context.Response.WriteAsync(result);
                            return;
                        }
                    }
                    await next();
                });
            });
           
            app.UseMvc();
        }
    }
}
