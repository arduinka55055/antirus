using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Text.Json;
using System.Xml;
using System.Xml.Linq;

using antirus.Util;
using Microsoft.Extensions.Caching.Memory;

namespace antirus.Models;


[DebuggerDisplay("{" + nameof(ToString) + "()}")]
public class Player(string name)
{
    public static readonly double profileWeight = 1.0;
    public static readonly double gameWeight = 0.8;
    public static readonly double groupWeight = 0.5;
    public static readonly double friendsWeight = 0.2;
    public static readonly double friendScore = 0;


    public string SteamId { get; set; } = "";
    public string? SteamId64 { get; set; }
    public string Name { get; set; } = name;
    public string Avatar { get; set; } = "";
    public string? Nationality { get; set; }
    public string? MemberSince { get; set; }
    public string? Summary { get; set; }
    public bool IsPrivate { get; set; } = false;
    public List<Game> Games { get; set; } = [];
    public List<Group> Groups { get; set; } = [];
    public List<Player> Friends { get; set; } = [];
    public Summary SummaryObj { get; set; } = new Summary();

    private bool _ProfileLoaded = false;
    private bool _GamesLoaded = false;
    private bool _FriendsLoaded = false;
    public bool isCached { get; set; } = false;

    private static ILogger<Player>? _logger = null;
    private static IMemoryCache? _cache = null;
    

    public static void Init(ILogger<Player>? logger=null, IMemoryCache? cache=null){
        _logger = logger;
        _cache = cache;
    }

    //USE THIS TO GET PLAYER, IT ENABLES CACHING
    public static Player Get(string id){
        //try to get name from cache
        if(_cache != null){
            if(_cache.TryGetValue<Player>(id, out var cachedPlayer)){
                if(cachedPlayer != null){
                    _logger?.LogDebug($"Found player {id} in cache");
                    return cachedPlayer;
                }
            }
        }
        //if not found, create new
        _logger?.LogInformation($"Creating new player {id}");
        Player p = new(id);
        p.SteamId64 = id;
        //save to cache
        p.isCached = true;
        _cache?.Set(id, p);
        return p;
    }
    private void Cache(){
        isCached = true;
        _cache?.Set(SteamId64 ?? Name, this);
    }

    public void OrderGames()
    {
        Games = [.. Games.OrderByDescending(x => x.IsRussian).ThenByDescending(x => x.PlaytimeHours)];
    }
    public async Task LoadPlayer(bool force = false)
    {
        if(_ProfileLoaded && !force)
            return;
        
        string req = await API.loadPlayer(SteamId64 ?? Name);
        var tree = XDocument.Parse(req);
        if(req.Contains("The specified profile could not be found.")){
            throw new System.Exception("Player not found");
        }
        //check rate limit
        if(tree.Root?.Element("error") != null){
            throw new System.Exception(tree.Root?.Element("error")?.Value ?? "Ratelimit, cool down our butts");
        }
        //fill all fields
        SteamId = tree?.Root?.Element("steamID")?.Value ?? "";
        SteamId64 = tree?.Root?.Element("steamID64")?.Value;
        Name = tree?.Root?.Element("steamID")?.Value ?? "";
        //replace html entities using built-in parser
        Name = System.Net.WebUtility.HtmlDecode(Name);

        Avatar = tree?.Root?.Element("avatarFull")?.Value ?? "";
        Nationality = tree?.Root?.Element("location")?.Value ?? "";
        MemberSince = tree?.Root?.Element("memberSince")?.Value ?? "";
        Summary = tree?.Root?.Element("summary")?.Value ?? "";
        IsPrivate = tree?.Root?.Element("privacyState")?.Value == "private";

        //retrieve groups from tree just beacuse its available as a bonus
        var grpnode = tree?.Root?.Element("groups");
        if(grpnode != null){
            foreach (var group in grpnode.Elements("group"))
            {
                Group g = new()
                {
                    Id = group.Element("groupID64")?.Value ?? "",
                    Name = group.Element("groupName")?.Value ?? "",
                    Members = int.Parse(group.Element("memberCount")?.Value ?? "0"),
                    Description = (group.Element("headline")?.Value ?? "") + "\r\n<br>" + (group.Element("summary")?.Value ?? ""),
                    Avatar = group.Element("avatarFull")?.Value ?? "",
                    Invoker = this
                };
                Groups.Add(g);
            }
        }
        _ProfileLoaded = true;
        Cache();
    }
    
