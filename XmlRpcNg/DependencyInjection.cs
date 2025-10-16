using Microsoft.Extensions.DependencyInjection;

namespace XmlRpcNg;

/// <summary>
/// Extension methods for registering XML-RPC services with Dependency Injection.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds XML-RPC client services to the dependency injection container.
    /// </summary>
    /// <param name="services">The service collection to add services to</param>
    /// <param name="configure">Configuration action for the XML-RPC client</param>
    /// <returns>The service collection for method chaining</returns>
    public static IServiceCollection AddXmlRpcClient(this IServiceCollection services, Action<XmlRpcClientBuilder>? configure = null)
    {
        if (services == null)
            throw new ArgumentNullException(nameof(services));

        // Register core services as singletons for performance
        services.AddSingleton<IXmlRpcSerializer, XmlRpcSerializer>();
        services.AddSingleton<IRequestBuilder, XmlRpcRequestBuilder>();
        services.AddSingleton<IResponseParser, XmlRpcResponseParser>();

        // Register HttpClient factory
        services.AddHttpClient<XmlRpcClient>();

        // Register the client builder
        services.AddSingleton(sp =>
        {
            var builder = new XmlRpcClientBuilder();

            // Configure with default HttpClient if no custom one is provided
            builder.WithHttpClient(sp.GetRequiredService<HttpClient>());

            // Apply user configuration
            configure?.Invoke(builder);

            return builder.Build();
        });

        return services;
    }

    /// <summary>
    /// Adds XML-RPC client services with a named endpoint.
    /// </summary>
    /// <param name="services">The service collection to add services to</param>
    /// <param name="name">The name for this client configuration</param>
    /// <param name="endpoint">The endpoint URL for the XML-RPC server</param>
    /// <param name="configure">Optional additional configuration</param>
    /// <returns>The service collection for method chaining</returns>
    public static IServiceCollection AddXmlRpcClient(this IServiceCollection services, string name, string endpoint, Action<XmlRpcClientBuilder>? configure = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be null or empty.", nameof(name));

        return services.AddXmlRpcClient(builder =>
        {
            builder.WithUrl(endpoint);
            configure?.Invoke(builder);
        });
    }

    /// <summary>
    /// Adds XML-RPC client services with timeout configuration.
    /// </summary>
    /// <param name="services">The service collection to add services to</param>
    /// <param name="endpoint">The endpoint URL for the XML-RPC server</param>
    /// <param name="timeout">The timeout duration for requests</param>
    /// <param name="configure">Optional additional configuration</param>
    /// <returns>The service collection for method chaining</returns>
    public static IServiceCollection AddXmlRpcClient(this IServiceCollection services, string endpoint, TimeSpan timeout, Action<XmlRpcClientBuilder>? configure = null)
    {
        return services.AddXmlRpcClient(builder =>
        {
            builder
                .WithUrl(endpoint)
                .WithTimeout(timeout);
            configure?.Invoke(builder);
        });
    }
}