namespace XmlRpcNg;

/// <summary>
/// Handles HTTP communication for XML-RPC requests and responses.
/// Provides a clean abstraction over HttpClient with XML-RPC specific optimizations.
/// </summary>
public interface ITransportHandler
{
    /// <summary>
    /// Sends an XML-RPC request and returns the XML response.
    /// </summary>
    /// <param name="request">The XML-RPC request content</param>
    /// <param name="cancellationToken">Cancellation token for the operation</param>
    /// <returns>The XML response content</returns>
    /// <exception cref="XmlRpcNetworkException">Thrown when network errors occur</exception>
    /// <exception cref="XmlRpcTimeoutException">Thrown when the operation times out</exception>
    Task<string> SendAsync(string request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets the timeout for HTTP requests.
    /// </summary>
    /// <param name="timeout">The timeout duration</param>
    void SetTimeout(TimeSpan timeout);

    /// <summary>
    /// Sets the User-Agent header for HTTP requests.
    /// </summary>
    /// <param name="userAgent">The user agent string</param>
    void SetUserAgent(string userAgent);

    /// <summary>
    /// Gets the current timeout setting.
    /// </summary>
    TimeSpan Timeout { get; }

    /// <summary>
    /// Gets the current User-Agent setting.
    /// </summary>
    string UserAgent { get; }
}

/// <summary>
/// Default implementation of ITransportHandler using HttpClient.
/// Provides production-ready HTTP communication with proper XML-RPC headers.
/// </summary>
public class HttpTransportHandler : ITransportHandler, IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly Uri _endpoint;
    private TimeSpan _timeout = TimeSpan.FromSeconds(30);
    private string _userAgent = "XmlRpcNg/1.0";
    private bool _disposed = false;

    public HttpTransportHandler(Uri endpoint, HttpClient? httpClient = null)
    {
        _endpoint = endpoint ?? throw new ArgumentNullException(nameof(endpoint));
        _httpClient = httpClient ?? new HttpClient();

        ConfigureDefaultHeaders();
    }

    public TimeSpan Timeout
    {
        get => _timeout;
        set
        {
            _timeout = value;
            _httpClient.Timeout = value;
        }
    }

    public string UserAgent
    {
        get => _userAgent;
        set
        {
            _userAgent = value ?? throw new ArgumentNullException(nameof(value));
            _httpClient.DefaultRequestHeaders.UserAgent.Clear();
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(value);
        }
    }

    public void SetTimeout(TimeSpan timeout) => Timeout = timeout;

    public void SetUserAgent(string userAgent) => UserAgent = userAgent;

    public async Task<string> SendAsync(string request, CancellationToken cancellationToken = default)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(HttpTransportHandler));

        if (string.IsNullOrWhiteSpace(request))
            throw new ArgumentException("Request content cannot be null or empty.", nameof(request));

        try
        {
            using var content = new StringContent(request, System.Text.Encoding.UTF8, "text/xml");

            using var httpResponse = await _httpClient.PostAsync(_endpoint, content, cancellationToken)
                .ConfigureAwait(false);

            httpResponse.EnsureSuccessStatusCode();

            var responseContent = await httpResponse.Content.ReadAsStringAsync()
                .ConfigureAwait(false);

            if (string.IsNullOrWhiteSpace(responseContent))
            {
                throw new XmlRpcProtocolException("Server returned empty response.");
            }

            return responseContent;
        }
        catch (HttpRequestException ex)
        {
            throw new XmlRpcNetworkException($"HTTP request failed: {ex.Message}", ex);
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            throw new XmlRpcTimeoutException(_timeout, "Request timed out.", ex);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new XmlRpcNetworkException($"Unexpected error during HTTP communication: {ex.Message}", ex);
        }
    }

    private void ConfigureDefaultHeaders()
    {
        _httpClient.DefaultRequestHeaders.UserAgent.Clear();
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(_userAgent);
        _httpClient.DefaultRequestHeaders.Accept.Clear();
        _httpClient.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("text/xml"));
        _httpClient.Timeout = _timeout;
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _httpClient?.Dispose();
            _disposed = true;
        }
    }
}

/// <summary>
/// Mock implementation of ITransportHandler for testing purposes.
/// Allows predefined responses to be returned without actual HTTP communication.
/// </summary>
public class MockTransportHandler : ITransportHandler
{
    private readonly Queue<string> _responses = new();
    private TimeSpan _timeout = TimeSpan.FromSeconds(30);
    private string _userAgent = "XmlRpcNg/1.0-Mock";

    public TimeSpan Timeout
    {
        get => _timeout;
        set => _timeout = value;
    }

    public string UserAgent
    {
        get => _userAgent;
        set => _userAgent = value ?? throw new ArgumentNullException(nameof(value));
    }

    public void SetTimeout(TimeSpan timeout) => Timeout = timeout;

    public void SetUserAgent(string userAgent) => UserAgent = userAgent;

    /// <summary>
    /// Adds a response to be returned by the next SendAsync call.
    /// </summary>
    /// <param name="response">The XML response to return</param>
    public void AddResponse(string response)
    {
        _responses.Enqueue(response ?? throw new ArgumentNullException(nameof(response)));
    }

    /// <summary>
    /// Adds a fault response to be returned by the next SendAsync call.
    /// </summary>
    /// <param name="faultCode">The fault code</param>
    /// <param name="faultString">The fault string</param>
    public void AddFaultResponse(int faultCode, string faultString)
    {
        var faultXml = $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<methodResponse>
    <fault>
        <value>
            <struct>
                <member>
                    <name>faultCode</name>
                    <value><int>{faultCode}</int></value>
                </member>
                <member>
                    <name>faultString</name>
                    <value><string>{faultString}</string></value>
                </member>
            </struct>
        </value>
    </fault>
</methodResponse>";
        AddResponse(faultXml);
    }

    public Task<string> SendAsync(string request, CancellationToken cancellationToken = default)
    {
        if (_responses.Count == 0)
            throw new InvalidOperationException("No mock response configured.");

        return Task.FromResult(_responses.Dequeue());
    }
}