using System.Web;
using UrlShortener.ConfigModel;
using UrlShortener.Services;

SQLitePCL.Batteries_V2.Init();

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.Configure<SqliteConfig>(builder.Configuration.GetRequiredSection("Sqlite"));
builder.Services.AddTransient<IUrlStore, SqliteUrlStore>();

var app = builder.Build();

app.Services.GetRequiredService<IUrlStore>().Init();

// Configure the HTTP request pipeline.
if (builder.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

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
