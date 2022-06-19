using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class QueryInfo {

    //Contains the corresponding character to display on the square for each type of response. 
    private static Dictionary<IconType, string> sharingIcons = new Dictionary<IconType, string>()
    {
        { IconType.Correct, "🟩" },
        { IconType.Incorrect, "⬜" },
        { IconType.WrongPos, "🟨" },
        { IconType.Higher, "🔼" },
        { IconType.Lower, "🔽" }
    };
    //Stores the module that was entered to get this result
    public ModuleInfo submission { get; private set; }
    //Stores the info that's displayed on the squares.
    public IconType[] results { get; private set; }
    public QueryInfo(ModuleInfo submission, IconType[] results)
    {
        this.submission = submission;
        this.results = results;
    }
    public override string ToString()
    {
        Debug.Log(results.Length);
        return results.Select(icon => sharingIcons[icon]).Join("") + " ||" + submission.symbol[0] + submission.symbol.Skip(1).Join("").ToLowerInvariant() + "||";
    }
}
