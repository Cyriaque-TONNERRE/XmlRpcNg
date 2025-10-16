namespace XmlRpcNg;

/// <summary>
/// Main interface for XML-RPC client functionality.
/// Provides type-safe method calling with automatic parameter and return type conversion.
/// </summary>
public interface IXmlRpcClient
{
    /// <summary>
    /// Calls an XML-RPC method with automatic type conversion for the return value.
    /// </summary>
    /// <typeparam name="T">The expected return type</typeparam>
    /// <param name="methodName">The name of the method to call</param>
    /// <param name="parameters">The parameters to pass to the method</param>
    /// <returns>The method call result converted to the specified type</returns>
    /// <example>
    /// <code>
    /// var client = new XmlRpcClientBuilder()
    ///     .WithUrl("https://api.example.com/xmlrpc")
    ///     .Build();
    ///
    /// // Call with automatic type conversion
    /// var result = await client.CallAsync&lt;string&gt;("echo", "Hello World");
    /// var sum = await client.CallAsync&lt;int&gt;("add", 5, 3);
    /// </code>
    /// </example>
    Task<T> CallAsync<T>(string methodName, params object[] parameters);

  
    /// <summary>
    /// Calls an XML-RPC method returning the raw XmlRpcValue.
    /// </summary>
    /// <param name="methodName">The name of the method to call</param>
    /// <param name="parameters">The parameters to pass to the method</param>
    /// <returns>The method call result as an XmlRpcValue</returns>
    Task<XmlRpcValue> CallAsync(string methodName, params object[] parameters);

  
    /// <summary>
    /// Gets the endpoint URL this client is configured to use.
    /// </summary>
    Uri Endpoint { get; }

    /// <summary>
    /// Gets the timeout duration for HTTP requests.
    /// </summary>
    TimeSpan Timeout { get; }
}

/// <summary>
/// Builder interface for creating configured XmlRpcClient instances.
/// Provides fluent configuration with validation.
/// </summary>
public interface IXmlRpcClientBuilder
{
    /// <summary>
    /// Sets the endpoint URL for the XML-RPC server.
    /// </summary>
    /// <param name="url">The endpoint URL (must be HTTP or HTTPS)</param>
    /// <returns>The builder instance for method chaining</returns>
    /// <example>
    /// <code>
    /// var builder = new XmlRpcClientBuilder()
    ///     .WithUrl("https://api.example.com/xmlrpc")
    ///     .WithTimeout(TimeSpan.FromSeconds(30));
    /// </code>
    /// </example>
    IXmlRpcClientBuilder WithUrl(string url);

    /// <summary>
    /// Sets the timeout duration for HTTP requests.
    /// </summary>
    /// <param name="timeout">The timeout duration</param>
    /// <returns>The builder instance for method chaining</returns>
    IXmlRpcClientBuilder WithTimeout(TimeSpan timeout);

    /// <summary>
    /// Sets a custom HttpClient to use for HTTP communication.
    /// </summary>
    /// <param name="httpClient">The HttpClient to use</param>
    /// <returns>The builder instance for method chaining</returns>
    IXmlRpcClientBuilder WithHttpClient(HttpClient httpClient);

    /// <summary>
    /// Sets a custom transport handler for advanced scenarios.
    /// </summary>
    /// <param name="transportHandler">The transport handler to use</param>
    /// <returns>The builder instance for method chaining</returns>
    IXmlRpcClientBuilder WithTransportHandler(ITransportHandler transportHandler);

    /// <summary>
    /// Sets a custom serializer for type conversion.
    /// </summary>
    /// <param name="serializer">The serializer to use</param>
    /// <returns>The builder instance for method chaining</returns>
    IXmlRpcClientBuilder WithSerializer(IXmlRpcSerializer serializer);

    /// <summary>
    /// Builds the configured XmlRpcClient instance.
    /// </summary>
    /// <returns>A fully configured XmlRpcClient</returns>
    IXmlRpcClient Build();
}

/// <summary>
/// Default implementation of IXmlRpcClient providing the main API for XML-RPC communication.
/// </summary>
public class XmlRpcClient : IXmlRpcClient
{
    private readonly ITransportHandler _transportHandler;
    private readonly IRequestBuilder _requestBuilder;
    private readonly IResponseParser _responseParser;
    private readonly IXmlRpcSerializer _serializer;

    public Uri Endpoint { get; }
    public TimeSpan Timeout { get; }

