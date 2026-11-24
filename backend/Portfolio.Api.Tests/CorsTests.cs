using System.Net;

namespace Portfolio.Api.Tests;

public class CorsTests
{
    [Theory]
    [InlineData("https://portfolio.example")]
    [InlineData("http://localhost:5173")]
    [InlineData("https://another-origin.example")]
    public async Task AnyOriginCanReadAndPreflightWriteRequests(string origin)
    {
        using var factory = new ApiFactory();
        using var client = factory.NewClient();
        client.DefaultRequestHeaders.Add("Origin", origin);
        var response = await client.GetAsync("/api/projects");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("*", Assert.Single(response.Headers.GetValues("Access-Control-Allow-Origin")));
        Assert.False(response.Headers.Contains("Access-Control-Allow-Credentials"));
        using var request = new HttpRequestMessage(HttpMethod.Options, "/api/projects/1");
        request.Headers.Add("Access-Control-Request-Method", "PUT");
        request.Headers.Add("Access-Control-Request-Headers", "content-type,x-csrf-token");
        var preflight = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.NoContent, preflight.StatusCode);
        Assert.Equal("*", Assert.Single(preflight.Headers.GetValues("Access-Control-Allow-Origin")));
        Assert.Contains("PUT", string.Join(",", preflight.Headers.GetValues("Access-Control-Allow-Methods")));
        Assert.Contains("x-csrf-token", string.Join(",", preflight.Headers.GetValues("Access-Control-Allow-Headers")));
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.DeleteAsync("/api/projects/1")).StatusCode);
    }
}
