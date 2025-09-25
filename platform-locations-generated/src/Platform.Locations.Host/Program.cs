using Asp.Versioning;
using MediatR;
using Platform.Locations.Application.Extensions;
using Platform.Locations.Application.Locations;
using Platform.Locations.HttpApi;
using Platform.Locations.HttpApi.Extensions;
using Platform.Locations.Infrastructure.Extensions;
using Platform.Shared.Cqrs.Mediatr;
using Platform.Shared.HttpApi.Extensions;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
builder.Host.UseSerilog((context, configuration) => 
    configuration.ReadFrom.Configuration(context.Configuration));

// Platform.Shared services are now added in the respective extension methods

// Add MediatR with transaction behavior
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));
// builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(TransactionBehavior<,>)); // TransactionBehavior may have moved

// Add application services
builder.Services.AddLocationApplication();
builder.Services.AddLocationInfrastructure(builder.Configuration);
builder.Services.AddLocationHttpApi(builder.Configuration, (IConfigurationBuilder)builder.Configuration, builder.Environment);


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
var versionSet = app.NewApiVersionSet().HasApiVersion(apiVersion).ReportApiVersions().Build();

app.UsePlatformCommonHttpApi("Locations", $"{version.Major}.{version.Minor}");
// Determine if authorization is required (disabled in development)
bool authorizationRequired = !app.Environment.IsDevelopment();

// Map location API routes
app.MapLocationApiRoutes(versionSet, authorizationRequired);

// Health check endpoint
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }))
    .WithTags("Health");

app.Run();

// Make Program class accessible to integration tests
public partial class Program { }