    public async Task LoadFriends(bool force = false, JsonParams jsonParams = default)
    {
        if(_FriendsLoaded && !force)
            return;
        //scan thru friends, its JSON, not XML
        string f = await API.loadFriends(SteamId64 ?? Name);
        if(f.Length == 0){//http error or private profile
            return;
        }
        var fjson = JsonSerializer.Deserialize<JsonElement>(f);
        foreach (var friend in fjson.GetProperty("friendslist").GetProperty("friends").EnumerateArray())
        {
            string? id = friend.GetProperty("steamid").GetString();
            if(string.IsNullOrEmpty(id))
                continue;
            Player p = Player.Get(id);
            Friends.Add(p);
        }
        //get friends info in parallel
        var tasks = new List<Task>();
        foreach (var friend in Friends)
        {
            tasks.Add(Task.Run(async() => {
                try{
                    await friend.LoadPlayer();
                    if(jsonParams.Games)
                        await friend.LoadGames();
                }
                catch(System.Exception e){
                    Console.WriteLine($"Не вдалося завантажити профіль {friend.Name}");
                    Console.WriteLine(e);   
                }
            }
            ));
        }
        await Task.WhenAll(tasks);
        //sort friends by russian
        Friends = Friends.OrderByDescending(x => x.IsRussian).ToList();
        
        _FriendsLoaded = true;
    }
    public async Task LoadGames(bool force = false)
    {
        if(_GamesLoaded && !force)
            return;

        if(string.IsNullOrEmpty(SteamId64)){
            throw new System.Exception("SteamId64 is null, load player first");
        }
        //scan thru games
        string greq = await API.loadGames(SteamId64);
        var gtree = XDocument.Parse(greq);

        //check rate limit
        if(gtree.Root?.Element("error") != null){
            throw new System.Exception(gtree.Root?.Element("error")?.Value ?? "Ratelimit, cool down our butts");
        }

        if(gtree.Root == null){
            throw new System.Exception("Root node is null, check games of player "+SteamId64);
        }
        foreach (var game in gtree.Root.Elements("games").Elements("game"))
        {
            Game g = new()
            {
                Name = game.Element("name")?.Value ?? "",
                Appid = game.Element("appID")?.Value ?? "",
                Playtime = game.Element("hoursOnRecord")?.Value ?? "",
                Logo = game.Element("logo")?.Value ?? "",
            };
            Games.Add(g);
        }
        OrderGames();
        _GamesLoaded = true;
    }
    public async Task LoadGroupsId(){
        var tasks = new List<Task>();
        foreach (var group in Groups)
        {
            if(!group.isPartial)
                continue;
            tasks.Add(Task.Run(async() => {
                try{
                    var data = await API.loadGroup(group.Id);
                    var tree = XDocument.Parse(data);
                    group.Name = tree.Root?.Element("groupDetails")?.Element("groupName")?.Value ?? "";
                    group.Members = int.Parse(tree.Root?.Element("groupDetails")?.Element("memberCount")?.Value ?? "0");
                    group.Description = (tree.Root?.Element("groupDetails")?.Element("headline")?.Value ?? "") + "\r\n<br>" + (tree.Root?.Element("groupDetails")?.Element("summary")?.Value ?? "");
                    group.Avatar = tree.Root?.Element("groupDetails")?.Element("avatarFull")?.Value ?? "";
                }
                catch(System.Exception e){
                    Console.WriteLine($"Не вдалося завантажити групу {group.Name}");
                    Console.WriteLine(e);   
                }
            }
            ));    
        }
        await Task.WhenAll(tasks);
    }

