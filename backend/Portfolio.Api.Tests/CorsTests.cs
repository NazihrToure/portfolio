using System.Net;

namespace Portfolio.Api.Tests;

public class CorsTests
{
    [Theory]
    [InlineData("https://nazihtoureportfolio.netlify.app", true)]
    [InlineData("http://localhost:2000", true)]
    [InlineData("http://127.0.0.1:2000", true)]
    [InlineData("http://localhost:5173", false)]
    [InlineData("https://another-origin.example", false)]
    public async Task OnlyConfiguredOriginsReceiveCorsPermission(string origin, bool allowed)
    {
        using var factory = new ApiFactory();
        using var client = factory.NewClient();
        client.DefaultRequestHeaders.Add("Origin", origin);
        var response = await client.GetAsync("/api/projects");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(allowed, response.Headers.Contains("Access-Control-Allow-Origin"));
        if (allowed)
        {
            Assert.Equal(origin, Assert.Single(response.Headers.GetValues("Access-Control-Allow-Origin")));
        }
        Assert.False(response.Headers.Contains("Access-Control-Allow-Credentials"));
        using var request = new HttpRequestMessage(HttpMethod.Options, "/api/projects/1");
        request.Headers.Add("Access-Control-Request-Method", "PUT");
        request.Headers.Add("Access-Control-Request-Headers", "content-type,x-csrf-token");
        var preflight = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.NoContent, preflight.StatusCode);
        Assert.Equal(allowed, preflight.Headers.Contains("Access-Control-Allow-Origin"));
        if (allowed)
        {
            Assert.Equal(origin, Assert.Single(preflight.Headers.GetValues("Access-Control-Allow-Origin")));
            Assert.Contains("PUT", string.Join(",", preflight.Headers.GetValues("Access-Control-Allow-Methods")));
            Assert.Contains("x-csrf-token", string.Join(",", preflight.Headers.GetValues("Access-Control-Allow-Headers")));
        }
        else
        {
            Assert.False(preflight.Headers.Contains("Access-Control-Allow-Methods"));
            Assert.False(preflight.Headers.Contains("Access-Control-Allow-Headers"));
        }
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.DeleteAsync("/api/projects/1")).StatusCode);
    }
}
