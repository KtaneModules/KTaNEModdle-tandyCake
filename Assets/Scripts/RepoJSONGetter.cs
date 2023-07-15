using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System.Linq;

public class RepoJSONGetter : MonoBehaviour {


	//Stores whether or not the getter was able to retrieve the json.
	private bool _success = false;
	public bool success { get { return _success; } } 

	//Stores the URL of the JSON that is grabbed.
	private string url;
	//Stores the module ID of the module which is hosting the getter.
	//Used for logging purposes.
	private int id;
	//Stores all of the ModuleInfos taken from the repo JSON.
	private List<ModuleInfo> _modules = null;
	//Only stores the ModuleInfos which have their isUsable property true and are not duplicate symbols.
	private List<ModuleInfo> _usableModules = null;
	public List<ModuleInfo> modules { get { return _modules ?? null; } }
	public List<ModuleInfo> usableModules { get { return _usableModules ?? null; } }
	//Stores all of the periodic table symbols of the modules on the repo.
    //If this HashSet already contains a symbol, only the first instance of it is added to _usableModules and can be used.
	private HashSet<string> symbols = new HashSet<string>();

	//Effectively serves as a constructor, since MonoBehaviours can only be instantiated with Instantiate or AddComponent.
	public void Set(string url, int id)
    {
		this.url = url;
		this.id = id;
	}
	public void Get()
    {
		StartCoroutine(GetFromRepo());
    }
	IEnumerator GetFromRepo()
    {
		//Stores the raw text of the grabbed json.
		string raw;
		UnityWebRequest request = UnityWebRequest.Get(url);
		//Waits until the web request returns the JSON file.
		yield return request.SendWebRequest();
		//If an error occurs, we need to default to the hardcoded file.
		if (request.error != null)
		{
			Log("Connection error! Resorting to hardcoded /json/raw dated July 15, 2023");
			Debug.LogFormat("<Moddle #{0}> Connection error: {1}", id, request.error);
			raw = KtaneWordleScript.getJson.text;
			_success = false;
        }
        else
        {
			Debug.Log("Gotten info!");
			raw = request.downloadHandler.text;
			_success = true;
        }
		//Turns the raw JSON into an instance of the container class, which contains a List of Dictionaries.
		List<KtaneModule> modData = RepoJSONParser.ParseRaw(raw);
		_modules = new List<ModuleInfo>(modData.Count);
		_usableModules = new List<ModuleInfo>();

		//Iterates for each module in the JSON.
		foreach (var dict in modData)
        {
			ModuleInfo info = new ModuleInfo(dict);
			if (info.isRegular)
            {
				_modules.Add(info);
				if (info.isUsable)
					_usableModules.Add(info);
            }
        }

		//Filter out duplicate symbols.
		foreach (ModuleInfo info in _usableModules.ToArray())
        {
			if (!symbols.Add(info.symbol))
            {
				Log("Duplicate symbol {0} on modules {1}. Please notify the repo maintainers about this.", info.symbol, 
					_usableModules.Where(x => x.symbol == info.symbol).Select(x => x.name).Join(" and "));
				_usableModules.Remove(info);
            }
        }
		

    }

	//Logs a message accepted by the LFA.
	//Uses the moduleID of the Moddle instance that is hosting this.
	void Log(string message, params object[] args)
	{
		Debug.LogFormat("[Moddle #{0}] {1}", id, string.Format(message, args));
	}
}
