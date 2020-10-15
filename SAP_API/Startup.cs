using System;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Reflection;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using SAP_API.Entities;
using SAP_API.Models;

namespace SAP_API {
    public class Startup {
        public Startup(IConfiguration configuration) {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services) {

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

            // ===== Add our DbContext ========
            services.AddDbContext<ApplicationDbContext>();

            // ===== Add Identity ========
            services.AddIdentity<User, IdentityRole>(options => {
                options.ClaimsIdentity.UserIdClaimType = "UserID";
            })
                //.AddDefaultUI()
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddDefaultUI()
                .AddDefaultTokenProviders();

            // ===== Add Jwt Authentication ========
            JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear(); // => remove default claims
            services
                .AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;

                })
                .AddJwtBearer(cfg =>
                {
                    cfg.RequireHttpsMetadata = false;
                    cfg.SaveToken = true;
                    cfg.TokenValidationParameters = new TokenValidationParameters
                    {
                        //ValidIssuers = Configuration["JwtIssuer"],
                        //ValidAudience = Configuration["JwtIssuer"],
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Configuration["JwtKey"])),
                        ValidateIssuer = false,
                        ValidateAudience = false,
                        ClockSkew = TimeSpan.Zero, // remove delay of token when expire
                        RoleClaimType = "role",
                        NameClaimType = "name",
                    };
                });

            services.AddMvc(options => {
                options.Filters.Add(new ResponseCacheAttribute { NoStore = true, Location = ResponseCacheLocation.None });
            }).AddJsonOptions(options => {
                options.SerializerSettings.ContractResolver = new DefaultContractResolver();
            }).SetCompatibilityVersion(CompatibilityVersion.Version_2_1);
            
            //services.Add(new ServiceDescriptor(typeof(SAPContext[]), new SAPContext[4]));
            services.Add(new ServiceDescriptor(typeof(SAPContext), new SAPContext()));

            services.AddSwaggerGen(c =>
            {
                //c.SwaggerDoc("v1", new OpenApiInfo
                //{
                //    Version = "v1",
                //    Title = "ToDo API",
                //    Description = "A simple example ASP.NET Core Web API",
                //    TermsOfService = new Uri("https://example.com/terms"),
                //    Contact = new OpenApiContact
                //    {
                //        Name = "Shayne Boyer",
                //        Email = string.Empty,
                //        Url = new Uri("https://twitter.com/spboyer"),
                //    },
                //    License = new OpenApiLicense
                //    {
                //        Name = "Use under LICX",
                //        Url = new Uri("https://example.com/license"),
                //    }
                //});

                // Set the comments path for the Swagger JSON and UI.
                var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                c.IncludeXmlComments(xmlPath);
            });

            services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();
            services.AddAuthorization(options =>
            {
                options.AddPolicy(Permission.Warehouses.View, builder =>
                {
                    builder.AddRequirements(new PermissionRequirement(Permission.Warehouses.View));
                });

                options.AddPolicy(Permission.Warehouses.Create, builder =>
                {
                    builder.AddRequirements(new PermissionRequirement(Permission.Warehouses.Create));
                });
            });

        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ApplicationDbContext dbContext) {

            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "My Api V1");
            });
            app.UseStatusCodePages();

            if (env.IsDevelopment()) {
                app.UseDeveloperExceptionPage();
            } else {
                app.UseHsts();
            }
            app.UseCors("CORS");
            app.UseHttpsRedirection();

            /*
            SAPContext[] SAPContext = app.ApplicationServices.GetService(typeof(SAPContext[])) as SAPContext[];

            for (int i = 0; i< SAPContext.Length; i++) {
                SAPContext[i] = new SAPContext();
            }

            app.UseWhen(context => !context.Request.Path.Value.Contains("values"), action => {
                action.Use(async (context, next) => {
                    for (int i = 0; i < SAPContext.Length; i++)
                    {
                        if (!SAPContext[i].oCompany.Connected)
                        {
                            int code = SAPContext[i].oCompany.Connect();
                            if (code != 0)
                            {
                                string error = SAPContext[i].oCompany.GetLastErrorDescription();
                                var result = JsonConvert.SerializeObject(new { error });
                                context.Response.ContentType = "application/json";
                                context.Response.StatusCode = 400;
                                await context.Response.WriteAsync(result);
                                return;
                            }
                        }
                    }
                    await next();
                });
            });

            */

            SAPContext SAPContext = app.ApplicationServices.GetService(typeof(SAPContext)) as SAPContext;

            app.UseWhen(context => !context.Request.Path.Value.Contains("values"), action => {
                action.Use(async (context, next) => {
                    if (!SAPContext.oCompany.Connected) {
                        int code = SAPContext.oCompany.Connect();
                        if (code != 0)
                        {
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

            app.UseAuthentication();
            app.UseMvc();

            // ===== Create tables ======
            dbContext.Database.EnsureCreated();
        }
    }
}
