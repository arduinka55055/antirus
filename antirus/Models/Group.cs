using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using antirus.Util;

namespace antirus.Models;

/*
class Group:
    name:str
    members:int
    description:str
    def __repr__(self):
        return f"{self.name} ({self.members} members)"
    
    @property
    def is_russian(self) -> bool:
        ret = rateText(self.description)
        if ret:
            logs.append(f"знайшли русню в групі {self.name}")
        return ret

*/

[DebuggerDisplay("{" + nameof(ToString) + "()}")]
public class Group
{
    public string Name { get; set; } = "";
    public int Members { get; set; } = 0;
    public string Description { get; set; } = "";
    
    [JsonIgnore]
    public Player? Invoker { get; set; } = null;//backref to user in a group

    public override string ToString()
    {
        return $"{(IsRussian ? "RU:" : "")}{Name} ({Members} members)";
    }

    [JsonIgnore]
    public bool IsRussian => TextRater.RateText(Description) > 0;
}