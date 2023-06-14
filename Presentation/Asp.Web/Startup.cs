using System.Security.Claims;
using src.Core;
using src.Data;
using src.Emails;
using src.NovellDirectoryLdap;
using src.Repositories.Authentication;
using src.Repositories.Domains;
using src.Repositories.Logging;
using src.Repositories.Messages;
using src.Repositories.Roles;
using src.Repositories.Settings;
using src.Repositories.Users;
using src.Web.Common;
using src.Web.Common.Mvc;
using src.Web.Common.Security;
using AutoMapper;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Serialization;
using NLog;
using NLog.Extensions.Logging;
using NLog.Web;
using src.Repositories.Category;
using Hangfire;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http.Features;
using src.Web.Filters;
using System.Globalization;
using Microsoft.AspNetCore.Localization;
using src.Localization;

using src.Web.Extensions;
using src.Repositories.ParentActivity;
using src.Repositories.TravelDeclarations;
using src.Repositories.RedZone;
using src.Repositories.TravellingRoutes;
using src.Web.AuthorizeHandler;
using src.Repositories.Configs;
using src.Repositories.IncidentReports;


namespace src.Web
{
    public class Startup
    {
        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables();

            Configuration = builder.Build();

            CurrentEnvironment = env;

            env.ConfigureNLog("nlog.config");
        }

        public IConfigurationRoot Configuration { get; }

