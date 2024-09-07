using HotChocolate.Execution.Configuration;
using LiveDocs.GraphQLApi.Models.Replication;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using RxDBDotNet.Security;

namespace RxDBDotNet.Tests.Setup;

/// <summary>
/// Provides configuration options for setting up test environments in RxDBDotNet.
/// This class allows fine-grained control over the application, service, and GraphQL configurations
/// used in test setups, enabling tests to either use default configurations or provide custom ones.
/// </summary>
public class TestSetupOptions
{
    /// <summary>
    /// Indicates whether to apply the default application configuration.
    /// When true, the default configuration from TestSetupBase.ConfigureAppDefaults will be applied
    /// before any custom configuration.
    /// Default value is true.
    /// </summary>
    public bool ApplyDefaultAppConfiguration { get; set; } = true;

    /// <summary>
    /// Indicates whether to apply the default service configuration.
    /// When true, the default configuration from TestSetupBase.ConfigureServiceDefaults will be applied
    /// before any custom configuration.
    /// Default value is true.
    /// </summary>
    public bool ApplyDefaultServiceConfiguration { get; set; } = true;

    /// <summary>
    /// Indicates whether to apply the default GraphQL configuration.
    /// When true, the default configuration from TestSetupBase.ConfigureGraphQLDefaults will be applied
    /// before any custom configuration.
    /// Default value is true.
    /// </summary>
    public bool ApplyDefaultGraphQLConfiguration { get; set; } = true;

    /// <summary>
    /// Gets or sets an optional action to configure the application.
    /// This action will be invoked after the default configuration if ApplyDefaultAppConfiguration is true,
    /// or as the sole configuration if ApplyDefaultAppConfiguration is false.
    /// </summary>
    public Action<IApplicationBuilder>? ConfigureApp { get; init; }

    /// <summary>
    /// Gets or sets an optional action to configure services.
    /// This action will be invoked after the default configuration if ApplyDefaultServiceConfiguration is true,
    /// or as the sole configuration if ApplyDefaultServiceConfiguration is false.
    /// </summary>
    public Action<IServiceCollection>? ConfigureServices { get; init; }

    /// <summary>
    /// Gets or sets an optional action to configure GraphQL.
    /// This action will be invoked after the default configuration if ApplyDefaultGraphQLConfiguration is true,
    /// or as the sole configuration if ApplyDefaultGraphQLConfiguration is false.
    /// </summary>
    public Action<IRequestExecutorBuilder>? ConfigureGraphQL { get; init; }

    /// <summary>
    /// Indicates whether to set up authorization.
    /// When true, authentication and authorization services will be configured.
    /// Default value is false.
    /// </summary>
    public bool SetupAuthorization { get; init; }

    /// <summary>
    /// Gets or sets an optional action to configure workspace security.
    /// This action allows for custom configuration of security options for the Workspace type.
    /// </summary>
    public Action<SecurityOptions<ReplicatedWorkspace>>? ConfigureWorkspaceSecurity { get; init; }

    /// <summary>
    /// Gets or sets an optional action to configure workspace errors.
    /// This action allows for custom configuration of error types for the Workspace type.
    /// </summary>
    public Action<List<Type>>? ConfigureWorkspaceErrors { get; init; }
}
