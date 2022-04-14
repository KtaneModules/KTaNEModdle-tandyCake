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

    private static bool hasLoadedModules = false;
    private static List<ModuleInfo> modulesFiltered;
    private static Dictionary<string, ModuleInfo> modLookup;

    public KMBombInfo Bomb;
    public KMAudio Audio;
    public KMBombModule Module;

    private static readonly Dictionary<IconType, Color> colors = new Dictionary<IconType, Color>()
    {
       { IconType.Correct, new Color32(106,170,100,255) },
       { IconType.Incorrect, new Color32(120,124,126,255) } ,
       { IconType.WrongPos, new Color32(201,180,88,255) },
       { IconType.Higher, new Color32(114,156,165,255) },
       { IconType.Lower, new Color32(114,156,165,255) }
    };

    public KMSelectable up, down;
    public MeshRenderer[] squares;
    public SpriteRenderer[] sprites;
    public Sprite[] icons;
    public TextMesh symbolDisp, remainingMovesDisp;
    public Text modNameDisp;

    private static int moduleIdCounter = 1;
    private int moduleId;
    private bool moduleSolved;
    private bool acceptingInput = false;
    
    private int guessesRemaining;
    private ModuleInfo solutionMod;

    private List<QueryInfo> queries = new List<QueryInfo>(NUMBER_OF_GUESSES);
    private int viewedQueryIndex;

    void Awake () {
        moduleId = moduleIdCounter++;
        up.OnInteract += () => { ButtonPress(up, +1); return false; };
        down.OnInteract += () => { ButtonPress(down, -1); return false; };
    }

    void ButtonPress(KMSelectable btn, int modifier)
    {
        btn.AddInteractionPunch(0.5f);
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, btn.transform);
        if (!acceptingInput || moduleSolved || queries.Count == 0)
            return;
        viewedQueryIndex += modifier;
        if (viewedQueryIndex < 0)
            viewedQueryIndex = 0;
        if (viewedQueryIndex > queries.Count - 1)
            viewedQueryIndex = queries.Count - 1;
        for (int i = 0; i < 5; i++)
            SetSquareState(i, queries[viewedQueryIndex].results[i], false);
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
        if (!hasLoadedModules)
        {
            hasLoadedModules = true;
            RepoJSONGetter getter = gameObject.AddComponent<RepoJSONGetter>();
            getter.Set(REPO_URL, moduleId);
            getter.Get();
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
        }
    }
    void GeneratePuzzle()
    {
        guessesRemaining = NUMBER_OF_GUESSES;
        solutionMod = modulesFiltered.PickRandom();
        Log("Solution module: {0}.", solutionMod.name);

        queries.Clear();
        modNameDisp.text = "";
        remainingMovesDisp.text = guessesRemaining.ToString();
    }

    void Update ()
    {
        if (!acceptingInput || moduleSolved)
            return;
        for (int i = 0; i < 26; i++)
            if (Input.GetKeyDown(KeyCode.A + i))
                AddCharToSymbol((char)('A' + i));
        for (int i = 0; i < 10; i++)
            if (Input.GetKeyDown(KeyCode.Alpha0 + i) || Input.GetKeyDown(KeyCode.Keypad0 + i))
                AddCharToSymbol((char)('0' + i));
        if (Input.GetKeyDown(KeyCode.Backspace) && symbolDisp.text.Length != 0)
            symbolDisp.text = symbolDisp.text.Substring(0, symbolDisp.text.Length - 1);
        if (Input.GetKeyDown(KeyCode.Return))
            Submit();
    }
    void AddCharToSymbol(char ch)
    {
        if (symbolDisp.text.Length == 4)
            return;
        symbolDisp.text += symbolDisp.text.Length == 0 ? char.ToUpper(ch) : char.ToLower(ch);
    }
    void Submit()
    {
        string submitted = symbolDisp.text;
        symbolDisp.text = "";
        if (submitted.Length == 0)
            return;
        if (!modLookup.ContainsKey(submitted.ToUpperInvariant()))
        {
            Log("Attempted to query: {0}, which is not an available symbol. Strike.", submitted);
            Module.HandleStrike();
        }
        else
        {
            ModuleInfo submittedInfo = modLookup[submitted.ToUpperInvariant()];
            StartCoroutine(Guess(submittedInfo));
        }

    }
    IEnumerator Guess(ModuleInfo sub)
    {
        acceptingInput = false;
        modNameDisp.text = sub.name.ToUpperInvariant();
        guessesRemaining--;
        remainingMovesDisp.text = guessesRemaining.ToString();
        Log("Queried {0}.", sub);

        queries.Add(new QueryInfo(sub, GetGuessResult(sub).Select(x => x.b).ToArray()));
        viewedQueryIndex = queries.Count - 1;

        foreach (var pair in GetGuessResult(sub))
            yield return FlipSquare((int)pair.a, pair.b, false);
        if (sub.Equals(solutionMod))
            Solve();
        else if (guessesRemaining == 0)
        {
            modNameDisp.text = "THE CORRECT ANSWER WAS:\n" + solutionMod.name.ToUpperInvariant();
            LogInputs();
            for (int i = 0; i < 5; i++)
                yield return FlipSquare(i, IconType.Incorrect, true);
            GeneratePuzzle();
        }
        acceptingInput = true;
    }

    void Solve()
    {
        moduleSolved = true;
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.CorrectChime, transform);
        Module.HandlePass();
        LogInputs();
    }

    void LogInputs()
    {
        Log("Moddle {0} {1}/{2}", moduleId, guessesRemaining == 0 ? "X" : (NUMBER_OF_GUESSES - guessesRemaining).ToString(), NUMBER_OF_GUESSES);
        Log("");
        foreach (QueryInfo query in queries)
            Log(query.ToString());
    }

    IEnumerable<Pair<InfoType, IconType>> GetGuessResult(ModuleInfo sub)
    {
        if (sub.firstContributor == solutionMod.firstContributor)
            yield return new Pair<InfoType, IconType>(InfoType.Author, IconType.Correct);
        else if (sub.contributors.Any(subCon => solutionMod.contributors.Contains(subCon)))
            yield return new Pair<InfoType, IconType>(InfoType.Author, IconType.WrongPos);
        else
            yield return new Pair<InfoType, IconType>(InfoType.Author, IconType.Incorrect);

        yield return new Pair<InfoType, IconType>(InfoType.ReleaseYear, CompareProperties(DaysSince0(solutionMod.date), DaysSince0(sub.date)));
        yield return new Pair<InfoType, IconType>(InfoType.DefuserDiff, CompareProperties((double)solutionMod.defuserDifficulty, (double)sub.defuserDifficulty));
        yield return new Pair<InfoType, IconType>(InfoType.ExpertDiff, CompareProperties((double)solutionMod.expertDifficulty, (double)sub.expertDifficulty));
        yield return new Pair<InfoType, IconType>(InfoType.TPScore, CompareProperties(solutionMod.tpScore, sub.tpScore));
    }
    int DaysSince0(DateTime date)
    {
        return 365 * date.Year + date.DayOfYear;
    }
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

    IEnumerator FlipSquare(int pos, IconType state, bool clearTile)
    {
        bool changedYet = false;
        float delta = 0;
        while (delta < 1)
        {
            delta += Time.deltaTime / 0.33f;
            if (!changedYet && delta >= 0.5)
            {
                changedYet = true;
                SetSquareState(pos, state, clearTile);
            }
            yield return null;
            float lerp = delta < 0.5 ? 2 * delta : 2 * (1f - delta);
            squares[pos].transform.localScale = new Vector3(0.03f, Easing.InSine(lerp, 0.03f, 0, 1), 1);
        }
    }
    void SetSquareState(int pos, IconType state, bool clearTile)
    {
        squares[pos].material.color = colors[state];
        sprites[pos].sprite = clearTile ? null : icons[(int)state];
    }

    void Log(string message, params object[] args)
    {
        Debug.LogFormat("[Moddle #{0}] {1}", moduleId, string.Format(message, args));
    }

    #pragma warning disable 414
    private readonly string TwitchHelpMessage = @"Use <!{0} foobar> to do something.";
    #pragma warning restore 414

    IEnumerator ProcessTwitchCommand (string command)
    {
        command = command.Trim().ToUpperInvariant();
        List<string> parameters = command.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).ToList();
        yield return null;
    }

    IEnumerator TwitchHandleForcedSolve ()
    {
        yield return null;
    }
}
