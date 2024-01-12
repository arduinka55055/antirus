using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using antirus.Models;
using Microsoft.AspNetCore.Mvc;

namespace antirus.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MainController : ControllerBase
{
    [HttpPost("{id}/full")]
    public async Task<Player> GetFull(string id, JsonParams jsonParams)
    {
        Player player = new(id);
        await player.LoadPlayer();
        await player.LoadGames();
        await player.LoadFriends();
        return player.ExcludeJson(jsonParams);
    }
   [HttpGet]
   [Route("demo")]
   public List<Player> Demo()
   {
      var lol = new Player("gameplayer55055");
       lol.LoadPlayer().Wait();
       return new List<Player>(){lol, new Player("l5cker")};
   }
    [HttpPost("{id}")]
    public async Task<Player> GetPlayer(string id, JsonParams jsonParams)
    {
        Player player = Player.Get(id);
        await player.LoadPlayer();
        return player.ExcludeJson(jsonParams);
    }
    [HttpGet("{id}/games")]
    public async Task<List<Game>> GetGames(string id)
    {
        Player player = Player.Get(id);
        await player.LoadGames();
        return player.Games;
    }
    [HttpGet("{id}/friends")]
    public async Task<List<Player>> GetFriends(string id)
    {
        Player player = Player.Get(id);
        await player.LoadFriends();
        return player.Friends;
    }
    
}