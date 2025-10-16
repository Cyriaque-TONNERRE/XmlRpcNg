namespace XmlRpcNg;

/// <summary>
/// Base exception class for all XML-RPC related errors.
/// </summary>
public abstract class XmlRpcException : Exception
{
    /// <summary>
    /// Initializes a new instance of the XmlRpcException class with a specified error message and optional inner exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception</param>
    /// <param name="inner">The exception that is the cause of the current exception, or null if no inner exception is specified</param>
    protected XmlRpcException(string message, Exception? inner = null)
        : base(message, inner) { }

    /// <summary>
    /// Initializes a new instance of the XmlRpcException class with a default error message.
    /// </summary>
    protected XmlRpcException()
        : base("An XML-RPC error occurred.") { }
}

/// <summary>
/// Exception thrown when the XML-RPC server returns a fault response.
/// </summary>
public class XmlRpcFaultException : XmlRpcException
{
    /// <summary>
    /// The fault code returned by the XML-RPC server.
    /// </summary>
    public int FaultCode { get; }

    /// <summary>
    /// The fault string/message returned by the XML-RPC server.
    /// </summary>
    public string FaultString { get; }

    /// <summary>
    /// Initializes a new instance of the XmlRpcFaultException class with the specified fault code and message.
    /// </summary>
    /// <param name="faultCode">The fault code returned by the XML-RPC server</param>
    /// <param name="faultString">The fault string/message returned by the XML-RPC server</param>
    public XmlRpcFaultException(int faultCode, string faultString)
        : base($"XML-RPC Fault {faultCode}: {faultString}")
    {
        FaultCode = faultCode;
        FaultString = faultString ?? string.Empty;
    }

    /// <summary>
    /// Initializes a new instance of the XmlRpcFaultException class with the specified fault code, message, and inner exception.
    /// </summary>
    /// <param name="faultCode">The fault code returned by the XML-RPC server</param>
    /// <param name="faultString">The fault string/message returned by the XML-RPC server</param>
    /// <param name="inner">The exception that is the cause of the current exception</param>
    public XmlRpcFaultException(int faultCode, string faultString, Exception? inner)
        : base($"XML-RPC Fault {faultCode}: {faultString}", inner)
    {
        FaultCode = faultCode;
        FaultString = faultString ?? string.Empty;
    }
}

/// <summary>
/// Exception thrown when network-related errors occur during XML-RPC communication.
/// </summary>
public class XmlRpcNetworkException : XmlRpcException
{
    public XmlRpcNetworkException(string message, Exception? inner = null)
        : base(message, inner) { }

    public XmlRpcNetworkException()
        : base("A network error occurred during XML-RPC communication.") { }
}

/// <summary>
/// Exception thrown when XML-RPC protocol violations are detected.
/// </summary>
public class XmlRpcProtocolException : XmlRpcException
{
    public XmlRpcProtocolException(string message, Exception? inner = null)
        : base(message, inner) { }

    public XmlRpcProtocolException()
        : base("An XML-RPC protocol violation was detected.") { }
}

/// <summary>
/// Exception thrown when XML parsing or validation errors occur.
/// </summary>
public class XmlRpcXmlException : XmlRpcException
{
    public XmlRpcXmlException(string message, Exception? inner = null)
        : base(message, inner) { }

    public XmlRpcXmlException()
        : base("An XML parsing error occurred during XML-RPC processing.") { }
}

/// <summary>
/// Exception thrown when type conversion errors occur between .NET types and XML-RPC values.
/// </summary>
public class XmlRpcTypeConversionException : XmlRpcException
{
    public XmlRpcTypeConversionException(string message, Exception? inner = null)
        : base(message, inner) { }

    public XmlRpcTypeConversionException()
        : base("A type conversion error occurred during XML-RPC processing.") { }

    public XmlRpcTypeConversionException(Type sourceType, Type targetType)
        : base($"Cannot convert from {sourceType.Name} to {targetType.Name} in XML-RPC context.") { }
}

/// <summary>
/// Exception thrown when invalid method names or parameters are detected.
/// </summary>
public class XmlRpcInvalidMethodException : XmlRpcException
{
    /// <summary>
    /// Gets the name of the invalid method that caused the exception.
    /// </summary>
    public string MethodName { get; }

    public XmlRpcInvalidMethodException(string methodName, string message)
        : base(message)
    {
        MethodName = methodName;
    }

    public XmlRpcInvalidMethodException(string methodName)
        : base($"Invalid XML-RPC method name: '{methodName}'")
    {
        MethodName = methodName;
    }
}

/// <summary>
/// Exception thrown when timeout errors occur during XML-RPC communication.
/// </summary>
public class XmlRpcTimeoutException : XmlRpcException
{
    /// <summary>
    /// Gets the timeout duration that was exceeded when the exception occurred.
    /// </summary>
    public TimeSpan Timeout { get; }

    public XmlRpcTimeoutException(TimeSpan timeout, string message, Exception? inner = null)
        : base(message, inner)
    {
        Timeout = timeout;
    }

    public XmlRpcTimeoutException(TimeSpan timeout)
        : base($"XML-RPC operation timed out after {timeout.TotalSeconds} seconds.")
    {
        Timeout = timeout;
    }
}