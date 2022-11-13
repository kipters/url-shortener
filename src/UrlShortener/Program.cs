using System.Reflection;
using System.Runtime.InteropServices;
using System.Web;
using UrlShortener.ConfigModel;
using UrlShortener.HostedServices;
using UrlShortener.Services;

SQLitePCL.Batteries_V2.Init();

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.Configure<SqliteConfig>(builder.Configuration.GetRequiredSection("Sqlite"));

builder.Services
    .AddSingleton<SqliteManager>()
    .AddSingleton<IHostedService>(_ => _.GetRequiredService<SqliteManager>())
    .AddSingleton<IUrlStore>(_ => _.GetRequiredService<SqliteManager>());

var app = builder.Build();

// Configure the HTTP request pipeline.
if (builder.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseAuthorization();

app.MapGet("/env", async context =>
{
    var assembly = Assembly.GetExecutingAssembly();
    var env = context.RequestServices.GetRequiredService<IWebHostEnvironment>();
    var info = new
    {
        Version = assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()!
            .InformationalVersion,
        Runtime = Environment.Version.ToString() + "+" + RuntimeInformation.RuntimeIdentifier,
        OS = RuntimeInformation.OSDescription,
        Env = env.EnvironmentName,
    };
    await context.Response.WriteAsJsonAsync(info);
});

app.MapGet("/stop", (IHostApplicationLifetime lifetime) =>
{
    lifetime.StopApplication();
});

app.MapFallback(context =>
{
    var path = context.Request.Path.ToString().Substring(1);
    path = HttpUtility.UrlDecode(path);
    var db = context.RequestServices.GetRequiredService<IUrlStore>();

    if (db.TryGetUrl(path, out var uri))
    {
        context.Response.GetTypedHeaders().Location = uri;
        context.Response.StatusCode = 302;
    }
    else
    {
        context.Response.StatusCode = 404;
    }

    return Task.CompletedTask;
});

app.Run();
