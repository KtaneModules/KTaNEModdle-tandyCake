using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
public struct ModuleInfo {


    //Converts the difficulty as listed in the repo JSON to a value in the enum.
    private static Dictionary<string, Difficulty> diffLookup = new Dictionary<string, Difficulty>()
    {
        { "VeryEasy", Difficulty.VeryEasy },
        { "Easy", Difficulty.Easy },
        { "Medium", Difficulty.Medium },
        { "Hard", Difficulty.Hard },
        { "VeryHard", Difficulty.VeryHard }
    };

    //Converts a JSON entry as listed in ktane.timwi.de/json/raw to a usable member of the class.
    public ModuleInfo(KtaneModule module)
    {
        isUsable = true;

        name = module.Name;
        //We only want solvable modules; 
        //Also, there's about 9 modules that have "DisplayName" set instead of "Name", so ignore those.
        if (module.Type != "Regular" || module.Name == null)
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

        //Check if the symbol isn't filled in.
        if (module.Symbol != null)
        {
            symbol = module.Symbol.ToUpperInvariant();
            if (symbol.Length > 4 || symbol.Any(ch => !char.IsDigit(ch) && !char.IsLetter(ch)))
                isUsable = false;
        }
        else
        {
            isUsable = false;
            symbol = null;
        }



        //Check if mod has TP support and get its score.
        if (module.TwitchPlays != null)
            tpScore = module.TwitchPlays.Score;
        else
        {
            isUsable = false;
            tpScore = -1;
        }

        //Use the first author.
        string[] authorFieldContributors = module.Author.Split(new[] { ", " }, System.StringSplitOptions.RemoveEmptyEntries);
        firstContributor = authorFieldContributors[0];
        if (module.Contributors == null)
            contributors = authorFieldContributors;
        else contributors = module.Contributors.GetAllContributors().ToArray();

        //Take only the year of the publish date.
        date = DateTime.ParseExact(module.Published, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);

        defuserDifficulty = diffLookup[module.DefuserDifficulty];
        expertDifficulty = diffLookup[module.ExpertDifficulty];

        //Do not allow translations in the mod.
        if (module.TranslationOf != null)
            isUsable = false;
    }

    //Stores the display name of the module this represents.
    public string name { get; private set; }
    //Stores the periodic table symbol of this module, which is used as a lookup.
    public string symbol { get; private set; }

    //Stores the list of contributors to the module, under the "contributors" field.
    public string[] contributors { get; private set; }
    //Stores the first listed contributor in the "authors" field.
    //Note that this uses a different field and a different ordering from the string[] contributors.
    public string firstContributor { get; private set; }
    //Stores the date that the module was released.
    public DateTime date { get; private set; }
    //Stores the defuser and expert difficulty of the module as enum values.
    public Difficulty defuserDifficulty { get; private set; }
    public Difficulty expertDifficulty { get; private set; }
    //Stores the TP score of the module as listed in the JSON.
    //The JSON value is taken from the TP Scoring Sheet by the repository.
    //Scores for dynamically-scored modules (e.g. Forget Me Not) are taken as if there are 10 modules on the bomb.
    public double tpScore { get; private set; }

    //Stores if this module can be used by the module. Excludes the following types of modules:
    // * Needy modules, widgets, and holdables.
    // * Modules which have no set periodic table symbol.
    // * Modules whose periodic table symbol is longer than 4 characters.
    // * Modules whose periodic table symbol contains non-alphanumeric characters.
    // * Modules which are not on the TP Scoring Sheet and thus have no TP score.
    // * Modules which are translations of other modules.
    public bool isUsable { get; private set; }
    //Stores whether or not the module is a solvable module.
    public bool isRegular { get; private set; }

    public override string ToString()
    {
        return string.Format("{0} ({1}) {2} (d) {3} (e), by {4}, published on {5}",
            name,
            symbol[0] + symbol.Skip(1).Join("").ToLowerInvariant(), //Capitalizes only the first letter of the symbol.
            defuserDifficulty,
            expertDifficulty,
            contributors.Join(", "),
            date.ToString("yyyy-MM-dd")); //Date format used by the repo.
    }
}
