using APIGateway.Auth;
using Contracts.Common;
using Kernel.Middlewares;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using System.Text;
using System.Threading.RateLimiting;
using Wolverine;
using Wolverine.Http;

var builder = WebApplication.CreateBuilder(args);
builder.ConfigureEshopDockerKestrel();

builder.AddServiceDefaults();
builder.Services.AddWolverineHttp();
builder.Host.UseWolverine();
// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));
if (builder.Environment.IsEnvironment("Testing"))
{
    var testDbName = builder.Configuration["Auth:TestDbName"] ?? $"auth-test-db-{Guid.NewGuid():N}";
    builder.Services.AddDbContext<AuthDbContext>(options =>
        options.UseInMemoryDatabase(testDbName));
}
else
{
    builder.Services.AddDbContext<AuthDbContext>(options =>
        options.UseNpgsql(builder.Configuration.GetConnectionString("authdb")));
}
builder.Services
    .AddIdentityCore<Account>(options =>
    {
        options.User.RequireUniqueEmail = true;
        options.Password.RequireDigit = true;
        options.Password.RequiredLength = 8;
        options.Password.RequireUppercase = true;
    })
    .AddRoles<Role>()
    .AddEntityFrameworkStores<AuthDbContext>()
    .AddSignInManager();

var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = jwtOptions.Issuer,
        ValidateAudience = true,
        ValidAudience = jwtOptions.Audience,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey)),
        ValidateLifetime = true,
        ClockSkew = TimeSpan.FromSeconds(30)
    };
});

builder.Services.AddAuthorizationBuilder()
    .AddPolicy(PolicyNames.IsOrderOwner, policy => policy.RequireAuthenticatedUser().RequireClaim(AuthClaimTypes.CustomerId))
    .AddPolicy(PolicyNames.CanModifyOwnedWarehouse, policy => policy.RequireAuthenticatedUser().RequireClaim(AuthClaimTypes.WarehouseId));

builder.Services.AddAuthorization(options =>
{
    foreach (var permission in PermissionConstants.All)
    {
        var policyName = PolicyNames.GetPolicyByPermission(permission);
        options.AddPolicy(policyName,
            policy => policy.RequireAuthenticatedUser().RequireClaim(AuthClaimTypes.Permission, permission));
    }
});

builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<AuthDataSeeder>();

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    // Policy "fixed" - 100 requests / 1 minute / 1 IP
    options.AddFixedWindowLimiter("fixed", opt =>
    {
        opt.PermitLimit = 100;
        opt.Window = TimeSpan.FromMinutes(1);
        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        opt.QueueLimit = 5;
    });

    // Policy "sliding" - 10 requests / 10s / 1 IP (for sensitive endpoints)
    options.AddSlidingWindowLimiter("sliding", opt =>
    {
        opt.PermitLimit = 10;
        opt.Window = TimeSpan.FromSeconds(10);
        opt.SegmentsPerWindow = 2;
        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        opt.QueueLimit = 2;
    });

    // Global limiter — apply default for all requests based on IP
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 100,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 5
            }));
});

var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>() ?? [];

builder.Services.AddCors(options =>
{
    // Policy 1: Allow All for Development
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });

    // Policy 2: Allow Specific domains for Production
    options.AddPolicy("AllowSpecificOrigins", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

var app = builder.Build();

app.MapDefaultEndpoints();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
if (app.Environment.IsDevelopment())
{
    app.UseCors("AllowAll");
}
else
{
    app.UseCors("AllowSpecificOrigins");
}

app.UseMiddleware<GlobalExceptionHandler>();

app.MapReverseProxy();
app.MapWolverineEndpoints();
app.MapAuthEndpoints();
app.MapGet("/", () => Results.Redirect("scalar/v1"));

//app.MapGet("/hello", () => "Ok")
//    .RequireRateLimiting("sliding");

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
    if (db.Database.IsRelational())
    {
        await db.Database.MigrateAsync();
    }

    await db.Database.EnsureCreatedAsync();
    var seeder = scope.ServiceProvider.GetRequiredService<AuthDataSeeder>();
    await seeder.SeedAsync();
}

app.Run();

public partial class Program;
