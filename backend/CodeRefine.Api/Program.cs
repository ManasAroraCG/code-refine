using CodeRefine.Api.Configuration;
using CodeRefine.Api.Data;
using CodeRefine.Api.Middleware;
using CodeRefine.Api.Services.AI;
using CodeRefine.Api.Services.Analysis;
using CodeRefine.Api.Services.Git;
using CodeRefine.Api.Services.GitHub;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<GitHubSettings>(builder.Configuration.GetSection(GitHubSettings.SectionName));
builder.Services.Configure<AiServiceSettings>(builder.Configuration.GetSection(AiServiceSettings.SectionName));

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Named client plus a singleton provider so the installation token cache is
// shared across requests instead of being rebuilt per scope.
builder.Services.AddHttpClient(GitHubAppTokenProvider.HttpClientName, (provider, client) =>
{
    var settings = provider.GetRequiredService<IOptions<GitHubSettings>>().Value;
    client.BaseAddress = new Uri(settings.ApiBaseUrl);
    client.DefaultRequestHeaders.UserAgent.ParseAdd(settings.UserAgent);
    client.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
});

builder.Services.AddSingleton<IGitHubAppTokenProvider>(provider => new GitHubAppTokenProvider(
    provider.GetRequiredService<IHttpClientFactory>(),
    provider.GetRequiredService<IOptions<GitHubSettings>>(),
    provider.GetRequiredService<ILogger<GitHubAppTokenProvider>>()));

builder.Services.AddScoped<IGitHubService, GitHubService>();

builder.Services.AddHttpClient<IAiService, AiService>((provider, client) =>
{
    var settings = provider.GetRequiredService<IOptions<AiServiceSettings>>().Value;

    if (!string.IsNullOrWhiteSpace(settings.BaseUrl))
    {
        client.BaseAddress = new Uri(settings.BaseUrl);
    }

    client.Timeout = TimeSpan.FromSeconds(settings.TimeoutSeconds);
});

builder.Services.AddScoped<IGitService, GitService>();
builder.Services.AddScoped<IAnalysisService, AnalysisService>();

builder.Services.AddCors(options =>
    options.AddDefaultPolicy(policy => policy
        .AllowAnyOrigin()
        .AllowAnyHeader()
        .AllowAnyMethod()));

builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "CodeRefine API",
        Version = "v1",
        Description = "AI code quality gate: analysis orchestration, human review and GitHub delivery."
    }));

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "CodeRefine API v1");
        options.RoutePrefix = "swagger";
    });
}

app.UseHttpsRedirection();
app.UseCors();
app.MapControllers();

app.Run();
