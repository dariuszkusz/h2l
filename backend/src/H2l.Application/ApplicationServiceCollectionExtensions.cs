using FluentValidation;
using H2l.Application.Connectors.Services;
using H2l.Application.Forms.Expressions;
using H2l.Application.Forms.Schema;
using H2l.Application.Tasks;
using Mapster;
using MapsterMapper;
using Microsoft.Extensions.DependencyInjection;

namespace H2l.Application;

public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = typeof(ApplicationServiceCollectionExtensions).Assembly;

        services.AddValidatorsFromAssembly(assembly);

        var typeAdapterConfig = TypeAdapterConfig.GlobalSettings;
        typeAdapterConfig.Scan(assembly);

        services.AddSingleton(typeAdapterConfig);
        services.AddSingleton<IMapper>(new Mapper(typeAdapterConfig));

        services.AddSingleton<ISafeExpressionEvaluator, SafeExpressionEvaluator>();
        services.AddSingleton<IFormSchemaValidator, FormSchemaValidator>();
        services.AddSingleton<IFormSubmissionValidator, FormSubmissionValidator>();
        services.AddSingleton<IConnectorConfigSerializer, ConnectorConfigSerializer>();
        services.AddSingleton<IConnectorPayloadBuilder, ConnectorPayloadBuilder>();
        services.AddSingleton<ICamundaElementTemplateGenerator, CamundaElementTemplateGenerator>();
        services.AddSingleton<IN8nWorkflowTemplateGenerator, N8nWorkflowTemplateGenerator>();
        services.AddScoped<ITaskAccessEvaluator, TaskAccessEvaluator>();

        return services;
    }
}
