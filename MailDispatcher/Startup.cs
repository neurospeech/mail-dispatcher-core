using MailDispatcher.Core.Auth;
using MailDispatcher.Core.Insights;
using MailDispatcher.Core.Serialization;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.Caching;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.PlatformAbstractions;
using Microsoft.Net.Http.Headers;
using NeuroSpeech;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MailDispatcher
{
    public class Startup
    {
        public readonly IConfiguration Configuration;

        public Startup(IConfiguration configuration)
        {
            this.Configuration = configuration;
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            var insightsInstalled = AddApplicationInsights(services);

            // Register all services marked with DIRegister attribute
            services.RegisterAssembly(typeof(Startup).Assembly);

            services.AddHttpContextAccessor();

            services.AddHttpClient();
            services.AddMemoryCache();
            services.AddSingleton(typeof(AppCache<>));

            services.AddMvc(c => {
                if(insightsInstalled)
                    c.Filters.Add<CustomErrorHandler>();
                c.Filters.Add<DisallowAnonymousFilter>();
            })
            .AddRazorPagesOptions(options => {
                options.RootDirectory = "/Pages";
            })
            .SetCompatibilityVersion(CompatibilityVersion.Version_3_0)
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
                            // options.JsonSerializerOptions.date = Newtonsoft.Json.DateFormatHandling.IsoDateFormat;
                            // options.SerializerSettings.DateTimeZoneHandling = Newtonsoft.Json.DateTimeZoneHandling.Utc;
                            options.JsonSerializerOptions.Converters.Add(new DateTimeConverter());
                options.JsonSerializerOptions.IgnoreNullValues = true;
                // SerializerSettings = options.SerializerSettings;
            });

            services
                .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie((c) => {
                    c.SlidingExpiration = true;
                    c.Cookie.Name = "Auth.MD";
                    c.Cookie.HttpOnly = true;
                    c.ExpireTimeSpan = TimeSpan.FromDays(7);
                });

            services.AddResponseCompression((options) => {
                options.EnableForHttps = true;
                options.Providers.Add<GzipCompressionProvider>();
            });

            services.AddResponseCaching((options) => {
            });

            services.AddSwaggerGen(c => {
                c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
                {
                    Title = "WebAtoms API",
                    Version = "v1"
                });
                var dir = new System.IO.DirectoryInfo(PlatformServices.Default.Application.ApplicationBasePath);
                foreach (var file in dir.EnumerateFiles("*.xml"))
                {
                    c.IncludeXmlComments(file.FullName);
                }
            });
        }

        private bool AddApplicationInsights(IServiceCollection services)
        {
            var ai = Configuration.GetSection("ApplicationInsights");
            if (ai.Exists())
            {
                var authKey = ai.GetValue<string>("AuthKey", null);
                if (authKey != null)
                {
                    services.ConfigureTelemetryModule<Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector.QuickPulse.QuickPulseTelemetryModule>((m, o) =>
                    {
                        m.AuthenticationApiKey = authKey;
                    });
                }
                services.AddApplicationInsightsTelemetry(x => x.InstrumentationKey = ai.GetValue<string>("InstrumentationKey"));
                return true;
            }
            return false;
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {

            app.UseDeveloperExceptionPage();


            app.UseSwagger();

            app.UseSwaggerUI(c => {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1");
            });

            app.UseResponseCaching();

            app.UseResponseCompression();

            app.UseRouting();
            app.UseAuthentication();

            app.UseCookiePolicy(new CookiePolicyOptions
            {
                MinimumSameSitePolicy = Microsoft.AspNetCore.Http.SameSiteMode.None,
                Secure = env.IsDevelopment() ? CookieSecurePolicy.None : CookieSecurePolicy.Always,
                HttpOnly = Microsoft.AspNetCore.CookiePolicy.HttpOnlyPolicy.Always
            });

            app.UseDefaultFiles();

            app.UseEndpoints((endpoints) => {
                endpoints.MapControllers();
                endpoints.MapRazorPages();
                //endpoints.MapHub<ConsoleHub>("/play-console", (a) => {
                //});
                endpoints.MapFallback((r) => {
                    r.Response.Redirect("/index.html");
                    return Task.CompletedTask;
                });
            });
        }
    }
}
