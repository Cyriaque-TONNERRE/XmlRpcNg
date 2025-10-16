using System.Xml;
using System.Text;
using System.Collections;

namespace XmlRpcNg;

/// <summary>
/// Represents an XML-RPC fault response.
/// </summary>
public record XmlRpcFault(int FaultCode, string FaultString);

/// <summary>
/// Represents an XML-RPC response containing either parameters or a fault.
/// </summary>
public record XmlRpcResponse
{
    /// <summary>
    /// Gets the list of parameters returned by the XML-RPC method call.
    /// </summary>
    public List<XmlRpcValue> Parameters { get; init; } = new();

    /// <summary>
    /// Gets the fault information if the response contains an error.
    /// </summary>
    public XmlRpcFault? Fault { get; init; }

    /// <summary>
    /// Gets a value indicating whether the response represents a successful result.
    /// </summary>
    public bool IsSuccess => Fault == null;

    /// <summary>
    /// Gets a value indicating whether the response represents a fault/error.
    /// </summary>
    public bool IsFault => Fault != null;
}

/// <summary>
/// Builds XML-RPC method call requests from method names and parameters.
/// Provides validation and secure XML generation with proper escaping.
/// </summary>
public interface IRequestBuilder
{
    /// <summary>
    /// Builds an XML-RPC method call request.
    /// </summary>
    /// <param name="methodName">The method name to call</param>
    /// <param name="parameters">The parameters to pass to the method</param>
    /// <returns>XML string representing the method call</returns>
    string BuildXmlRequest(string methodName, params object[] parameters);

    /// <summary>
    /// Validates that a method name is valid for XML-RPC.
    /// </summary>
    /// <param name="methodName">The method name to validate</param>
    /// <exception cref="XmlRpcInvalidMethodException">Thrown if the method name is invalid</exception>
    void ValidateMethodName(string methodName);

    /// <summary>
    /// Validates that parameters are suitable for XML-RPC serialization.
    /// </summary>
    /// <param name="parameters">The parameters to validate</param>
    /// <exception cref="ArgumentException">Thrown if parameters are invalid</exception>
    void ValidateParameters(object[] parameters);
}

/// <summary>
/// Parses XML-RPC method response messages into structured objects.
/// Handles both successful responses and fault responses with proper error handling.
/// </summary>
public interface IResponseParser
{
    /// <summary>
    /// Parses an XML-RPC response string into a structured response object.
    /// </summary>
    /// <param name="xml">The XML response string to parse</param>
    /// <returns>Parsed response object with parameters or fault information</returns>
    XmlRpcResponse ParseResponse(string xml);

    /// <summary>
    /// Determines if the XML represents a fault response.
    /// </summary>
    /// <param name="xml">The XML to check</param>
    /// <returns>True if the XML represents a fault response</returns>
    bool IsFaultResponse(string xml);

    /// <summary>
    /// Parses a fault response XML string into a fault object.
    /// </summary>
    /// <param name="xml">The fault XML to parse</param>
    /// <returns>Parsed fault object</returns>
    XmlRpcFault ParseFault(string xml);
}

/// <summary>
/// Default implementation of IRequestBuilder using secure XML generation.
/// </summary>
public class XmlRpcRequestBuilder : IRequestBuilder
{
    private readonly IXmlRpcSerializer _serializer;

    public XmlRpcRequestBuilder(IXmlRpcSerializer serializer)
    {
        _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
    }

    public string BuildXmlRequest(string methodName, params object[] parameters)
    {
        ValidateMethodName(methodName);
        ValidateParameters(parameters);

        var xml = new StringBuilder();
        xml.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
        xml.AppendLine("<methodCall>");
        xml.AppendLine($"  <methodName>{EscapeXml(methodName)}</methodName>");

        if (parameters.Length > 0)
        {
            xml.AppendLine("  <params>");
            foreach (var param in parameters)
            {
                xml.AppendLine("    <param>");
                var paramXml = _serializer.SerializeValue(param);
                xml.AppendLine($"      {paramXml.Trim()}");
                xml.AppendLine("    </param>");
            }
            xml.AppendLine("  </params>");
        }

        xml.AppendLine("</methodCall>");

        return xml.ToString();
    }

    public void ValidateMethodName(string methodName)
    {
        if (string.IsNullOrWhiteSpace(methodName))
            throw new XmlRpcInvalidMethodException(methodName, "Method name cannot be null or empty.");

        if (methodName.Length > 1000)
            throw new XmlRpcInvalidMethodException(methodName, "Method name cannot exceed 1000 characters.");

        // Basic validation for allowed characters (letters, digits, underscore, dot)
        if (!System.Text.RegularExpressions.Regex.IsMatch(methodName, @"^[a-zA-Z0-9_.]+$"))
            throw new XmlRpcInvalidMethodException(methodName, "Method name contains invalid characters.");
    }

    public void ValidateParameters(object[] parameters)
    {
        if (parameters == null)
            throw new ArgumentNullException(nameof(parameters));

        if (parameters.Length > 100)
            throw new ArgumentException("Cannot have more than 100 parameters in a single XML-RPC call.", nameof(parameters));

        // Check for nested arrays/structs that might be too deep
        foreach (var param in parameters)
        {
            ValidateParameterDepth(param, 0);
        }
    }

    private static string EscapeXml(string value)
    {
        return value.Replace("&", "&amp;")
                    .Replace("<", "&lt;")
                    .Replace(">", "&gt;")
                    .Replace("\"", "&quot;")
                    .Replace("'", "&apos;");
    }

