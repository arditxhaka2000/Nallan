using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Repository;
using Microsoft.EntityFrameworkCore;
using Services;
using AutoMapper;
using Web.Mapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Web.Infrastructure;
using Microsoft.AspNetCore.Mvc.Razor;
using Web.Resources;
using Services.Languages;
using System.Globalization;
using Microsoft.AspNetCore.Localization;
using Web.DI;
using Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Newtonsoft.Json.Serialization;
using Microsoft.Extensions.FileProviders;
using System.IO;
using Microsoft.AspNetCore.Http.Features;
using Services.Localizations;
using Web.Controllers;
using Services.Logs;
using Web.Providers;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using AspNetCore.ReCaptcha;
using Web.Components;
using Services.TEBPayments;
using Web.Models.Payments;
using Web.Models.TebBank;
using Services.JwtService;

namespace OA_Web
{
    public class Startup
    {
        public Startup(Microsoft.AspNetCore.Hosting.IHostingEnvironment env)
        {

            var builder = new ConfigurationBuilder()
            .SetBasePath(env.ContentRootPath)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true) //load base settings
                 .AddEnvironmentVariables();

            Configuration = builder.Build();
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddHttpContextAccessor();
            services.AddReCaptcha(Configuration.GetSection("RecaptchaSettings"));
            var MaxFileLength = Configuration.GetValue<int>("MySettings:MaxFileLengthInMB");
            //  services.AddSingleton<IFileProvider>(new PhysicalFileProvider(Path.Combine(Directory.GetCurrentDirectory(), "Documents")));
            services.Configure<FormOptions>(x =>
             {
                 x.MultipartBodyLengthLimit = 104857600;//100MB


             });

            //cart
            services.AddSession();
            services.AddDistributedMemoryCache(); // Required for session
            services.AddControllersWithViews();


            // Auto Mapper Configurations
            var mapperConfig = new MapperConfiguration(mc =>
            {
                mc.AddProfile(new MappingProfile());
            });
            IMapper mapper = mapperConfig.CreateMapper();
            services.AddSingleton(mapper);
            services.AddSingleton<IActionContextAccessor, ActionContextAccessor>();
            services.AddSingleton<ILog, LogNLog>();


            //services.AddDbContext<ApplicationContext>(options => options.UseLazyLoadingProxies().UseSqlServer(Configuration.GetConnectionString("DefaultConnection")).EnableSensitiveDataLogging());
            services.AddDbContext<ApplicationContext>(options => options.UseSqlServer(Configuration.GetConnectionString("DefaultConnection")).EnableSensitiveDataLogging());


            //services.AddIdentity<IdentityUser, IdentityRole>()
            //    .AddEntityFrameworkStores<ApplicationContext>();

            services.AddIdentity<AppUser, ApplicationRole>(options => options.SignIn.RequireConfirmedAccount = true)
                    .AddEntityFrameworkStores<ApplicationContext>()
                    .AddDefaultTokenProviders();

            services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
            services.AddScoped(typeof(IUserRepository<>), typeof(UserRepository<>));
            services.AddScoped<JwtService>();
            services.AddScoped<IGjirafaPartnerService, GjirafaPartnerService>();
            services.AddScoped<IGjirafaFeedService, GjirafaFeedService>();
            services.AddDependencies();

