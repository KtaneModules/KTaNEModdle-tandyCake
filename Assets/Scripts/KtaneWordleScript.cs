using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
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

    static int moduleIdCounter = 1;
    int moduleId;
    private bool moduleSolved;
    private bool active = false;

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
        RepoJSONGetter getter = new RepoJSONGetter(REPO_URL, moduleId);
        getter.Start();
        float time = 0;
        while (getter.modules == null)
        {
            time += Time.deltaTime;
            yield return null;
        }
        Log("All modules gotten in {0} ms.", time * 1000);
    }

    void Update ()
    {
        if (!active)
            return;

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
