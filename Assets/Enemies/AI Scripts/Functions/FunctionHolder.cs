using UnityEngine;

// This base class can contain shared properties or methods
public abstract class FunctionHolder : ScriptableObject
{
    public string functionName;
    public abstract void CallFunction();
}
