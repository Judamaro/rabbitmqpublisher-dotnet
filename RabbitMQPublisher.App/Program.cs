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
        .AddPresentation()
        .AddObservability();

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

    int metricsPort = app.Configuration.GetMetricsPort();
    app.UseWhen(context => context.Connection.LocalPort != metricsPort, branch => branch.UseHttpsRedirection());

    app.MapControllers();
    app.MapHealthEndpoints();
    app.MapMetricsEndpoint();

    await app.RunAsync();
}
catch (Exception exception) when (exception is not HostAbortedException)
{
    Log.Fatal(exception, "The application terminated unexpectedly");
    Environment.ExitCode = 1;
}
finally
{
    await Log.CloseAndFlushAsync();
}
