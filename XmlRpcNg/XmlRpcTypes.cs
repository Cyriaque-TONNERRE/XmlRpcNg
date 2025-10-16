namespace XmlRpcNg;

/// <summary>
/// Base abstract class for all XML-RPC value types.
/// Provides the foundation for type-safe XML-RPC value handling.
/// </summary>
public abstract class XmlRpcValue
{
    /// <summary>
    /// Converts the XML-RPC value to the specified .NET type.
    /// </summary>
    /// <typeparam name="T">The target .NET type</typeparam>
    /// <returns>The converted value</returns>
    public abstract T As<T>();

    /// <summary>
    /// Converts the XML-RPC value to the specified .NET type.
    /// </summary>
    /// <param name="targetType">The target .NET type</param>
    /// <returns>The converted value</returns>
    public abstract object As(Type targetType);

    /// <summary>
    /// Converts the XML-RPC value to its XML representation.
    /// </summary>
    /// <returns>XML string representation</returns>
    public abstract string ToXml();
}

/// <summary>
/// Represents an XML-RPC integer (4-byte signed integer).
/// </summary>
public class XmlRpcInt : XmlRpcValue
{
    /// <summary>
    /// Gets the integer value.
    /// </summary>
    public int Value { get; }

    public XmlRpcInt(int value) => Value = value;

    public override T As<T>() => (T)Convert.ChangeType(Value, typeof(T));
    public override object As(Type targetType) => Convert.ChangeType(Value, targetType);
    public override string ToXml() => $"<value><i4>{Value}</i4></value>";
}

/// <summary>
/// Represents an XML-RPC boolean value.
/// </summary>
public class XmlRpcBoolean : XmlRpcValue
{
    /// <summary>
    /// Gets the boolean value.
    /// </summary>
    public bool Value { get; }

    public XmlRpcBoolean(bool value) => Value = value;

    public override T As<T>() => (T)Convert.ChangeType(Value, typeof(T));
    public override object As(Type targetType) => Convert.ChangeType(Value, targetType);
    public override string ToXml() => $"<value><boolean>{(Value ? "1" : "0")}</boolean></value>";
}

/// <summary>
/// Represents an XML-RPC string value.
/// </summary>
public class XmlRpcString : XmlRpcValue
{
    /// <summary>
    /// Gets the string value.
    /// </summary>
    public string Value { get; }

    public XmlRpcString(string value) => Value = value ?? string.Empty;

    public override T As<T>()
    {
        if (typeof(T) == typeof(string)) return (T)(object)Value;
        return (T)Convert.ChangeType(Value, typeof(T));
    }

    public override object As(Type targetType)
    {
        if (targetType == typeof(string)) return Value;
        return Convert.ChangeType(Value, targetType);
    }

    public override string ToXml()
    {
        var escapedValue = Value.Replace("&", "&amp;")
                                .Replace("<", "&lt;")
                                .Replace(">", "&gt;")
                                .Replace("\"", "&quot;")
                                .Replace("'", "&apos;");
        return $"<value><string>{escapedValue}</string></value>";
    }
}

/// <summary>
/// Represents an XML-RPC double precision floating point value.
/// </summary>
public class XmlRpcDouble : XmlRpcValue
{
    /// <summary>
    /// Gets the double precision floating point value.
    /// </summary>
    public double Value { get; }

    public XmlRpcDouble(double value) => Value = value;

    public override T As<T>() => (T)Convert.ChangeType(Value, typeof(T));
    public override object As(Type targetType) => Convert.ChangeType(Value, targetType);
    public override string ToXml() => $"<value><double>{Value}</double></value>";
}

/// <summary>
/// Represents an XML-RPC dateTime.iso8601 value.
/// </summary>
public class XmlRpcDateTime : XmlRpcValue
{
    /// <summary>
    /// Gets the DateTime value.
    /// </summary>
    public DateTime Value { get; }

    public XmlRpcDateTime(DateTime value) => Value = value;

    public override T As<T>() => (T)Convert.ChangeType(Value, typeof(T));
    public override object As(Type targetType) => Convert.ChangeType(Value, targetType);
    public override string ToXml() => $"<value><dateTime.iso8601>{Value:yyyyMMdd\\THH\\:mm\\:ss}</dateTime.iso8601></value>";
}

/// <summary>
/// Represents an XML-RPC base64 encoded binary value.
/// </summary>
public class XmlRpcBase64 : XmlRpcValue
{
    /// <summary>
    /// Gets the binary data as a byte array.
    /// </summary>
    public byte[] Value { get; }

    public XmlRpcBase64(byte[] value) => Value = value ?? Array.Empty<byte>();

