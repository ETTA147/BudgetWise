using BudgetWise.Models;
using BudgetWise.Areas.Identity.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using Microsoft.AspNetCore.Identity.UI.Services;
using Syncfusion.Blazor;
using BudgetWise.Data;
using BudgetWise.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
ConfigureServices(builder.Services, builder.Configuration);

var app = builder.Build();

// Configure the HTTP request pipeline
ConfigureApp(app);

app.Run();

void ConfigureServices(IServiceCollection services, IConfiguration configuration)
{
    // Database Configuration
    var connectionString = configuration.GetConnectionString("DefaultConnection");
    if (string.IsNullOrEmpty(connectionString))
    {
        throw new InvalidOperationException("Database connection string is not configured.");
    }
    services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(connectionString));
    services.AddDbContext<AuthDbContext>(options => options.UseSqlServer(connectionString));

    // Identity Configuration
    services.AddDefaultIdentity<ApplicationUser>(options =>
    {
        options.SignIn.RequireConfirmedAccount = false;
        options.Password.RequireUppercase = false;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>() // Use ApplicationDbContext instead of AuthDbContext
    .AddDefaultTokenProviders();

    // MVC and Razor Pages
    services.AddControllersWithViews()
        .AddRazorRuntimeCompilation();
    services.AddRazorPages();

    // HTTP Client
    services.AddHttpClient();

    // Core Services
    services.AddMemoryCache();
    services.AddLogging();

    services.AddScoped<IDashboardService, UserDashboardService>(sp => {
        var context = sp.GetRequiredService<ApplicationDbContext>();
        var userManager = sp.GetRequiredService<UserManager<ApplicationUser>>();
        var httpContext = sp.GetRequiredService<IHttpContextAccessor>();
        var userId = userManager.GetUserId(httpContext.HttpContext?.User);
        return new UserDashboardService(context, userManager, userId ?? string.Empty);
    });
    services.AddTransient<DemoDashboardService>();
    services.AddHttpContextAccessor();

    // Cookie Policy Configuration
    services.Configure<CookiePolicyOptions>(options =>
    {
        options.MinimumSameSitePolicy = SameSiteMode.None;
        options.Secure = CookieSecurePolicy.Always;
        options.HttpOnly = Microsoft.AspNetCore.CookiePolicy.HttpOnlyPolicy.Always;
    });

    // Identity Cookie Configuration
    services.ConfigureApplicationCookie(options =>
    {
        options.Events.OnRedirectToLogin = context =>
        {
            context.HttpContext.Response.Redirect("/Identity/Account/Login");
            return Task.CompletedTask;
        };
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always; // Ensure HTTPS is being used
        options.Cookie.SameSite = SameSiteMode.Lax; // Change to 'Lax' if 'Strict' causes issues
    });

    // Syncfusion Configuration
    var directLicenseKey = "Ngo9BigBOggjHTQxAR8/V1NMaF5cXmBCf1FpRmJGdld5fUVHYVZUTXxaS00DNHVRdkdmWXxcc3VQR2ZZWE10X0c=";
    Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense(directLicenseKey);
}

void ConfigureApp(WebApplication app)
{
    // Development/Production environment configuration
    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/Home/Error");
        app.UseHsts();
    }

    // Middleware Pipeline
    // app.UseHttpsRedirection();
    app.UseStaticFiles();
    app.UseRouting();
    app.UseAuthentication();
    app.UseAuthorization();
    app.UseCookiePolicy();

    // Middleware to handle domain redirection
    app.Use(async (context, next) =>
    {
        var request = context.Request;
        var host = request.Host.ToString();
        // Redirect from Heroku domain to custom domain
        if (host == "budgetwise-expense-tracker-f4aae4b8ebbc.herokuapp.com")
        {
            var newUrl = $"https://www.budget-wise.net{request.Path}{request.QueryString}";
            context.Response.Redirect(newUrl);
        }
        else
        {
            await next();
        }
    });

    // Route Configuration
    app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}");

    app.MapControllerRoute(
        name: "dashboard",
        pattern: "Dashboard/{action=Index}/{id?}",
        defaults: new { controller = "Dashboard" });

    app.MapControllerRoute(
        name: "category",
        pattern: "Category/{action=Index}/{id?}",
        defaults: new { controller = "Category" });

    app.MapControllerRoute(
        name: "transaction",
        pattern: "Transaction/{action=Index}/{id?}",
        defaults: new { controller = "Transaction" });
        
    app.MapControllerRoute(
        name: "demodasboard",
        pattern: "DemoDashboard/{action=Demo}/{id?}",
        defaults: new { controller = "DemoDashboard" });

    app.MapRazorPages();
    using (var scope = app.Services.CreateScope())
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        dbContext.Database.Migrate();
    }
}