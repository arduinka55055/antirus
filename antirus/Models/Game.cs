using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using antirus.Util;

namespace antirus.Models;



[DebuggerDisplay("{" + nameof(ToString) + "()}")]
public class Game
{
    public static readonly string URI = "https://store.steampowered.com/app/";

    //lazy csv of russian games into list of pairs (appid, name)
    private static readonly Lazy<List<(string, string)>> _russianGames = new(() =>
    {
        var ret = new List<(string, string)>();
        var lines = System.IO.File.ReadAllLines( Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "../../../russiangames.csv"));
        foreach (var line in lines)
        {
            var parts = line.Split(',');
            ret.Add((parts[0], parts[1]));
        }
        return ret;
    });


    public string Name { get; set; } = "";
    public string Appid { get; set; } = "";
    public string Playtime { get; set; } = "";
    public string? Logo { get; set; }

    public override string ToString()
    {
        return $"{(IsRussian ? "RU:" : "")}{Name} ({Appid}) - {Playtime} hours";
    }

    [JsonIgnore]
    public float PlaytimeHours
    {
        get
        {
            if (Playtime == "")
            {
                return 0;
            }

            return float.Parse(Playtime.Replace(",", ""), System.Globalization.CultureInfo.InvariantCulture);
        }
    }
    
    public bool IsRussian {
      get{
        bool ret = _russianGames.Value.Any(x => x.Item1 == Appid);
        if (ret)
        {
            //Invoker?.Logs.Add($"знайшли русню в групі {Name}");
        }

        return ret;
      }
    }

}