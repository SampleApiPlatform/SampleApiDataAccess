using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using NuGet.SampleSharedModels.Interfaces;
using SampleApi.Services.MovieServices;
using SampleDataAccessApi.Data;
using SampleDataAccessApi.Extensions;
using SampleDataAccessApi.Interfaces.MovieInterfaces;
using SampleDataAccessApi.Models;
using SampleDataAccessApi.Validators;


var builder = WebApplication.CreateBuilder(args);

// Register services
builder.Services.AddScoped<IMovieRepository, MovieRepository>();
builder.Services.AddScoped<IMovieService, MovieService>();
builder.Services.AddScoped<IValidator<Movie>, MovieValidator>();

// EF Core SQL Azure with retry in case that there are transient connection issues
builder.Services.AddDbContext<MoviesDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("MoviesDb"),
        sqlOptions => sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(10),
            errorNumbersToAdd: null
        )
    )
);
//Another database from another area of the business
//builder.Services.AddDbContext<BillingDbContext>(options =>
//    options.UseSqlServer(
//        builder.Configuration.GetConnectionString("DefaultConnection"),
//        sqlOptions => sqlOptions.EnableRetryOnFailure(
//            maxRetryCount: 5,
//            maxRetryDelay: TimeSpan.FromSeconds(10),
//            errorNumbersToAdd: null
//        )
//    )
//);

//Swagger
builder.Services.AddSwaggerDocumentation();

// Controllers
builder.Services.AddControllers();

// JWT from microsoft account
// App->App  (accepts the same token sampleappapi received)
var clientId = builder.Configuration["AzureAd:ClientId"];
var appIdUri = builder.Configuration["AzureAd:Audience"];
var tenantId = builder.Configuration["AzureAd:TenantId"];
var scope    = builder.Configuration["AzureAd:Scope"] ?? "access_as_user";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = $"https://login.microsoftonline.com/{tenantId}/v2.0";
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateAudience = true,
            ValidAudiences   = new[] { appIdUri, clientId },
            ClockSkew        = TimeSpan.FromMinutes(2)
        };
        options.MapInboundClaims = false;
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AccessAsUser", policy =>
        policy.RequireAssertion(ctx =>
        {
            var scopeClaim = ctx.User.FindFirst(c => c.Type == "scp");
            if (scopeClaim is null) return false;

            return scopeClaim.Value
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Contains(scope, StringComparer.OrdinalIgnoreCase);
        }));
});
//


var app = builder.Build();

app.UseMiddleware<GlobalExceptionMiddleware>();


if (app.Environment.IsDevelopment() || app.Environment.IsStaging())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}


// ⭐ Authentication + Authorization middleware
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();


app.Run();
