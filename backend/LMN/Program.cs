using System.Web;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text;
using Microsoft.AspNetCore.Http;
using System.Collections.Concurrent;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast =  Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast")
.WithOpenApi();

// In-memory state store (for demo; use distributed cache in production)
static ConcurrentDictionary<string, (IdpConfig Idp, string Email)> stateStore = new();

app.MapPost("/api/auth/begin", (EmailRequest req, HttpRequest httpRequest) =>
{
    var idpConfigs = new Dictionary<string, IdpConfig>
    {
        ["companya.com"] = new IdpConfig
        {
            Authority = "https://login.companya.com",
            ClientId = "companyA-client-id",
            ClientSecret = "companyA-client-secret",
            RedirectUri = "https://localhost:5001/api/auth/callback",
            Scope = "openid profile email"
        },
        ["companyb.org"] = new IdpConfig
        {
            Authority = "https://login.companyb.org",
            ClientId = "companyB-client-id",
            ClientSecret = "companyB-client-secret",
            RedirectUri = "https://localhost:5001/api/auth/callback",
            Scope = "openid profile email"
        }
    };

    var email = req.Email?.Trim().ToLower();
    if (string.IsNullOrEmpty(email) || !email.Contains("@"))
        return Results.BadRequest(new { message = "Invalid email address." });

    var domain = email.Split('@').Last();
    if (!idpConfigs.TryGetValue(domain, out var idp))
        return Results.BadRequest(new { message = $"No IDP configured for domain: {domain}" });

    // Build the authorization URL
    var state = Guid.NewGuid().ToString();
    stateStore[state] = (idp, email); // Store state -> IDP config and email
    var query = HttpUtility.ParseQueryString(string.Empty);
    query["client_id"] = idp.ClientId;
    query["redirect_uri"] = idp.RedirectUri;
    query["response_type"] = "code";
    query["scope"] = idp.Scope;
    query["state"] = state;

    var authUrl = $"{idp.Authority}/connect/authorize?{query}";
    return Results.Ok(new { redirectUrl = authUrl });
});

app.MapGet("/api/auth/callback", async (string code, string state, HttpResponse httpResponse) =>
{
    // Retrieve IDP config and email from state
    if (!stateStore.TryRemove(state, out var stateInfo))
    {
        return Results.BadRequest(new { message = "Invalid or expired state." });
    }
    var idp = stateInfo.Idp;
    var email = stateInfo.Email;

    using var httpClient = new HttpClient();
    var tokenEndpoint = $"{idp.Authority}/connect/token";
    var postData = new List<KeyValuePair<string, string>>
    {
        new("grant_type", "authorization_code"),
        new("code", code),
        new("redirect_uri", idp.RedirectUri),
        new("client_id", idp.ClientId),
        new("client_secret", idp.ClientSecret ?? "")
    };
    var content = new FormUrlEncodedContent(postData);
    var response = await httpClient.PostAsync(tokenEndpoint, content);
    var responseBody = await response.Content.ReadAsStringAsync();
    if (!response.IsSuccessStatusCode)
    {
        return Results.BadRequest(new { message = "Token exchange failed", details = responseBody });
    }
    var tokenResult = JsonSerializer.Deserialize<JsonElement>(responseBody);

    // Create a session (for demo, just a GUID; in production, store tokens securely)
    var sessionId = Guid.NewGuid().ToString();
    // In production, store sessionId and tokens in a distributed cache or DB
    httpResponse.Cookies.Append("LMN_SESSION", sessionId, new CookieOptions
    {
        HttpOnly = true,
        Secure = true,
        SameSite = SameSiteMode.Strict,
        Path = "/"
    });

    // Redirect to downstream app (placeholder)
    var downstreamAppUrl = "https://downstream-app.example.com/home";
    return Results.Redirect(downstreamAppUrl);
});

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}

record EmailRequest(string Email);
record IdpConfig
{
    public string Authority { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string? ClientSecret { get; set; } = string.Empty;
    public string RedirectUri { get; set; } = string.Empty;
    public string Scope { get; set; } = string.Empty;
}
