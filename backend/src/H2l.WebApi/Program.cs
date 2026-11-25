using H2l.Application;
using H2l.Application.Common;
using H2l.Infrastructure;
using H2l.Infrastructure.Persistence;
using H2l.WebApi.Configuration;
using H2l.WebApi.Hubs;
using H2l.WebApi.Middleware;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Serilog;
using Wolverine;
using Wolverine.FluentValidation;

namespace H2l.WebApi;

public class Program
{
    public static async Task Main(string[] args)
    {
        EnvFileLoader.LoadIfPresent();

        var builder = WebApplication.CreateBuilder(args);

        builder.Host.UseSerilog((context, loggerConfiguration) =>
        {
            loggerConfiguration
                .ReadFrom.Configuration(context.Configuration)
                .Enrich.FromLogContext()
                .WriteTo.Console();
        });

        builder.Host.UseWolverine(options =>
        {
            options.Discovery.IncludeAssembly(typeof(ApplicationServiceCollectionExtensions).Assembly);
            options.UseFluentValidation();
        });

        builder.Services.AddApplication();
        builder.Services.AddInfrastructure(builder.Configuration);
        builder.Services.AddScoped<IRealtimeTaskNotifier, SignalRTaskNotifier>();

        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer();
        builder.Services.AddSingleton<IConfigureOptions<JwtBearerOptions>, JwtBearerOptionsSetup>();
        builder.Services.AddAuthorization();

        builder.Services.AddControllers();
        builder.Services.AddSignalR();
        builder.Services.AddOpenApi();
        builder.Services.AddProblemDetails();
        builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

        var app = builder.Build();

        app.UseMiddleware<CorrelationIdMiddleware>();
        app.UseExceptionHandler();
        app.UseSerilogRequestLogging(options =>
        {
            options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
        });

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapOpenApi();
        app.MapControllers();
        app.MapHub<TasksHub>(TasksHub.HubPath);

        await app.Services.ApplyDatabaseMigrationsAndSeedAsync();

        await app.RunAsync();
    }
}
