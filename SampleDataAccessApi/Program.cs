using Microsoft.EntityFrameworkCore;
using SampleApi.Services.MovieServices;
using SampleDataAccessApi.Data;
using SampleDataAccessApi.Extensions;
using SampleDataAccessApi.Interfaces.MovieInterfaces;


var builder = WebApplication.CreateBuilder(args);

// Load JWT settings
//var jwtSettings = builder.Configuration.GetSection("Jwt");
//var key = jwtSettings.GetValue<string>("Key")
//    ?? throw new Exception("JWT Key is missing in configuration");
//
//var issuer = jwtSettings.GetValue<string>("Issuer")
//    ?? throw new Exception("JWT Issuer is missing in configuration");
//
//var audience = jwtSettings.GetValue<string>("Audience")
//    ?? throw new Exception("JWT Audience is missing in configuration");

// Register services: DONT THEY COME FROM NUGET NOW??
//var sharedServicesUrl = builder.Configuration["ServiceUrls:SharedServices"];
//ArgumentException.ThrowIfNullOrWhiteSpace(sharedServicesUrl);
//builder.Services.AddHttpClient<ISharedServicesClient, SharedServicesClient>(client =>
//{
//    client.BaseAddress = new Uri(sharedServicesUrl);
//});

// Register services
builder.Services.AddScoped<IMovieRepository, MovieRepository>();
builder.Services.AddScoped<IMovieService, MovieService>();

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

// ⭐ Register Authentication + JWT Bearer
//builder.Services.AddAuthentication(options =>
//{
//    options.DefaultAuthenticateScheme = "JwtBearer";
//    options.DefaultChallengeScheme = "JwtBearer";
//})
//.AddJwtBearer("JwtBearer", options =>
//{
//    options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
//    {
//        ValidateIssuer = true,
//        ValidateAudience = true,
//        ValidateLifetime = true,
//        ValidateIssuerSigningKey = true,
//
//        ValidIssuer = issuer,
//        ValidAudience = audience,
//        IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(
//            System.Text.Encoding.UTF8.GetBytes(key)
//        )
//    };
//});
// App->App
builder.Services.AddAuthentication("Bearer")
    .AddJwtBearer("Bearer", options =>
    {
        options.Authority = "https://login.microsoftonline.com/9ca003dc-da5d-4b31-808b-9d52337fbf6a/v2.0";
        options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
        {
            ValidateAudience = true,
            ValidAudience = "api://c2c22245-dc03-4cb8-ad4e-4d3471f04ab6"
        };
    });


// Authorization
builder.Services.AddAuthorization();

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