    public override T As<T>()
    {
        if (typeof(T) == typeof(byte[])) return (T)(object)Value;
        throw new InvalidOperationException($"Cannot convert base64 to {typeof(T).Name}");
    }

    public override object As(Type targetType)
    {
        if (targetType == typeof(byte[])) return Value;
        throw new InvalidOperationException($"Cannot convert base64 to {targetType.Name}");
    }

    public override string ToXml()
    {
        var base64 = Convert.ToBase64String(Value);
        return $"<value><base64>{base64}</base64></value>";
    }
}

/// <summary>
/// Represents an XML-RPC array value.
/// </summary>
public class XmlRpcArray : XmlRpcValue
{
    /// <summary>
    /// Gets the collection of items in the array.
    /// </summary>
    public List<XmlRpcValue> Items { get; }

    public XmlRpcArray() => Items = new List<XmlRpcValue>();
    public XmlRpcArray(IEnumerable<XmlRpcValue> items) => Items = items?.ToList() ?? new List<XmlRpcValue>();

    public override T As<T>()
    {
        if (typeof(T).IsArray)
        {
            var elementType = typeof(T).GetElementType()!;
            var array = Array.CreateInstance(elementType, Items.Count);
            for (int i = 0; i < Items.Count; i++)
            {
                array.SetValue(Items[i].As(elementType), i);
            }
            return (T)(object)array;
        }

        if (typeof(T) == typeof(List<XmlRpcValue>) || typeof(T) == typeof(IList<XmlRpcValue>))
            return (T)(object)Items;

        throw new InvalidOperationException($"Cannot convert array to {typeof(T).Name}");
    }

    public override object As(Type targetType)
    {
        if (targetType.IsArray)
        {
            var elementType = targetType.GetElementType()!;
            var array = Array.CreateInstance(elementType, Items.Count);
            for (int i = 0; i < Items.Count; i++)
            {
                array.SetValue(Items[i].As(elementType), i);
            }
            return array;
        }

        if (targetType == typeof(List<XmlRpcValue>) || targetType == typeof(IList<XmlRpcValue>))
            return Items;

        throw new InvalidOperationException($"Cannot convert array to {targetType.Name}");
    }

    public override string ToXml()
    {
        var xml = "<value><array><data>";
        foreach (var item in Items)
        {
            xml += item.ToXml();
        }
        xml += "</data></array></value>";
        return xml;
    }
}

/// <summary>
/// Represents an XML-RPC struct (dictionary/map) value.
/// </summary>
public class XmlRpcStruct : XmlRpcValue
{
    /// <summary>
    /// Gets the dictionary of struct members with string keys and XML-RPC values.
    /// </summary>
    public Dictionary<string, XmlRpcValue> Members { get; }

    public XmlRpcStruct() => Members = new Dictionary<string, XmlRpcValue>();
    public XmlRpcStruct(Dictionary<string, XmlRpcValue> members) => Members = members ?? new Dictionary<string, XmlRpcValue>();

    public override T As<T>()
    {
        // For Dictionary<string, object> or similar
        if (typeof(T) == typeof(Dictionary<string, object>) || typeof(T) == typeof(IDictionary<string, object>))
        {
            var dict = new Dictionary<string, object>();
            foreach (var kvp in Members)
            {
                dict[kvp.Key] = kvp.Value.As(typeof(object));
            }
            return (T)(object)dict;
        }

        if (typeof(T) == typeof(Dictionary<string, XmlRpcValue>) || typeof(T) == typeof(IDictionary<string, XmlRpcValue>))
            return (T)(object)Members;

        throw new InvalidOperationException($"Cannot convert struct to {typeof(T).Name}");
    }

    public override object As(Type targetType)
    {
        if (targetType == typeof(Dictionary<string, object>) || targetType == typeof(IDictionary<string, object>))
        {
            var dict = new Dictionary<string, object>();
            foreach (var kvp in Members)
            {
                dict[kvp.Key] = kvp.Value.As(typeof(object));
            }
            return dict;
        }

        if (targetType == typeof(Dictionary<string, XmlRpcValue>) || targetType == typeof(IDictionary<string, XmlRpcValue>))
            return Members;

        throw new InvalidOperationException($"Cannot convert struct to {targetType.Name}");
    }

    public override string ToXml()
    {
        var xml = "<value><struct>";
        foreach (var kvp in Members)
        {
            var escapedName = kvp.Key.Replace("&", "&amp;")
                                      .Replace("<", "&lt;")
                                      .Replace(">", "&gt;")
                                      .Replace("\"", "&quot;")
                                      .Replace("'", "&apos;");
            xml += $"<member><name>{escapedName}</name>{kvp.Value.ToXml()}</member>";
        }
        xml += "</struct></value>";
        return xml;
    }
}