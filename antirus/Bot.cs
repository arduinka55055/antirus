using antirus.Models;
using antirus.Util;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Discord.Net;
using Discord.API;
using Discord.Rest;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Text.Json;
using System.Text;

namespace antirus.bot;

public class Bot {
    private DiscordSocketClient _client;
    private static readonly string[] messages = [
        "майнинг біткоїнтів 🪙",
        "грію процесор 🔥",
        "reforce rtx 9090ti 🎮",
        "псую тобі катку 🎮",
        "розстріл малоросів 🔫",
        "шукаю фейки 🕵️",
        "душу пітона 🐍",
        "це решітка C# 🧱",
        "[.....] | sill idealTree buildDeps",
        "deltree /y C:\\Windows\\System32",
        "розчехляємо проксі 🛠",
        "тут може бути ваша реклама 📺",
        "поцілуй мене за блискучий металевий зад 🤖"
    ];
    private Task SetGame() => _client.SetGameAsync(messages[new Random().Next(0, messages.Length)]);
    private Task Log(LogMessage msg)
    {
        Console.WriteLine(msg.ToString());
        return Task.CompletedTask;
    }
    public static void Launch() => new Bot().MainAsync().GetAwaiter().GetResult();
    public async Task EmbedLoadFriends(SocketMessageComponent interaction){
        //tell the user we're waiting
        await interaction.DeferLoadingAsync();
        //get the player steamcommunity url from message
        var url = interaction.Message.Embeds.First().Url;
        //get the player
        var player = Player.Get(url.Split("/").Last());
        //load groups
        await player.LoadGroupsId();
        //load friends
        await player.LoadFriends();
        Console.WriteLine(player.FriendScore);
        //update report in embed
        var embed = interaction.Message.Embeds.First().ToEmbedBuilder();
        embed.WithDescription(player.Report);
        //update the message
        await interaction.Message.ModifyAsync(msg => msg.Embed = embed.Build());
        //create new ComponentBuilder and fill it with buttons without first button
        var button = new ComponentBuilder();
        button.WithButton("Посилання 📜",customId: "loadLinks",ButtonStyle.Success);
        //add the buttons to the message
        await interaction.Message.ModifyAsync(msg => msg.Components = button.Build());
        await interaction.FollowupAsync("Завантаження завершено", ephemeral: true);
    }
    public async Task EmbedLoadLinks(SocketMessageComponent interaction){
        //tell the user we're waiting
        await interaction.DeferLoadingAsync();
        //get the player steamcommunity url from message
        var url = interaction.Message.Embeds.First().Url;
        //get the player
        var player = Player.Get(url.Split("/").Last());
        //lambda that returns emoji for RU or empty string
        static string emoji(bool p) => p ? "🐖" : "";
        // for each element return [name](steamcommunity link)
        StringBuilder sb = new();
        var linksGrp = player.Groups.Select(group => $"[{emoji(group.IsRussian)}{group.Name}]({group.ToSteamGID()})");
        var linksFriends = player.Friends.Select(friend => $"[{emoji(friend.IsRussian>0)}{friend.Name}]({friend.ToSteamGID()})");
        if(linksGrp.Any())
            sb.AppendLine("## Групи:\n"+string.Join(", ",linksGrp));
        if(linksFriends.Any())
            sb.AppendLine("## Друзі:\n"+string.Join(", ",linksFriends));
        sb.AppendLine($"### Посилання на ігри🎮: [Steam]({API.URI_ID+player.SteamId64}/games/?tab=all)");
        sb.AppendLine($"### Посилання на рецензії📜: [Steam]({API.URI_ID+player.SteamId64}/reviews/)");
        sb.AppendLine($"### Посилання на скріншоти📷: [Steam]({API.URI_ID+player.SteamId64}/screenshots/)");
        
        //create a new embed for the links
        var linksEmbed = new EmbedBuilder();
        linksEmbed.WithTitle("Посилання для перевірки 🔍");
        linksEmbed.WithDescription(sb.ToString());
        //respond with the message only to the user
        await interaction.FollowupAsync(embed: linksEmbed.Build(), ephemeral: true);
        Console.WriteLine("done");

    }

