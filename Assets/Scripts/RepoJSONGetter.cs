using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;

public class RepoJSONGetter : MonoBehaviour {

	private readonly string url;
	private readonly int id;
	private ModuleInfo[] _modules = null;
	public ModuleInfo[] modules { get { return _modules; } }

	public RepoJSONGetter(string url, int id)
    {
		this.url = url;
		this.id = id;
    }
	public void Start()
    {
		StartCoroutine(GetFromRepo());
    }
	IEnumerator GetFromRepo()
    {
		WWW request = new WWW(url);
		yield return request;
		if (request.error != null)
        {
			Log("Connection error!");
			//Set default mods here.
			yield break;
        }
    }
	void Log(string message, params object[] args)
	{
		Debug.LogFormat("[Moddle #{0}] {1}", id, string.Format(message, args));
	}
}
