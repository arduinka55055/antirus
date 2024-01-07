using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using antirus.Models;
using Microsoft.AspNetCore.Mvc;

namespace antirus.Controllers;

[ApiController]
[Route("[controller]")]
public class MainController : ControllerBase
{
    [HttpGet]
    public async Task<Player> Get(string id)
    {
        Player player = new(id);
        await player.LoadPlayer();
        await player.LoadGames();
        await player.LoadFriends();
        return player;
    }
}