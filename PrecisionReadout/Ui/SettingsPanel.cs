using UnityEngine;

namespace PrecisionReadout.Ui;

// A small draggable IMGUI window (hotkey: see ModActions, rebindable in the key bindings screen). Changes apply immediately and are saved by Settings.
public sealed class SettingsPanel : MonoBehaviour
{
    private const int WindowId = 0x50524552; // "PREC"
    private Rect window = new Rect(40f, 120f, 330f, 150f);
    private bool open;

    internal bool IsOpen => open;

    private static Texture2D backing;

    private static Texture2D Backing
    {
        get
        {
            if (backing == null)
            {
                backing = new Texture2D(1, 1, TextureFormat.RGBA32, false) { hideFlags = HideFlags.HideAndDontSave };
                backing.SetPixel(0, 0, new Color(0.13f, 0.15f, 0.20f, 1f));
                backing.Apply();
            }
            return backing;
        }
    }

    // IMGUI is invisible to the EventSystem, so the game's tools would still receive clicks made on the window.
    // An invisible uGUI image over the same rectangle makes EventSystem.IsPointerOverGameObject() true there.
    private RectTransform blocker;

    private void Awake()
    {
        var canvasObject = new GameObject("PrecisionReadoutBlocker", typeof(RectTransform));
        canvasObject.transform.SetParent(transform, false);
        var canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = short.MaxValue;
        canvasObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();

        var imageObject = new GameObject("Blocker", typeof(RectTransform));
        imageObject.transform.SetParent(canvasObject.transform, false);
        var image = imageObject.AddComponent<UnityEngine.UI.Image>();
        image.color = new Color(0f, 0f, 0f, 0f);
        image.raycastTarget = true;
        blocker = imageObject.GetComponent<RectTransform>();
        blocker.anchorMin = blocker.anchorMax = Vector2.zero;
        blocker.pivot = Vector2.zero;
        imageObject.SetActive(false);
    }

    // True while the pointer is over the open window; the camera's wheel zoom is suppressed then (see CameraPatches).
    internal static bool PointerOver { get; private set; }

    // Row rectangles (window-local GUI units) recorded while drawing, used to route the wheel to the row under the pointer.
    private Rect massRow, temperatureRow;

    private void Update()
    {
        if (ModActions.TogglePressed()) open = !open;
        SyncBlocker();

        var scale = Mathf.Max(1f, Screen.height / 1080f);
        var mouse = new Vector2(Input.mousePosition.x / scale, (Screen.height - Input.mousePosition.y) / scale);
        PointerOver = open && window.Contains(mouse);
        if (!PointerOver) return;

        var wheel = Input.mouseScrollDelta.y;
        if (wheel == 0f) return;
        var step = wheel > 0f ? 1 : -1;
        var local = mouse - window.position;
        if (massRow.Contains(local)) Settings.MassDigits += step;
        else if (temperatureRow.Contains(local)) Settings.TemperatureDigits += step;
    }

    private void SyncBlocker()
    {
        if (blocker == null) return;
        if (blocker.gameObject.activeSelf != open) blocker.gameObject.SetActive(open);
        if (!open) return;
        var scale = Mathf.Max(1f, Screen.height / 1080f);
        blocker.sizeDelta = new Vector2(window.width * scale, window.height * scale);
        blocker.anchoredPosition = new Vector2(window.x * scale, Screen.height - (window.y + window.height) * scale);
    }

    private void OnGUI()
    {
        if (!open) return;
        var scale = Mathf.Max(1f, Screen.height / 1080f);
        var old = GUI.matrix;
        GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
        // The default window skin is translucent, so text behind it shows through. Paint a solid backing under it.
        GUI.DrawTexture(window, Backing);
        window = GUILayout.Window(WindowId, window, Draw, (string)STRINGS.UI.TITLE, GUILayout.Width(330f));
        GUI.matrix = old;
    }

    private void Draw(int id)
    {
        Digits((string)STRINGS.UI.MASS, Settings.MassDigits, v => Settings.MassDigits = v, ref massRow);
        Digits((string)STRINGS.UI.TEMPERATURE, Settings.TemperatureDigits, v => Settings.TemperatureDigits = v, ref temperatureRow);

        var dim = GUILayout.Toggle(Settings.DimUntrusted, (string)STRINGS.UI.DIM);
        if (dim != Settings.DimUntrusted) Settings.DimUntrusted = dim;

        GUILayout.Label(string.Format((string)STRINGS.UI.HINT, ModActions.KeyName()));
        if (GUILayout.Button((string)STRINGS.UI.CLOSE)) open = false;
        GUI.DragWindow();
    }

    private static void Digits(string label, int value, System.Action<int> set, ref Rect row)
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label(label, GUILayout.Width(190f));
        var shown = value == 0 ? (string)STRINGS.UI.OFF : value.ToString();
        GUILayout.Label(shown, GUILayout.Width(30f));
        var next = Mathf.RoundToInt(GUILayout.HorizontalSlider(value, 0f, Settings.MaxDigits, GUILayout.Width(90f)));
        GUILayout.EndHorizontal();
        if (Event.current.type == EventType.Repaint) row = GUILayoutUtility.GetLastRect();
        if (next != value) set(next);
    }
}
