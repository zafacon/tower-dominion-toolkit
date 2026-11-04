using MelonLoader;
using TowerDominion.Toolkit.Modules;
using UnityEngine;
using UnityEngine.EventSystems;

namespace TowerDominion.Toolkit.Game;

/// <summary>Immediate-mode console UI; command execution remains transport-neutral.</summary>
internal sealed class ToolkitConsoleOverlay
{
    private const int VisibleLines = 18;
    private static readonly Color BackdropColor = new(0.075f, 0.085f, 0.105f, 0.95f);
    private static readonly Color PanelColor = new(0.045f, 0.055f, 0.075f, 0.98f);
    private static Texture2D? backdropTexture;
    private static Texture2D? panelTexture;
    private readonly CanonicalCommandExecutor executor;
    private readonly ConsoleCommandAssistance? assistance;
    private IReadOnlyDictionary<int, string[]>? tilePreview;
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

    internal ToolkitConsoleOverlay(CanonicalCommandExecutor executor, CommandGrammar grammar)
    {
        assistance = new ConsoleCommandAssistance(grammar);
        this.executor = executor;
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

    internal bool IsVisible => visible;

    internal void Draw()
    {
        if (!visible) return;
        float width = Math.Min(950, Screen.width - 30);
        float height = Math.Min(510, Screen.height - 30);
        float left = (Screen.width - width) / 2;
        float top = 15;
        DrawBackground(new Rect(left, top, width, height), BackdropColor);
        GUI.Box(new Rect(left, top, width, height), "Tower Dominion Toolkit");
        DrawBackground(new Rect(left + 9, top + 27, width - 18, height - 112), PanelColor);
        DrawBackground(new Rect(left + 9, top + height - 79, width - 18, 34), PanelColor);
        float outputWidth = tilePreview is { Count: > 0 } ? Math.Max(200, width * 0.42f) : width - 30;
        float availableHeight = height - 120;
        var labelStyle = GUI.skin.label;
        bool previousWrap = labelStyle.wordWrap;
        TextClipping previousClipping = labelStyle.clipping;
        labelStyle.wordWrap = true;
        labelStyle.clipping = TextClipping.Clip;
        try
        {
            GUI.BeginGroup(new Rect(left + 15, top + 32, outputWidth, availableHeight));
            try
            {
                float bottom = availableHeight;
                int end = Math.Max(0, output.Count - scroll);
                // Lay out from the bottom so wrapped entries occupy their actual height
                // without overlapping adjacent entries or escaping the output panel.
                for (int i = end - 1; i >= 0; i--)
                {
                    float rowHeight = Math.Max(19f, labelStyle.CalcHeight(new GUIContent(output[i]), outputWidth) + 2f);
                    bottom -= rowHeight;
                    GUI.Label(new Rect(0, bottom, outputWidth, rowHeight), output[i], labelStyle);
                    if (bottom <= 0) break;
                }
            }
            finally { GUI.EndGroup(); }
        }
        finally
        {
            labelStyle.wordWrap = previousWrap;
            labelStyle.clipping = previousClipping;
        }
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
        GUI.Label(new Rect(left + 15, top + height - 72, width - 30, 24), "> " + input.Insert(Math.Clamp(cursor, 0, input.Length), "|"));
        GUI.Label(new Rect(left + 15, top + height - 46, width - 30, 22), "Enter execute | Tab complete | Up/Down history | Page Up/Down scroll | ` or Esc close");
    }

    private static void DrawBackground(Rect area, Color tint)
    {
        // Reuse the GUI.Box texture path already exercised by StartingTileMosaic;
        // GUI.DrawTexture/Texture2D.whiteTexture are not reliably unstripped in IL2CPP.
        Texture2D? texture = tint.a == BackdropColor.a ? backdropTexture : panelTexture;
        if (texture == null)
        {
            texture = new Texture2D(1, 1);
            texture.SetPixels(new[] { tint });
            texture.Apply();
            if (tint.a == BackdropColor.a) backdropTexture = texture;
            else panelTexture = texture;
        }
        var style = GUI.skin.box;
        var previous = style.normal.background;
        try
        {
            style.normal.background = texture;
            GUI.Box(area, "", style);
        }
        finally { style.normal.background = previous; }
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
            bool help = assistance is not null && (command.Equals("help", StringComparison.OrdinalIgnoreCase)
                || command.StartsWith("help ", StringComparison.OrdinalIgnoreCase));
            CommandOutcome outcome = help
                ? CommandOutcome.Ok(assistance!.Help(command.Length == 4 ? null : command[5..]))
                : executor.ExecuteDetailed(command);
            Write(outcome.Message);
            if (outcome.Success && outcome.Details.Preview is { } preview)
                ApplyPreview(preview);

        }
        catch (Exception exception)
        {
            Write("Command failed: " + exception.Message);
            MelonLogger.Error($"[Toolkit Console] Command failed: {exception}");
        }
    }

    private void ApplyPreview(CommandPreview preview)
    {
        if (!preview.Kind.Equals("starting tiles", StringComparison.OrdinalIgnoreCase)) return;
        if (!preview.Data.TryGetValue("relative", out object? relative)
            || relative is not IReadOnlyDictionary<string, string[]> entries) return;
        var mapped = new Dictionary<int, string[]>();
        foreach (var entry in entries)
        {
            if (entry.Key.Length != 2 || entry.Key[0] != 'D'
                || !int.TryParse(entry.Key[1..], out int difficulty) || difficulty is < 1 or > 4) return;
            mapped[difficulty] = entry.Value.ToArray();
        }
        tilePreview = mapped;
    }

    private void Complete()
    {
        if (assistance is null) { Write("Command completion unavailable."); return; }
        CommandCompletionResult result = assistance.Complete(input, cursor);
        if (result.Changed)
        {
            input = result.Text;
            cursor = result.Cursor;
        }
        else
        {
            string message = result.Candidates.Count == 0 ? "No matches." : "Matches: " + string.Join(", ", result.Candidates);
            if (output.Count == 0 || output[^1] != message) Write(message);
        }
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
