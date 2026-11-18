using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public sealed class MainMenuController : MonoBehaviour {
    [SerializeField] private InputActionAsset playerControls;

    private const int ProfileCount = 4;
    // Ignore Esc/Enter briefly after a key capture so the same press doesn't also navigate.
    private const float CaptureCooldown = 0.2f;

    private sealed class BindingRow {
        public string Label;
        public InputAction Action;
        public int Index;
        public Button Row;
        public Label Key;
    }

    private readonly List<BindingRow> rows = new();
    private readonly Button[] profiles = new Button[ProfileCount];
    private InputActionAsset editableControls;
    private InputActionRebindingExtensions.RebindingOperation rebind;
    private IVisualElementScheduledItem animation;
    private VisualElement root, home, settings, bindings, hints, backdrop;
    private Label status;
    private Button play, controls;
    private int profile;
    private float captureEndedAt = float.NegativeInfinity;

    private bool InSettings => !settings.ClassListContains("hidden");
    private bool CoolingDown => Time.unscaledTime - captureEndedAt < CaptureCooldown;

    private void OnEnable() {
        editableControls = Instantiate(playerControls);
        KeyboardBindingPreferences.Apply(editableControls);

        root = GetComponent<UIDocument>().rootVisualElement;
        // The document root sizes to its content by default; stretch it so the menu covers the screen.
        root.style.flexGrow = 1;
        home = root.Q("home");
        settings = root.Q("settings");
        bindings = root.Q("bindings");
        hints = root.Q("hints");
        status = root.Q<Label>("status");
        backdrop = root.Q("backdrop");

        play = root.Q<Button>("play");
        controls = root.Q<Button>("controls");
        play.clicked += Play;
        controls.clicked += ShowSettings;
        root.Q<Button>("quit").clicked += Quit;
        root.Q<Button>("back").clicked += ShowHome;
        root.Q<Button>("reset").clicked += ResetLayout;
        root.Query<Button>(className: "menu-item").ForEach(FocusOnHover);
        FocusOnHover(root.Q<Button>("back"));
        FocusOnHover(root.Q<Button>("reset"));

        var tabs = root.Q("profiles");
        tabs.Clear();
        for (int i = 0; i < ProfileCount; i++) {
            int index = i;
            var tab = new Button(() => SelectProfile(index)) { text = $"Keyboard {i + 1}" };
            tab.AddToClassList("tab");
            FocusOnHover(tab);
            tabs.Add(tab);
            profiles[i] = tab;
        }

        backdrop.generateVisualContent += SpaceBackdrop.Draw;
        animation = backdrop.schedule.Execute(backdrop.MarkDirtyRepaint).Every(33);
        root.RegisterCallback<NavigationCancelEvent>(OnNavigationCancel, TrickleDown.TrickleDown);
        root.RegisterCallback<NavigationSubmitEvent>(OnNavigationSubmit, TrickleDown.TrickleDown);

        ShowHome();
        root.schedule.Execute(() => play.Focus());
    }

    private void OnDisable() {
        // Dispose directly: the UI document is being torn down, so don't update it.
        rebind?.Dispose();
        rebind = null;
        animation?.Pause();
        if (editableControls != null) Destroy(editableControls);
        rows.Clear();
        if (root == null) return;

        backdrop.generateVisualContent -= SpaceBackdrop.Draw;
        root.UnregisterCallback<NavigationCancelEvent>(OnNavigationCancel, TrickleDown.TrickleDown);
        root.UnregisterCallback<NavigationSubmitEvent>(OnNavigationSubmit, TrickleDown.TrickleDown);
        play.clicked -= Play;
        controls.clicked -= ShowSettings;
        root.Q<Button>("quit").clicked -= Quit;
        root.Q<Button>("back").clicked -= ShowHome;
        root.Q<Button>("reset").clicked -= ResetLayout;
    }

    private static void FocusOnHover(Button button) => button.RegisterCallback<PointerEnterEvent>(_ => button.Focus());

    private void OnNavigationCancel(NavigationCancelEvent evt) {
        evt.StopImmediatePropagation();
        if (rebind != null || CoolingDown) return;
        if (InSettings) ShowHome();
    }

    private void OnNavigationSubmit(NavigationSubmitEvent evt) {
        if (rebind != null || CoolingDown) evt.StopImmediatePropagation();
    }

    private void Play() {
        play.SetEnabled(false);
        SceneManager.LoadScene(SceneNames.PlayerJoin);
    }

    private static void Quit() {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void ShowHome() {
        rebind?.Cancel();
        settings.AddToClassList("hidden");
        home.RemoveFromClassList("hidden");
        SetHints(("Enter", "Select"));
        controls.Focus();
    }

    private void ShowSettings() {
        home.AddToClassList("hidden");
        settings.RemoveFromClassList("hidden");
        SetHints(("Enter", "Change key"), ("Esc", "Back"));
        SelectProfile(profile);
        profiles[profile].Focus();
    }

    private void SetHints(params (string key, string text)[] items) {
        hints.Clear();
        foreach (var (key, text) in items) {
            var cap = new Label(key);
            cap.AddToClassList("hint-key");
            var label = new Label(text);
            label.AddToClassList("hint-text");
            hints.Add(cap);
            hints.Add(label);
        }
    }

    private void SelectProfile(int index) {
        if (rebind != null) return;
        profile = index;
        for (int i = 0; i < ProfileCount; i++) profiles[i].EnableInClassList("selected", i == index);

        bindings.Clear();
        rows.Clear();
        AddGroup("Movement");
        AddRow("Up", "Move", "up");
        AddRow("Down", "Move", "down");
        AddRow("Left", "Move", "left");
        AddRow("Right", "Move", "right");
        AddGroup("Actions");
        AddRow("Jump", "Jump");
        AddRow("Interact", "Interact");
        AddRow("Leave console", "Back");

        RefreshKeys();
        status.text = "Pick an action to change its key. Changes save automatically.";
    }

    private void AddGroup(string title) {
        var label = new Label(title);
        label.AddToClassList("group-label");
        bindings.Add(label);
    }

    private void AddRow(string label, string actionName, string partName = null) {
        var action = editableControls.FindActionMap("Gameplay", true).FindAction(actionName, true);
        var mask = InputBinding.MaskByGroup($"KeyboardP{profile + 1}");
        mask.name = partName;
        int index = action.GetBindingIndex(mask);
        if (index < 0) return;

        var entry = new BindingRow { Label = label, Action = action, Index = index, Row = new Button(), Key = new Label() };
        entry.Row.AddToClassList("bind-row");
        entry.Row.clicked += () => BeginCapture(entry);
        FocusOnHover(entry.Row);
        var title = new Label(label);
        title.AddToClassList("bind-label");
        entry.Key.AddToClassList("keycap");
        entry.Row.Add(title);
        entry.Row.Add(entry.Key);
        bindings.Add(entry.Row);
        rows.Add(entry);
    }

    private void RefreshKeys() {
        foreach (var row in rows) row.Key.text = row.Action.GetBindingDisplayString(row.Index);
    }

    private void BeginCapture(BindingRow row) {
        if (rebind != null || CoolingDown) return;
        if (Keyboard.current == null) {
            status.text = "Connect a keyboard to change keys.";
            return;
        }

        row.Row.AddToClassList("listening");
        row.Key.text = "...";
        status.text = $"Press a new key for {row.Label}.";
        SetHints(("Esc", "Cancel"));

        rebind = row.Action.PerformInteractiveRebinding(row.Index)
            .WithExpectedControlType("Key")
            .WithControlsHavingToMatchPath("<Keyboard>")
            .WithCancelingThrough("<Keyboard>/escape")
            .OnMatchWaitForAnother(0)
            .OnCancel(operation => EndCapture(operation, row, "Nothing changed."))
            .OnComplete(operation => {
                KeyboardBindingPreferences.Save(editableControls);
                EndCapture(operation, row,
                    $"{row.Label} is now {row.Action.GetBindingDisplayString(row.Index)} for Keyboard {profile + 1}.");
            });
        rebind.Start();
    }

    private void EndCapture(InputActionRebindingExtensions.RebindingOperation operation, BindingRow row, string message) {
        rebind = null;
        operation.Dispose();
        captureEndedAt = Time.unscaledTime;
        row.Row.RemoveFromClassList("listening");
        RefreshKeys();
        status.text = message;
        SetHints(("Enter", "Change key"), ("Esc", "Back"));
    }

    private void ResetLayout() {
        if (rebind != null) return;
        foreach (var row in rows) row.Action.RemoveBindingOverride(row.Index);
        KeyboardBindingPreferences.Save(editableControls);
        RefreshKeys();
        status.text = $"Keyboard {profile + 1} is back to its default keys.";
    }
}
