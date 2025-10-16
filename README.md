# XML-RPC-ng

[![.NET](https://img.shields.io/badge/.NET-9.0-purple.svg)](https://dotnet.microsoft.com/)
[![License](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![Build Status](https://img.shields.io/badge/Build-Passing-green.svg)]()

A modern, high-performance C# **client** implementation of the XML-RPC protocol for .NET 9.0 applications.

## 🎯 Project Overview

**XML-RPC-ng** ("XML-RPC Next Generation") is a complete rewrite of the XML-RPC protocol implementation designed specifically for modern .NET applications. This library provides a clean, type-safe, and performant way to integrate XML-RPC communication into your .NET projects.

### Key Features

- ✅ **Modern .NET 9.0**: Built with the latest .NET features and performance optimizations
- ✅ **Async/Await Support**: First-class support for asynchronous operations
- ✅ **Type-Safe**: Strong typing with automatic serialization/deserialization
- ✅ **Flexible Configuration**: Fluent API for easy client configuration
- ✅ **Comprehensive Error Handling**: Structured exception hierarchy for different error scenarios
- ✅ **HTTP/HTTPS Support**: Full support for both HTTP and secure HTTPS connections
- ✅ **Extensible Architecture**: Easy to extend with custom serializers and transports
- ✅ **Production Ready**: Thoroughly tested and designed for production workloads

### 🚀 What Makes XML-RPC-ng Different?

Unlike legacy XML-RPC implementations, XML-RPC-ng is built from the ground up to take advantage of modern .NET capabilities:

- **Performance Optimized**: Minimal allocations and efficient memory usage
- **Modern C#**: Uses latest language features like records, pattern matching, and nullable reference types
- **DI Friendly**: Designed with dependency injection in mind
- **Extensible**: Plugin architecture for custom behaviors

## 📦 Installation

```bash
dotnet add package XmlRpcNg
```

### Prerequisites

- .NET 9.0 or later
- Visual Studio 2022 or JetBrains Rider

## 🏗️ Architecture Overview

### Core Components

- **XmlRpcClient**: Main client interface for making XML-RPC calls with automatic type conversion, timeout configuration, and comprehensive error handling
- **XmlRpcClientBuilder**: Fluent configuration API for setting up clients with `WithUrl()`, `WithTimeout()`, `WithHttpClient()`, and `WithSerializer()` methods
- **IXmlRpcSerializer**: Pluggable serialization system handling automatic conversions between .NET types and XML-RPC values
- **IXmlRpcTransport**: Pluggable transport layer with HTTP/HTTPS support, configurable timeouts, and cancellation token support
- **XmlRpcTypes**: Type-safe XML-RPC data representations including `XmlRpcInt`, `XmlRpcBoolean`, `XmlRpcString`, `XmlRpcDouble`, `XmlRpcDateTime`, `XmlRpcBase64`, `XmlRpcArray`, and `XmlRpcStruct`

### Data Type Mappings

The library automatically converts between .NET types and XML-RPC values:
- `int` ↔ `XmlRpcInt`
- `bool` ↔ `XmlRpcBoolean`
- `string` ↔ `XmlRpcString`
- `double`/`float` ↔ `XmlRpcDouble`
- `DateTime` ↔ `XmlRpcDateTime`
- `byte[]` ↔ `XmlRpcBase64`
- `Dictionary<string, T>` ↔ `XmlRpcStruct`
- Arrays/Collections ↔ `XmlRpcArray`

### Design Patterns

- **Builder Pattern**: For fluent client configuration
- **Strategy Pattern**: For pluggable serialization and transport strategies
- **Dependency Injection**: Service-friendly design with registration support
- **Async/Await**: Modern asynchronous programming model throughout

## Common Usage Patterns

### Basic Method Call
```csharp
// Basic usage

// Example: Get state name by code
const string endpoint = "http://betty.userland.com/RPC2";
const string methodName = "examples.getStateName";
const int stateCode = 23;

// Create client
var client = new XmlRpcClientBuilder()
     .WithUrl(endpoint)
     .WithTimeout(TimeSpan.FromSeconds(30))
     .Build();

// Call method 
var result = await client.CallAsync<string>(methodName, stateCode);

Console.WriteLine($"State name: {result}");
// Result: "State name: Minnesota"
```

### Dictionary/Struct Parameter
```csharp
var data = new Dictionary<string, object>
{
    { "name", "John" },
    { "age", 30 },
    { "active", true }
};

var result = await client.CallAsync<string>("processUser", data);
```

### Advanced Usage with Dependency Injection
```csharp
// In Program.cs or Startup.cs
services.AddXmlRpcClient(options =>
{
    options.Url = "https://api.example.com/xmlrpc";
    options.Timeout = TimeSpan.FromSeconds(30);
});

// In your service
public class MyService
{
    private readonly XmlRpcClient _client;

    public MyService(XmlRpcClient client)
    {
        _client = client;
    }

    public async Task<string> GetResult()
    {
        return await _client.CallAsync<string>("methodName", parameter);
    }
}
```

### Custom Serialization
```csharp
// Register custom serializer
services.AddXmlRpcClient(options =>
{
    options.Url = "https://api.example.com/xmlrpc";
})
.WithCustomSerializer<CustomXmlRpcSerializer>();
```

## 🔧 Error Handling

XML-RPC-ng provides comprehensive error handling with specific exception types:

```csharp
try
{
    var result = await client.CallAsync<string>("method", param);
}
catch (XmlRpcFaultException ex)
{
    // Server returned a fault
    Console.WriteLine($"Server fault: {ex.FaultCode} - {ex.FaultString}");
}
catch (XmlRpcNetworkException ex)
{
    // Network communication error
    Console.WriteLine($"Network error: {ex.Message}");
}
catch (XmlRpcProtocolException ex)
{
    // XML-RPC protocol violation
    Console.WriteLine($"Protocol error: {ex.Message}");
}
catch (XmlRpcXmlException ex)
{
    // XML parsing error
    Console.WriteLine($"XML error: {ex.Message}");
}
catch (XmlRpcTypeConversionException ex)
{
    // Type conversion failed
    Console.WriteLine($"Type conversion error: {ex.Message}");
}
```

## 🧪 Testing

```bash
# Run all tests
dotnet test

# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage"

# Run specific test project
dotnet test XmlRpcNg.Tests
```

## 📚 API Reference

### XmlRpcClient Methods

| Method | Description | Example |
|--------|-------------|---------|
| `CallAsync<T>(method, params)` | Call XML-RPC method with return type | `await client.CallAsync<string>("method", 42)` |
| `CallAsync(method, returnType, params)` | Call with explicit return type | `await client.CallAsync("method", typeof(string), 42)` |

### Configuration Options

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `Url` | `string` | - | XML-RPC endpoint URL |
| `Timeout` | `TimeSpan` | 30 seconds | Request timeout |
| `HttpClient` | `HttpClient` | `null` | Custom HTTP client |


## 🤝 Contributing

Contributions are welcome! Feel free to fork the repository and submit pull requests.

### Development Setup

```bash
# Clone the repository
git clone https://github.com/yourusername/XmlRpcNg.git
cd XmlRpcNg

# Restore dependencies
dotnet restore

# Build the solution
dotnet build

# Run tests
dotnet test
```

## 📄 License

This project is licensed under MIT License - see the [LICENSE](LICENSE) file for details.

## 🤖 AI-Assisted Development

For transparency, this project was developed with the assistance of AI tools, specifically **GLM-4.6** (General Language Model 4.6). 