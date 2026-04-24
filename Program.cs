using FluentValidation;
using FluentValidation.AspNetCore;
using pearlxcore.dev.Data;
using pearlxcore.dev.Models.Entities;
using pearlxcore.dev.Services.Implementations;
using pearlxcore.dev.Services.Interfaces;
using pearlxcore.dev.Web.Data;
using Markdig;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Serilog;
using System.Net;

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
                .CreateLogger();

            try
            {
                Log.Information("Starting application");

                var builder = WebApplication.CreateBuilder(args);

                // Use Serilog
                builder.Host.UseSerilog();

                // Add services to the container.
                builder.Services.AddControllersWithViews();
                builder.Services.AddFluentValidationAutoValidation();
                builder.Services.AddFluentValidationClientsideAdapters();
                builder.Services.AddAntiforgery(options =>
                {
                    options.HeaderName = "RequestVerificationToken";
                });

                // Register validators
                builder.Services.AddValidatorsFromAssemblyContaining<Program>();

                // Trust the local reverse proxy so HTTPS and client IPs survive the hop.
                builder.Services.Configure<ForwardedHeadersOptions>(options =>
                {
                    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
                    options.KnownIPNetworks.Clear();
                    options.KnownProxies.Clear();
                    options.KnownProxies.Add(IPAddress.Loopback);
                    options.KnownProxies.Add(IPAddress.IPv6Loopback);
                });

                // Register DbContext
                var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
                if (string.IsNullOrWhiteSpace(connectionString))
                {
                    throw new InvalidOperationException(
                        "Missing ConnectionStrings:DefaultConnection. Set it in the server environment file " +
                        "(/etc/pearlxcore/pearlxcore.env on Ubuntu) or another environment variable source before starting the service.");
                }

                builder.Services.AddDbContext<ApplicationDbContext>(options =>
                    options.UseSqlServer(connectionString));

                // Configure Identity
                builder.Services
                    .AddIdentity<ApplicationUser, IdentityRole>(options =>
                    {
                        options.Password.RequireDigit = true;
                        options.Password.RequireLowercase = true;
                        options.Password.RequireUppercase = false;
                        options.Password.RequireNonAlphanumeric = false;
                        options.Password.RequiredLength = 8;

                        options.Lockout.MaxFailedAccessAttempts = 5;
                        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(10);

                        options.User.RequireUniqueEmail = true;
                    })
                    .AddEntityFrameworkStores<ApplicationDbContext>()
                    .AddDefaultTokenProviders();

                builder.Services.ConfigureApplicationCookie(options =>
                {
                    options.LoginPath = "/Account/Login";
                    options.AccessDeniedPath = "/Account/AccessDenied";
                });

                // Register application services
                builder.Services.AddScoped<IPostService, PostService>();
                builder.Services.AddScoped<ICategoryService, CategoryService>();
                builder.Services.AddScoped<ITagService, TagService>();
                builder.Services.AddScoped<IProjectService, ProjectService>();
                builder.Services.AddScoped<IAdminProfileService, AdminProfileService>();
                builder.Services.AddScoped<IMediaService, MediaService>();
                builder.Services.AddScoped<IAuditLogService, AuditLogService>();

                // Register background services
                builder.Services.AddHostedService<pearlxcore.dev.Services.BackgroundServices.PostPublishingService>();

            var app = builder.Build();

                // Configure the HTTP request pipeline.
                if (!app.Environment.IsDevelopment())
                {
                    app.UseExceptionHandler("/Home/Error");
                    app.UseHsts();
                    app.UseStatusCodePagesWithReExecute("/Home/StatusCode", "?code={0}");
                }

                app.UseForwardedHeaders();
                app.UseHttpsRedirection();

                app.Use(async (context, next) =>
                {
                    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
                    context.Response.Headers["X-Frame-Options"] = "DENY";
                    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
                    context.Response.Headers["Permissions-Policy"] = "geolocation=(), microphone=(), camera=()";
                    context.Response.Headers["Content-Security-Policy"] =
                        "default-src 'self'; " +
                        "base-uri 'self'; " +
                        "frame-ancestors 'none'; " +
                        "form-action 'self'; " +
                        "object-src 'none'; " +
                        "script-src 'self' 'unsafe-inline' https://static.cloudflareinsights.com; " +
                        "style-src 'self' 'unsafe-inline'; " +
                        "img-src 'self' data: https:; " +
                        "font-src 'self' data: https:; " +
                        "connect-src 'self' https://cloudflareinsights.com https://static.cloudflareinsights.com;";

                    await next();
                });

                // Serve uploaded files from a shared folder outside the release tree
                // so the running service can write them without touching the deployment root.
                var sharedUploadsRoot = Path.GetFullPath(Path.Combine(app.Environment.ContentRootPath, "..", "..", "shared", "uploads"));
                var postImagesPath = Path.Combine(sharedUploadsRoot, "posts");
                var projectImagesPath = Path.Combine(sharedUploadsRoot, "projects");
                Directory.CreateDirectory(postImagesPath);
                Directory.CreateDirectory(projectImagesPath);

                var legacyPostImagesPath = Path.Combine(app.Environment.WebRootPath, "images", "posts");
                var legacyProjectImagesPath = Path.Combine(app.Environment.WebRootPath, "images", "projects");

                var postImageProviders = new List<IFileProvider>
                {
                    new PhysicalFileProvider(postImagesPath)
                };

                if (Directory.Exists(legacyPostImagesPath))
                {
                    postImageProviders.Add(new PhysicalFileProvider(legacyPostImagesPath));
                }

                app.UseStaticFiles(new StaticFileOptions
                {
                    FileProvider = new CompositeFileProvider(postImageProviders),
                    RequestPath = "/images/posts"
                });

                var projectImageProviders = new List<IFileProvider>
                {
                    new PhysicalFileProvider(projectImagesPath)
                };

                if (Directory.Exists(legacyProjectImagesPath))
                {
                    projectImageProviders.Add(new PhysicalFileProvider(legacyProjectImagesPath));
                }

                app.UseStaticFiles(new StaticFileOptions
                {
                    FileProvider = new CompositeFileProvider(projectImageProviders),
                    RequestPath = "/images/projects"
                });

                app.UseStaticFiles();
                app.UseRouting();
                app.UseAuthentication();
                app.UseAuthorization();

                app.MapControllerRoute(
                    name: "areas",
                    pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}"
                );

                app.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");

                await DbInitializer.MigrateAsync(app.Services);

                if (app.Environment.IsDevelopment())
                {
                    await DbInitializer.SeedAsync(app.Services);
                }
                else
                {
                    Log.Information("Skipping startup database seeding in Production.");
                }

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
