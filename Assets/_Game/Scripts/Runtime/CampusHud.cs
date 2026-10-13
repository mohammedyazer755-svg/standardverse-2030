using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace CampusPulse
{
    public sealed class CampusHud : MonoBehaviour
    {
        public CampusSession Session;
        Font font;
        Text summary, detail, time, title;
        GameObject menu, inspector;
        readonly Color ink = new Color(.025f, .06f, .095f, .96f);
        readonly Color muted = new Color(.57f, .71f, .77f);
        readonly Color cyan = new Color(.18f, .84f, .9f);
        void Start()
        {
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var root = new GameObject("Campus Interface", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            root.transform.SetParent(transform, false);
            root.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080); scaler.matchWidthOrHeight = .5f;
            if (!EventSystem.current)
                new GameObject("Event System", typeof(EventSystem), typeof(InputSystemUIInputModule));
            var header = Panel(root.transform, "Header", new Vector2(0, 1), new Vector2(1, 1), new Vector2(24, -110), new Vector2(-24, -24));
            Label(header, "CAMPUS / PULSE 2030", 26, cyan, 24, 16, 410, 35);
            Label(header, "OPERATIONS ATLAS    /    LIVING CAMPUS", 12, muted, 26, 53, 500, 23);
            summary = Label(header, "", 18, Color.white, 560, 18, 910, 30);
            time = Label(header, "", 13, muted, 560, 52, 960, 24);
            Button(header, "OVERVIEW", 1610, 20, 200, () => Session.Overview());
            var footer = Panel(root.transform, "Navigation", Vector2.zero, new Vector2(1, 0), new Vector2(24, 24), new Vector2(-24, 84));
            Label(footer, "WASD  PAN     Q / E  ORBIT     SCROLL  ZOOM     F  FOCUS     HOME  RESET     ESC  PAUSE", 15, muted, 22, 20, 1240, 30);
            Button(footer, "PAUSE / RESUME", 1350, 8, 230, () => Session.TogglePause());
            Button(footer, "RESTART", 1600, 8, 200, () => Session.Restart());
            inspector = Panel(root.transform, "Facility Inspector", Vector2.one, Vector2.one, new Vector2(-380, -575), new Vector2(-24, -132)).gameObject;
            var panel = inspector.transform;
            Label(panel, "FACILITY / INSPECTION", 12, cyan, 22, 20, 305, 24);
            title = Label(panel, "Select a facility", 26, Color.white, 22, 58, 310, 65);
            detail = Label(panel, "", 17, muted, 22, 132, 310, 230);
            Button(panel, "FOCUS FACILITY", 22, 376, 310, () => Session.FocusSelected());
            menu = Panel(root.transform, "Welcome", new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(-320, -220), new Vector2(320, 220)).gameObject;
            Label(menu.transform, "ONE CAMPUS.\nEVERY DECISION MATTERS.", 33, Color.white, 38, 45, 565, 100);
            Label(menu.transform, "Explore the living campus. Select a facility to inspect its initial resource profile.\n\nPhase 1 environment preview — resource interventions arrive in Phase 2.", 20, muted, 38, 158, 565, 150);
            Button(menu.transform, "ENTER CAMPUS   →", 38, 342, 565, () => Session.Begin());
        }
        void Update()
        {
            if (!summary) return;
            var s = Session.State;
            summary.text = $"WATER  {s.WaterStoredLiters / 1000:0} kL     ENERGY  {s.PowerSupplyKW:0} kW     BUDGET  INR {s.BudgetINR:N0}";
            time.text = $"DAY {s.Minute / 1440 + 1:00}   {s.Minute / 60 % 24:00}:{s.Minute % 60:00}    •    {Session.Mode.ToString().ToUpperInvariant()}    /    SATISFACTION {s.Satisfaction}    SUSTAINABILITY {s.Sustainability}    /    ILLUSTRATIVE DATA";
            menu.SetActive(Session.Mode == SessionMode.Menu);
            inspector.SetActive(Session.Mode != SessionMode.Menu && Session.Selected);
            if (!Session.Selected) return;
            var f = s.Facilities[Session.Selected.Id];
            title.text = f.Name;
            detail.text = $"{f.Purpose}\n\nWATER        {f.WaterLitersPerMinute:0.0} L/min\nPOWER        {f.PowerKW:0.0} kW\nWASTE        {f.WasteKG:0.0} kg\nOCCUPANCY    {f.Occupants} / {f.Capacity}\n\nSTAFF        {s.StaffAvailable} available\nID   {f.Id}";
        }
        RectTransform Panel(Transform parent, string name, Vector2 min, Vector2 max, Vector2 lo, Vector2 hi)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image)); go.transform.SetParent(parent, false);
            var r = (RectTransform)go.transform; r.anchorMin = min; r.anchorMax = max; r.offsetMin = lo; r.offsetMax = hi;
            go.GetComponent<Image>().color = ink; return r;
        }
        Text Label(Transform parent, string value, int size, Color color, float x, float y, float w, float h)
        {
            var go = new GameObject("Label", typeof(RectTransform), typeof(Text)); go.transform.SetParent(parent, false);
            Place((RectTransform)go.transform, x, y, w, h);
            var t = go.GetComponent<Text>(); t.font = font; t.text = value; t.fontSize = size; t.color = color;
            t.raycastTarget = false; t.horizontalOverflow = HorizontalWrapMode.Wrap; return t;
        }
        void Button(Transform parent, string caption, float x, float y, float w, UnityEngine.Events.UnityAction action)
        {
            var go = new GameObject(caption, typeof(RectTransform), typeof(Image), typeof(Button)); go.transform.SetParent(parent, false);
            Place((RectTransform)go.transform, x, y, w, 44);
            go.GetComponent<Image>().color = new Color(.08f, .25f, .3f);
            var button = go.GetComponent<Button>(); button.onClick.AddListener(action);
            var t = Label(go.transform, caption, 15, cyan, 0, 0, w, 44); t.alignment = TextAnchor.MiddleCenter;
        }
        static void Place(RectTransform r, float x, float y, float w, float h)
        {
            r.anchorMin = r.anchorMax = new Vector2(0, 1); r.pivot = new Vector2(0, 1);
            r.anchoredPosition = new Vector2(x, -y); r.sizeDelta = new Vector2(w, h);
        }
    }
}
