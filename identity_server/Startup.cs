using IdentityServer4;
using Julio.Francisco.De.Iriarte.IdentityServer.Configuration;
using Julio.Francisco.De.Iriarte.IdentityServer.Caching;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using IdentityServer4.Services;
using identity_server;
using IdentityServer4.Test;
using Julio.Francisco.De.Iriarte.IdentityServer.Models.Account;
using Microsoft.EntityFrameworkCore;
using System.Reflection;
using IdentityServer4.EntityFramework.Mappers;
using IdentityServer4.EntityFramework.DbContexts;
using System.Linq;
using Julio.Francisco.De.Iriarte.Data;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

namespace Julio.Francisco.De.Iriarte.IdentityServer
{
    public class Startup
    {
        public IConfigurationRoot Configuration { get; set; }

        public Startup(IHostingEnvironment env)
        {
            // Set up configuration sources.

            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .AddEnvironmentVariables();

            Configuration = builder.Build();
        }
        
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc();

            // For use with CachedPropertiesDataFormat. In load-balanced scenarios 
            // you should use a persistent cache such as Redis or SQL Server.
            services.AddDistributedMemoryCache();

            /*services.AddTransient<IProfileService, ProfileService>();
            services.AddTransient<TestUserStore>((provider => new TestUserStore(TestUsers.Users)));
            services.AddIdentityServer()
                .AddInMemoryApiResources(Config.GetApiResources())
                .AddInMemoryIdentityResources(Config.GetIdentityResources())
                .AddInMemoryClients(Config.GetClients(Configuration))
                .AddTemporarySigningCredential(); //TODO: To be removed with a more stable use of asymetric keys*/

            var connectionString = Configuration.GetConnectionString("SalesianosDatabase");
            var migrationsAssembly = typeof(Startup).GetTypeInfo().Assembly.GetName().Name;

            services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(connectionString));
            services.AddIdentity<IdentityUser, IdentityRole>().AddEntityFrameworkStores<ApplicationDbContext>();

            // configure identity server with in-memory users, but EF stores for clients and resources
            services.AddIdentityServer()
                .AddTemporarySigningCredential()
                .AddAspNetIdentity<IdentityUser>()
                .AddConfigurationStore(builder =>
                    builder.UseSqlServer(connectionString, options =>
                        options.MigrationsAssembly(migrationsAssembly)))
                .AddOperationalStore(builder =>
                    builder.UseSqlServer(connectionString, options =>
                        options.MigrationsAssembly(migrationsAssembly)));
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole(LogLevel.Debug);

            InitializeDatabase(app);

            app.UseCors(builder =>
                builder
                .WithOrigins(
                    "http://localhost:5200", 
                    "http://juliofranciscodeiriarte166.org", 
                    "http://wordpress.juliofranciscodeiriarte166.org",
                    "https://wordpress.salesianos.cotillo-corp.com")
                .AllowAnyHeader()
                .AllowAnyMethod());

            app.UseDeveloperExceptionPage();

            app.UseIdentity();
            
            app.UseIdentityServer();

            app.UseCookieAuthentication(new CookieAuthenticationOptions
            {
                AuthenticationScheme = IdentityServerConstants.ExternalCookieAuthenticationScheme,

                AutomaticAuthenticate = false,
                AutomaticChallenge = false
            });

            ///
            /// Setup Custom Data Format
            /// 
            var schemeName = "oidc";
            var dataProtectionProvider = app.ApplicationServices.GetRequiredService<IDataProtectionProvider>();
            var distributedCache = app.ApplicationServices.GetRequiredService<IDistributedCache>();

            var dataProtector = dataProtectionProvider.CreateProtector(
                typeof(OpenIdConnectMiddleware).FullName,
                typeof(string).FullName, schemeName,
                "v1");

            var dataFormat = new CachedPropertiesDataFormat(distributedCache, dataProtector);

            ///
            /// Azure AD Configuration
            /// 
            var clientId = Configuration["oidc-salesianos:ClientId"];
            var tenantId = Configuration["oidc-salesianos:Tenant"];
            var authority = string.Format(Configuration["oidc-salesianos:AadInstance"], tenantId);

            app.UseOpenIdConnectAuthentication(new OpenIdConnectOptions
            {
                AuthenticationScheme = schemeName,
                DisplayName = "Office 365",
                SignInScheme = IdentityServerConstants.ExternalCookieAuthenticationScheme,
                SignOutScheme = IdentityServerConstants.SignoutScheme,
                ClientId = clientId,
                Authority = authority,
                ResponseType = OpenIdConnectResponseType.IdToken,
                StateDataFormat = dataFormat
            });

            app.UseStaticFiles();
            app.UseMvcWithDefaultRoute();
        }

        private void InitializeDatabase(IApplicationBuilder app)
        {
            using (var serviceScope = app.ApplicationServices.GetService<IServiceScopeFactory>().CreateScope())
            {
                serviceScope.ServiceProvider.GetRequiredService<PersistedGrantDbContext>().Database.Migrate();

                var context = serviceScope.ServiceProvider.GetRequiredService<ConfigurationDbContext>();
                context.Database.Migrate();
                if (!context.Clients.Any())
                {
                    foreach (var client in Config.GetClients(Configuration))
                    {
                        context.Clients.Add(client.ToEntity());
                    }
                    context.SaveChanges();
                }

                if (!context.IdentityResources.Any())
                {
                    foreach (var resource in Config.GetIdentityResources())
                    {
                        context.IdentityResources.Add(resource.ToEntity());
                    }
                    context.SaveChanges();
                }

                if (!context.ApiResources.Any())
                {
                    foreach (var resource in Config.GetApiResources())
                    {
                        context.ApiResources.Add(resource.ToEntity());
                    }
                    context.SaveChanges();
                }
            }
        }
    }
}
