using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct ModuleInfo {
    public ModuleInfo(string name, string symbol, string[] contributors, Difficulty defuserDifficulty, Difficulty expertDifficult, int tpScore)
    {
        this.name = name;
        this.symbol = symbol;
        this.contributors = contributors;
        this.defuserDifficulty = defuserDifficulty;
        this.expertDifficult = expertDifficult;
        this.tpScore = tpScore;
    }

    public string name { get; private set; }
    public string symbol { get; private set; }
    public string[] contributors { get; private set; }
    public Difficulty defuserDifficulty { get; private set; }
    public Difficulty expertDifficult { get; private set; }
    public int tpScore { get; private set; }
    public string firstContributor { get { return contributors[0]; } }


}
