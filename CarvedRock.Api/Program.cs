using CarvedRock.Api;
using CarvedRock.Core;
using CarvedRock.Data;
using CarvedRock.Domain;
using CarvedRock.ServiceDefaults;
using FluentValidation;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();

builder.Services.AddValidatorsFromAssemblyContaining<NewProductValidator>();
builder.Services.AddProblemDetails(opts => opts.CustomizeProblemDetails = CustomizeProblemDetails);
builder.Services.AddExceptionHandler<ValidationExceptionHandler>();

JwtSecurityTokenHandler.DefaultMapInboundClaims = false;
builder.Services.AddAuthentication("Bearer")
    .AddJwtBearer("Bearer", options =>
    {
        options.Authority = builder.Configuration.GetValue<string>("Auth:Authority"); ;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            NameClaimType = "email",
            ValidateAudience = false
        };
    });
builder.Services.AddTransient<IClaimsTransformation, AdminClaimsTransformation>();

builder.Services.AddTransient<IConfigureOptions<SwaggerGenOptions>, SwaggerOptions>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddScoped<IProductLogic, ProductLogic>();

builder.AddNpgsqlDbContext<LocalContext>("CarvedRockPostgres"); // aspire integration
// "old-school" postgres ef core db context creation
// var cstr = builder.Configuration.GetConnectionString("CarvedRockPostgres");
// builder.Services.AddDbContext<LocalContext>(options =>
//     options.UseNpgsql(cstr));

builder.Services.AddScoped<ICarvedRockRepository, CarvedRockRepository>();

var app = builder.Build();

app.MapDefaultEndpoints();
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    SetupDevelopment(app);
}

app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<UserScopeMiddleware>();
app.MapControllers().RequireAuthorization();

app.Run();

static void SetupDevelopment(WebApplication app)
{
    using (var scope = app.Services.CreateScope())
    {
        var services = scope.ServiceProvider;
        var context = services.GetRequiredService<LocalContext>();
        context.MigrateAndCreateData();
    }
    ;

    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.OAuthClientId("interactive.public");
        options.OAuthAppName("CarvedRock API");
        options.OAuthUsePkce();
    });
}

static void CustomizeProblemDetails(ProblemDetailsContext context)
{
    context.ProblemDetails.Detail = "Provide the instance value when contacting us for support";
    context.ProblemDetails.Instance = Activity.Current?.RootId;
}