    private void ValidateParameterDepth(object obj, int depth)
    {
        if (depth > 10)
            throw new ArgumentException("Parameter nesting is too deep (maximum 10 levels).");

        if (obj == null)
            return;

        var type = obj.GetType();

        if (type.IsArray || ImplementsIEnumerable(type))
        {
            if (obj is IEnumerable enumerable)
            {
                foreach (var item in enumerable)
                {
                    ValidateParameterDepth(item, depth + 1);
                }
            }
        }
    }

    private static bool ImplementsIEnumerable(Type type)
    {
        return type.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>));
    }
}

/// <summary>
/// Default implementation of IResponseParser using secure XML parsing.
/// </summary>
public class XmlRpcResponseParser : IResponseParser
{
    private readonly IXmlRpcSerializer _serializer;

    public XmlRpcResponseParser(IXmlRpcSerializer serializer)
    {
        _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
    }

    public XmlRpcResponse ParseResponse(string xml)
    {
        if (string.IsNullOrWhiteSpace(xml))
            throw new ArgumentException("XML response cannot be null or empty.", nameof(xml));

        try
        {
            var settings = new XmlReaderSettings
            {
                DtdProcessing = DtdProcessing.Prohibit,
                XmlResolver = null,
                MaxCharactersFromEntities = 1024,
                MaxCharactersInDocument = 1024 * 1024, // 1MB limit
                IgnoreWhitespace = true,
                IgnoreComments = true
            };

            using var stringReader = new StringReader(xml);
            using var xmlReader = XmlReader.Create(stringReader, settings);

            xmlReader.MoveToContent();

            if (xmlReader.Name != "methodResponse")
                throw new XmlRpcXmlException($"Expected 'methodResponse' element, found '{xmlReader.Name}'");

            xmlReader.ReadStartElement("methodResponse");

            // Check for fault or params
            if (xmlReader.Name == "fault")
            {
                var fault = ParseFaultElement(xmlReader);
                return new XmlRpcResponse { Fault = fault };
            }
            else if (xmlReader.Name == "params")
            {
                var parameters = ParseParamsElement(xmlReader);
                return new XmlRpcResponse { Parameters = parameters };
            }
            else
            {
                throw new XmlRpcXmlException("Expected 'fault' or 'params' element in methodResponse.");
            }
        }
        catch (XmlException ex)
        {
            throw new XmlRpcXmlException($"Invalid XML format: {ex.Message}", ex);
        }
    }

    public bool IsFaultResponse(string xml)
    {
        if (string.IsNullOrWhiteSpace(xml))
            return false;

        try
        {
            var settings = new XmlReaderSettings
            {
                DtdProcessing = DtdProcessing.Prohibit,
                XmlResolver = null,
                MaxCharactersFromEntities = 1024,
                MaxCharactersInDocument = 1024 * 1024,
                IgnoreWhitespace = true,
                IgnoreComments = true
            };

            using var stringReader = new StringReader(xml);
            using var xmlReader = XmlReader.Create(stringReader, settings);

            xmlReader.MoveToContent();
            if (xmlReader.Name != "methodResponse")
                return false;

            xmlReader.ReadStartElement("methodResponse");
            return xmlReader.Name == "fault";
        }
        catch
        {
            return false;
        }
    }

    public XmlRpcFault ParseFault(string xml)
    {
        var response = ParseResponse(xml);
        if (response.Fault != null)
            return response.Fault;

        throw new XmlRpcProtocolException("The provided XML is not a fault response.");
    }

    private static XmlRpcFault ParseFaultElement(XmlReader reader)
    {
        reader.ReadStartElement("fault");
        reader.ReadStartElement("value");

        var faultCode = 0;
        var faultString = string.Empty;

        if (reader.Name == "struct")
        {
            reader.ReadStartElement("struct");

            while (reader.NodeType != XmlNodeType.EndElement)
            {
                if (reader.Name == "member")
                {
                    ParseFaultMember(reader, ref faultCode, ref faultString);
                }
                else
                {
                    reader.Skip();
                }
            }

            reader.ReadEndElement();
        }

        reader.ReadEndElement(); // value
        reader.ReadEndElement(); // fault

        return new XmlRpcFault(faultCode, faultString);
    }

    private static void ParseFaultMember(XmlReader reader, ref int faultCode, ref string faultString)
    {
        reader.ReadStartElement("member");

        var name = string.Empty;
        var value = string.Empty;

        while (reader.NodeType != XmlNodeType.EndElement)
        {
            switch (reader.Name)
            {
                case "name":
                    name = reader.ReadElementContentAsString() ?? string.Empty;
                    break;
                case "value":
                    value = reader.ReadInnerXml();
                    break;
                default:
                    reader.Skip();
                    break;
            }
        }

        reader.ReadEndElement();

        if (name == "faultCode")
        {
            if (int.TryParse(value.Trim(), out var code))
                faultCode = code;
        }
        else if (name == "faultString")
        {
            faultString = value.Trim();
        }
    }

    private List<XmlRpcValue> ParseParamsElement(XmlReader reader)
    {
        var parameters = new List<XmlRpcValue>();

        reader.ReadStartElement("params");

        while (reader.NodeType != XmlNodeType.EndElement)
        {
            if (reader.Name == "param")
            {
                reader.ReadStartElement("param");

                if (reader.Name == "value")
                {
                    var xmlValue = _serializer.DeserializeXmlValue(reader.ReadOuterXml());
                    parameters.Add(xmlValue);
                }

                // Skip to the end of param element
                while (reader.NodeType != XmlNodeType.EndElement || reader.Name != "param")
                {
                    reader.Read();
                }
                reader.ReadEndElement();
            }
            else
            {
                reader.Skip();
            }
        }

        reader.ReadEndElement();
        return parameters;
    }
}