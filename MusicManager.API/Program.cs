using Elasticsearch;
using dotenv.net;
using System.IO;
using System;
using Microsoft.AspNetCore.Builder;
using Serilog;
using Serilog.Sinks.Elasticsearch;
using Microsoft.Extensions.Hosting;
using MusicManager.Infrastructure.MiddleWares;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using MusicManager.Core.Models;
using MusicManager.Core.ViewModules;
using Microsoft.EntityFrameworkCore;
using MusicManager.Infrastructure;
using Microsoft.Extensions.Logging;

const string envFileName = ".env";
var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
DotEnv.Load(options: new DotEnvOptions(envFilePaths: new[] { Path.Combine(baseDirectory, envFileName) }));

var builder = WebApplication.CreateBuilder(args);


builder.Host.UseSerilog((context, configuration) =>
{
    var logsEnvironment = context.Configuration.GetSection("AppSettings:AppEnvironment").Value ?? "dev";
    var version = "1.45.55";

    Console.WriteLine($"Contnet API ({logsEnvironment}) - V {version}");

    configuration.Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.Elasticsearch(new ElasticsearchSinkOptions(node: new Uri(context.Configuration.GetSection("AppSettings:elasticsearch:url").Value))
    {
        IndexFormat = $"medialibrary-{logsEnvironment}-api-log-{{0:yyyy-MM}}", 
        AutoRegisterTemplate = true,
        NumberOfShards = 2,
        NumberOfReplicas = 1
    })
    .Enrich.WithProperty("Envirenment", context.HostingEnvironment.EnvironmentName)
    .ReadFrom.Configuration(context.Configuration);
});

builder.Services.AddEndpointsApiExplorer();

IConfiguration configuration = builder.Configuration;

builder.Services.AddDbContextPool<MLContext>(builder =>
builder.UseNpgsql(configuration.GetSection("AppSettings:NpgConnection").Value));
builder.Services.Configure<AppSettings>(configuration.GetSection("AppSettings"));
builder.Services.AddMemoryCache();
builder.Services.AddElasticsearch(configuration);
builder.Services.AddInfrastructure();
builder.Services.AddAwsServices();
builder.Services.AddControllers();
builder.Services.AddHttpClient();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<ExceptionMiddleware>();

app.UseCors(
    options => options.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()
);

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.UseStaticFiles();
app.UseRouting();
app.UseHttpsRedirection();
app.UseAuthorization();

app.Run();


