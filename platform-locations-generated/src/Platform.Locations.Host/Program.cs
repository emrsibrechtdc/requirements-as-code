using Asp.Versioning;
using MediatR;
using Platform.Locations.Application.Extensions;
using Platform.Locations.HttpApi;
using Platform.Locations.HttpApi.Extensions;
using Platform.Locations.Infrastructure.Extensions;
using Platform.Locations.SqlServer.Extensions;
using Platform.Shared.Cqrs;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
builder.Host.UseSerilog((context, configuration) => 
    configuration.ReadFrom.Configuration(context.Configuration));

// Add Platform.Shared services
builder.Services.AddPlatformCommonHttpApi(builder.Configuration, builder => { }, builder.Environment, "Locations");
builder.Services.AddPlatformCommonAuditing();
builder.Services.AddIntegrationEventsServices();
builder.Services.AddMultiProductServices();

// Add MediatR with transaction behavior
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(TransactionBehavior<,>));

// Add application services
builder.Services.AddLocationApplication();
builder.Services.AddLocationInfrastructure();
builder.Services.AddLocationHttpApi();

// Add data store
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? "Server=(localdb)\\mssqllocaldb;Database=Platform.Locations;Trusted_Connection=true;MultipleActiveResultSets=true";
builder.Services.AddLocationSqlServer(connectionString);

// Add API versioning
builder.Services.AddApiVersioning(opt =>
{
    opt.DefaultApiVersion = new ApiVersion(1, 0);
    opt.AssumeDefaultVersionWhenUnspecified = true;
    opt.ReportApiVersions = true;
    opt.ApiVersionReader = ApiVersionReader.Combine(
        new UrlSegmentApiVersionReader(),
        new QueryStringApiVersionReader("version"),
        new HeaderApiVersionReader("X-Version"),
        new MediaTypeApiVersionReader("ver")
    );
}).AddApiExplorer(setup =>
{
    setup.GroupNameFormat = "'v'VVV";
    setup.SubstituteApiVersionInUrl = true;
});

// Add OpenAPI/Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Add request middleware for product context
app.UseMiddleware<RequestMiddleware>();

// Configure API versioning
var versionString = builder.Configuration.GetValue<string>("Version");
if (string.IsNullOrEmpty(versionString))
{
    versionString = "1.0.0.0";
}
var version = Version.Parse(versionString);
ApiVersion apiVersion = new ApiVersion(version.Major, version.Minor);
var versionSet = app.NewApiVersionSet().HasApiVersions(new ApiVersion[] { apiVersion }).ReportApiVersions().Build();

// Determine if authorization is required (disabled in development)
bool authorizationRequired = !app.Environment.IsDevelopment();

// Map location API routes
app.MapLocationApiRoutes(versionSet, authorizationRequired);

// Health check endpoint
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }))
    .WithTags("Health")
    .WithOpenApi();

app.Run();
