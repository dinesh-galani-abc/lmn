using System.Web;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text;
using Microsoft.AspNetCore.Http;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authentication;
using static System.Net.WebRequestMethods;

var builder = WebApplication.CreateBuilder(args);

var idpConfigs = new Dictionary<string, IdpConfig>
{
    ["engage.com"] = new IdpConfig
    {
        Authority = "https://localhost:444",
        ClientId = "fb41429a-2f43-4d12-8c8c-9175173e7344",
        RedirectUri = "http://localhost:4200/callback",
        PostLogoutRedirectUri = "http://localhost:4200/login",
        Scope = "openid profile"
    },
    ["ansibytecode.com"] = new IdpConfig
    {
        Authority = "https://localhost",
        ClientId = "cbb05eb0-2044-41dd-94ad-723df58d91d4",
        RedirectUri = "http://localhost:4200/callback",
        PostLogoutRedirectUri = "http://localhost:4200/login",
        Scope = "openid profile"
    }
};
// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

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

app.UseCors();

app.MapGet("/weatherforecast", () =>
{
    var forecast = Enumerable.Range(1, 5).Select(index =>
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
ConcurrentDictionary<string, (IdpConfig Idp, string Email, string codeVerifier)> stateStore = new();

app.MapPost("/api/auth/begin", (EmailRequest req, HttpRequest httpRequest) =>
{


    var email = req.Email?.Trim().ToLower();
    if (string.IsNullOrEmpty(email) || !email.Contains("@"))
        return Results.BadRequest(new { message = "Invalid email address." });

    var domain = email.Split('@').Last();
    if (!idpConfigs.TryGetValue(domain, out var idp))
        return Results.BadRequest(new { message = $"No IDP configured for domain: {domain}" });

    // Generate code verifier and code challenge
    var codeVerifier = GenerateCodeVerifier();
    var codeChallenge = GenerateCodeChallenge(codeVerifier);

    var state = Guid.NewGuid().ToString();
    stateStore[state] = (idp, email, codeVerifier); // Store state + codeVerifier for later use

    var query = HttpUtility.ParseQueryString(string.Empty);
    query["client_id"] = idp.ClientId;
    query["redirect_uri"] = idp.RedirectUri;
    query["response_type"] = "code";
    query["scope"] = idp.Scope;
    query["state"] = state;
    query["code_challenge"] = codeChallenge;
    query["code_challenge_method"] = "S256";

    var authUrl = $"{idp.Authority}/connect/authorize?{query}";
    return Results.Ok(new { authUrl });
});

app.MapPost("/api/auth/logout", async (HttpContext context, LogoutRequest req) =>
{
    var email = req.Email?.Trim().ToLower();
    if (string.IsNullOrEmpty(email) || !email.Contains("@"))
        return Results.BadRequest(new { message = "Invalid email." });

    var domain = email.Split('@').Last();
    if (!idpConfigs.TryGetValue(domain, out var idp))
        return Results.BadRequest(new { message = $"No IDP configured for domain: {domain}" });

    // Build IDP logout URL
    var logoutUrl = $"{idp.Authority}/connect/endsession";
    var query = HttpUtility.ParseQueryString(string.Empty);
    query["id_token_hint"] = req.IdToken;
    query["post_logout_redirect_uri"] = idp.PostLogoutRedirectUri;
    logoutUrl = $"{logoutUrl}?{query}";

    return Results.Ok(new { logoutUrl });
});


// Helpers
string GenerateCodeVerifier()
{
    var rng = RandomNumberGenerator.Create();
    var bytes = new byte[32];
    rng.GetBytes(bytes);
    return Base64UrlEncode(bytes);
}

string GenerateCodeChallenge(string codeVerifier)
{
    var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(codeVerifier));
    return Base64UrlEncode(bytes);
}

string Base64UrlEncode(byte[] input)
{
    return Convert.ToBase64String(input)
        .TrimEnd('=')
        .Replace('+', '-')
        .Replace('/', '_');
}

app.MapPost("/api/auth/callback", async (AuthCallbackRequest req) =>
{
    if (!stateStore.TryGetValue(req.State, out var stateData))
        return Results.BadRequest(new { message = "Invalid state." });

    var tokenClient = new HttpClient();

    var parameters = new Dictionary<string, string>
    {
        { "grant_type", "authorization_code" },
        { "client_id", stateData.Idp.ClientId },
        { "redirect_uri", stateData.Idp.RedirectUri },
        { "code", req.Code },
        { "code_verifier", stateData.codeVerifier }
    };

    if (!string.IsNullOrEmpty(stateData.Idp.ClientSecret))
    {
        parameters.Add("client_secret", stateData.Idp.ClientSecret);
    }

    var response = await tokenClient.PostAsync($"{stateData.Idp.Authority}/connect/token", new FormUrlEncodedContent(parameters));
    if (!response.IsSuccessStatusCode)
    {
        var error = await response.Content.ReadAsStringAsync();
        return Results.BadRequest(new { message = "Token exchange failed", error });
    }

    var content = await response.Content.ReadAsStringAsync();
    return Results.Ok(JsonDocument.Parse(content));
});


app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}

record EmailRequest(string Email);

record LogoutRequest(string Email,string IdToken);
record IdpConfig
{
    public string Authority { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string? ClientSecret { get; set; } = string.Empty;
    public string RedirectUri { get; set; } = string.Empty;
    public string Scope { get; set; } = string.Empty;
    public string PostLogoutRedirectUri { get; set; } = string.Empty;
}

record AuthCallbackRequest(string Code, string State);
