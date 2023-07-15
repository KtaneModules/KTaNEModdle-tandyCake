using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;
using KModkit;

public class KtaneWordleScript : MonoBehaviour {

    private const int NUMBER_OF_GUESSES = 8;
    private const string REPO_URL = "https://ktane.timwi.de/json/raw";

    //Tries and receive the JSON from the repo.
    private static RepoJSONGetter getter;
    private static bool doneLoadingMods;
    //Stores the modules which can be used for the module lookup.
    private static List<ModuleInfo> modulesFiltered;
    //Serves as a lookup of symbol -> module.
    //Will have no duplicate keys because of the duplicate symbols checker in RepoJSONGetter.
    private static Dictionary<string, ModuleInfo> modLookup;

    public KMAudio Audio;
    public KMBombModule Module;
    public KMSelectable Selectable;

    private static readonly Dictionary<IconType, Color> colors = new Dictionary<IconType, Color>()
    {
       { IconType.Correct, new Color32(106,170,100,255) }, //Green
       { IconType.Incorrect, new Color32(120,124,126,255) }, //Gray
       { IconType.WrongPos, new Color32(201,180,88,255) }, //Yellow
       { IconType.Higher, new Color32(114,156,165,255) }, //Blue
       { IconType.Lower, new Color32(114,156,165,255) } //Blue
    };
    //Manually-stored /json/raw dated June 20, 2022.
    //Used if the module cannot connect to the repo.
    public TextAsset defaultJson;
    public static TextAsset getJson { get { return FindObjectOfType<KtaneWordleScript>().defaultJson; } }

    public KMSelectable up, down;
    public MeshRenderer[] squares;
    public SpriteRenderer[] sprites;
    public Sprite[] icons;
    public TextMesh symbolDisp, remainingMovesDisp;
    public Text modNameDisp;

    private static int moduleIdCounter = 1;
    private int moduleId;
    private bool moduleSolved;
    private static bool playedIntro;

    private bool selected, acceptingInput;
    
    //Module resets if this hits 0.
    private int guessesRemaining = NUMBER_OF_GUESSES;
    private ModuleInfo solutionMod;

    private List<QueryInfo> queries = new List<QueryInfo>(NUMBER_OF_GUESSES);
    //Stores the index of the query that is being shown on the module.
    //Modified by the up and down arrows.
    private int viewedQueryIndex;

    void Awake () {
        moduleId = moduleIdCounter++;
        up.OnInteract += () => { ButtonPress(up, +1); return false; };
        down.OnInteract += () => { ButtonPress(down, -1); return false; };
        Selectable.OnFocus += () => selected = true;
        Selectable.OnDefocus += () => selected = false;
        //In the TestHarness, OnFocus is not called on selection.
        if (Application.isEditor)
            selected = true;
        //Plays sound when the module activates.
        Module.OnActivate += () =>
        {
            //Static variable used so only one instance of the mod plays the sound, preventing OWWWWWW OWWW OOOOOWWWWW.
            if (!playedIntro) 
                Audio.PlaySoundAtTransform("cashin", transform);
            playedIntro = true;
        };
    }
    //Resets the static variable when the bomb disappears.
    void OnDestroy() { playedIntro = false; }

    void ButtonPress(KMSelectable btn, int modifier)
    {
        btn.AddInteractionPunch(0.5f);
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, btn.transform);
        //If there are no queries, this would throw an IndexOutOfRangeException
        //Plus, there would be no queries to view.
        if (!acceptingInput || moduleSolved || queries.Count == 0)
            return;
        //Either +1 or -1
        viewedQueryIndex += modifier;

        //Keep viewedQueryIndex within 0 - the number of queries.
        if (viewedQueryIndex < 0)
            viewedQueryIndex = 0;
        if (viewedQueryIndex > queries.Count - 1)
            viewedQueryIndex = queries.Count - 1;

