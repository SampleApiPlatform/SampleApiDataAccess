using Microsoft.EntityFrameworkCore;
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
