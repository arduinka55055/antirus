using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Net.Http;
using System.Threading.Tasks;
using System;

namespace antirus.Util;

public class API{
    public static readonly string URI = "https://steamcommunity.com/id/";
    public static readonly string URI_ID = "https://steamcommunity.com/profiles/";
    public static readonly string STEAMKEY = Environment.GetEnvironmentVariable("STEAMKEY") ?? 
                                             throw new Exception("STEAMKEY not found, visit https://steamcommunity.com/dev/apikey to get one");
    public static readonly string FRIENDSURI = "https://api.steampowered.com/ISteamUser/GetFriendList/v0001/?key="+STEAMKEY+"&relationship=friend&steamid=";


    private static string GetNameID(string name){
        if(name.All(char.IsDigit)){
            return URI_ID+name;
        }
        else{
            return URI+name;
        }
    }

    //xml request
    public static async Task<string> loadPlayer(string name){
        return await CachedRequest.Get(GetNameID(name)+"?xml=1");
    }
    //xml request
    public static async Task<string> loadGames(string id){
        return await CachedRequest.Get(GetNameID(id)+"/games?xml=1");
    }

    //json request
    public static async Task<string> loadFriends(string id){
        return await CachedRequest.Get(FRIENDSURI+id);
    }


}