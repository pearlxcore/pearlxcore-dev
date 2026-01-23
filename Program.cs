using FluentValidation;
using FluentValidation.AspNetCore;
using pearlxcore.dev.Data;
using pearlxcore.dev.Models.Entities;
using pearlxcore.dev.Services.Implementations;
using pearlxcore.dev.Services.Interfaces;
using pearlxcore.dev.Web.Data;
using Markdig;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace pearlxcore.dev
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            // Configure Serilog from appsettings.json
            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json")
                    .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", optional: true)
                    .Build())
                .Enrich.FromLogContext()
                .WriteTo.Console()
                .WriteTo.File("logs/pearlxcore-.txt", rollingInterval: RollingInterval.Day)
                .CreateLogger();

            try
            {
                Log.Information("Starting application");

                var builder = WebApplication.CreateBuilder(args);

                // Use Serilog
                builder.Host.UseSerilog();

            // Add services to the container.
            builder.Services.AddControllersWithViews();
            // FluentValidation (CORRECT)
            builder.Services.AddFluentValidationAutoValidation();
            builder.Services.AddFluentValidationClientsideAdapters();

            // Register validators
            builder.Services.AddValidatorsFromAssemblyContaining<Program>();

            // Register DbContext
            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(
                    builder.Configuration.GetConnectionString("DefaultConnection")
            ));

            // Configure Identity
            builder.Services
                .AddIdentity<ApplicationUser, IdentityRole>(options =>
                {
                    // Password policy (reasonable, not annoying)
                    options.Password.RequireDigit = true;
                    options.Password.RequireLowercase = true;
                    options.Password.RequireUppercase = false;
                    options.Password.RequireNonAlphanumeric = false;
                    options.Password.RequiredLength = 8;

                    // Lockout
                    options.Lockout.MaxFailedAccessAttempts = 5;
                    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(10);

                    // User
                    options.User.RequireUniqueEmail = true;
                })
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddDefaultTokenProviders();

            // Configure application cookie
            builder.Services.ConfigureApplicationCookie(options =>
            {
                options.LoginPath = "/Account/Login";
                options.AccessDeniedPath = "/Account/AccessDenied";
            });

            // Register application services
            builder.Services.AddScoped<IPostService, PostService>();
            builder.Services.AddScoped<ICategoryService, CategoryService>();
            builder.Services.AddScoped<ITagService, TagService>();
            builder.Services.AddScoped<IAdminProfileService, AdminProfileService>();
            builder.Services.AddScoped<INewsletterService, NewsletterService>();
            builder.Services.AddScoped<IMediaService, MediaService>();
            builder.Services.AddScoped<IAuditLogService, AuditLogService>();

            // Register background services
            builder.Services.AddHostedService<pearlxcore.dev.Services.BackgroundServices.PostPublishingService>();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();

            // Serve static files from wwwroot
            app.UseStaticFiles();

            // Serve static files
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseStatusCodePagesWithReExecute("/Home/StatusCode", "?code={0}");
            }

            app.UseStaticFiles();
            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();


            // Area routing
            app.MapControllerRoute(
                name: "areas",
                pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}"
            );

            // Default routing
            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            await DbInitializer.SeedAsync(app.Services);

                app.Run();
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Application terminated unexpectedly");
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }
    }
}
