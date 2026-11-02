using MelonLoader;
using TowerDominion.Toolkit.Model;
using TowerDominion.Toolkit.Modules;
using UnityEngine;
using UnityEngine.EventSystems;

namespace TowerDominion.Toolkit.Game;

/// <summary>Immediate-mode console UI; command execution remains transport-neutral.</summary>
internal sealed class ToolkitConsoleOverlay
{
    private const int VisibleLines = 18;
    private readonly CommandRegistry commands;
    private readonly EntityCatalog catalog;
    private readonly StartingTilePresets? tilePresets;
    private IReadOnlyDictionary<int, string[]>? tilePreview;
    private IReadOnlyDictionary<StartPosition, StartingTile>? legacyPreview;
    private readonly List<string> output = new();
    private readonly List<string> history = new();
    private string input = "";
    private string draft = "";
    private int historyIndex;
    private int scroll;
    private int cursor;
    private KeyCode repeatingKey = KeyCode.None;
    private float nextRepeat;
    private bool visible;
    private EventSystem? suspended;
    private bool wasEnabled;

    internal ToolkitConsoleOverlay(CommandRegistry commands, EntityCatalog catalog, StartingTilePresets? tilePresets = null)
    {
        this.commands = commands;
        this.catalog = catalog;
        this.tilePresets = tilePresets;
        Write("Tower Dominion Toolkit ready. Press ` to open; type help for commands.");
    }

