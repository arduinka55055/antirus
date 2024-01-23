using System.Globalization;
using System.Threading.RateLimiting;
using antirus.bot;
using antirus.Models;
using antirus.Util;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.SpaServices.AngularCli;
using Microsoft.Extensions.Caching.Memory;

//set invariant culture for the whole app
CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;
CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;

var dotenv = Path.Combine(Directory.GetCurrentDirectory(), ".env");
DotEnv.Load(dotenv);


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddControllers();
builder.Services.AddSwaggerGen();
builder.Services.AddResponseCaching();
builder.Services.AddResponseCompression();
builder.Services.AddMemoryCache();

builder.Services.AddRateLimiter(_ => _
        .AddFixedWindowLimiter(policyName: "fixed", options =>
        {
            options.PermitLimit = 4;
            options.Window = TimeSpan.FromSeconds(5);
            options.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
            options.QueueLimit = 2;
        }));


var app = builder.Build();

Player.Init(app.Services.GetService<ILogger<Player>>(), app.Services.GetService<IMemoryCache>());

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseCors();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Main}/{action=Index}/{id?}");

Bot.Launch();

app.Run();
