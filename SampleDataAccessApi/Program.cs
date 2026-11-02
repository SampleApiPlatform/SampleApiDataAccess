using System.Text.Json;
using System.Text.Json.Serialization;
using Azure.Identity;
using Azure.Messaging.ServiceBus;
using Azure.Monitor.OpenTelemetry.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using NuGet.SampleSharedModels.Interfaces;
using NuGet.SampleSharedModels.Interfaces.ServiceBus;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using SampleApi.Services.MovieServices;
using SampleDataAccessApi.Data;
using SampleDataAccessApi.Extensions;
using SampleDataAccessApi.Interfaces.MovieInterfaces;
using SampleDataAccessApi.Models;
using SampleDataAccessApi.Validators;

var builder = WebApplication.CreateBuilder(args);
// setting up applicatons insights
var keyVaultUri = builder.Configuration["KeyVaultUri"];

if (string.IsNullOrWhiteSpace(keyVaultUri))
{
    throw new InvalidOperationException("KeyVaultUri configuration value is missing.");
}
// ---------------------------------------------------------
// Key Vault integrate
// We access the key vault with the System Identity
// Then we can query durectly values from the Key Vault as:
// var clientSecret = builder.Configuration["AzureAdClientSecret2"];
// ---------------------------------------------------
builder.Configuration.AddAzureKeyVault(
    new Uri(keyVaultUri),
    new DefaultAzureCredential());

//recover the connection string from the key vault
var ai_connectionString = builder.Configuration["applications-insights-connection-string"];
// Zusätzliche Instrumentierung VOR dem Distro registrieren
builder.Services.ConfigureOpenTelemetryTracerProvider((sp, b) => b
    .AddSource("sampleapiplatform")
    .AddEntityFrameworkCoreInstrumentation());

builder.Services.ConfigureOpenTelemetryMeterProvider((sp, b) => b
    .AddRuntimeInstrumentation());

// Distro bringt ASP.NET Core, HttpClient, SqlClient, ILogger, Standard-Metriken
builder.Services.AddOpenTelemetry()
    .UseAzureMonitor(o => o.ConnectionString = ai_connectionString);


// 1. ServiceBusClient als Singleton (lebt solange wie die App)
builder.Services.AddSingleton(sp =>
{
    // reading values from appsettings.json
    var cfg = sp.GetRequiredService<IConfiguration>();
    var ns  = cfg["ServiceBus:FullyQualifiedNamespace"]
              ?? throw new InvalidOperationException("ServiceBus:FullyQualifiedNamespace missing");
    //var q   = cfg["ServiceBus:QueueName"]
    //          ?? throw new InvalidOperationException("ServiceBus:QueueName missing");

    //we pickup explicitly the system managed identity of samplesataccessapi. The defult one that should also that gives issues.
    var credential = new ManagedIdentityCredential(
        ManagedIdentityId.SystemAssigned
    );

    var client = new ServiceBusClient(ns, credential);
    //var client = new ServiceBusClient(ns, new DefaultAzureCredential());
    return client;
    //return client.CreateReceiver(q);      // disposed when the application shuts down
});
// 2. ServiceBusReceiver als Singleton (vom Client erzeugt)
builder.Services.AddSingleton<ServiceBusReceiver>(sp =>
{
    var client = sp.GetRequiredService<ServiceBusClient>();
    var cfg = sp.GetRequiredService<IConfiguration>();
    var q = cfg["ServiceBus:QueueName"]
            ?? throw new InvalidOperationException("ServiceBus:QueueName missing");
    return client.CreateReceiver(q);
});
// 3. JsonSerializerOptions (falls du sie customisierst)
builder.Services.AddSingleton<JsonSerializerOptions>(sp =>
{
    return new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        Converters = { new JsonStringEnumConverter() } //alllows to deserialize string Add into 0, etc.
    };
});
// 4. BusReceiver als Scoped
builder.Services.AddScoped<IBusReceiver, BusReceiver>();
// Register services
builder.Services.AddScoped<IMovieRepository, MovieRepository>();
builder.Services.AddScoped<IBusRequestRepository, BusRequestRepository>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

//builder.Services.AddScoped<IMovieService, MovieService>();
builder.Services.AddScoped<IMovieBusService, MovieBusService>();
builder.Services.AddScoped<IMovieService, MovieService>();
builder.Services.AddScoped<IBusRequestService, BusRequestService>();

//Validators are singeltons because they do not contain properties that change with every request
//Validators they just scan an object and can be the same instance for the whole life of the application.
//builder.Services.AddSingleton<IValidator<Movie>, MovieValidator>();
// So registrierst du alle Fluent Validatoren auf einmal
//builder.Services.AddValidatorsFromAssembly(typeof(MovieValidator).Assembly);
//builder.Services.AddValidatorsFromAssembly(typeof(BusRequestValidator).Assembly);
//builder.Services.AddValidatorsFromAssemblyContaining<Program>();
builder.Services.AddSingleton<IValidator<BusRequest>, BusRequestValidator>();
builder.Services.AddSingleton<IValidator<Movie>, MovieValidator>();

// Service that continuosly listens to the Service Queue: it is like a background thread in our program
// we do not need to implement anything in Azure Portal (define a service host) 
// it is created by the application as soon as it starts and it reamins alive while the application is running
// once the application finish this background process alsi finishes.
builder.Services.AddHostedService<MessageProcessorBackgroundService>();


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
