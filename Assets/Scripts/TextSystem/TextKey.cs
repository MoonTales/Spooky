using System;

[Serializable]

// The CSV Mapper Key!
public struct TextKey
{
    public string place;
    public string id;

    public bool IsValid => !string.IsNullOrWhiteSpace(place) && !string.IsNullOrWhiteSpace(id);
    public override string ToString() => $"{place}.{id}";

    public static TextKey Empty => new TextKey { place = "", id = "" };
}
