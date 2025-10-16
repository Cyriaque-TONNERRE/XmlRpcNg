using System.Xml;
using System.Collections;
using System.Globalization;

namespace XmlRpcNg;

/// <summary>
/// Provides serialization between .NET objects and XML-RPC values.
/// Handles type conversion with proper validation and error handling.
/// </summary>
public interface IXmlRpcSerializer
{
    /// <summary>
    /// Serializes a .NET object to its XML-RPC representation.
    /// </summary>
    /// <param name="value">The .NET object to serialize</param>
    /// <returns>XML string representation of the value</returns>
    string SerializeValue(object value);

    /// <summary>
    /// Deserializes an XML-RPC value to a .NET object.
    /// </summary>
    /// <param name="xmlValue">The XML-RPC value to deserialize</param>
    /// <param name="targetType">The target .NET type</param>
    /// <returns>The deserialized .NET object</returns>
    object DeserializeValue(XmlRpcValue xmlValue, Type targetType);

    /// <summary>
    /// Parses an XML string into an XmlRpcValue.
    /// </summary>
    /// <param name="xml">The XML string to parse</param>
    /// <returns>The parsed XmlRpcValue</returns>
    XmlRpcValue DeserializeXmlValue(string xml);
}

/// <summary>
/// Default implementation of IXmlRpcSerializer using System.Xml for secure XML processing.
/// Provides type-safe serialization with comprehensive validation.
/// </summary>
public class XmlRpcSerializer : IXmlRpcSerializer
{
    private static readonly Dictionary<Type, Func<object, XmlRpcValue>> TypeToXmlRpcConverters = new();
    private static readonly Dictionary<Type, Func<XmlRpcValue, object>> XmlRpcToTypeConverters = new();

    static XmlRpcSerializer()
    {
        InitializeConverters();
    }

    public string SerializeValue(object value)
    {
        if (value == null)
            return new XmlRpcString(string.Empty).ToXml();

        var valueType = value.GetType();

        if (TypeToXmlRpcConverters.TryGetValue(valueType, out var converter))
        {
            return converter(value).ToXml();
        }

        // Handle enums
        if (valueType.IsEnum)
        {
            return new XmlRpcString(value.ToString()!).ToXml();
        }

        // Handle dictionaries as structs
        if (IsDictionaryType(valueType))
        {
            return SerializeDictionary(value);
        }

        // Handle arrays and collections
        if (valueType.IsArray || ImplementsIEnumerable(valueType))
        {
            return SerializeCollection(value);
        }

        // Fallback to string representation
        return new XmlRpcString(value.ToString() ?? string.Empty).ToXml();
    }

    public object DeserializeValue(XmlRpcValue xmlValue, Type targetType)
    {
        if (xmlValue == null)
            throw new ArgumentNullException(nameof(xmlValue));

        if (targetType == typeof(XmlRpcValue))
            return xmlValue;

        // Handle nullable types
        var underlyingType = Nullable.GetUnderlyingType(targetType);
        if (underlyingType != null)
        {
            if (xmlValue is XmlRpcString { Value: "" })
                return null!;
            targetType = underlyingType;
        }

        if (XmlRpcToTypeConverters.TryGetValue(targetType, out var converter))
        {
            return converter(xmlValue);
        }

        // Handle enums
        if (targetType.IsEnum)
        {
            if (xmlValue is XmlRpcString stringValue)
            {
                return Enum.Parse(targetType, stringValue.Value);
            }
            throw new XmlRpcTypeConversionException(xmlValue.GetType(), targetType);
        }

        // Handle arrays and collections
        if (targetType.IsArray)
        {
            return DeserializeArray(xmlValue, targetType);
        }

        // Fallback conversion
        try
        {
            return xmlValue.As(targetType);
        }
        catch
        {
            throw new XmlRpcTypeConversionException(xmlValue.GetType(), targetType);
        }
    }

