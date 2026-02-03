using TMPro;
using UnityEngine;

// super cool CSV Mapper! This does the   mapping

[RequireComponent(typeof(TMP_Text))]
public class TextKeyTMP : MonoBehaviour
{
    public enum Field
    {
        Text,
        Name,
        Desc,
        Prompt
    }

    [Header("CSV lookup (row key)")]
    public string place;
    public string id;

    [Tooltip("Overrides place+id and uses this full key directly (for example, mainmenu.play) if set here.")]
    public string fullKeyOverride;

    [Header("Column to display")]
    public Field field = Field.Text;

    private TMP_Text tmp;

    private void Awake()
    {
        tmp = GetComponent<TMP_Text>();
        Refresh();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (tmp == null) tmp = GetComponent<TMP_Text>();
        if (!Application.isPlaying && tmp != null)
            Refresh();
    }
#endif

    public void Refresh()
    {
        if (tmp == null) return;

        // build the row key
        string key = !string.IsNullOrWhiteSpace(fullKeyOverride)
            ? fullKeyOverride.Trim()
            : TextDB.MakeKey(place, id);

        // choose which field to display
        string value = field switch
        {
            Field.Text => TextDB.GetTextByKey(key),
            Field.Name => TextDB.GetNameByKey(key),
            Field.Desc => TextDB.GetDescByKey(key),
            Field.Prompt => TextDB.GetPromptByKey(key),
            _ => ""
        };

        if (tmp.text != value)
            tmp.text = value;
    }
}
