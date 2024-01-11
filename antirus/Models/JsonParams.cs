using System.Dynamic;

namespace antirus.Models;

public struct JsonParams{
    public JsonParams()
    {
    }
    public JsonParams(bool games, bool friends, bool groups)
    {
        Games = games;
        Friends = friends;
        Groups = groups;
    }

    public bool Games { get; set; } = false;
    public bool Friends { get; set; } = false;
    public bool Groups { get; set; } = false;
    public bool Full {
        readonly get
        {
            return Games && Friends && Groups;
        }
        internal set{
            Games = value;
            Friends = value;
            Groups = value;
        }
    }
    
    public List<string> ExcludedFields {
        get{
            var ret = new List<string>();
            if(!Games)
                ret.Add("Games");
            if(!Friends)
                ret.Add("Friends");
            if(!Groups)
                ret.Add("Groups");
            return ret;
        }
    }
}