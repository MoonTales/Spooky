using UnityEngine;
using System.Collections.Generic;

public class AttractorHiveMind : MonoBehaviour
{
	[System.Serializable]
	public class SingletonReference
	{
		public string singletonName;
		public string[] variableNames;
	}

	[Header("Singleton Variables")]
	public List<SingletonReference> singletons;

	[System.Serializable]
	public class ScriptReference
	{
		public GameObject container;
		public string scriptName;
		public string[] variableNames;
	}

	[Header("Script Variables")]
	public List<SingletonReference> scripts;

	[Header("Arbitrary AI Variables")]
	public List<SingletonReference> aiConditions;

	private void Start()
	{
		#region SingletonTime

		

		#endregion
	}


}
