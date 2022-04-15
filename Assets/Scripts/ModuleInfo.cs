using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
public struct ModuleInfo {

    private static Dictionary<string, Difficulty> diffLookup = new Dictionary<string, Difficulty>()
    {
        { "VeryEasy", Difficulty.VeryEasy },
        { "Easy", Difficulty.Easy },
        { "Medium", Difficulty.Medium },
        { "Hard", Difficulty.Hard },
        { "VeryHard", Difficulty.VeryHard }
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
            date = DateTime.MinValue;
            defuserDifficulty = Difficulty.VeryEasy;
            expertDifficulty = Difficulty.VeryEasy;
            tpScore = -1;
            return;
        }
        else isRegular = true;

        name = (string)json["Name"];
        
        //Check if the symbol isn't filled in.
        if (json.ContainsKey("Symbol"))
        {
            symbol = ((string)json["Symbol"]).ToUpperInvariant();
            if (symbol.Length > 4 || symbol.Any(ch => !char.IsDigit(ch) && !char.IsLetter(ch)))
                isUsable = false;
        }
        else
        {
            isUsable = false;
            symbol = null;
        }

        

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

        //Take only the year of the publish date.
        date = DateTime.ParseExact((string)json["Published"], "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);

        defuserDifficulty = diffLookup[(string)json["DefuserDifficulty"]];
        expertDifficulty = diffLookup[(string)json["ExpertDifficulty"]];

        //Do not allow translations in the mod.
        if (json.ContainsKey("TranslationOf"))
            isUsable = false;
    }

    public Dictionary<string, object> json { get; private set; }

    public string name { get; private set; }
    public string symbol { get; private set; }

    public string[] contributors { get; private set; }
    public string firstContributor { get; private set; }
    public DateTime date { get; private set; }
    public Difficulty defuserDifficulty { get; private set; }
    public Difficulty expertDifficulty { get; private set; }
    public double tpScore { get; private set; }

    public bool isUsable { get; private set; }
    public bool isRegular { get; private set; }

    public override string ToString()
    {
        return string.Format("{0} ({1}) {2} (d) {3} (e), by {4}, published on {5}",
            name,
            symbol[0] + symbol.Skip(1).Join("").ToLowerInvariant(),
            defuserDifficulty,
            expertDifficulty,
            contributors.Join(", "),
            date.ToString("yyyy-MM-dd"));
    }
}