    public async Task MainAsync()
    {
        //set invariant culture for the whole app
        CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;
        
        _client = new DiscordSocketClient();

        _client.Log += Log;
        _client.Ready += Client_Ready;

        var token = Environment.GetEnvironmentVariable("DISCORDTOKEN");
        // var token = File.ReadAllText("token.txt");
        // var token = JsonConvert.DeserializeObject<AConfigurationClass>(File.ReadAllText("config.json")).Token;

        await _client.LoginAsync(TokenType.Bot, token);
        await _client.StartAsync();

        _client.MessageReceived += async (message) =>
        {
            await onReceivedMsg(message);
        };

        _client.ButtonExecuted += async (interaction) =>
        {
            Console.WriteLine(interaction.Data.CustomId);
            if (interaction.Data.CustomId == "loadFriends")
                await EmbedLoadFriends(interaction);
            else if (interaction.Data.CustomId == "loadLinks")
                await EmbedLoadLinks(interaction);
            else
                await interaction.RespondAsync("Гадки не маю що це таке");
        };

        
        _client.SlashCommandExecuted += async (interaction) =>
        {
           //reply with ping
              if (interaction.CommandName == "first-command")
              {
                //await onReceivedMsg(interaction);
              }
              await interaction.RespondAsync("pong!");
        };

        // Block this task until the program is closed.
        await Task.Delay(-1);
    }
    public async Task Client_Ready()
    {
        await SetGame();
        // Let's build a guild command! We're going to need a guild so lets just put that in a variable.

        // Next, lets create our slash command builder. This is like the embed builder but for slash commands.
        var guildCommand = new SlashCommandBuilder();

        // Note: Names have to be all lowercase and match the regular expression ^[\w-]{3,32}$
        guildCommand.WithName("first-command");

        // Descriptions can have a max length of 100.
        guildCommand.WithDescription("This is my first guild slash command!");

        // Let's do our global command
        var globalCommand = new SlashCommandBuilder();
        globalCommand.WithName("first-global-command");
        globalCommand.WithDescription("This is my first global slash command");

        try
        {
            // With global commands we don't need the guild.
            //await _client.CreateGlobalApplicationCommandAsync(globalCommand.Build());
            // Using the ready event is a simple implementation for the sake of the example. Suitable for testing and development.
            // For a production bot, it is recommended to only run the CreateGlobalApplicationCommandAsync() once for each command.
        }
        catch(HttpException exception)
        {
            Console.WriteLine(exception.Reason);
            // If our command was invalid, we should catch an ApplicationCommandException. This exception contains the path of the error as well as the error message. You can serialize the Error field in the exception to get a visual of where your error is.
            //var json = JsonSerializer.Serialize(exception.Reason);

            // You can send this error somewhere or just print it to the console, for this example we're just going to print it.
            //Console.WriteLine(json);
        }
    }
    private async Task onReceivedMsg(SocketMessage message)
    {
        if (message.Content.Contains("https://steamcommunity.com/"))
        {
            await _client.SetGameAsync("пошук малоросів 🔍");
            //erase last / if any
            var url = message.Content.TrimEnd('/');
            //extract last part of url, which is the steam id or vanity url
            url = url.Split("/").Last();
            var player = Player.Get(url);
            await player.LoadPlayer();
            await player.LoadGames();
            //print out the report embed
            EmbedBuilder embedBuilder = new();
            embedBuilder.WithTitle(player.Name);
            embedBuilder.WithDescription(player.Report);
            embedBuilder.WithUrl("https://steamcommunity.com/profiles/"+player.SteamId64);
            embedBuilder.WithColor(player.IsRussian>0.01 ? Color.Red : Color.Green);
            //make a button that invokes EmbedLoadLinks
            var button = new ComponentBuilder()
                .WithButton("Свинодрузі 🐖",customId: "loadFriends",ButtonStyle.Primary)
                .WithButton("Посилання 📜",customId: "loadLinks",ButtonStyle.Success);
                //.WithButton("Завантажити ігри 🎮",customId: "loadGames",ButtonStyle.Success);
                
            //add the button to the embe
            
            await message.Channel.SendMessageAsync(embed: embedBuilder.Build(),
                                                    components: button.Build());
            await SetGame();
        }
    }

}