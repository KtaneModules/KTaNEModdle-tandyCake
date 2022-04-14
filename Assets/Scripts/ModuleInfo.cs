using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
public struct ModuleInfo {

    private class ContributorsField
    { 
        public Dictionary<string, object> Contributors { get; set; } 
    }
    private class TPField
    {
        public Dictionary<string, object> TwitchPlays { get; set; }
    }

    private static Dictionary<string, Difficulty> diffLookup = new Dictionary<string, Difficulty>()
    {
        { "VeryEasy", Difficulty.Very_Easy },
        { "Easy", Difficulty.Easy },
        { "Medium", Difficulty.Medium },
        { "Hard", Difficulty.Hard },
        { "VeryHard", Difficulty.Very_Hard }
    };

    public ModuleInfo(Dictionary<string, object> json)
    {
        isUsable = true;
        this.json = json;

        //We only want solvable modules;
        if ((string)json["Type"] != "Regular")
        {
            isRegular = false;
            isUsable = false;
            name = null;
            symbol = null;
            contributors = null;
            firstContributor = null;
            defuserDifficulty = Difficulty.Very_Easy;
            expertDifficulty = Difficulty.Very_Easy;
            tpScore = -1;
            return;
        }
        else isRegular = true;

        name = (string)json["Name"];
        
        //Check if the symbol isn't filled in.
        if (json.ContainsKey("Symbol"))
        {
            symbol = (string)json["Symbol"];
            if (symbol.Length > 4)
                isUsable = false;
        }
        else
        {
            isUsable = false;
            symbol = null;
        }

        
        Debug.Log(name);

        //Check if mod has TP support and get its score.
        if (json.ContainsKey("TwitchPlays"))
        {
            JObject tp = (JObject)json["TwitchPlays"];
            tpScore = tp["Score"].ToObject<int>();
        }
        else
        {
            isUsable = false;
            tpScore = -1;
        }
        
        //Use the first author.
        contributors = ((string)json["Author"]).Split(new[] { ", " }, System.StringSplitOptions.RemoveEmptyEntries);
        firstContributor = contributors[0];
        Debug.Log(contributors.Join(" | "));
        

        defuserDifficulty = diffLookup[(string)json["DefuserDifficulty"]];
        expertDifficulty = diffLookup[(string)json["ExpertDifficulty"]];
    }

    public Dictionary<string, object> json { get; private set; }

    public string name { get; private set; }
    public string symbol { get; private set; }
    public string[] contributors { get; private set; }
    public string firstContributor { get; private set; }
    public Difficulty defuserDifficulty { get; private set; }
    public Difficulty expertDifficulty { get; private set; }
    public int tpScore { get; private set; }

    public bool isUsable { get; private set; }
    public bool isRegular { get; private set; }
}
