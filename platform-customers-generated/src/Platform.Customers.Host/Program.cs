using Asp.Versioning;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Platform.Customers.Application.Extensions;
using Platform.Customers.HttpApi;
using Platform.Customers.HttpApi.Extensions;
using Platform.Customers.Infrastructure.Extensions;
using Platform.Shared.Cqrs.Mediatr;
using Platform.Shared.IntegrationEvents;
using Serilog;

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.Seq("http://localhost:5341")
    .CreateLogger();

    var builder = WebApplication.CreateBuilder(args);
    
    // Use Serilog for logging
    builder.Host.UseSerilog();

    // Add services to the container
    var services = builder.Services;
    var configuration = builder.Configuration;
    var environment = builder.Environment;

    // Platform.Shared services are now added in the respective extension methods

    // Add MediatR with transaction behavior
    services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));
    // services.AddTransient(typeof(IPipelineBehavior<,>), typeof(TransactionBehavior<,>)); // TransactionBehavior may have moved

    // Add application services
    services.AddCustomersApplication();
    
    // Add data store
    
    services.AddCustomersInfrastructure(configuration);
    
    services.AddCustomerHttpApi(configuration, builder.Configuration, environment);

    // Add integration events (temporarily disabled due to missing dependencies)
    // services.AddCustomersIntegrationEvents(configuration);
    
    services.AddApiVersioning(options =>
    {
        options.DefaultApiVersion = new ApiVersion(1, 0);
        options.AssumeDefaultVersionWhenUnspecified = true;
    }).AddApiExplorer(setup =>
    {
        setup.GroupNameFormat = "'v'VVV";
        setup.SubstituteApiVersionInUrl = true;
    });


    // Add OpenAPI/Swagger
    services.AddEndpointsApiExplorer();
    services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new() { Title = "Platform Customers API", Version = "v1" });
    });

    var app = builder.Build();

    // Configure the HTTP request pipeline
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(c => 
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "Platform Customers API v1");
            c.RoutePrefix = "swagger";
        });
    }

    app.UseHttpsRedirection();

    // Add request middleware before routing (temporarily disabled due to dependency issues)
    // app.UseMiddleware<RequestMiddleware>();

    app.UseRouting();
    app.UseAuthorization();

    // Configure API versioning
    var versionString = configuration.GetValue<string>("Version");
    if (string.IsNullOrEmpty(versionString))
    {
        versionString = "1.0.0.0";
    }
    var version = Version.Parse(versionString);
    ApiVersion apiVersion = new ApiVersion(version.Major, version.Minor);
    var versionSet = app.NewApiVersionSet().HasApiVersion(apiVersion).ReportApiVersions().Build();

    // Map API routes
    app.MapCustomersApiRoutes(versionSet, authorizationRequired: false);

    // Health check endpoint
    app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }))
        .WithTags("Health");

    app.Run();