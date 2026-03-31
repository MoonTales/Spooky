using UnityEngine;
using System;

public class ShowIfAttribute : PropertyAttribute
{
    public string ConditionName;
    public ShowIfAttribute(string conditionName) => ConditionName = conditionName;
}
