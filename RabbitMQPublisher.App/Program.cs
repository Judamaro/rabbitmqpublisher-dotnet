// Bootstrap logger: captures errors raised before the host and its configuration are built.
Log.Logger = new LoggerConfiguration().WriteTo.Console().CreateBootstrapLogger();

try
{
    WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

    builder.Services.AddSerilog((services, configuration) => configuration
        .ReadFrom.Configuration(builder.Configuration)
        .ReadFrom.Services(services));

    builder.Services
        .AddApplication()
        .AddInfrastructure(builder.Configuration)
        .AddPresentation();

    WebApplication app = builder.Build();

    app.UseRequestLogging();
    app.UseExceptionHandler();
    app.UsePetitionResponseStatusCodePages();

    await app.ApplyMigrationsIfEnabledAsync();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseHttpsRedirection();

    app.MapControllers();
    app.MapHealthEndpoints();

    await app.RunAsync();
}
catch (Exception exception) when (exception is not HostAbortedException)
{
    Log.Fatal(exception, "The application terminated unexpectedly");
}
finally
{
    await Log.CloseAndFlushAsync();
}