    public double ProfileScore {
        get {
            double score = 0;
            List<string> logs = [];
            if(!string.IsNullOrEmpty(Nationality)){
                Nationality = Nationality.ToLower();
                //ukraine in nationality
                if(Nationality.Contains("ukraine")){
                    score -= 0.2;
                    logs.Add("профіль український");
                }
                //russian in nationality
                if(Nationality.Contains("russia")){
                    score += 10;
                    logs.Add("профіль російський");
                }
                //japanese in nationality
                if(Nationality.Contains("japan") || Nationality.Contains("tokyo")){
                    score += 0.05;
                    logs.Add("профіль анімешнік");
                }
            }
            SummaryObj.ProfileLogs = logs;
            return score * profileWeight;
        }
    }
    public double SummaryScore {
        get {
            double score = 0;
            List<string> logs = [];
            if(!string.IsNullOrEmpty(Summary)){
                int txtscore = TextRater.RateText(Summary+" "+Name); //а раптом імʼя кацапською
                if(txtscore > 0){
                    score += 0.5;
                    logs.Add($"профіль містить кацапський текст {txtscore} слів");
                }
                else if(txtscore < 0){
                    score -= 0.2;
                    logs.Add($"профіль містить український текст {txtscore} слів");
                }
            }
            SummaryObj.DescriptionLogs = logs;
            return score * profileWeight;
        }
    }
    public double GameScore {
        get {
            double score = 0;
            List<string> logs = [];
            foreach (var game in Games)
            {
                if(game.IsRussian){
                    score += 1;
                    logs.Add($"Кацапська гра {game.Name}");
                }
            }
            if(Games.Count != 0){
                score /= Games.Count;
                score *= gameWeight;
            }
            SummaryObj.GameLogs = logs;
            return score;
        }
    }

    public double GroupScore {
        get {
            double score = 0;
            List<string> logs = [];
            foreach (var group in Groups)
            {
                if(group.IsRussian){
                    score += 1;
                    logs.Add($"Кацапська групу {group.Name}");
                }
            }
            if(Groups.Count != 0){
                score /= Groups.Count;
                score *= groupWeight;
            }
            SummaryObj.GroupLogs = logs;
            return score;
        }
    }
    public double FriendScore {
        get {
            //return 0;//fix stackoverflow
            double score = 0;
            List<string> logs = [];
            foreach (var friend in Friends)
            {
                if(friend.SteamId64 == SteamId64)
                    continue;
                var rate = friend.IsRussianSafe();
                if(rate > 0){
                    score += rate;
                    if(rate > 0.5)
                        logs.Add($"Кацап {friend.SteamId64}");
                    else if(rate > 0)
                        logs.Add($"Малорос {friend.SteamId64}");
                }
            }
            if(Friends.Count != 0){
                score /= Friends.Count;
                score *= friendsWeight;
            }
            SummaryObj.FriendLogs = logs;
            return score;
        }
    }

    public double IsRussian {
        get{
            double score = ProfileScore + SummaryScore + GameScore + GroupScore;
            if(Friends.Count > 0){
                score += FriendScore;
            }
            SummaryObj.ScannedRate = score;
            return score;
        }
    }
    public double IsRussianSafe(){
        double score = ProfileScore + SummaryScore + GameScore + GroupScore;
        return score;
    }

    public override string ToString()
    {
        var coeff = IsRussian;
        return  $"{(coeff>0 ? "RU:" : "")}{Name}  {(string.IsNullOrEmpty(Nationality)?"":Nationality)} - {coeff} ({SteamId64})";
    }
    public string ToSteamGID(){
        return API.URI_ID+SteamId64;
    }

    public string Report {
        get {
            StringBuilder sb = new(1024);
            //use Markdown for formatting
            sb.Append($"**Звіт користувача {Name} ({SteamId64})**\r\n");
            if(IsRussian>0){
                sb.Append($"__**Профіль малорос**__\r\n");
            }
            sb.Append("```diff\r\n");
            foreach (var log in SummaryObj.Logs)
            {
                sb.Append($"- {log}\r\n");
            }
            sb.Append("```\r\n");
            
            return sb.ToString();
        }
    }
    

    public Player ExcludeJson(JsonParams jsonParams)
    {
        if(jsonParams.Full)
            return this;
        //make a copy of this object
        var ret = new Player(Name);
        //copy all fields except excluded
        foreach (PropertyInfo prop in typeof(Player).GetProperties())
        {
            if(!jsonParams.ExcludedFields.Contains(prop.Name)){
                //check if property can be set
                if(prop.CanWrite)
                    prop.SetValue(ret, prop.GetValue(this));
            }
        }
        return ret;
        
    }
}