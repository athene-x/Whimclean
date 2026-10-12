using System.Net;
using System.Net.Http.Json;

namespace Athenex.Whimclean.Identity.Keycloak.Tests;

internal sealed class StubHandler : HttpMessageHandler
{
    public List<HttpRequestMessage> Requests { get; } = [];

    public List<string> Bodies { get; } = [];

    public Queue<Func<HttpRequestMessage, HttpResponseMessage>> Responses { get; } = new();

    public int TokenRequests { get; private set; }

    public StubHandler Enqueue(Func<HttpRequestMessage, HttpResponseMessage> response)
    {
        Responses.Enqueue(response);
        return this;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        if (request.RequestUri!.AbsolutePath.EndsWith("/protocol/openid-connect/token", StringComparison.Ordinal))
        {
            TokenRequests++;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new { access_token = $"token-{TokenRequests}", expires_in = 300 })
            };
        }

        Requests.Add(request);
        Bodies.Add(request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken));

        return Responses.Count > 0
            ? Responses.Dequeue()(request)
            : new HttpResponseMessage(HttpStatusCode.InternalServerError) { Content = new StringContent("no stub") };
    }
}

internal sealed class MutableTimeProvider(DateTimeOffset now) : TimeProvider
{
    public DateTimeOffset Now { get; set; } = now;

    public override DateTimeOffset GetUtcNow() => Now;
}
