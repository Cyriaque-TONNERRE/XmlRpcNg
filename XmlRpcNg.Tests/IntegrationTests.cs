using Xunit;
using XmlRpcNg;
using Xunit.Abstractions;

namespace XmlRpcNg.Tests;

/// <summary>
/// Integration test for XmlRpcNg using the UserLand test server.
/// Tests the examples.getStateName method with state code 23.
/// </summary>
public class IntegrationTests
{
    private readonly ITestOutputHelper _testOutputHelper;

    public IntegrationTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    [Fact]
    public async Task ExamplesGetStateName_StateCode23_ReturnsMinnesota()
    {
        // Arrange
        const string endpoint = "http://betty.userland.com/RPC2";
        const string methodName = "examples.getStateName";
        const int stateCode = 23;

        var client = new XmlRpcClientBuilder()
            .WithUrl(endpoint)
            .WithTimeout(TimeSpan.FromSeconds(30))
            .Build();

        // Act
        var result = await client.CallAsync<string>(methodName, stateCode);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Minnesota", result); // State code 23 returns "Minnesota"
    }

    [Fact]
    public void BuildXmlRequest_StateCode23_GeneratesExpectedXml()
    {
        // Arrange
        const string methodName = "examples.getStateName";
        var stateCodes = 23;

        var serializer = new XmlRpcSerializer();
        var requestBuilder = new XmlRpcRequestBuilder(serializer);

        // Act
        var xmlRequest = requestBuilder.BuildXmlRequest(methodName, stateCodes);
        // Assert - Start with <?xml version="1.0"?> and options for encoding
        Assert.StartsWith("<?xml version=\"1.0\" encoding=\"UTF-8\"?>", xmlRequest);
        // Take the rest of the XML 
        xmlRequest = xmlRequest.Substring("<?xml version=\"1.0\" encoding=\"UTF-8\"?>".Length).Trim();
        // Trim any leading/trailing whitespace
        xmlRequest = xmlRequest.Trim();
        // Remove all whitespace between tags for comparison
        xmlRequest = System.Text.RegularExpressions.Regex.Replace(xmlRequest, @">\s+<", "><");
        // Remove all newlines for comparison
        xmlRequest = xmlRequest.Replace("\n", "").Replace("\r", "");
        const string start = "<methodCall><methodName>examples.getStateName</methodName><params><param><value>";
        const string end = "</value></param></params></methodCall>";
        // XML-RPC supports both <int> and <i4> as equivalent
        const string option1 = "<int>23</int>";
        const string option2 = "<i4>23</i4>";
        // Assert that the XML matches one of the expected formats
        Assert.True(xmlRequest == start + option1 + end || xmlRequest == start + option2 + end,
                     "Generated XML does not match expected format.");
    }

    [Fact]
    public async Task ExamplesGetStateList_ArrayOfStateCodes_ReturnsCommaSeparatedStateNames()
    {
        // Arrange
        const string endpoint = "http://betty.userland.com/RPC2";
        const string methodName = "examples.getStateList";
        var stateCodes = new[] { 15, 25, 35, 45 };

        var client = new XmlRpcClientBuilder()
            .WithUrl(endpoint)
            .WithTimeout(TimeSpan.FromSeconds(30))
            .Build();

        // Act
        var result = await client.CallAsync<string>(methodName, stateCodes);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Iowa,Missouri,Ohio,Vermont", result);
    }

    [Fact]
    public void BuildXmlRequest_ArrayOfStateCodes_GeneratesExpectedXml()
    {
        // Arrange
        const string methodName = "examples.getStateList";
        var stateCodes = new[] { 15, 25, 35, 45 };

        var serializer = new XmlRpcSerializer();
        var requestBuilder = new XmlRpcRequestBuilder(serializer);

        // Act
        var xmlRequest = requestBuilder.BuildXmlRequest(methodName, stateCodes);
        // Assert - Start with <?xml version="1.0"?> and options for encoding
        Assert.StartsWith("<?xml version=\"1.0\" encoding=\"UTF-8\"?>", xmlRequest);
        // Take the rest of the XML
        xmlRequest = xmlRequest.Substring("<?xml version=\"1.0\" encoding=\"UTF-8\"?>".Length).Trim();
        // Trim any leading/trailing whitespace
        xmlRequest = xmlRequest.Trim();
        // Remove all whitespace between tags for comparison
        xmlRequest = System.Text.RegularExpressions.Regex.Replace(xmlRequest, @">\s+<", "><");
        // Remove all newlines for comparison
        xmlRequest = xmlRequest.Replace("\n", "").Replace("\r", "");
        const string start = "<methodCall><methodName>examples.getStateList</methodName><params><param><value><array><data>";
        const string end = "</data></array></value></param></params></methodCall>";
        // XML-RPC supports both <int> and <i4> as equivalent
        const string option1 = "<value><int>15</int></value><value><int>25</int></value><value><int>35</int></value><value><int>45</int></value>";
        const string option2 = "<value><i4>15</i4></value><value><i4>25</i4></value><value><i4>35</i4></value><value><i4>45</i4></value>";
        // Assert that the XML matches one of the expected formats
        Assert.True(xmlRequest == start + option1 + end || xmlRequest == start + option2 + end,
                     "Generated XML does not match expected format.");
    }
    