    internal void Update()
    {
        if (Input.GetKeyDown(KeyCode.BackQuote))
        {
            SetVisible(!visible);
            return;
        }
        if (!visible) return;
        Capture();
        if (Input.GetKeyDown(KeyCode.Escape)) { SetVisible(false); return; }
        if (Input.GetKeyDown(KeyCode.UpArrow)) { Navigate(-1); return; }
        if (Input.GetKeyDown(KeyCode.DownArrow)) { Navigate(1); return; }
        if (Input.GetKeyDown(KeyCode.PageUp)) { Scroll(VisibleLines / 2); return; }
        if (Input.GetKeyDown(KeyCode.PageDown)) { Scroll(-VisibleLines / 2); return; }
        bool ctrl = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
        if (ctrl && Input.GetKeyDown(KeyCode.Home)) { scroll = Math.Max(0, output.Count - VisibleLines); return; }
        if (ctrl && Input.GetKeyDown(KeyCode.End)) { scroll = 0; return; }
        if (Repeat(KeyCode.LeftArrow)) { cursor = Math.Max(0, cursor - 1); return; }
        if (Repeat(KeyCode.RightArrow)) { cursor = Math.Min(input.Length, cursor + 1); return; }
        if (Input.GetKeyDown(KeyCode.Home)) { cursor = 0; return; }
        if (Input.GetKeyDown(KeyCode.End)) { cursor = input.Length; return; }
        if (Repeat(KeyCode.Delete)) { if (cursor < input.Length) input = input.Remove(cursor, 1); return; }
        if (Repeat(KeyCode.Backspace)) { if (cursor > 0) { input = input.Remove(cursor - 1, 1); cursor--; } return; }
        float wheel = Input.mouseScrollDelta.y;
        if (wheel > 0.01f) { Scroll(3); return; }
        if (wheel < -0.01f) { Scroll(-3); return; }
        if (Input.GetKeyDown(KeyCode.Tab)) { Complete(); return; }
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)) { Submit(); return; }
        if (((Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) && Input.GetKeyDown(KeyCode.V))
            || ((Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) && Input.GetKeyDown(KeyCode.Insert)))
        {
            Insert((GUIUtility.systemCopyBuffer ?? "").Replace('\r', ' ').Replace('\n', ' '));
            return;
        }
        foreach (char character in Input.inputString)
            if (!char.IsControl(character) && character != '`' && !ctrl) Insert(character.ToString());
    }

    internal void Draw()
    {
        if (!visible) return;
        float width = Math.Min(950, Screen.width - 30);
        float height = Math.Min(510, Screen.height - 30);
        float left = (Screen.width - width) / 2;
        float top = 15;
        GUI.Box(new Rect(left, top, width, height), "Tower Dominion Toolkit");
        int lines = Math.Max(1, Math.Min(VisibleLines, (int)((height - 115) / 19)));
        int start = Math.Max(0, output.Count - lines - scroll);
        int end = Math.Min(output.Count, start + lines);
        float outputWidth = tilePreview is { Count: > 0 } || legacyPreview != null ? Math.Max(200, width * 0.42f) : width - 30;
        for (int i = start; i < end; i++)
            GUI.Label(new Rect(left + 15, top + 32 + (i - start) * 19, outputWidth, 20), output[i]);
        if (tilePreview is { Count: > 0 } preview)
        {
            float previewLeft = left + width * 0.43f;
            float available = width * 0.55f;
            float each = available / 4;
            const float previewGap = 9f;
            foreach (var pair in preview.OrderBy(p => p.Key))
                if (pair.Key is >= 1 and <= 4)
                    StartingTileMosaic.Draw(new Rect(previewLeft + (pair.Key - 1) * each, top + 45, each - previewGap, each - previewGap + 26), pair.Key, pair.Value);
        }
        if (legacyPreview != null)
            StartingTileMosaic.DrawMap(new Rect(left + width * 0.53f, top + 45, width * 0.27f, width * 0.3f), legacyPreview, "Legacy tileset");
        GUI.Label(new Rect(left + 15, top + height - 72, width - 30, 24), "> " + input.Insert(Math.Clamp(cursor, 0, input.Length), "|"));
        GUI.Label(new Rect(left + 15, top + height - 46, width - 30, 22), "Enter execute | Tab complete | Up/Down history | Page Up/Down scroll | ` or Esc close");
    }

    internal void Dispose() => SetVisible(false);

    private void SetVisible(bool value)
    {
        visible = value;
        if (value) Capture(); else Restore();
        MelonLogger.Msg($"[Toolkit Console] {(value ? "Opened" : "Closed")}");
    }

    private void Capture()
    {
        EventSystem? current = EventSystem.current;
        if (current == null || current == suspended) return;
        Restore();
        suspended = current;
        wasEnabled = current.enabled;
        current.enabled = false;
    }

    private void Restore()
    {
        if (suspended == null) return;
        suspended.enabled = wasEnabled;
        suspended = null;
    }

    private void Scroll(int delta) => scroll = Math.Clamp(scroll + delta, 0, Math.Max(0, output.Count - VisibleLines));

    private void Navigate(int direction)
    {
        if (history.Count == 0) return;
        if (direction < 0)
        {
            if (historyIndex == history.Count) draft = input;
            historyIndex = Math.Max(0, historyIndex - 1);
        }
        else historyIndex = Math.Min(history.Count, historyIndex + 1);
        input = historyIndex == history.Count ? draft : history[historyIndex];
        cursor = input.Length;
    }

    private void Submit()
    {
        string command = input.Trim();
        if (command.Length == 0) return;
        Write("> " + command);
        if (history.Count == 0 || history[^1] != command) history.Add(command);
        if (history.Count > 200) history.RemoveAt(0);
        historyIndex = history.Count;
        draft = "";
        input = "";
        cursor = 0;
        scroll = 0;
        try
        {
            string response = commands.Execute(command);
            Write(response);
            if (tilePresets != null && !response.StartsWith("Usage:", StringComparison.OrdinalIgnoreCase))
            {
                if (command.StartsWith("list starting-tile-preset ", StringComparison.OrdinalIgnoreCase))
                {
                    string name = command[26..].Trim();
                    tilePreview = tilePresets.PreviewSavedRelative(name);
                    legacyPreview = tilePreview.Count == 0 ? tilePresets.Read(name) : null;
                }
                else if (command.StartsWith("set starting tiles ", StringComparison.OrdinalIgnoreCase) ||
                         command.StartsWith("clear starting tiles ", StringComparison.OrdinalIgnoreCase) ||
                         command.Equals("list starting tiles", StringComparison.OrdinalIgnoreCase) ||
                         command.Equals("list starting-tiles", StringComparison.OrdinalIgnoreCase) ||
                         command.StartsWith("load starting tiles ", StringComparison.OrdinalIgnoreCase) ||
                         command.StartsWith("activate tileset ", StringComparison.OrdinalIgnoreCase))
                {
                    tilePreview = tilePresets.Relative.ToDictionary(p => p.Key, p => p.Value);
                    legacyPreview = tilePreview.Count == 0 ? tilePresets.Active : null;
                }
            }
        }
        catch (Exception exception)
        {
            Write("Command failed: " + exception.Message);
            MelonLogger.Error($"[Toolkit Console] Command failed: {exception}");
        }
    }

    private void Complete()
    {
        string trimmed = input.TrimStart();
        string[] tokens = trimmed.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        bool trailing = trimmed.EndsWith(' ');
        int index = trailing ? tokens.Length : Math.Max(0, tokens.Length - 1);
        string prefix = trailing || tokens.Length == 0 ? "" : tokens[^1];
        IEnumerable<string> candidates;
        if (index == 0) candidates = commands.Commands.Select(x => x.Name).Append("help");
        else if (tokens[0].Equals("list", StringComparison.OrdinalIgnoreCase) && index == 1)
            candidates = new[] { "foes", "waves", "blueprints", "doctrines", "frontiers", "selected-frontiers", "banned" };
        else if ((tokens[0].Equals("ban", StringComparison.OrdinalIgnoreCase) || tokens[0].Equals("unban", StringComparison.OrdinalIgnoreCase)) && index == 1)
            candidates = BanSelections.Categories;
        else if ((tokens[0].Equals("ban", StringComparison.OrdinalIgnoreCase) || tokens[0].Equals("unban", StringComparison.OrdinalIgnoreCase)) && tokens.Length >= 2 && BanSelections.Categories.Contains(tokens[1], StringComparer.OrdinalIgnoreCase))
        {
            int comma = input.LastIndexOf(',');
            int offset = comma >= 0 ? comma + 1 : input.IndexOf(tokens[1], StringComparison.OrdinalIgnoreCase) + tokens[1].Length;
            while (offset < input.Length && char.IsWhiteSpace(input[offset])) offset++;
            string fragment = input[offset..].Trim().Trim('"');
            string[] names = catalog.List(tokens[1]).Select(x => x.Name).Append("all")
                .Where(x => x.Contains(fragment, StringComparison.OrdinalIgnoreCase)).OrderBy(x => x).ToArray();
            CompleteEntityNames(names, offset, fragment, "No matching entries.");
            return;
        }
        else if ((tokens[0].Equals("select", StringComparison.OrdinalIgnoreCase) || tokens[0].Equals("unselect", StringComparison.OrdinalIgnoreCase)) && index == 1)
            candidates = new[] { "frontier" };
        else if ((tokens[0].Equals("select", StringComparison.OrdinalIgnoreCase) || tokens[0].Equals("unselect", StringComparison.OrdinalIgnoreCase)) && tokens.Length >= 2 && tokens[1].Equals("frontier", StringComparison.OrdinalIgnoreCase))
        {
            int comma = input.LastIndexOf(',');
            int offset = comma >= 0 ? comma + 1 : input.IndexOf("frontier", StringComparison.OrdinalIgnoreCase) + "frontier".Length;
            while (offset < input.Length && char.IsWhiteSpace(input[offset])) offset++;
            string fragment = input[offset..].Trim().Trim('"');
            string[] names = catalog.List("frontiers").Select(x => x.Name).Append("all")
                .Where(x => x.Contains(fragment, StringComparison.OrdinalIgnoreCase)).OrderBy(x => x).ToArray();
            CompleteEntityNames(names, offset, fragment, "No matching Frontiers.");
            return;
        }
        else { Write("No completion available here."); return; }
        string[] matches = candidates.Where(x => x.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)).OrderBy(x => x).ToArray();
        if (matches.Length == 1)
        {
            List<string> result = tokens.ToList();
            if (index < result.Count) result[index] = matches[0]; else result.Add(matches[0]);
            input = string.Join(" ", result) + " ";
            cursor = input.Length;
        }
        else Write(matches.Length == 0 ? "No matches." : "Matches: " + string.Join(", ", matches));
    }

    private void CompleteEntityNames(string[] names, int offset, string fragment, string emptyMessage)
    {
        if (names.Length == 0) { Write(emptyMessage); return; }
        // Prefix matches take priority over incidental substring matches.
        string[] leading = names.Where(x => x.StartsWith(fragment, StringComparison.OrdinalIgnoreCase)).ToArray();
        if (leading.Length > 0) names = leading;
        if (names.Length == 1)
        {
            input = input[..offset] + (names[0] == "all" ? "all" : "\"" + names[0] + "\"");
            cursor = input.Length;
            return;
        }
        // Prefer the shared leading text, even when matching by substring.
        string common = names[0];
        foreach (string name in names.Skip(1))
        {
            int i = 0;
            while (i < common.Length && i < name.Length && char.ToUpperInvariant(common[i]) == char.ToUpperInvariant(name[i])) i++;
            common = common[..i];
        }
        if (common.Length > fragment.Length && common.StartsWith(fragment, StringComparison.OrdinalIgnoreCase))
        {
            input = input[..offset] + common;
            cursor = input.Length;
        }
        else Write("Matches: " + string.Join(", ", names));
    }

    private void Insert(string value)
    {
        input = input.Insert(cursor, value);
        cursor += value.Length;
    }

    private bool Repeat(KeyCode key)
    {
        if (!Input.GetKey(key))
        {
            if (repeatingKey == key) repeatingKey = KeyCode.None;
            return false;
        }
        float now = Time.unscaledTime;
        if (Input.GetKeyDown(key) || repeatingKey != key)
        {
            repeatingKey = key;
            nextRepeat = now + 0.4f;
            return true;
        }
        if (now < nextRepeat) return false;
        nextRepeat = now + 0.045f;
        return true;
    }

    private void Write(string message)
    {
        foreach (string line in message.Split('\n')) output.Add(line.TrimEnd('\r'));
        if (output.Count > 1000) output.RemoveRange(0, output.Count - 1000);
    }
}
