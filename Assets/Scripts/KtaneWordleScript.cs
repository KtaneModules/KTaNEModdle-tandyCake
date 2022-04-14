using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;
using KModkit;

public class KtaneWordleScript : MonoBehaviour {

    private const string REPO_URL = "https://ktane.timwi.de/json/raw";
    public KMBombInfo Bomb;
    public KMAudio Audio;
    public KMBombModule Module;

    public KMSelectable up, down;
    public MeshRenderer[] squares;
    public SpriteRenderer[] sprites;
    public Sprite[] icons;
    public TextMesh symbolDisp, remainingMovesDisp;
    public Text modNameDisp;

    static int moduleIdCounter = 1;
    int moduleId;
    private bool moduleSolved;
    private bool active = false;

    private static bool loadedModules = false;
    private static ModuleInfo[] allModules = null;
    private static ModuleInfo[] modulesFiltered = null;
    

    void Awake () {
        moduleId = moduleIdCounter++;
        /*
        foreach (KMSelectable button in Buttons) 
            button.OnInteract += delegate () { ButtonPress(button); return false; };
        */
        
        //Button.OnInteract += delegate () { ButtonPress(); return false; };

    }

    IEnumerator Start ()
    {
        if (!loadedModules)
        {
            loadedModules = true;
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
            active = true;
        }
    }

    void Update ()
    {
        if (!active)
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