    internal XmlRpcClient(
        Uri endpoint,
        ITransportHandler transportHandler,
        IRequestBuilder requestBuilder,
        IResponseParser responseParser,
        IXmlRpcSerializer serializer)
    {
        Endpoint = endpoint ?? throw new ArgumentNullException(nameof(endpoint));
        _transportHandler = transportHandler ?? throw new ArgumentNullException(nameof(transportHandler));
        _requestBuilder = requestBuilder ?? throw new ArgumentNullException(nameof(requestBuilder));
        _responseParser = responseParser ?? throw new ArgumentNullException(nameof(responseParser));
        _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
        Timeout = transportHandler.Timeout;
    }

    public async Task<T> CallAsync<T>(string methodName, params object[] parameters)
    {
        var result = await CallAsync(methodName, parameters).ConfigureAwait(false);

        try
        {
            var converted = _serializer.DeserializeValue(result, typeof(T));
            return (T)converted;
        }
        catch (Exception ex)
        {
            throw new XmlRpcTypeConversionException($"Cannot convert XML-RPC result to {typeof(T).Name}", ex);
        }
    }

    public async Task<XmlRpcValue> CallAsync(string methodName, params object[] parameters)
    {
        if (string.IsNullOrWhiteSpace(methodName))
            throw new ArgumentException("Method name cannot be null or empty.", nameof(methodName));

        try
        {
            // Build the XML request
            var requestXml = _requestBuilder.BuildXmlRequest(methodName, parameters);

            // Send the request (sans CancellationToken - utilise default)
            var responseXml = await _transportHandler.SendAsync(requestXml, default)
                .ConfigureAwait(false);

            // Parse the response
            var response = _responseParser.ParseResponse(responseXml);

            // Handle fault responses
            if (response.IsFault)
            {
                throw new XmlRpcFaultException(response.Fault!.FaultCode, response.Fault!.FaultString);
            }

            // Return the first parameter (XML-RPC methods return a single value)
            if (response.Parameters.Count == 0)
            {
                throw new XmlRpcProtocolException("Server returned no parameters in response.");
            }

            if (response.Parameters.Count > 1)
            {
                throw new XmlRpcProtocolException($"Server returned {response.Parameters.Count} parameters, expected 1.");
            }

            return response.Parameters[0];
        }
        catch (XmlRpcException)
        {
            // Re-throw XML-RPC specific exceptions as-is
            throw;
        }
        catch (Exception ex)
        {
            throw new XmlRpcNetworkException($"Failed to call XML-RPC method '{methodName}': {ex.Message}", ex);
        }
    }
}

/// <summary>
/// Default implementation of IXmlRpcClientBuilder providing fluent configuration.
/// </summary>
public class XmlRpcClientBuilder : IXmlRpcClientBuilder
{
    private Uri? _endpoint;
    private TimeSpan? _timeout;
    private HttpClient? _httpClient;
    private ITransportHandler? _transportHandler;
    private IXmlRpcSerializer? _serializer;

    public IXmlRpcClientBuilder WithUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            throw new ArgumentException("URL cannot be null or empty.", nameof(url));

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            throw new ArgumentException("Invalid URL format.", nameof(url));

        if (uri.Scheme != "http" && uri.Scheme != "https")
            throw new ArgumentException("URL must use HTTP or HTTPS scheme.", nameof(url));

        _endpoint = uri;
        return this;
    }

    public IXmlRpcClientBuilder WithTimeout(TimeSpan timeout)
    {
        if (timeout <= TimeSpan.Zero)
            throw new ArgumentException("Timeout must be greater than zero.", nameof(timeout));

        _timeout = timeout;
        return this;
    }

    public IXmlRpcClientBuilder WithHttpClient(HttpClient httpClient)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        return this;
    }

    public IXmlRpcClientBuilder WithTransportHandler(ITransportHandler transportHandler)
    {
        _transportHandler = transportHandler ?? throw new ArgumentNullException(nameof(transportHandler));
        return this;
    }

    public IXmlRpcClientBuilder WithSerializer(IXmlRpcSerializer serializer)
    {
        _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
        return this;
    }

    public IXmlRpcClient Build()
    {
        // Validate required configuration
        if (_endpoint == null)
            throw new InvalidOperationException("Endpoint URL must be configured using WithUrl().");

        // Create or use provided components
        var serializer = _serializer ?? new XmlRpcSerializer();
        var transportHandler = CreateTransportHandler();
        var requestBuilder = new XmlRpcRequestBuilder(serializer);
        var responseParser = new XmlRpcResponseParser(serializer);

        return new XmlRpcClient(_endpoint, transportHandler, requestBuilder, responseParser, serializer);
    }

    private ITransportHandler CreateTransportHandler()
    {
        if (_transportHandler != null)
            return _transportHandler;

        var handler = new HttpTransportHandler(_endpoint!, _httpClient);

        if (_timeout.HasValue)
            handler.SetTimeout(_timeout.Value);

        return handler;
    }
}