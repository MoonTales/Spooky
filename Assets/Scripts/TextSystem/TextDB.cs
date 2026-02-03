using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

// The CSV Parser! It loads all CSVs and reads them, and drags their text kicking and screaming into a dictionary :D
public static class TextDB
{
    private static bool loaded;

    [Serializable]
    private struct TextRow
    {
        public string text;   // general / UI string
        public string name;   // inspectable name
        public string desc;   // inspectable description
        public string prompt; // interaction prompt

        public bool IsEmpty =>
            string.IsNullOrEmpty(text) &&
            string.IsNullOrEmpty(name) &&
            string.IsNullOrEmpty(desc) &&
            string.IsNullOrEmpty(prompt);
    }

    private static readonly Dictionary<string, TextRow> map = new Dictionary<string, TextRow>();

    public static void LoadAllIfNeeded()
    {
        if (loaded) return;
        loaded = true;

        map.Clear();

        TextAsset[] csvAssets = Resources.LoadAll<TextAsset>("Text");
        foreach (var asset in csvAssets)
        {
            if (asset == null) continue;
            TryLoadCsv(asset);
        }
    }

    // makes the key - place.id (think mainmenu.play, for example) 
    // just return "" if nothing is found
    public static string MakeKey(string place, string id)
    {
        place = (place ?? "").Trim();
        id = (id ?? "").Trim();
        return $"{place}.{id}";
    }

    // get the text with the key
    public static string Get(string place, string id) => GetText(place, id);

    // if you already have the key for some reason (weirdo)
    public static string GetByKey(string key) => GetTextByKey(key);

    // field-specific getters
    public static string GetText(string place, string id) => GetField(place, id, Field.Text);
    public static string GetName(string place, string id) => GetField(place, id, Field.Name);
    public static string GetDesc(string place, string id) => GetField(place, id, Field.Desc);
    public static string GetPrompt(string place, string id) => GetField(place, id, Field.Prompt);

    public static string GetTextByKey(string key) => GetFieldByKey(key, Field.Text);
    public static string GetNameByKey(string key) => GetFieldByKey(key, Field.Name);
    public static string GetDescByKey(string key) => GetFieldByKey(key, Field.Desc);
    public static string GetPromptByKey(string key) => GetFieldByKey(key, Field.Prompt);



    private enum Field { Text, Name, Desc, Prompt }

    private static string GetField(string place, string id, Field field)
    {
        LoadAllIfNeeded();
        string key = MakeKey(place, id);
        return GetFieldByKey(key, field);
    }

    // no fallbacks - blank maps to blank. The idea is that anything without some of these fields
    // will not be inspectable as applicable
    private static string GetFieldByKey(string key, Field field)
    {
        LoadAllIfNeeded();

        if (!map.TryGetValue(key, out var row))
            return $"missing {key}";

        return field switch
        {
            Field.Text => row.text ?? "",
            Field.Name => row.name ?? "",
            Field.Desc => row.desc ?? "",
            Field.Prompt => row.prompt ?? "",
            _ => ""
        };
    }

    private static void TryLoadCsv(TextAsset asset)
    {
        try
        {
            List<List<string>> rows = Csv.Parse(asset.text);
            if (rows.Count == 0) return;

            var header = rows[0];

            int placeCol = FindCol(header, "place");
            int idCol = FindCol(header, "id");
            int textCol = FindCol(header, "text");
            int nameCol = FindCol(header, "name");
            int descCol = FindCol(header, "desc");
            int promptCol = FindCol(header, "prompt");

            if (placeCol < 0 || idCol < 0)
            {
                Debug.LogError($"[TextDB] CSV '{asset.name}' is missing required headers. Need at least: place,id");
                return;
            }

            // At least one content column should exist, otherwise it’s useless.
            if (textCol < 0 && nameCol < 0 && descCol < 0 && promptCol < 0)
            {
                Debug.LogError($"[TextDB] CSV '{asset.name}' has no content columns. Add one of: text,name,desc,prompt");
                return;
            }

            for (int r = 1; r < rows.Count; r++)
            {
                var row = rows[r];
                if (row.Count == 0) continue;

                string place = GetField(row, placeCol);
                string id = GetField(row, idCol);
                string key = MakeKey(place, id);

                if (string.IsNullOrWhiteSpace(place) || string.IsNullOrWhiteSpace(id))
                    continue;

                if (map.ContainsKey(key))
                {
                    Debug.LogError($"[TextDB] Duplicate key '{key}' in CSV '{asset.name}' at data row {r + 1}.");
                    continue;
                }

                TextRow tr = new TextRow
                {
                    text = textCol >= 0 ? GetField(row, textCol) : "",
                    name = nameCol >= 0 ? GetField(row, nameCol) : "",
                    desc = descCol >= 0 ? GetField(row, descCol) : "",
                    prompt = promptCol >= 0 ? GetField(row, promptCol) : "",
                };

                map[key] = tr;
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[TextDB] Failed parsing CSV '{asset.name}': {ex}");
        }
    }

    private static int FindCol(List<string> header, string name)
    {
        for (int i = 0; i < header.Count; i++)
        {
            if (string.Equals((header[i] ?? "").Trim(), name, StringComparison.OrdinalIgnoreCase))
                return i;
        }
        return -1;
    }

    private static string GetField(List<string> row, int index)
    {
        if (index < 0 || index >= row.Count) return "";
        return row[index] ?? "";
    }

    // csv parser supporting commas, quotes, escaped quotes, and multiline quoted fields
    private static class Csv
    {
        public static List<List<string>> Parse(string csv)
        {
            var rows = new List<List<string>>();
            var row = new List<string>();
            var field = new StringBuilder();

            bool inQuotes = false;
            int i = 0;

            while (i < csv.Length)
            {
                char c = csv[i];

                if (inQuotes)
                {
                    if (c == '"')
                    {
                        if (i + 1 < csv.Length && csv[i + 1] == '"')
                        {
                            field.Append('"');
                            i += 2;
                            continue;
                        }
                        inQuotes = false;
                        i++;
                        continue;
                    }

                    field.Append(c);
                    i++;
                    continue;
                }

                if (c == '"')
                {
                    inQuotes = true;
                    i++;
                    continue;
                }

                if (c == ',')
                {
                    row.Add(field.ToString());
                    field.Clear();
                    i++;
                    continue;
                }

                if (c == '\r')
                {
                    i++;
                    continue;
                }

                if (c == '\n')
                {
                    row.Add(field.ToString());
                    field.Clear();
                    rows.Add(row);
                    row = new List<string>();
                    i++;
                    continue;
                }

                field.Append(c);
                i++;
            }

            row.Add(field.ToString());
            rows.Add(row);

            for (int r = rows.Count - 1; r >= 0; r--)
            {
                bool allEmpty = true;
                foreach (var f in rows[r])
                {
                    if (!string.IsNullOrEmpty(f))
                    {
                        allEmpty = false;
                        break;
                    }
                }
                if (allEmpty) rows.RemoveAt(r);
                else break;
            }

            return rows;
        }
    }
}
