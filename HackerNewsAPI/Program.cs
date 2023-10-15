using HackerNewsAPI.Interfaces;
using HackerNewsAPI.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using Polly;
using Polly.Extensions.Http;
using System.Net;
using Serilog;
using Serilog.Sinks;
using Serilog.Events;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddMemoryCache();
builder.Services.AddHttpClient();
builder.Services.AddMemoryCache();
//builder.Services.AddHttpClient("HackerNewsApiClient")
//    .UseHttpClientRetryPolicy(retry => retry.WaitAndRetryAsync(3, _ => TimeSpan.FromSeconds(2)))
//    .UseHttpClientCircuitBreaker(cb => cb.CircuitBreakerAsync(3, TimeSpan.FromSeconds(15)));

builder.Services.AddSingleton<IStoryService, StoryService>();

// Configure Serilog for logging
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
    .MinimumLevel.Override("System", LogEventLevel.Information)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpClient("HackerNewsApiClient")
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
    {
        // Any custom handler configurations can be done here
    })
    .AddPolicyHandler(GetRetryPolicy);

IAsyncPolicy<HttpResponseMessage> GetRetryPolicy(HttpRequestMessage message)
{
    return HttpPolicyExtensions
       .HandleTransientHttpError()
       .OrResult(response => response.StatusCode == HttpStatusCode.NotFound)
       .WaitAndRetryAsync(50, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
}
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Information) // Set minimum log level for Microsoft namespace
    .MinimumLevel.Override("System", LogEventLevel.Information)   // Set minimum log level for System namespace
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseDeveloperExceptionPage();
}
// Enable CORS
app.UseCors(options =>
{
    options.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
});


app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();


app.Run();
