using System.Text.Json;
using System.Threading.RateLimiting;
using AcxiomCRM.Api.Data;
using AcxiomCRM.Api.Dtos;
using AcxiomCRM.Api.Filters;
using AcxiomCRM.Api.Middleware;
using AcxiomCRM.Api.Models;
using AcxiomCRM.Api.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
var config = builder.Configuration;

// ---------- Database (EF Core + SQLite) ----------
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(config.GetConnectionString("DefaultConnection")));

// ---------- ASP.NET Core Identity: password hashing, password policy, lockout, roles ----------
var security = config.GetSection("Security");
builder.Services
    .AddIdentity<ApplicationUser, ApplicationRole>(options =>
    {
        options.Password.RequiredLength = security.GetValue("PasswordMinLength", 8);
        options.Password.RequireUppercase = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireDigit = true;
        options.Password.RequireNonAlphanumeric = true;

        options.Lockout.MaxFailedAccessAttempts = security.GetValue("MaxFailedLogins", 5);
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(security.GetValue("LockoutMinutes", 15));
        options.Lockout.AllowedForNewUsers = true;

        options.User.RequireUniqueEmail = true;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddErrorDescriber<CrmIdentityErrorDescriber>()
    .AddDefaultTokenProviders();

// Secure authentication cookie. For an API we return 401/403 instead of redirecting to a login page.
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.Name = "AcxiomCRM.Auth";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment() ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;
    // MVC pages redirect to the login / access denied page, API calls get 401 / 403 JSON
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.Events.OnRedirectToLogin = context =>
    {
        if (!context.Request.Path.StartsWithSegments("/api"))
        {
            context.Response.Redirect(context.RedirectUri);
            return Task.CompletedTask;
        }
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        return context.Response.WriteAsJsonAsync(new ErrorResponse("Please login to continue."));
    };
    options.Events.OnRedirectToAccessDenied = context =>
    {
        if (!context.Request.Path.StartsWithSegments("/api"))
        {
            context.Response.Redirect(context.RedirectUri);
            return Task.CompletedTask;
        }
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        return context.Response.WriteAsJsonAsync(new ErrorResponse("You do not have permission to perform this action."));
    };
});

// Check the security stamp on every request, so logout / deactivation / role change / password reset
// take effect immediately on all sessions of that user.
builder.Services.Configure<SecurityStampValidatorOptions>(options => options.ValidationInterval = TimeSpan.Zero);

// ---------- Anti-forgery (CSRF) ----------
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-XSRF-TOKEN";
    options.Cookie.Name = "AcxiomCRM.Antiforgery";
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment() ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
});

// ---------- Rate limiting (brute force protection on login/register) ----------
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("auth", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 20, Window = TimeSpan.FromMinutes(15) }));
    options.OnRejected = async (context, token) =>
    {
        await context.HttpContext.Response.WriteAsJsonAsync(
            new ErrorResponse("Too many attempts from this IP. Please try again after 15 minutes."), token);
    };
});

// ---------- MVC (Razor views) + REST API controllers ----------
builder.Services.AddControllersWithViews(options =>
    {
        // CSRF check for every POST/PUT/PATCH/DELETE of the REST API.
        // MVC form posts use [ValidateAntiForgeryToken] on each action.
        options.Filters.Add<ValidateAntiforgeryFilter>();
    })
    .ConfigureApiBehaviorOptions(options =>
    {
        // DataAnnotation errors -> { message, errors: [ { field, message } ] }
        options.InvalidModelStateResponseFactory = context =>
        {
            var errors = context.ModelState
                .Where(e => e.Value?.Errors.Count > 0)
                .Select(e =>
                {
                    var field = e.Key.StartsWith("$.") ? e.Key[2..] : e.Key;
                    field = string.IsNullOrEmpty(field) || field == "request" ? "general" : JsonNamingPolicy.CamelCase.ConvertName(field);
                    var error = e.Value!.Errors[0];
                    // JSON conversion errors (e.g. text in a number field) have technical messages
                    var message = string.IsNullOrEmpty(error.ErrorMessage) || error.Exception != null || error.ErrorMessage.Contains("JSON")
                        ? "Invalid value."
                        : error.ErrorMessage;
                    return new FieldError(field, message);
                })
                .ToList();
            // a wrong type in one field also adds a generic "request field is required" error - drop it if we have real field errors
            if (errors.Any(e => e.Field != "general")) errors = errors.Where(e => e.Field != "general").ToList();
            return new BadRequestObjectResult(new ErrorResponse("Please correct the highlighted fields.", errors));
        };
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new() { Title = "AcxiomCRM API", Version = "v1", Description = "REST API for AcxiomCRM. See docs/API.md for details." });
});

builder.Services.AddHttpContextAccessor();

// ---------- Application services ----------
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<IScopeService, ScopeService>();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IPasswordHistoryService, PasswordHistoryService>();
builder.Services.AddScoped<ICustomerService, CustomerService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ILeadService, LeadService>();
builder.Services.AddScoped<IOpportunityService, OpportunityService>();
builder.Services.AddScoped<IFollowUpService, FollowUpService>();
builder.Services.AddScoped<IActivityService, ActivityService>();
builder.Services.AddScoped<IAuditLogService, AuditLogService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IReportService, ReportService>();

var app = builder.Build();

// ---------- Create / upgrade the database and add demo data on first run ----------
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await db.Database.MigrateAsync();
    await DbSeeder.SeedAsync(scope.ServiceProvider);
}

// ---------- HTTP pipeline ----------
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    // Friendly error page for MVC, HTTPS only in production
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
    app.UseHttpsRedirection();
}
app.UseStatusCodePagesWithReExecute("/Home/StatusCode/{0}");
app.UseMiddleware<ExceptionMiddleware>();

// Basic security headers
app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["Referrer-Policy"] = "no-referrer";
    await next();
});

// css / js / libraries for the Razor views
app.UseStaticFiles();
app.UseRouting();

app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapControllerRoute(name: "default", pattern: "{controller=Dashboard}/{action=Index}/{id?}");
app.MapGet("/api/health", () => Results.Ok(new { status = "ok" }));

app.Run();
