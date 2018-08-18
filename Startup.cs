using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Swashbuckle.AspNetCore.Swagger;
using System;
using System.Collections.Generic;

namespace SwaggerAuthDemo
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
            services.AddMvc(options =>
            {
                var policy = new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .AddAuthenticationSchemes(CookieAuthenticationDefaults.AuthenticationScheme, JwtBearerDefaults.AuthenticationScheme, OpenIdConnectDefaults.AuthenticationScheme)
                    .Build();
                options.Filters.Add(new AuthorizeFilter(policy));
            }).SetCompatibilityVersion(CompatibilityVersion.Version_2_1);

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.Audience = $"{Configuration["AzureAd:Audience"]}";
                options.Authority = $"https://login.microsoftonline.com/{Configuration["AzureAd:DirectoryId"]}/";
            })
            .AddAzureAd(options => Configuration.Bind("AzureAd", options))
            .AddCookie();

            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new Info { Title = "My API", Version = "v1" });
                c.OperationFilter<AuthorizationHeaderParameterOperationFilter>();
                c.AddSecurityDefinition("oauth2", new OAuth2Scheme
                {
                    Type = "oauth2",
                    AuthorizationUrl = $"https://login.microsoftonline.com/{Configuration["AzureAd:DirectoryId"]}/oauth2/authorize",
                    TokenUrl = $"https://login.microsoftonline.com/{Configuration["AzureAd:DirectoryId"]}/oauth2/token",
                    Flow = "implicit"
                });
                c.AddSecurityRequirement(new Dictionary<string, IEnumerable<string>> {
                    { "oauth2", new string []  { "user_impersonation" } }
                });
            });

            services.AddCors(options =>
            {
                options.AddPolicy("CorsPolicy",
                                build => build.AllowAnyOrigin()
                                    .AllowAnyMethod()
                                    .AllowCredentials()
                                    .AllowAnyHeader());
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseHsts();
            }
            // ORDER MATTERS!
            app.UseHttpsRedirection();
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.RoutePrefix = String.Empty;
                c.DocExpansion(DocExpansion.None);
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1");
                c.OAuth2RedirectUrl($"{Configuration["AzureAd:OAuth2RedirectUrl"]}");
                c.OAuthClientId($"{Configuration["AzureAd:ClientId"]}");
                c.OAuthAdditionalQueryStringParams(new Dictionary<string, string>()
                {
                   { "resource", $"{Configuration["AzureAd:Audience"]}" }
                });
                c.OAuthScopeSeparator(" ");
            });
            app.UseCors("CorsPolicy");
    
            app.UseAuthentication();
            app.UseMvc();

        }
    }
}
