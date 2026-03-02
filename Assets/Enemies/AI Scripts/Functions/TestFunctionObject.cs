using UnityEngine;

[CreateAssetMenu(fileName = "TestFunctionObject", menuName = "Function/Test Function")]
public class TestFunctionObject : FunctionHolder
{
    public float testF;
    public bool testB;

    public override void CallFunction()
    {
        if (testB)
        {
            Debug.Log($"Works!! Also float is {testF}");
        }
    }
}
