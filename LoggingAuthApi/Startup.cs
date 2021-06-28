using System;
using System.IdentityModel.Tokens.Jwt;
using System.Threading.Tasks;
using AspNet.Security.OpenIdConnect.Primitives;
using LoggingAuthApi.Constants;
using LoggingAuthApi.Data;
using LoggingAuthApi.Settings;
using LoggingAuthApi.Settings.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Abstractions;
using OpenIddict.Core;
using OpenIddict.EntityFrameworkCore.Models;

namespace LoggingAuthApi
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }
        private IConfigurationSettings Settings { get; set; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            //// AUTOFAC
            //var appName = "AuthAPI";
            //// Create the container builder.
            //var builder = InitApi.Init(appName, Configuration);



            Settings = new JsonConfigurationSettings(Configuration);


            // EMAIL
            //var emailConfig = Configuration
            //    .GetSection("EmailConfiguration")
            //    .Get<EmailConfiguration>();
            //services.AddSingleton(emailConfig);




            //var startupLogger = new FileLogger("PawsAuthTemp", LogLevel.Info, new FileSystem());

            services.AddCors();


            //services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_2);
            services.AddMvc();



            services.AddDbContext<ApplicationDbContext>(options =>
            {
                // Configure the context to use Microsoft SQL Server.
                options.UseSqlServer(Configuration.GetConnectionString("DefaultConnection"));

                // Register the entity sets needed by OpenIddict.
                // Note: use the generic overload if you need
                // to replace the default OpenIddict entities.
                //options.UseOpenIddict();
                options.UseOpenIddict<Guid>();
            });

            // Register the Identity services.
            services.AddIdentity<ApplicationUser, IdentityRole<Guid>>()
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddDefaultTokenProviders();
            //services.AddDefaultIdentity<ApplicationUser>()
            //    .AddEntityFrameworkStores<ApplicationDbContext>()
            //    .AddDefaultTokenProviders();

            // Configure Identity to use the same JWT claims as OpenIddict instead
            // of the legacy WS-Federation claims it uses by default (ClaimTypes),
            // which saves you from doing the mapping in your authorization controller.
            services.Configure<IdentityOptions>(options =>
            {
                options.ClaimsIdentity.UserNameClaimType = OpenIdConnectConstants.Claims.Name;
                options.ClaimsIdentity.UserIdClaimType = OpenIdConnectConstants.Claims.Subject;
                options.ClaimsIdentity.RoleClaimType = OpenIdConnectConstants.Claims.Role;
            });

            services.AddOpenIddict()
                // Register the OpenIddict core services.
                .AddCore(options =>
                {
                    // Register the Entity Framework stores and models.
                    options.UseEntityFrameworkCore()
                        .UseDbContext<ApplicationDbContext>()
                        .ReplaceDefaultEntities<Guid>()
                        ;
                })

                // Register the OpenIddict server handler.
                .AddServer(options =>
                {

                    // Register the ASP.NET Core MVC binder used by OpenIddict.
                    // Note: if you don't call this method, you won't be able to
                    // bind OpenIdConnectRequest or OpenIdConnectResponse parameters.
                    options.UseMvc();

                    options.RegisterScopes(OpenIdConnectConstants.Scopes.Email,
                        OpenIdConnectConstants.Scopes.Profile,
                        OpenIddictConstants.Scopes.Roles,
                        OpenIddictConstants.Scopes.OfflineAccess,
                        Scopes.PawsAppApi,
                        Scopes.Device);

                    // Enable the authorization, logout, token and userinfo endpoints.
                    options.EnableAuthorizationEndpoint("/connect/authorize")
                        .EnableLogoutEndpoint("/connect/logout")
                        .EnableTokenEndpoint("/connect/token")
                        .EnableIntrospectionEndpoint("/connect/introspect")
                        .EnableUserinfoEndpoint("/api/userinfo");

                    // Enable the password and the refresh token flows.
                    options.AllowPasswordFlow()
                        .AllowRefreshTokenFlow().Configure(a => a.UseSlidingExpiration = true)
                        .AllowAuthorizationCodeFlow();

                    // Accept anonymous clients (i.e clients that don't send a client_id).
                    //options.AcceptAnonymousClients();

                    // During development, you can disable the HTTPS requirement.
                    //options
                    //    .DisableHttpsRequirement(); //                            #######################   SET THIS ???   #######################

                    // Note: to use JWT access tokens instead of the default
                    // encrypted format, the following lines are required:
                    //
                    options.UseJsonWebTokens();
                    // options.AddEphemeralSigningKey(); //    SEE BELOW: options.AddSigningCertificate   #######################   SET THIS WHEN NO CERT   #######################
                    // Peter updated with LastPass thumbprint 5 May 2020 
                     options.AddSigningCertificate("f53ea7fd79a56a94c3010daa3715265835268721"); // Thumbprint of KeysPawasNz cert in Personal Cert Store
                    // Doug updated for Azure Key Store 01 Sept 2020 
                    //options.AddSigningCertificate("f53ea7fd79a56a94c3010daa3715265835268721", StoreName.My, StoreLocation.CurrentUser);




                })

                // Register the OpenIddict validation handler.
                // Note: the OpenIddict validation handler is only compatible with the
                // default token format or with reference tokens and cannot be used with
                // JWT tokens. For JWT tokens, use the Microsoft JWT bearer handler.
                // .AddValidation()
                ;

            // If you prefer using JWT, don't forget to disable the automatic
            // JWT -> WS-Federation claims mapping used by the JWT middleware:
            //
            JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();
            JwtSecurityTokenHandler.DefaultOutboundClaimTypeMap.Clear();

            //Add authentication and set default authentication scheme
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme) //same as "Bearer"
                .AddJwtBearer(options =>
                {
                    //Authority must be a url. It does not have a default value.
                    options.Authority = Settings.Api.Auth0Domain;
                    options.Audience = Settings.Api.Auth0Audience; //This must be included in ticket creation
                    options.RequireHttpsMetadata = false;
                    options.IncludeErrorDetails = true; //
                    options.TokenValidationParameters = new TokenValidationParameters()
                    {
                        NameClaimType = OpenIdConnectConstants.Claims.Subject,
                        RoleClaimType = OpenIdConnectConstants.Claims.Role,
                    };
                });






            //services.AddAuthentication()
            //    .AddJwtBearer(options =>
            //    {
            //        options.Authority = Settings.Api.Auth0Domain;
            //        options.Audience = Settings.Api.Auth0Audience;
            //        options.RequireHttpsMetadata = true; //                            #######################   SET THIS ???   #######################
            //        options.TokenValidationParameters = new TokenValidationParameters
            //        {
            //            NameClaimType = OpenIdConnectConstants.Claims.Subject,
            //            RoleClaimType = OpenIdConnectConstants.Claims.Role
            //        };
            //    });









            /*
             *



            // Register dependencies, populate the services from
            // the collection, and build the container.
            //
            // Note that Populate is basically a foreach to add things
            // into Autofac that are in the collection. If you register
            // things in Autofac BEFORE Populate then the stuff in the
            // ServiceCollection can override those things; if you register
            // AFTER Populate those registrations can override things
            // in the ServiceCollection. Mix and match as needed.
            builder.Populate(services);

            ApplicationContainer = builder.Build();
            //var managementApi = ApplicationContainer.Resolve<IManagementApi>();
            //managementApi.EnsureSeeded();

            StartupLogger = ApplicationContainer.Resolve<ILogger>();
            var settings = ApplicationContainer.Resolve<IConfigurationSettings>();
            var connectionString = Configuration.GetConnectionString("DefaultConnection");
            StartupLogger.Log(LogLevel.Error, connectionString);

            // Create the IServiceProvider based on the container.
            //return new AutofacServiceProvider(ApplicationContainer);
             */
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();   // ********************************************************************************************************************************************
            }

            app.UseHttpsRedirection();  //  **********************************************************************************************************************************

            app.UseCors(builder =>
            {
                builder.WithOrigins(Settings.Api.CorsOrigins, Settings.Api.CorsOriginsLocalHost, Settings.Api.CorsOriginsScanner, "https://jwt.io");
                builder.AllowAnyHeader();
                builder.AllowAnyMethod();
            });

            //app.UseAuthentication();

            //app.UseMvcWithDefaultRoute();

            // Seed the database with the sample applications.
            // Note: in a real world application, this step should be part of a setup script.
            //InitializeAsync(app.ApplicationServices).GetAwaiter().GetResult();










            // New for 3.1
            app.UseStaticFiles();

            app.UseStatusCodePagesWithReExecute("/error");

            app.UseRouting();

            //must come before using MVC
            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(options =>
            {
                options.MapControllers();
                //options.MapDefaultControllerRoute();
            });

            // Seed the database with the sample applications.
            // Note: in a real world application, this step should be part of a setup script.
            InitializeAsync(app.ApplicationServices).GetAwaiter().GetResult();
            //StartupLogger.Log(LogLevel.Info, "Initialized OK");
        }

        private async Task InitializeAsync(IServiceProvider services)
        {
            // Create a new service scope to ensure the database context is correctly disposed when this methods returns.
            using (var scope = services.GetRequiredService<IServiceScopeFactory>().CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                //StartupLogger.Log(LogLevel.Info, $"Can connect to Database: {context.Database.CanConnect()}");
                //StartupLogger.Log(LogLevel.Info, $"Can connect to Database: {context.Database.GetDbConnection().ConnectionString}");

                await context.Database.EnsureCreatedAsync();

                await CreateApplicationsAsync();
                await CreateScopesAsync();
                await CreateRolesAsync();
                //await CreateUsersAsync();


                async Task CreateApplicationsAsync()
                {
                    var manager = scope.ServiceProvider.GetRequiredService<OpenIddictApplicationManager<OpenIddictApplication<Guid>>>();

                    if (await manager.FindByClientIdAsync(Settings.Api.PawsApplicationClientId) == null)
                    {
                        var descriptor = new OpenIddictApplicationDescriptor
                        {
                            ClientId = Settings.Api.PawsApplicationClientId,
                            //ClientSecret = Settings.Api.PawsApplicationClientSecret,
                            DisplayName = Settings.Api.PawsApplicationDisplayName,
                            PostLogoutRedirectUris = { new Uri("https://localhost:44379/signout-oidc") },
                            RedirectUris = { new Uri("https://localhost:44379/signin-oidc") },
                            Permissions =
                            {
                                OpenIddictConstants.Permissions.Endpoints.Logout,
                                OpenIddictConstants.Permissions.Endpoints.Token,
                                OpenIddictConstants.Permissions.Endpoints.Introspection,
                                OpenIddictConstants.Permissions.GrantTypes.RefreshToken,
                                OpenIddictConstants.Permissions.GrantTypes.Password,
                                OpenIddictConstants.Permissions.Scopes.Email,
                                OpenIddictConstants.Permissions.Scopes.Profile,
                                OpenIddictConstants.Permissions.Scopes.Roles,
                                OpenIddictConstants.Permissions.Prefixes.Scope + Scopes.PawsAppApi,
                                OpenIddictConstants.Permissions.Prefixes.Scope + Scopes.Device
                            },
                            Type = OpenIddictConstants.ClientTypes.Public
                        };

                        await manager.CreateAsync(descriptor);
                    }
                }

                async Task CreateScopesAsync()
                {
                    var manager = scope.ServiceProvider.GetRequiredService<OpenIddictScopeManager<OpenIddictScope<Guid>>>();

                    if (await manager.FindByNameAsync(Scopes.PawsAppApi) == null)
                    {
                        var descriptor = new OpenIddictScopeDescriptor
                        {
                            Name = Scopes.PawsAppApi,
                            Resources = { Scopes.PawsAppApiResources },
                            DisplayName = Scopes.PawsAppApiDisplayName,
                            Description = Scopes.PawsAppApiDescription
                        };

                        await manager.CreateAsync(descriptor);
                    }

                    if (await manager.FindByNameAsync(Scopes.Device) == null)
                    {
                        var descriptor = new OpenIddictScopeDescriptor
                        {
                            Name = Scopes.Device,
                            Resources = { Scopes.PawsAppApiResources },
                            DisplayName = "Device ID",
                            Description = "The GUID that is the Device Identifier"
                        };

                        await manager.CreateAsync(descriptor);
                    }
                }

                async Task CreateRolesAsync()
                {
                    var manager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();

                    if (await manager.FindByNameAsync(Roles.SuperAdmin.ToString()) == null) { var descriptor = new IdentityRole<Guid> { Name = Roles.SuperAdmin.ToString() }; await manager.CreateAsync(descriptor); }
                    if (await manager.FindByNameAsync(Roles.Admin.ToString()) == null) { var descriptor = new IdentityRole<Guid> { Name = Roles.Admin.ToString() }; await manager.CreateAsync(descriptor); }
                    if (await manager.FindByNameAsync(Roles.ManagerAccounting.ToString()) == null) { var descriptor = new IdentityRole<Guid> { Name = Roles.ManagerAccounting.ToString() }; await manager.CreateAsync(descriptor); }
                    if (await manager.FindByNameAsync(Roles.ManagerWarehouse.ToString()) == null) { var descriptor = new IdentityRole<Guid> { Name = Roles.ManagerWarehouse.ToString() }; await manager.CreateAsync(descriptor); }
                    if (await manager.FindByNameAsync(Roles.ManagerManufacturing.ToString()) == null) { var descriptor = new IdentityRole<Guid> { Name = Roles.ManagerManufacturing.ToString() }; await manager.CreateAsync(descriptor); }
                    if (await manager.FindByNameAsync(Roles.StandardAccounting.ToString()) == null) { var descriptor = new IdentityRole<Guid> { Name = Roles.StandardAccounting.ToString() }; await manager.CreateAsync(descriptor); }
                    if (await manager.FindByNameAsync(Roles.StandardWarehouse.ToString()) == null) { var descriptor = new IdentityRole<Guid> { Name = Roles.StandardWarehouse.ToString() }; await manager.CreateAsync(descriptor); }
                    if (await manager.FindByNameAsync(Roles.StandardManufacturing.ToString()) == null) { var descriptor = new IdentityRole<Guid> { Name = Roles.StandardManufacturing.ToString() }; await manager.CreateAsync(descriptor); }
                }
            }
        }
    }
}
