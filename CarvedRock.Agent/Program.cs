using CarvedRock.Agent;
using CarvedRock.Core;
using CarvedRock.ServiceDefaults;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddProblemDetails(opts => opts.CustomizeProblemDetails = CustomizeProblemDetails);

// package: Aspire.Azure.AI.OpenAI
// builder.AddAzureOpenAIClient("kyt-AzureOpenAI", configureSettings: settings =>
// {
//     settings.EnableSensitiveTelemetryData = true;
//     settings.Endpoint = new Uri(builder.Configuration.GetValue<string>("AIConnection:Endpoint")!);
//     settings.Key = builder.Configuration.GetValue<string>("AIConnection:Key")!;
// }).AddChatClient(builder.Configuration.GetValue<string>("AIConnection:Deployment")!);

// package: Aspire.OpenAI
// Then add your API key for OpenAI to user secrets for the AIConnection:OpenAIKey value
builder.AddOpenAIClient("kyt-OpenAI", configureSettings: settings =>
{
    settings.EnableSensitiveTelemetryData = true;
    settings.Key = builder.Configuration.GetValue<string>("AIConnection:OpenAIKey");
}).AddChatClient(builder.Configuration.GetValue<string>("AIConnection:OpenAIModel"));

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<Agent>();

var authority = builder.Configuration.GetValue<string>("Auth:Authority");
JwtSecurityTokenHandler.DefaultMapInboundClaims = false;
builder.Services.AddAuthentication("Bearer")
    .AddJwtBearer("Bearer", options =>
    {
        options.Authority = authority;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            NameClaimType = "email",
            ValidateAudience = false
        };
    });
builder.Services.AddAuthorization();
builder.Services.AddTransient<IClaimsTransformation, AdminClaimsTransformation>();

builder.Services.AddTransient<IConfigureOptions<SwaggerGenOptions>, SwaggerOptions>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.MapDefaultEndpoints();
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.OAuthClientId("interactive.public");
        options.OAuthAppName("CarvedRock Agent API");
        options.OAuthUsePkce();
    });
}

app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<UserScopeMiddleware>();

app.MapGet("/agent", async (string message, Agent agent, CancellationToken cancellationToken) =>
{
    return agent.GetAgentResponse(message, cancellationToken);
});

app.Run();

static void CustomizeProblemDetails(ProblemDetailsContext context)
{
    context.ProblemDetails.Detail = "Provide the instance value when contacting us for support";
    context.ProblemDetails.Instance = Activity.Current?.RootId;
}

record AIConnection(string Endpoint, string Key, string Deployment);
