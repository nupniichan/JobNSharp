using System.Net;
using System.Text.Json.Serialization;
using JobNSharp.Interfaces;
using JobNSharp.Services;
using JobNSharp.Sites;
using JobNSharp.Utils;
using Polly;
using Polly.Extensions.Http;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddSingleton<IProxyService, ProxyService>();
builder.Services.AddSingleton<IRateLimiter, RateLimiter>();

builder.Services.AddHttpClient("JobCrawler", client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
    client.DefaultRequestHeaders.UserAgent.ParseAdd(
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36");
    client.DefaultRequestHeaders.AcceptLanguage.ParseAdd("en-US,en;q=0.9");
})
.ConfigurePrimaryHttpMessageHandler(sp =>
{
    var proxyService = sp.GetRequiredService<IProxyService>();
    var handler = new SocketsHttpHandler
    {
        AutomaticDecompression = DecompressionMethods.All,
        PooledConnectionLifetime = TimeSpan.FromMinutes(2)
    };
    var proxy = proxyService.GetNextProxy();
    if (proxy != null) handler.Proxy = proxy;
    return handler;
})
.SetHandlerLifetime(TimeSpan.FromMinutes(1))
.AddPolicyHandler(GetRetryPolicy())
.AddPolicyHandler(GetCircuitBreakerPolicy());

builder.Services.AddHttpClient("IndeedApi", client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
    client.DefaultRequestHeaders.Add("indeed-api-key", "161092c2017b5bbab13edb12461a62d5a833871e7cad6d9d475304573de67ac8");
    client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
    client.DefaultRequestHeaders.Add("indeed-locale", "en-US");
    client.DefaultRequestHeaders.AcceptLanguage.ParseAdd("en-US,en;q=0.9");
    client.DefaultRequestHeaders.UserAgent.ParseAdd(
        "Mozilla/5.0 (iPhone; CPU iPhone OS 16_6_1 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Mobile/15E148 Indeed App 193.1");
    client.DefaultRequestHeaders.Add("indeed-app-info", "appv=193.1; appid=com.indeed.jobsearch; osv=16.6.1; os=ios; dtype=phone");
})
.ConfigurePrimaryHttpMessageHandler(sp =>
{
    var proxyService = sp.GetRequiredService<IProxyService>();
    var handler = new SocketsHttpHandler
    {
        AutomaticDecompression = DecompressionMethods.All,
        PooledConnectionLifetime = TimeSpan.FromMinutes(2)
    };
    var proxy = proxyService.GetNextProxy();
    if (proxy != null) handler.Proxy = proxy;
    return handler;
})
.SetHandlerLifetime(TimeSpan.FromMinutes(1))
.AddPolicyHandler(GetRetryPolicy())
.AddPolicyHandler(GetCircuitBreakerPolicy());

builder.Services.AddSingleton<IJobProvider, LinkedInProvider>();
builder.Services.AddSingleton<IJobProvider, IndeedProvider>();
builder.Services.AddSingleton<IJobProviderFactory, JobProviderFactory>();
builder.Services.AddScoped<IJobCrawlService, JobCrawlService>();

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.MapControllers();
app.Run();

static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
{
    return HttpPolicyExtensions
        .HandleTransientHttpError()
        .OrResult(r => r.StatusCode == HttpStatusCode.TooManyRequests)
        .WaitAndRetryAsync(3, attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)));
}

static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy()
{
    return HttpPolicyExtensions
        .HandleTransientHttpError()
        .CircuitBreakerAsync(5, TimeSpan.FromSeconds(30));
}