    public XmlRpcValue DeserializeXmlValue(string xml)
    {
        if (string.IsNullOrWhiteSpace(xml))
            throw new ArgumentException("XML content cannot be null or empty.", nameof(xml));

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

            if (xmlReader.Name != "value")
                throw new XmlRpcXmlException($"Expected 'value' element, found '{xmlReader.Name}'");

            return ParseValueElement(xmlReader);
        }
        catch (XmlException ex)
        {
            throw new XmlRpcXmlException($"Invalid XML format: {ex.Message}", ex);
        }
    }

    private static XmlRpcValue ParseValueElement(XmlReader reader)
    {
        reader.ReadStartElement("value");

        if (reader.IsEmptyElement || reader.NodeType == XmlNodeType.EndElement)
        {
            reader.ReadEndElement();
            return new XmlRpcString(string.Empty);
        }

        var valueName = reader.Name;
        XmlRpcValue result = valueName switch
        {
            "i4" or "int" => ParseInt(reader),
            "boolean" => ParseBoolean(reader),
            "string" => ParseString(reader),
            "double" => ParseDouble(reader),
            "dateTime.iso8601" => ParseDateTime(reader),
            "base64" => ParseBase64(reader),
            "struct" => ParseStruct(reader),
            "array" => ParseArray(reader),
            _ => throw new XmlRpcXmlException($"Unsupported XML-RPC type: {valueName}")
        };

        // Read any remaining end element
        if (reader.NodeType == XmlNodeType.EndElement && reader.Name == "value")
        {
            reader.ReadEndElement();
        }

        return result;
    }

    private static XmlRpcInt ParseInt(XmlReader reader)
    {
        var value = reader.ReadElementContentAsString();
        if (int.TryParse(value, out var result))
            return new XmlRpcInt(result);
        throw new XmlRpcXmlException($"Invalid integer value: {value}");
    }

    private static XmlRpcBoolean ParseBoolean(XmlReader reader)
    {
        var value = reader.ReadElementContentAsString();
        return new XmlRpcBoolean(value == "1");
    }

    private static XmlRpcString ParseString(XmlReader reader)
    {
        var value = reader.ReadElementContentAsString() ?? string.Empty;
        return new XmlRpcString(value);
    }

    private static XmlRpcDouble ParseDouble(XmlReader reader)
    {
        var value = reader.ReadElementContentAsString();
        if (double.TryParse(value, out var result))
            return new XmlRpcDouble(result);
        throw new XmlRpcXmlException($"Invalid double value: {value}");
    }

    private static XmlRpcDateTime ParseDateTime(XmlReader reader)
    {
        var value = reader.ReadElementContentAsString();
        if (DateTime.TryParseExact(value, "yyyyMMddTHH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out var result))
            return new XmlRpcDateTime(result);
        throw new XmlRpcXmlException($"Invalid dateTime value: {value}");
    }

    private static XmlRpcBase64 ParseBase64(XmlReader reader)
    {
        var value = reader.ReadElementContentAsString();
        try
        {
            var bytes = Convert.FromBase64String(value);
            return new XmlRpcBase64(bytes);
        }
        catch (FormatException ex)
        {
            throw new XmlRpcXmlException($"Invalid base64 value: {ex.Message}", ex);
        }
    }

    private static XmlRpcStruct ParseStruct(XmlReader reader)
    {
        var result = new XmlRpcStruct();

        reader.ReadStartElement("struct");

        while (reader.NodeType != XmlNodeType.EndElement)
        {
            if (reader.Name == "member")
            {
                ParseStructMember(reader, result);
            }
            else
            {
                reader.Skip();
            }
        }

        reader.ReadEndElement();
        return result;
    }

    private static void ParseStructMember(XmlReader reader, XmlRpcStruct result)
    {
        reader.ReadStartElement("member");

        var name = string.Empty;
        var value = (XmlRpcValue?)null;

        while (reader.NodeType != XmlNodeType.EndElement)
        {
            switch (reader.Name)
            {
                case "name":
                    name = reader.ReadElementContentAsString() ?? string.Empty;
                    break;
                case "value":
                    value = ParseValueElement(reader);
                    break;
                default:
                    reader.Skip();
                    break;
            }
        }

        reader.ReadEndElement();

        if (!string.IsNullOrEmpty(name) && value != null)
        {
            result.Members[name] = value;
        }
    }

    private static XmlRpcArray ParseArray(XmlReader reader)
    {
        var result = new XmlRpcArray();

        reader.ReadStartElement("array");

        if (reader.Name == "data")
        {
            reader.ReadStartElement("data");

            while (reader.NodeType != XmlNodeType.EndElement)
            {
                if (reader.Name == "value")
                {
                    result.Items.Add(ParseValueElement(reader));
                }
                else
                {
                    reader.Skip();
                }
            }

            reader.ReadEndElement();
        }

        reader.ReadEndElement();
        return result;
    }

    private static string SerializeCollection(object value)
    {
        var items = new List<XmlRpcValue>();

        if (value is IEnumerable enumerable)
        {
            foreach (var item in enumerable)
            {
                if (item != null)
                {
                    var xmlValue = SerializeObjectToXmlRpcValue(item);
                    items.Add(xmlValue);
                }
            }
        }

        return new XmlRpcArray(items).ToXml();
    }

    private static XmlRpcValue SerializeObjectToXmlRpcValue(object obj)
    {
        var type = obj.GetType();

        if (TypeToXmlRpcConverters.TryGetValue(type, out var converter))
        {
            return converter(obj);
        }

        if (type.IsEnum)
        {
            return new XmlRpcString(obj.ToString()!);
        }

        return new XmlRpcString(obj.ToString() ?? string.Empty);
    }

    private static object DeserializeArray(XmlRpcValue xmlValue, Type targetType)
    {
        if (xmlValue is not XmlRpcArray array)
            throw new XmlRpcTypeConversionException(xmlValue.GetType(), targetType);

        var elementType = targetType.GetElementType()!;
        var result = Array.CreateInstance(elementType, array.Items.Count);

        for (int i = 0; i < array.Items.Count; i++)
        {
            var convertedValue = Convert.ChangeType(array.Items[i].As(elementType), elementType);
            result.SetValue(convertedValue, i);
        }

        return result;
    }

    private static bool IsDictionaryType(Type type)
    {
        return type.IsGenericType &&
               (type.GetGenericTypeDefinition() == typeof(IDictionary<,>) ||
                type.GetGenericTypeDefinition() == typeof(Dictionary<,>));
    }

    private static string SerializeDictionary(object value)
    {
        var structValue = new XmlRpcStruct();

        // Use reflection to access the dictionary's key-value pairs
        var methodInfo = value.GetType().GetMethod("GetEnumerator");
        if (methodInfo == null)
        {
            throw new XmlRpcTypeConversionException(value.GetType(), typeof(XmlRpcStruct));
        }

        var enumerator = methodInfo.Invoke(value, null);
        if (enumerator == null)
        {
            throw new XmlRpcTypeConversionException(value.GetType(), typeof(XmlRpcStruct));
        }

        var moveNextMethod = enumerator.GetType().GetMethod("MoveNext");
        var currentProperty = enumerator.GetType().GetProperty("Current");

        while (moveNextMethod?.Invoke(enumerator, null) as bool? == true)
        {
            var keyValuePair = currentProperty?.GetValue(enumerator);
            if (keyValuePair == null) continue;

            var keyProperty = keyValuePair.GetType().GetProperty("Key");
            var valueProperty = keyValuePair.GetType().GetProperty("Value");

            var key = keyProperty?.GetValue(keyValuePair) as string ?? string.Empty;
            var val = valueProperty?.GetValue(keyValuePair);

            if (!string.IsNullOrEmpty(key) && val != null)
            {
                var xmlValue = SerializeObjectToXmlRpcValue(val);
                structValue.Members[key] = xmlValue;
            }
        }

        return structValue.ToXml();
    }

    private static bool ImplementsIEnumerable(Type type)
    {
        return type.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>));
    }

    private static void InitializeConverters()
    {
        TypeToXmlRpcConverters[typeof(int)] = obj => new XmlRpcInt((int)obj);
        TypeToXmlRpcConverters[typeof(bool)] = obj => new XmlRpcBoolean((bool)obj);
        TypeToXmlRpcConverters[typeof(string)] = obj => new XmlRpcString((string?)obj ?? string.Empty);
        TypeToXmlRpcConverters[typeof(double)] = obj => new XmlRpcDouble((double)obj);
        TypeToXmlRpcConverters[typeof(float)] = obj => new XmlRpcDouble((float)obj);
        TypeToXmlRpcConverters[typeof(DateTime)] = obj => new XmlRpcDateTime((DateTime)obj);
        TypeToXmlRpcConverters[typeof(byte[])] = obj => new XmlRpcBase64((byte[])obj!);

        XmlRpcToTypeConverters[typeof(int)] = val => ((XmlRpcInt)val).Value;
        XmlRpcToTypeConverters[typeof(bool)] = val => ((XmlRpcBoolean)val).Value;
        XmlRpcToTypeConverters[typeof(string)] = val => ((XmlRpcString)val).Value;
        XmlRpcToTypeConverters[typeof(double)] = val => ((XmlRpcDouble)val).Value;
        XmlRpcToTypeConverters[typeof(float)] = val => (float)((XmlRpcDouble)val).Value;
        XmlRpcToTypeConverters[typeof(DateTime)] = val => ((XmlRpcDateTime)val).Value;
        XmlRpcToTypeConverters[typeof(byte[])] = val => ((XmlRpcBase64)val).Value;
    }
}