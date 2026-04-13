using Projects;

var builder = DistributedApplication.CreateBuilder(args);

var carvedrockdb = builder.AddPostgres("postgres")
    .WithUrlForEndpoint("tcp", u => u.DisplayLocation = UrlDisplayLocation.DetailsOnly)
    .AddDatabase("CarvedRockPostgres");

var api = builder.AddProject<CarvedRock_Api>("api")
    .WithUrlForEndpoint("https", url => url.DisplayText = "Swagger UI")
    .WithReference(carvedrockdb)
    .WaitFor(carvedrockdb);

var mailpit = builder.AddMailPit("smtp")
    .WithUrlForEndpoint("http", u => u.DisplayText = "Email Inbox")
    .WithUrlForEndpoint("smtp", u => u.DisplayLocation = UrlDisplayLocation.DetailsOnly);

var mcp = builder.AddProject<CarvedRock_Mcp>("mcp")
    .WithUrlForEndpoint("https", u => u.DisplayLocation = UrlDisplayLocation.DetailsOnly)
    .WaitFor(api)
    .WithReference(api);

var openAiKeyParam = builder.AddParameter("openaiKey", secret: true)
    .WithDescription("OpenAI API Key.  Get one from [OpenAI](https://platform.openai.com). Note " +
     "that without this set, all features except the AI chat and the Agent API will still work fine.",
                enableMarkdown: true);

var agent = builder.AddProject<CarvedRock_Agent>("agent")
    .WithUrlForEndpoint("https", u => u.DisplayText = "Agent Swagger")
    .WithEnvironment("AIConnection__OpenAIKey", openAiKeyParam)
    .WaitFor(api)
    .WithReference(mcp);

builder.AddProject<CarvedRock_WebApp>("webapp")
    .WithUrlForEndpoint("https", u => u.DisplayText = "Web App")
    .WithReference(api)
    .WithReference(agent)
    .WithReference(mailpit)
    .WaitFor(mailpit)
    .WaitFor(api);

builder.AddMcpInspector("mcp-inspector", options => options.InspectorVersion = "0.21.1")
    .WithMcpServer(mcp, path: "")
    .WithUrlForEndpoint("client", u => u.DisplayText = "MCP Inspector")
    .WithUrlForEndpoint("server-proxy", u => u.DisplayLocation = UrlDisplayLocation.DetailsOnly);

builder.Build().Run();
