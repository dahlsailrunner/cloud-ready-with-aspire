using System.Diagnostics;
using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace Microsoft.Extensions.Hosting;

// Adds common Aspire services: service discovery, resilience, health checks, and OpenTelemetry.
// This project should be referenced by each service project in your solution.
// To learn more about using this project, see https://aka.ms/dotnet/aspire/service-defaults
public static class Extensions
{
    private const string HealthEndpointPath = "/health";
    private const string AlivenessEndpointPath = "/alive";
    private const string ServiceNamespace = "carvedrock-sample";
    private const string OtelResourceAttributesKey = "OTEL_RESOURCE_ATTRIBUTES";

    public static TBuilder AddServiceDefaults<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        builder.ConfigureOpenTelemetry();

        builder.AddDefaultHealthChecks();

        builder.Services.AddServiceDiscovery();

        builder.Services.ConfigureHttpClientDefaults(http =>
        {
            // Turn on resilience by default
            http.AddStandardResilienceHandler();

            // Turn on service discovery by default
            http.AddServiceDiscovery();
        });

        // Uncomment the following to restrict the allowed schemes for service discovery.
        // builder.Services.Configure<ServiceDiscoveryOptions>(options =>
        // {
        //     options.AllowedSchemes = ["https"];
        // });

        return builder;
    }

    public static TBuilder ConfigureOpenTelemetry<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        // docs: https://aspire.dev/fundamentals/telemetry/        

        var entryAssembly = Assembly.GetEntryAssembly();

        var resourceAttributes = MergeOtelResourceAttributes(
            builder.Configuration[OtelResourceAttributesKey],
            new KeyValuePair<string, string>("service.namespace", ServiceNamespace),
            new KeyValuePair<string, string>("deployment.environment.name", builder.Environment.EnvironmentName),
            new KeyValuePair<string, string>("telemetry.sdk.version", Environment.Version.ToString()));

        builder.Configuration[OtelResourceAttributesKey] = resourceAttributes;
        Environment.SetEnvironmentVariable(OtelResourceAttributesKey, resourceAttributes);

        builder.Logging.AddOpenTelemetry(logging =>
        {
            logging.IncludeScopes = true;
            logging.AddProcessor(new ExceptionDataProcessor());
        });

        builder.Services.AddOpenTelemetry()
            .WithMetrics(metrics =>
            {
                metrics
                    .AddMeter("CarvedRock.*")
                    .AddMeter("Experimental.ModelContextProtocol")
                    //.AddMeter("*.*") // review what's available
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation();
            })
            .WithTracing(tracing =>
            {
                tracing
                    .AddSource(builder.Environment.ApplicationName)
                    //.AddSource("*.*") // review what's available
                    .AddSource("Experimental.ModelContextProtocol")
                    .AddAspNetCoreInstrumentation(tracing =>
                        // Exclude health check requests from tracing
                        tracing.Filter = context =>
                            !context.Request.Path.StartsWithSegments(HealthEndpointPath)
                            && !context.Request.Path.StartsWithSegments(AlivenessEndpointPath)
                    )
                    .AddHttpClientInstrumentation();
            });
        builder.Services.AddSingleton(new ActivitySource(builder.Environment.ApplicationName));

        builder.AddOpenTelemetryExporters();

        return builder;
    }

    private static TBuilder AddOpenTelemetryExporters<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        var useOtlpExporter = !string.IsNullOrWhiteSpace(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]);

        if (useOtlpExporter)
        {
            builder.Services.AddOpenTelemetry().UseOtlpExporter();
        }
        return builder;
    }

    public static TBuilder AddDefaultHealthChecks<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        builder.Services.AddHealthChecks()
            // Add a default liveness check to ensure app is responsive
            .AddCheck("self", () => HealthCheckResult.Healthy(), ["live"]);

        return builder;
    }

    public static WebApplication MapDefaultEndpoints(this WebApplication app)
    {
        // Adding health checks endpoints to applications in non-development environments has security implications.
        // See https://aka.ms/dotnet/aspire/healthchecks for details before enabling these endpoints in non-development environments.
        if (app.Environment.IsDevelopment())
        {
            // All health checks must pass for app to be considered ready to accept traffic after starting
            app.MapHealthChecks(HealthEndpointPath);

            // Only health checks tagged with the "live" tag must pass for app to be considered alive
            app.MapHealthChecks(AlivenessEndpointPath, new HealthCheckOptions
            {
                Predicate = r => r.Tags.Contains("live")
            });
        }

        return app;
    }

    private static string MergeOtelResourceAttributes(
        string? existingValue,
        params KeyValuePair<string, string>[] attributes)
    {
        var mergedAttributes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        if (!string.IsNullOrWhiteSpace(existingValue))
        {
            foreach (var attribute in existingValue.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                var separatorIndex = attribute.IndexOf('=');
                if (separatorIndex <= 0 || separatorIndex == attribute.Length - 1)
                {
                    continue;
                }

                var key = attribute[..separatorIndex].Trim();
                var value = attribute[(separatorIndex + 1)..].Trim();
                if (key.Length == 0 || value.Length == 0)
                {
                    continue;
                }

                mergedAttributes[key] = value;
            }
        }

        foreach (var attribute in attributes)
        {
            mergedAttributes[attribute.Key] = attribute.Value;
        }

        return string.Join(',', mergedAttributes.Select(attribute => $"{attribute.Key}={attribute.Value}"));
    }
}