        //Set the squares to what was shown when you queried that module.
        for (int i = 0; i < 5; i++)
            SetSquareState(i, queries[viewedQueryIndex].results[i], false);
        //Set the mod name display to the module at the query selected.
        modNameDisp.text = queries[viewedQueryIndex].submission.name.ToUpperInvariant();
    }

    IEnumerator Start ()
    {
        yield return LoadModules();
        GeneratePuzzle();
        acceptingInput = true;
    }
    IEnumerator LoadModules()
    {
        //If the JSON hasn't been grabbed yet, make a new getter and run it.
        //getter is static, and so this if statement only runs once per instance.
        if (getter == null || (doneLoadingMods && !getter.success))
        {
            getter = gameObject.AddComponent<RepoJSONGetter>();
            getter.Set(REPO_URL, moduleId);
            getter.Get();
            //Loop stores the time it takes to get the JSON.
            float time = 0;
            while (getter.modules == null)
            {
                time += Time.deltaTime;
                yield return null;
            }
            Log("All modules gotten in {0} ms.", time * 1000);
            modNameDisp.text = "";

            modulesFiltered = getter.usableModules;
            modLookup = modulesFiltered.ToDictionary(mod => mod.symbol);
            doneLoadingMods = true;
        }
        while (!doneLoadingMods)
            yield return null;
    }
    void GeneratePuzzle()
    {
        //Sets the solution mod to a random mod which the getter deems valid.
        guessesRemaining = NUMBER_OF_GUESSES;
        solutionMod = modulesFiltered.PickRandom();
        Log("Solution module: {0}.", solutionMod);

        //Clears out the queries made if resetting.
        queries.Clear();
        modNameDisp.text = "";
        remainingMovesDisp.text = guessesRemaining.ToString();
    }

    void Update ()
    {
        //Only run the keyboard checker if we're actually focusing on the module.
        //Also stop running once the module is solved because idk this seems like it'd be at least a little costly.
        if (selected && acceptingInput && !moduleSolved)
        {
            //Check every letter for being depressed.
            for (int i = 0; i < 26; i++)
                if (Input.GetKeyDown(KeyCode.A + i))
                    AddCharToSymbol((char)('A' + i));
            //Check every digit for being depressed.
            for (int i = 0; i < 10; i++)
                if (Input.GetKeyDown(KeyCode.Alpha0 + i) || Input.GetKeyDown(KeyCode.Keypad0 + i))
                    AddCharToSymbol((char)('0' + i));
            //If backspace is pressed and there's text to shorten, nip off the last character of the symbol display.
            if (Input.GetKeyDown(KeyCode.Backspace) && symbolDisp.text.Length != 0)
                symbolDisp.text = symbolDisp.text.Substring(0, symbolDisp.text.Length - 1);
            if (Input.GetKeyDown(KeyCode.Return))
                Submit();
        }
    }
    void AddCharToSymbol(char ch)
    {
        if (symbolDisp.text.Length == 4)
            return;
        //If it's the first letter, add it uppercase, otherwise add it lowercase.
        symbolDisp.text += symbolDisp.text.Length == 0 ? char.ToUpper(ch) : char.ToLower(ch);
    }
    void Submit()
    {
        //Cache the display so we can clear it.
        string submitted = symbolDisp.text;
        symbolDisp.text = "";
        if (submitted.Length == 0)
            return;
        //If the p-table symbol doesn't exist in the JSON.
        if (!modLookup.ContainsKey(submitted.ToUpperInvariant()))
        {
            //If we have the latest JSON from the repo, strike the defuser.
            if (getter.success)
            {
                Log("Attempted to query: {0}, which is not an available symbol. Strike.", submitted);
                Module.HandleStrike();
            }
            //Otherwise there's a likelihood the defuser entered a module newer than the hardcoded JSON.
            //We shouldn't strike them for this.
            else
            {
                Log("Attempted to query: {0}, which is not an available symbol. No strike given because the getter failed to connect.", submitted);
                modNameDisp.text = "ENTER A MOD PRIOR TO 07/15/2023";   
            }
        }
        //else guess the mod that you submitted.    
        else
        {
            ModuleInfo submittedInfo = modLookup[submitted.ToUpperInvariant()];
            StartCoroutine(Guess(submittedInfo));
        }

    }
    IEnumerator Guess(ModuleInfo sub)
    {
        //Update what's on the module.
        acceptingInput = false;
        modNameDisp.text = sub.name.ToUpperInvariant();
        guessesRemaining--;
        remainingMovesDisp.text = guessesRemaining.ToString();
        Log("Queried {0}.", sub);

        //GetGuessResult returns pairs whose b component is an IconType
        queries.Add(new QueryInfo(sub, GetGuessResult(sub).Select(x => x.b).ToArray()));
        viewedQueryIndex = queries.Count - 1;

        //Pair<InfoType, IconType> -> a pair of which square to change and what color/symbol to set it to.
        foreach (var pair in GetGuessResult(sub))
            yield return FlipSquare((int)pair.a, pair.b, false);
        if (sub.name == solutionMod.name)
            Solve();
        //If our last guess didn't get it, reset.
        else if (guessesRemaining == 0)
        {
            modNameDisp.text = "THE CORRECT ANSWER WAS:\n" + solutionMod.name.ToUpperInvariant();
            LogInputs();
            //Set the squares back to blank.
            for (int i = 0; i < 5; i++)
                //We want the squares to have no X on them when we reset.
                yield return FlipSquare(i, IconType.Incorrect, true);
            GeneratePuzzle();
        }
        acceptingInput = true;
    }

    void Solve()
    {
        moduleSolved = true;
        Audio.PlaySoundAtTransform("cashout", transform);
        Module.HandlePass();
        LogInputs();
    }

    //Logs the queries in the style of Wordle's share function.
    void LogInputs()
    {
        Log("Moddle {0} {1}/{2}", moduleId, guessesRemaining == 0 ? "X" : (NUMBER_OF_GUESSES - guessesRemaining).ToString(), NUMBER_OF_GUESSES);
        Log(" ");
        foreach (QueryInfo query in queries)
            Log(query.ToString());
    }

    //Gets the info on what to say for each guess.
    IEnumerable<Pair<InfoType, IconType>> GetGuessResult(ModuleInfo sub)
    {
        //If the first contributor is the solution's first contributor, return green.
        if (sub.firstContributor == solutionMod.firstContributor)
            yield return new Pair<InfoType, IconType>(InfoType.Author, IconType.Correct);
        //If we have any shared contributors, return yellow.
        else if (sub.contributors.Any(subCon => solutionMod.contributors.Contains(subCon)))
            yield return new Pair<InfoType, IconType>(InfoType.Author, IconType.WrongPos);
        //Otherwise return gray.
        else
            yield return new Pair<InfoType, IconType>(InfoType.Author, IconType.Incorrect);

        //Compares the remaining properties and gets either a green, a higher, or a lower.
        yield return new Pair<InfoType, IconType>(InfoType.ReleaseYear, CompareProperties(DaysSince0(solutionMod.date), DaysSince0(sub.date)));
        yield return new Pair<InfoType, IconType>(InfoType.DefuserDiff, CompareProperties((double)solutionMod.defuserDifficulty, (double)sub.defuserDifficulty));
        yield return new Pair<InfoType, IconType>(InfoType.ExpertDiff, CompareProperties((double)solutionMod.expertDifficulty, (double)sub.expertDifficulty));
        yield return new Pair<InfoType, IconType>(InfoType.TPScore, CompareProperties(solutionMod.tpScore, sub.tpScore));
    }
    //Converts a DateTime to an integer that can be compared as a double.
    int DaysSince0(DateTime date)
    {
        return 365 * date.Year + date.DayOfYear;
    }
    //Returns a check/higher/lower based on a given property of the solution mod and the submitted mod.
    //Double type used instead of int because TP scores can be decimal.
    IconType CompareProperties(double sol, double sub)
    {
        if (sol == sub)
            return IconType.Correct;
        if (sol > sub)
            return IconType.Higher;
        if (sol < sub)
            return IconType.Lower;
        return IconType.Incorrect;
    }
    //Sets the square in a given position to the right color and symbol for that IconType.
    //If clearTile is true, the symbol will not be displayed.
    //Works by squishing the square until it is just a line, changing the color and symbol, and then stretching it back to a square.
    IEnumerator FlipSquare(int pos, IconType state, bool clearTile)
    {
        //Stores if the square has changed its color yet.
        bool changedYet = false;
        float delta = 0;
        while (delta < 1)
        {
            //Animation occurs over 0.3 seconds.
            delta += Time.deltaTime / (1f/3);
            //Change the square state once we've passed half the animation.
            if (!changedYet && delta >= 0.5)
            {
                changedYet = true;
                SetSquareState(pos, state, clearTile);
            }
            yield return null;
            //Returns 2*d if we're less than halfway through, and 2*(1-d) if we're past halfway.
            /*To visualize this function, see the below graph.
                 1^    *(0.5, 1)
                  |   * *
                  |  *   *
              f(d)| *     *
                  |*       *
                 0+---------->
                  0   d     1
             */
            float lerp = delta < 0.5 ? 2 * delta : 2 * (1f - delta);
            //Use an easing function to add a little curve to the animation.
            squares[pos].transform.localScale = new Vector3(0.03f, Easing.InSine(lerp, 0.03f, 0, 1), 1);
        }
    }
    //Sets the square in the given position to the correct color and symbol for the IconType supplied.
    //clearTile sets the symbol to null, and is used when resetting (the same color is used as the incorrect answer as the blank)
    void SetSquareState(int pos, IconType state, bool clearTile)
    {
        squares[pos].material.color = colors[state];
        sprites[pos].sprite = clearTile ? null : icons[(int)state];
    }

    //Sends a log message parsable by the LFA and formatted with string.Format.
    void Log(string message, params object[] args)
    {
        Debug.LogFormat("[Moddle #{0}] {1}", moduleId, string.Format(message, args));
    }

    #pragma warning disable 414
    private readonly string TwitchHelpMessage = @"Use [!{0} enter Mdl] to submit that word into the module. Use [!{0} up/down] to press that button; specify a number after to press it that many times.";
    #pragma warning restore 414

    IEnumerator ProcessTwitchCommand (string command)
    {
        //Gets rid of the need for \s* at the beginning and end of the regexes and the need for RegexOptions
        command = command.Trim().ToUpperInvariant();
        Match m1 = Regex.Match(command, @"^(UP|DOWN)(?:\s+|$)([1-9]?)$");
        Match m2 = Regex.Match(command, @"^(?:ENTER\s+)?([A-Z0-9]{1,4})$");
        //Up or down command.
        if (m1.Success)
        {
            yield return null;
            yield return new WaitUntil(() => acceptingInput);
            //If we don't supply a number of presses, default to once.
            int timesPress = m1.Groups[2].Length > 0 ? int.Parse(m1.Groups[2].Value) : 1;
            for (int i = 0; i < timesPress; i++)
            {
                (m1.Groups[1].Value == "UP" ? up : down).OnInteract();
                yield return new WaitForSeconds(0.2f);
            }
        }
        //Enter module command.
        else if (m2.Success)
        {
            yield return null;
            yield return new WaitUntil(() => acceptingInput);
            //Mash backspace a bunch but not really.
            symbolDisp.text = "";
            //For each character, run the same function as pressing that key.
            foreach (char ch in m2.Groups[1].Value)
            {
                AddCharToSymbol(ch);
                yield return new WaitForSeconds(0.15f);
            }
            Submit();
        }
    }
    //Simulates a TP command with the answer.
    IEnumerator TwitchHandleForcedSolve ()
    {
        yield return ProcessTwitchCommand(solutionMod.symbol);
    }
}