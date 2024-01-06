using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text.Json;
using System.Xml;
using System.Xml.Linq;

using antirus.Util;

namespace antirus.Models;

/*
    def is_russian(self, count_friends:bool) -> float:
        
        #calculate probability of being russian
        profile_weight = 1.0
        game_weight = 0.8
        group_weight = 0.5
        friends_weight = 0.2
        
        friend_score = 0
        print("Перевіряємо профіль", self.name)
        profile_score = 0 
        #check nationality, if not ukraine then add 1
        if self.nationality != None:
            if "ukraine" in self.nationality.lower():
                profile_score -= 0.2
                print("профіль український")
                self.warns.append("профіль український")
            if "russia" in self.nationality.lower():
                profile_score += 10
                print("профіль російський")
                self.warns.append("профіль російський")
        profile_score *= profile_weight
        #check summary
        if self.summary != None:
            if rateText(self.summary):
                profile_score += 0.5
                print("профіль містить кацапський текст")
                self.warns.append("профіль містить кацапський текст")
            elif forgiveWord(self.summary):
                profile_score -= 0.2
                print("профіль містить український текст")
        
        
        if count_friends and len(self.friends) != 0:
            for friend in self.friends:
                rate = friend.is_russian(False) 
                if rate > 0:
                    friend_score += rate
                    print(f"Знайшли русню {friend.steamid}")
                    self.warns.append(f"Знайшли русню {friend.steamid}")
            #divide by total friends
            friend_score /= len(self.friends)
            print(f"відсоток кацапів у друзях: {friend_score}")
            self.warns.append(f"відсоток кацапів у друзях: {friend_score}")
            #multiply by weight
            friend_score *= friends_weight
        
        game_score = 0
        for game in self.games:
            if game.is_russian:
                game_score += 1
                print(f"Знайшли кацапську гру {game.name}")
                self.warns.append(f"Знайшли кацапську гру {game.name}")
                
        if(len(self.games) != 0):       
            game_score /= len(self.games)
            game_score *= game_weight
        
        group_score = 0
        for group in self.groups:
            if group.is_russian:
                group_score += 1
                print(f"Знайшли кацапську групу {group.name}")
                self.warns.append(f"Знайшли кацапську групу {group.name}")
        if(len(self.groups) != 0):
            group_score /= len(self.groups)
            group_score *= group_weight
            
        self.scannedRate = profile_score + game_score + group_score + friend_score / 4
        self.data = str(profile_score) + " " + str(game_score) + " " + str(group_score) + " " + str(friend_score)
        return self.scannedRate
        */
[DebuggerDisplay("{" + nameof(ToString) + "()}")]
public class Player(string name)
{
    public static readonly double profileWeight = 1.0;
    public static readonly double gameWeight = 0.8;
    public static readonly double groupWeight = 0.5;
    public static readonly double friendsWeight = 0.2;
    public static readonly double friendScore = 0;


    public string SteamId { get; set; } = "";
    public string SteamId64 { get; set; } = "";
    public string Name { get; set; } = name;
    public string Avatar { get; set; } = "";
    public string? Nationality { get; set; }
    public string? MemberSince { get; set; }
    public string? Summary { get; set; }
    public bool IsPrivate { get; set; } = false;
    public List<Game> Games { get; set; } = new List<Game>();
    public List<Group> Groups { get; set; } = new List<Group>();
    public List<Player> Friends { get; set; } = new List<Player>();
    public Summary SummaryObj { get; set; } = new Summary();

    public void OrderGames()
    {
        Games = [.. Games.OrderByDescending(x => x.IsRussian).ThenByDescending(x => x.PlaytimeHours)];
    }
    public async Task LoadPlayer(){
        string req = await API.loadPlayer(Name);
        var tree = XDocument.Parse(req);
        if(req.Contains("The specified profile could not be found.")){
            throw new System.Exception("Player not found");
        }
        //fill all fields
        SteamId = tree?.Root?.Element("steamID")?.Value ?? "";
        SteamId64 = tree?.Root?.Element("steamID64")?.Value ?? "";
        Name = tree?.Root?.Element("steamID")?.Value ?? "";
        //replace html entities using built-in parser
        Name = System.Net.WebUtility.HtmlDecode(Name);

        Avatar = tree?.Root?.Element("avatarIcon")?.Value ?? "";
        Nationality = tree?.Root?.Element("location")?.Value ?? "";
        MemberSince = tree?.Root?.Element("memberSince")?.Value ?? "";
        Summary = tree?.Root?.Element("summary")?.Value ?? "";
        IsPrivate = tree?.Root?.Element("privacyState")?.Value == "private";

        //retrieve groups from tree
        var grpnode = tree?.Root?.Element("groups");
        if(grpnode != null){
            foreach (var group in grpnode.Elements("group"))
            {
                Group g = new()
                {
                    Name = group.Element("groupName")?.Value ?? "",
                    Members = int.Parse(group.Element("memberCount")?.Value ?? "0"),
                    Description = group.Element("headline")?.Value ?? "",
                    Invoker = this
                };
                Groups.Add(g);
            }
        }

        //load games
        //LoadGames();
    }
    
    public async Task LoadFriends()
    {
        //scan thru friends, its JSON, not XML
        string f = await API.loadFriends(SteamId64);
        if(f.Length == 0){//http error or private profile
            return;
        }
        var fjson = JsonSerializer.Deserialize<JsonElement>(f);
        foreach (var friend in fjson.GetProperty("friendslist").GetProperty("friends").EnumerateArray())
        {
            Player p = new(friend.GetProperty("steamid").GetString() ?? "");
            Friends.Add(p);
        }
        //get friends info in parallel
        var tasks = new List<Task>();
        foreach (var friend in Friends)
        {
            tasks.Add(Task.Run(async() => {
                try{
                    await friend.LoadPlayer();
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
        Friends = Friends.OrderByDescending(x => x.IsRussian(false)).ToList();
    }
    public async Task LoadGames()
    {
        //scan thru games
        string greq = await API.loadGames(SteamId64);
        var gtree = XDocument.Parse(greq);
        if(gtree.Root == null){
            return;
        }
        foreach (var game in gtree.Root.Elements("games").Elements("game"))
        {
            Game g = new()
            {
                Name = game.Element("name")?.Value ?? "",
                Appid = game.Element("appID")?.Value ?? "",
                Playtime = game.Element("hoursOnRecord")?.Value ?? "",
            };
            Games.Add(g);
        }
        OrderGames();
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
                    logs.Add($"Знайшли кацапську гру {game.Name}");
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
                    logs.Add($"Знайшли кацапську групу {group.Name}");
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
            double score = 0;
            List<string> logs = [];
            foreach (var friend in Friends)
            {
                double rate = friend.IsRussian(false);
                if(rate > 0){
                    score += rate;
                    logs.Add($"Знайшли русню {friend.SteamId}");
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

    public double IsRussian(bool scanFiends=false){
        System.Console.WriteLine($"Перевіряємо профіль {Name}");

        double score = ProfileScore + SummaryScore + GameScore + GroupScore;
        if(scanFiends){
            score += FriendScore;
        }
        SummaryObj.ScannedRate = score;
        return score;
    }

    public override string ToString()
    {
        var coeff = IsRussian(false);
        return  $"{(coeff>0 ? "RU:" : "")}{Name}  {(string.IsNullOrEmpty(Nationality)?"":Nationality)} - {coeff} ({SteamId64})";
    }
}