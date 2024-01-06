
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using antirus.Util;

namespace antirus.Models;

/*
    Звіт про гравця який включає в себе резульати перевірки:
    опису
    груп
    ігор
    друзів

    має поле Result зі всіма результатами вище (у Visual Studio його можна побачити в дебагері)


    ці поля заповнюються сканером русні, при конфліктах перезаписується останніми даними
    uses Lock to ensure thread safety
*/

//use IEnumerable visualizer and show List<string> Logs 
[DebuggerDisplay("{" + nameof(ToString) + "()}")]
[DebuggerTypeProxy(typeof(SummaryDebugView))]
public class Summary{
    private List<string> _ProfileLogs { get; set; } = [];
    private List<string> _DescriptionLogs { get; set; } = [];
    private List<string> _GroupLogs { get; set; } = [];
    private List<string> _GameLogs { get; set; } = [];
    private List<string> _FriendLogs { get; set; } = [];
    //lock
    private object _lock = new object();
    public List<string> ProfileLogs{
        get{lock(_lock){return _ProfileLogs;}}
        set{lock(_lock){_ProfileLogs = value;}}
    }
    public List<string> DescriptionLogs{
        get{lock(_lock){return _DescriptionLogs;}}
        set{lock(_lock){_DescriptionLogs = value;}}
    }
    public List<string> GroupLogs{
        get{lock(_lock){return _GroupLogs;}}
        set{lock(_lock){_GroupLogs = value;}}
    }
    public List<string> GameLogs{
        get{lock(_lock){return _GameLogs;}}
        set{lock(_lock){_GameLogs = value;}}
    }
    public List<string> FriendLogs{
        get{lock(_lock){return _FriendLogs;}}
        set{lock(_lock){_FriendLogs = value;}}
    }
    public List<string> Logs => [.. ProfileLogs, .. DescriptionLogs, .. GroupLogs, .. GameLogs, .. FriendLogs];
    public double ScannedRate { get; set; } = 0;

    public override string ToString()
    {
        if(Logs.Count == 0){
            return "Немає до чого придертись";
        }
        return $" {ScannedRate} руснявості";
    }

    private class SummaryDebugView(Summary summary)
    {
        private Summary _summary = summary;

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public List<string> Logs => _summary.Logs;
    }
}