    [Fact]
    public async Task ExamplesGetStateNames_ListOfStateCodes_ReturnsNewLineSeparatedStateNames()
    {
        // Arrange
        const string endpoint = "http://betty.userland.com/RPC2";
        const string methodName = "examples.getStateNames";
        var stateCodes = new[] { 12, 22, 32, 42 };

        var client = new XmlRpcClientBuilder()
            .WithUrl(endpoint)
            .WithTimeout(TimeSpan.FromSeconds(30))
            .Build();

        // Act
        var result = await client.CallAsync<string>(methodName, stateCodes.Cast<object>().ToArray());

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Idaho\nMichigan\nNew York\nTennessee", result);
    }
    
    [Fact]
    public void BuildXmlRequest_ListOfStateCodes_GeneratesExpectedXml()
    {
        // Arrange
        const string methodName = "examples.getStateNames";
        var stateCodes = new[] { 12, 22, 32, 42 };

        var serializer = new XmlRpcSerializer();
        var requestBuilder = new XmlRpcRequestBuilder(serializer);

        // Act
        var xmlRequest = requestBuilder.BuildXmlRequest(methodName, stateCodes.Cast<object>().ToArray());
        
        // Assert - Start with <?xml version="1.0"?> and options for encoding
        Assert.StartsWith("<?xml version=\"1.0\" encoding=\"UTF-8\"?>", xmlRequest);
        // Take the rest of the XML
        xmlRequest = xmlRequest.Substring("<?xml version=\"1.0\" encoding=\"UTF-8\"?>".Length).Trim();
        // Trim any leading/trailing whitespace
        xmlRequest = xmlRequest.Trim();
        // Remove all whitespace between tags for comparison
        xmlRequest = System.Text.RegularExpressions.Regex.Replace(xmlRequest, @">\s+<","><");
        // Remove all newlines for comparison
        xmlRequest = xmlRequest.Replace("\n", "").Replace("\r", "");
        const string start = "<methodCall><methodName>examples.getStateNames</methodName><params>";
        const string end = "</params></methodCall>";
        // XML-RPC supports both <int> and <i4> as equivalent
        const string option1 = "<param><value><int>12</int></value></param><param><value><int>22</int></value></param><param><value><int>32</int></value></param><param><value><int>42</int></value></param>";
        const String option2 = "<param><value><i4>12</i4></value></param><param><value><i4>22</i4></value></param><param><value><i4>32</i4></value></param><param><value><i4>42</i4></value></param>";
        // Assert that the XML matches one of the expected formats
        Assert.True(xmlRequest == start + option1 + end || xmlRequest == start + option2 + end,
                     "Generated XML does not match expected format.");
    }
    
    // Dictionary : {"a": 22, "b": 48}
    [Fact]
    public async Task ExamplesGetStateStruct_DictionaryOfStateCodes_CommaSeparatedStateNames()
    {
        // Arrange
        const string endpoint = "http://betty.userland.com/RPC2";
        const string methodName = "examples.getStateStruct";
        var stateCodes = new Dictionary<string, int>
        {
            { "a", 22 },
            { "b", 48 }
        };

        var client = new XmlRpcClientBuilder()
            .WithUrl(endpoint)
            .WithTimeout(TimeSpan.FromSeconds(30))
            .Build();

        // Act
        var result = await client.CallAsync<string>(methodName, stateCodes);
        
        // Assert
        Assert.NotNull(result);
        Assert.Equal("Michigan,West Virginia", result);
    }

    [Fact]
    public void BuildXmlRequest_DictionaryOfStateCodes_GeneratesExpectedXml()
    {
        // Arrange
        const string methodName = "examples.getStateStruct";
        var stateCodes = new Dictionary<string, int>
        {
            { "a", 22 },
            { "b", 48 }
        };
        var serializer = new XmlRpcSerializer();
        var requestBuilder = new XmlRpcRequestBuilder(serializer);
        // Act
        var xmlRequest = requestBuilder.BuildXmlRequest(methodName, stateCodes);

        // Assert - Start with <?xml version="1.0"?> and options for encoding
        Assert.StartsWith("<?xml version=\"1.0\" encoding=\"UTF-8\"?>", xmlRequest);
        // Take the rest of the XML
        xmlRequest = xmlRequest.Substring("<?xml version=\"1.0\" encoding=\"UTF-8\"?>".Length).Trim();
        // Trim any leading/trailing whitespace
        xmlRequest = xmlRequest.Trim();
        // Remove all whitespace between tags for comparison
        xmlRequest = System.Text.RegularExpressions.Regex.Replace(xmlRequest, @">\s+<","><");
        // Remove all newlines for comparison
        xmlRequest = xmlRequest.Replace("\n", "").Replace("\r", "");
        const string start = "<methodCall><methodName>examples.getStateStruct</methodName><params><param><value><struct>";
        const string end = "</struct></value></param></params></methodCall>";
        // XML-RPC supports both <int> and <i4> as equivalent
        const string option1 = "<member><name>a</name><value><int>22</int></value></member><member><name>b</name><value><int>48</int></value></member>";
        const string option2 = "<member><name>a</name><value><i4>22</i4></value></member><member><name>b</name><value><i4>48</i4></value></member>";
        // Assert that the XML matches one of the expected formats
        Assert.True(xmlRequest == start + option1 + end || xmlRequest == start + option2 + end,
                     "Generated XML does not match expected format.");
    }
}