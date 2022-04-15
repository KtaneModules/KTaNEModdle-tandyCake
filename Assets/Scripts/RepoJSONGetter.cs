using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using System.Linq;

public class RepoJSONGetter : MonoBehaviour {

	private class ktaneData
	{
		public List<Dictionary<string, object>> KtaneModules { get; set; }
	}

	private bool _success = false;
	public bool success { get { return _success; } } 
	private string url;
	private int id;
	private List<ModuleInfo> _modules = null;
	private List<ModuleInfo> _usableModules = null;
	public List<ModuleInfo> modules { get { return _modules ?? null; } }
	public List<ModuleInfo> usableModules { get { return _usableModules ?? null; } }
	private HashSet<string> symbols = new HashSet<string>();
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
		string raw;
		WWW request = new WWW(url);
		yield return request;
		if (request.error != null)
        {
			Log("Connection error! Resorting to hardcoded /json/raw dated 04-15-2022");
			raw = KtaneWordleScript.getJson.text;
			_success = false;
        }
        else
        {
			Debug.Log("Gotten info!");
			raw = request.text;
			_success = true;
        }
		ktaneData deserial = JsonConvert.DeserializeObject<ktaneData>(raw);
		_modules = new List<ModuleInfo>(deserial.KtaneModules.Count);
		_usableModules = new List<ModuleInfo>();

		foreach (var dict in deserial.KtaneModules)
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
	void Log(string message, params object[] args)
	{
		Debug.LogFormat("[Moddle #{0}] {1}", id, string.Format(message, args));
	}
}