        private IHostingEnvironment CurrentEnvironment { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddOptions();
            services.Configure<AppSettings>(Configuration.GetSection("AppSettings"));
            services.Configure<EmailSettings>(Configuration.GetSection("EmailSettings"));
            services.Configure<userGroupSettings>(Configuration.GetSection("userGroupSettings"));
            services.AddDbContext<AppDbContext>(options =>
                options.UseSqlServer(Configuration.GetConnectionString("DefaultConnection")));
            services.AddHangfire(x => x.UseSqlServerStorage(Configuration.GetConnectionString("DefaultConnection")));
            services.AddAutoMapper();
            services.AddMemoryCache();
            services.AddSession();
            // Add AuthorizeFilter to demand the user to be authenticated in order to access resources.
            services.AddMvc(options => options.Filters.Add(new AuthorizeFilter(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build())))
                // Maintain property names during serialization. See: https://github.com/aspnet/Announcements/issues/194
                .AddJsonOptions(options => options.SerializerSettings.ContractResolver = new DefaultContractResolver());

            services.AddAuthentication(options =>
            {
                options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            })
                .AddCookie(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    options =>
                    {
                        options.AccessDeniedPath = "/Common/AccessDenied";
                        options.LoginPath = "/Account/Login";
                    }
                );

            services.AddAuthorization(options =>
            {

                options.AddPolicy(Constants.Permission_Workflow.IsOwner, policy => policy.Requirements.Add(new isOwnerPermission()));
                options.AddPolicy(Constants.Permission_Workflow.IsApprover, policy => policy.Requirements.Add(new isApproverPermission()));
                options.AddPolicy(Constants.Permission_Workflow.IsHeadOfDepartment, policy => policy.Requirements.Add(new isHeadOfDepartmentsPermission()));
                options.AddPolicy(Constants.Permission_Workflow.isECSDGroup, policy => policy.Requirements.Add(new isECSDGroupPermission()));
                options.AddPolicy(Constants.Permission_Workflow.IsApproverForMultiSelect, policy => policy.Requirements.Add(new isApproverMultSelectRequest()));
                options.AddPolicy(Constants.Permission_Workflow.IsApproverForMultiSelectForECSD, policy => policy.Requirements.Add(new isApproverMultSelectRequestForECSD()));
                options.AddPolicy(Constants.Permission_Workflow.isHRDivisionGroups, policy => policy.Requirements.Add(new isHRDivisionGroups()));

                //Covid

                options.AddPolicy(Constants.Permission_Workflow_Covid.IsOwner, policy => policy.Requirements.Add(new Covid_isOwnerPermission()));
                options.AddPolicy(Constants.Permission_Workflow_Covid.IsApprover, policy => policy.Requirements.Add(new Covid_isApproverPermission()));
                options.AddPolicy(Constants.Permission_Workflow_Covid.IsHeadOfDepartment, policy => policy.Requirements.Add(new Covid_isHeadOfDepartmentsPermission()));
                options.AddPolicy(Constants.Permission_Workflow_Covid.isECSDGroup, policy => policy.Requirements.Add(new Covid_isECSDGroupPermission()));
                options.AddPolicy(Constants.Permission_Workflow_Covid.IsApproverForMultiSelect, policy => policy.Requirements.Add(new Covid_isApproverMultSelectRequest()));
                options.AddPolicy(Constants.Permission_Workflow_Covid.IsApproverForMultiSelectForECSD, policy => policy.Requirements.Add(new Covid_isApproverMultSelectRequestForECSD()));
                options.AddPolicy(Constants.Permission_Workflow_Covid.isHRDivisionGroups, policy => policy.Requirements.Add(new Covid_isHRDivisionGroups()));


                options.AddPolicy(Constants.RoleNames.Administrator, policyBuilder =>
                {
                    policyBuilder.RequireAuthenticatedUser()
                        .RequireAssertion(context => context.User.HasClaim(ClaimTypes.Role, Constants.RoleNames.Administrator))
                        .Build();
                });
                options.AddPolicy(Constants.RoleNames.HRBP, policyBuilder =>
                {
                    policyBuilder.RequireAuthenticatedUser()
                        .RequireAssertion(context => context.User.HasClaim(ClaimTypes.Role, Constants.RoleNames.HRBP))
                        .Build();
                });

                options.AddPolicy(Constants.Permission.EditPhoneNumber, policyBuilder =>
                {
                    policyBuilder.RequireAuthenticatedUser()
                        .RequireAssertion(context => context.User.HasClaim(ClaimTypes.Role, Constants.RoleNames.Administrator) || context.User.HasClaim(ClaimTypes.Role, Constants.RoleNames.HRBP))
                        .Build();
                });
                options.AddPolicy(Constants.RoleNames.Parent, policyBuilder =>
                {
                    policyBuilder.RequireAuthenticatedUser()
                        .RequireAssertion(context => context.User.HasClaim(ClaimTypes.Role, Constants.RoleNames.Parent))
                        .Build();
                });

            });

            services.AddMvc(options =>
            {
                options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
                //options.InputFormatters.Insert(0, new RawJsonBodyInputFormatter());
            });
            services.Configure<FormOptions>(options =>
            {
                options.ValueLengthLimit = int.MaxValue;
                options.MultipartBodyLengthLimit = int.MaxValue;
                options.MultipartHeadersLengthLimit = int.MaxValue;
            });


            services.AddScoped<DeleteFileAttributeAfterDownload>();
            services.AddSignalR();

            services.AddDataProtection();
            //services.AddAuthentication();
            services.AddHttpContextAccessor();
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.AddScoped<IDbContext, AppDbContext>();
            services.AddScoped<IDomainRepository, DomainRepository>();
            services.AddScoped<IEmailSender, EmailSender>();
            services.AddScoped<IEmailTemplateRepository, EmailTemplateRepository>();
            services.AddScoped<ILogRepository, LogRepository>();
            services.AddScoped<IMessageService, MessageService>();
            services.AddScoped<LogFilter>();
            services.AddScoped<IRoleRepository, RoleRepository>();
            services.AddScoped<ISettingRepository, SettingRepository>();
            services.AddScoped<ISignInManager, SignInManager>();

            services.AddScoped<ITravelDeclarationRepository, TravelDeclarationRepository>();
            services.AddScoped<ITravellingRoutesRepository, TravellingRoutesRepository>();
            services.AddScoped<IIncidentReportRepository, IncidentReportRepository>();

            services.AddScoped<IRedZoneRepo, RedZoneRepo>();


            services.AddScoped<IEmailConfigRepository, EmailConfigRepository>();

            services.AddScoped<IUserSession, UserSession>();
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IBaseCategoryRepository, BaseCategoryRepository>();
            services.AddScoped<IParentActivityRepository, ParentActivityRepository>();
            services.AddSingleton<EncryptionService>();
            services.AddTransient<IDateTime, DateTimeAdapter>();

            services.AddScoped<IAuthorizationHandler, ApproverRequestAuthorizeHandler>();
            services.AddScoped<IAuthorizationHandler, IsOwerRequestAuthorizeHandler>();
            services.AddScoped<IAuthorizationHandler, IsHeadOfDepartmentRequestAuthorizeHandler>();
            services.AddScoped<IAuthorizationHandler, isECSDRequestAuthorizeHandler>();
            services.AddScoped<IAuthorizationHandler, IsHeadOfDepartmentRequestAuthorizeHandler>();
            services.AddScoped<IAuthorizationHandler, isApproverMultiSelectRequestAuthorizeHandler>();
            services.AddScoped<IAuthorizationHandler, isApproverMultiSelectForECSDRequestAuthorizeHandler>();
            services.AddScoped<IAuthorizationHandler, isHRDivisionGroupsRequestAuthorizeHandler>();
            //Covid
            services.AddScoped<IAuthorizationHandler, covid_ApproverRequestAuthorizeHandler>();
            services.AddScoped<IAuthorizationHandler, Covid_IsOwerRequestAuthorizeHandler>();
            services.AddScoped<IAuthorizationHandler, covid_IsHeadOfDepartmentRequestAuthorizeHandler>();
            services.AddScoped<IAuthorizationHandler, covid_isECSDRequestAuthorizeHandler>();
            services.AddScoped<IAuthorizationHandler, covid_IsHeadOfDepartmentRequestAuthorizeHandler>();
            services.AddScoped<IAuthorizationHandler, Covid_isApproverMultiSelectForECSDRequestAuthorizeHandler>();
            services.AddScoped<IAuthorizationHandler, Covid_isApproverMultiSelectRequestAuthorizeHandler>();
            services.AddScoped<IAuthorizationHandler, Covid_isHRDivisionGroupsRequestAuthorizeHandler>();



            services.AddLocalization();
            services.AddScoped<IAuthenticationService, LdapAuthenticationService>();

            services.AddTransient(ec => new EncryptionService(new KeyInfo("45BLO2yoJkvBwz99kBEMlNkxvL40vUSGaqr/WBu3+Vg=", "Ou3fn+I9SVicGWMLkFEgZQ==")));

            services.AddMvc().AddViewLocalization()
                .AddDataAnnotationsLocalization(options =>
                {
                    options.DataAnnotationLocalizerProvider = (type, factory) =>
                        factory.Create(typeof(SharedResource));
                });

            services.AddLocalization(options => options.ResourcesPath = "Resources");

            services.Configure<RequestLocalizationOptions>(options =>
            {
                var supportedCultures = new[]
                {
                    new CultureInfo("vi-VN"),
                    new CultureInfo("en-GB")

                };
                options.DefaultRequestCulture = new RequestCulture("vi-VN");
                options.SupportedCultures = supportedCultures;
                options.SupportedUICultures = supportedCultures;
            });

        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {

            if (env.IsDevelopment())
            {
                //app.UseStatusCodePagesWithRedirects("/Common/PageNotFound/{0}");
                app.UseDeveloperExceptionPage();
                app.UseBrowserLink();
                app.UseDatabaseErrorPage();
                /*app.UseStatusCodePagesWithRedirects("/Common/Error/{0}");
                app.UseExceptionHandler("/Common/Error");*/
            }
            else
            {
                app.UseStatusCodePagesWithRedirects("/Common/Error/{0}");
                app.UseExceptionHandler("/Common/Error");
            }

            loggerFactory.AddNLog();
            app.AddNLogWeb();
            LogManager.Configuration.Variables["connectionString"] = Configuration.GetConnectionString("DefaultConnection");

            //Hangfire

            app.UseHangfireDashboard();
            var options = new BackgroundJobServerOptions { WorkerCount = 1 };
            app.UseHangfireServer(options);

            app.UseAuthentication();

            app.UseRequestLocalization();
            app.UseStaticFiles();
            app.UseSession();
            app.UseSignalR(routes =>
            {
                routes.MapHub<NotificationHub>("/downloadFileHub");
            });
            app.UseMvc(routes =>
            {
                routes.MapRoute("areaRoute", "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");
                routes.MapRoute("Default", "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}
