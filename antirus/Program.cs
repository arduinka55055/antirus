using antirus.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseRouting();

app.UseHttpsRedirection();

app.MapGet("/analyze", async(string id) =>
{
    Player player = new(id);
    await player.LoadPlayer();
    await player.LoadGames();
    await player.LoadFriends();
    return player;
})
.WithName("Analyze")
.WithOpenApi();

app.Run();
