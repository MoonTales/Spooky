using UnityEngine;
using System;

public class ShowIfEnumAttribute : PropertyAttribute
{
    public string EnumName;
    public object[] TargetValues;
    public bool Invert;

    // Constructor for "Show if matches"
    public ShowIfEnumAttribute(string enumName, params object[] targetValues)
    {
        this.EnumName = enumName;
        this.TargetValues = targetValues;
        this.Invert = false;
    }

    // Constructor with Inversion option
    public ShowIfEnumAttribute(bool invert, string enumName, params object[] targetValues)
    {
        this.Invert = invert;
        this.EnumName = enumName;
        this.TargetValues = targetValues;
    }
}
