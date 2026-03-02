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
        public int act;   // for the to-be-renamed world clock - basically just a marker for when this text is meant to be shown 

        public bool IsEmpty =>
            string.IsNullOrEmpty(text) &&
            string.IsNullOrEmpty(name) &&
            string.IsNullOrEmpty(desc) &&
            string.IsNullOrEmpty(prompt);
    }

    private static readonly Dictionary<string, List<TextRow>> map = new Dictionary<string, List<TextRow>>();
    private static int currentAct = 1;

    public static void SetCurrentAct(int act)
    {
        currentAct = Mathf.Max(1, act);
    }

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

    public static int GetAct(string place, string id)
    {
        LoadAllIfNeeded();
        string key = MakeKey(place, id);
        return GetActByKey(key);
    }

    public static string GetTextByKey(string key) => GetFieldByKey(key, Field.Text);
    public static string GetNameByKey(string key) => GetFieldByKey(key, Field.Name);
    public static string GetDescByKey(string key) => GetFieldByKey(key, Field.Desc);
    public static string GetPromptByKey(string key) => GetFieldByKey(key, Field.Prompt);

    public static int GetActByKey(string key)
    {
        LoadAllIfNeeded();

        if (!TryGetRowForCurrentAct(key, out var row))
            return 1;

        return row.act;
    }

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

        if (!TryGetRowForCurrentAct(key, out var row))
            return "";

        return field switch
        {
            Field.Text => row.text ?? "",
            Field.Name => row.name ?? "",
            Field.Desc => row.desc ?? "",
            Field.Prompt => row.prompt ?? "",
            _ => ""
        };
    }

    private static bool TryGetRowForCurrentAct(string key, out TextRow chosen)
    {
        chosen = default;

        if (!map.TryGetValue(key, out var list) || list == null || list.Count == 0)
            return false;

        bool found = false;
        int bestAct = int.MinValue;

        for (int i = 0; i < list.Count; i++)
        {
            int a = list[i].act;
            if (a <= currentAct && a >= bestAct)
            {
                bestAct = a;
                chosen = list[i];
                found = true;
            }
        }

        return found;
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
            int actCol = FindCol(header, "act");

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

                TextRow tr = new TextRow
                {
                    text = textCol >= 0 ? GetField(row, textCol) : "",
                    name = nameCol >= 0 ? GetField(row, nameCol) : "",
                    desc = descCol >= 0 ? GetField(row, descCol) : "",
                    prompt = promptCol >= 0 ? GetField(row, promptCol) : "",
                    act = 1
                };

                if (actCol >= 0)
                {
                    string actStr = GetField(row, actCol).Trim();
                    if (!string.IsNullOrEmpty(actStr) && int.TryParse(actStr, out int parsedAct))
                        tr.act = parsedAct;
                }

                if (!map.TryGetValue(key, out var list))
                {
                    list = new List<TextRow>();
                    map[key] = list;
                }

                bool duplicateAct = false;
                for (int i = 0; i < list.Count; i++)
                {
                    if (list[i].act == tr.act)
                    {
                        duplicateAct = true;
                        break;
                    }
                }

                if (duplicateAct)
                {
                    Debug.LogError($"[TextDB] Duplicate key+act '{key}' act={tr.act} in CSV '{asset.name}' at data row {r + 1}.");
                    continue;
                }

                list.Add(tr);
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
            string h = (header[i] ?? "").Trim().TrimStart('\uFEFF');
            if (string.Equals(h, name, StringComparison.OrdinalIgnoreCase))
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