            // JWT Authentication for API
            var jwtSettings = Configuration.GetSection("JwtConfig");
            var secretKey = jwtSettings["secret"];
            var key = Encoding.ASCII.GetBytes(secretKey);

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = "MultiAuth";
                options.DefaultChallengeScheme = "MultiAuth";
            })
            .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };
            })
            .AddPolicyScheme("MultiAuth", "Identity or JWT", options =>
            {
                options.ForwardDefaultSelector = context =>
                {
                    string authHeader = context.Request.Headers["Authorization"].FirstOrDefault() ?? "";
                    if (authHeader.StartsWith("Bearer "))
                        return JwtBearerDefaults.AuthenticationScheme;

                    return "Identity.Application";
                };
            });
            services.AddSession();

            services.AddControllersWithViews().AddNewtonsoftJson(options =>
                                                                options.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore
                                                            ).AddRazorRuntimeCompilation();
            services.AddRazorPages();

            services.AddLocalization(options => options.ResourcesPath = "Resources");
            services.AddControllersWithViews()
                    .AddDataAnnotationsLocalization(options =>
                    {
                        options.DataAnnotationLocalizerProvider = (type, factory) =>
                            factory.Create(typeof(Common));
                    })
                    .AddViewLocalization(LanguageViewLocationExpanderFormat.SubFolder)
                    .AddViewLocalization(LanguageViewLocationExpanderFormat.Suffix);

            services.Configure<RouteOptions>(options =>
            {
                options.ConstraintMap.Add("lang", typeof(LanguageRouteConstraint));
            });

            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

            services.AddControllersWithViews().AddRazorPagesOptions(options =>
                          {
                              options.Conventions.AuthorizeAreaFolder("Identity", "/Account/Manage");
                              options.Conventions.AuthorizeAreaPage("Identity", "/Account/Logout");
                              options.Conventions.AuthorizeAreaPage("Identity", "/Account/Register");
                              options.Conventions.AddAreaFolderRouteModelConvention("Identity", "/Account/", model =>
                              {
                                  foreach (var x in model.Selectors)
                                  {
                                      if (x.AttributeRouteModel.Template.StartsWith("Identity"))
                                      {
                                          x.AttributeRouteModel = new AttributeRouteModel()
                                          {
                                              Order = -1,
                                              Template = AttributeRouteModel.CombineTemplates(("{lang=sq}"),
                                                  x.AttributeRouteModel.Template)
                                          };
                                      }
                                  }
                              });
                              options.Conventions.AddFolderRouteModelConvention("/ListNodeType/",
                                   model =>
                                  {
                                      foreach (var x in model.Selectors)
                                      {
                                          // if (x.AttributeRouteModel.Template.StartsWith("Identity"))
                                          {
                                              x.AttributeRouteModel = new AttributeRouteModel()
                                              {
                                                  Order = -1,
                                                  Template = AttributeRouteModel.CombineTemplates(("{lang=sq}"),
                                                      x.AttributeRouteModel.Template)
                                              };
                                          }
                                      }
                                  }



                                  );
                          });



            services.ConfigureApplicationCookie(options =>
            {
                options.LoginPath = "/sq/Identity/Account/Login";
                options.LogoutPath = "/sq/Identity/Account/Logout";
                options.AccessDeniedPath = "/sq/Identity/Account/AccessDenied";
            });

            services.AddTransient<CustomLocalizer>();

            //var cultureInfo = new CultureInfo("en-US");
            //cultureInfo.NumberFormat.NumberDecimalSeparator = ".";
            //cultureInfo.NumberFormat.CurrencyDecimalSeparator = ".";
            //CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
            //CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;


            //services.Configure<RecaptchaSettings>(Configuration.GetSection("RecaptchaSettings"));

            //services.ConfigureApplicationCookie(options =>
            //{
            //    options.Cookie.Name = "AspNetCore.Identity.Application";
            //    options.ExpireTimeSpan = TimeSpan.FromHours(24);
            //    options.SlidingExpiration = true;
            //});

            services.Configure<SecurityStampValidatorOptions>(o =>
            {
                o.ValidationInterval = TimeSpan.FromHours(24);
            });
            // ===== TEB BANK PAYMENT SERVICES REGISTRATION =====

            // Memory cache (required for session service)
            services.AddMemoryCache();

            // Configure TEB Bank settings
            services.Configure<TebBankSettings>(options =>
            {
                options.PostUrl = Environment.GetEnvironmentVariable("TEBBANK_POST_URL") ?? "https://ecommerce.teb-kos.com/fim/est3Dgate";
                options.ApiUrl = Environment.GetEnvironmentVariable("TEBBANK_API_URL") ?? "https://ecommerce.teb-kos.com/fim/api";
                options.Currency = "978"; // EUR
                options.Language = "en";
                options.TransactionType = "Auth";
                options.StoreType = "3d_pay_hosting";
                options.HashAlgorithm = "ver3";
                options.RefreshTime = "10";
                options.SessionTimeoutMinutes = 15;
                options.MaxAmount = 10000;
                options.MinAmount = 1;
            });

            // Configure TEB Bank credentials
            services.Configure<TebBankCredentials>(options =>
            {
                options.ClientId = Environment.GetEnvironmentVariable("TEBBANK_CLIENT_ID") ?? "";
                options.UserName = Environment.GetEnvironmentVariable("TEBBANK_USERNAME") ?? "";
                options.Password = Environment.GetEnvironmentVariable("TEBBANK_PASSWORD") ?? "";
                options.StoreKey = Environment.GetEnvironmentVariable("TEBBANK_STORE_KEY") ?? "";
                options.ApiUserName = Environment.GetEnvironmentVariable("TEBBANK_API_USERNAME") ?? "";
                options.ApiPassword = Environment.GetEnvironmentVariable("TEBBANK_API_PASSWORD") ?? "";
            });

            // Register TEB Bank payment services
            services.AddScoped<IHashService, HashService>();
            services.AddScoped<ISecureCredentialsService, EnvironmentVariableCredentialsService>();
            services.AddScoped<IPaymentSessionService, PaymentSessionService>(); // <- THIS IS YOUR _sessionService

            // Validate environment variables
            ValidateRequiredEnvironmentVariables();
        }
        private static void ValidateRequiredEnvironmentVariables()
        {
            var requiredVars = new[]
            {
        "TEBBANK_CLIENT_ID",
        "TEBBANK_STORE_KEY",
    };

            var missingVars = requiredVars.Where(varName => string.IsNullOrEmpty(Environment.GetEnvironmentVariable(varName))).ToList();

            if (missingVars.Any())
            {
                var errorMessage = $"Missing required TEB Bank environment variables: {string.Join(", ", missingVars)}";
                throw new InvalidOperationException(errorMessage);
            }
        }
        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILog logger)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }
            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.Use(async (context, next) =>
            {
                var path = context.Request.Path.Value?.ToLower() ?? "";

                if (path.Contains("/bankpayment") || path.Contains("/payment"))
                {
                    context.Response.OnStarting(() =>
                    {
                        var headers = context.Response.Headers;
                        headers.Remove("Content-Security-Policy");
                        headers.Remove("Content-Security-Policy-Report-Only");
                        headers.Remove("X-Frame-Options");
                        headers.Remove("X-Content-Type-Options");

                        // Allow TEB Bank form submission
                        headers.Add("Content-Security-Policy",
                            "default-src * 'unsafe-inline' 'unsafe-eval' data: blob:; " +
                            "form-action * https://ecommerce.teb-kos.com; " +
                            "frame-ancestors *;");

                        return Task.CompletedTask;
                    });
                }

                await next();
            });
            //app.UseFileServer(new FileServerOptions
            //{
            //    FileProvider = new PhysicalFileProvider(Path.Combine(env.ContentRootPath, "Documents")),
            //    RequestPath = "/StaticFiles",
            //    EnableDirectoryBrowsing = true

            //});
            //cart
            app.UseSession(); // Use session
            app.UseMiddleware<UnderConstructionMiddleware>();
            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();
            app.ConfigureExceptionHandler(logger);
            app.UseSession();

            var options = app.ApplicationServices.GetService<IOptions<RequestLocalizationOptions>>();
            LocalizationPipeline.ConfigureOptions(options.Value);

            app.UseRequestLocalization(options.Value);
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();  // API routes

                // Redirect / to /sq
                endpoints.MapGet("/", context =>
                {
                    context.Response.Redirect("/sq");
                    return Task.CompletedTask;
                });

                endpoints.MapControllerRoute(
                   name: "Default",
                   pattern: "{lang:lang}/{controller=Home}/{action=Index}/{id?}"
               );

                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{*catchall}",
                    defaults: new { controller = "Home", action = "RedirectToDefaultLanguage", lang = "sq" },
                    constraints: new { catchall = "^(?!api).*" }
                );

                endpoints.MapRazorPages();
            });
        }





    }
}
