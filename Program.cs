using System.Text;
using lms_api.Data;
using lms_api.Models;
using lms_api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// ─── Database ────────────────────────────────────────────────────────────────

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// ─── Identity ────────────────────────────────────────────────────────────────

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireDigit = false;
    options.User.RequireUniqueEmail = true;
    options.SignIn.RequireConfirmedEmail = false;
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();

// ─── JWT Authentication ───────────────────────────────────────────────────────

string jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("Jwt:Key is required in configuration.");
string jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "lms-api";
string jwtAudience = builder.Configuration["Jwt:Audience"] ?? "lms-frontend";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        ClockSkew = TimeSpan.FromMinutes(1)
    };
})
.AddGoogle(options =>
{
    options.ClientId = builder.Configuration["Authentication:Google:ClientId"] ?? "REPLACE_ME";
    options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"] ?? "REPLACE_ME";
})
.AddMicrosoftAccount(options =>
{
    options.ClientId = builder.Configuration["Authentication:Microsoft:ClientId"] ?? "REPLACE_ME";
    options.ClientSecret = builder.Configuration["Authentication:Microsoft:ClientSecret"] ?? "REPLACE_ME";
});

builder.Services.AddAuthorization();

// ─── CORS ─────────────────────────────────────────────────────────────────────

builder.Services.AddCors(options =>
{
    options.AddPolicy("ViteDev", policy =>
    {
        policy.WithOrigins("http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// ─── Application Services ─────────────────────────────────────────────────────

builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IEmailService, EmailService>();

// ─── Controllers + Swagger ────────────────────────────────────────────────────

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "LMS API", Version = "v1" });
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter your JWT token."
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

WebApplication app = builder.Build();

// ─── Middleware ────────────────────────────────────────────────────────────────

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("ViteDev");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// ─── Database Seed ────────────────────────────────────────────────────────────

await SeedDatabaseAsync(app);

app.Run();

// ─── Seed Method ──────────────────────────────────────────────────────────────

static async Task SeedDatabaseAsync(WebApplication app)
{
    using IServiceScope scope = app.Services.CreateScope();
    IServiceProvider services = scope.ServiceProvider;
    ILogger<Program> logger = services.GetRequiredService<ILogger<Program>>();

    try
    {
        AppDbContext db = services.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();

        RoleManager<IdentityRole> roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        UserManager<ApplicationUser> userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

        // Ensure roles exist
        string[] roles = new string[] { "Admin", "Learner" };
        foreach (string role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
                logger.LogInformation("Created role: {Role}", role);
            }
        }

        // Seed admin user
        const string adminEmail = "admin@lms.local";
        const string adminPassword = "Admin123!";

        ApplicationUser? adminUser = await userManager.FindByEmailAsync(adminEmail);
        if (adminUser == null)
        {
            adminUser = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                FirstName = "Admin",
                LastName = "User",
                IsAdmin = true,
                EmailConfirmed = true
            };

            IdentityResult result = await userManager.CreateAsync(adminUser, adminPassword);
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, "Admin");
                logger.LogInformation("Seeded admin user: {Email}", adminEmail);
            }
            else
            {
                logger.LogError("Failed to seed admin user: {Errors}",
                    string.Join(", ", result.Errors.Select(e => e.Description)));
            }
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred during database seeding.");
    }
}
