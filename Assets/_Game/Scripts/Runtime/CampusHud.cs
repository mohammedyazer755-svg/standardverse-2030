using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using CampusPulse.Simulation;

namespace CampusPulse
{
    public sealed class CampusHud : MonoBehaviour
    {
        public CampusSession Session;
        Font font;
        Text summary, timeText, alertsText, feedbackText;
        Text inspectorTitle, inspectorDetail, activeOrdersText;
        GameObject menu, inspectorPanel, actionDrawer;
        Transform actionButtonContainer;
        readonly List<GameObject> spawnedButtons = new List<GameObject>();

        readonly Color ink = new Color(.02f, .05f, .08f, .95f);
        readonly Color darkPanel = new Color(.04f, .09f, .14f, .92f);
        readonly Color muted = new Color(.56f, .70f, .78f);
        readonly Color cyan = new Color(.16f, .84f, .92f);
        readonly Color gold = new Color(1f, .78f, .22f);
        readonly Color alertRed = new Color(.94f, .22f, .18f);

        ResourceCategory activeCategory = ResourceCategory.Water;
        string lastFeedback = "Nominal operations. Select a facility or discipline to execute interventions.";
        float feedbackTimer = 0f;

        void Start()
        {
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var root = new GameObject("Campus Interface", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            root.transform.SetParent(transform, false);
            root.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = .5f;

            if (!EventSystem.current)
                new GameObject("Event System", typeof(EventSystem), typeof(InputSystemUIInputModule));

            // 1. Top Header (Command Center Bar)
            var header = Panel(root.transform, "Header", new Vector2(0, 1), new Vector2(1, 1), new Vector2(20, -115), new Vector2(-20, -20));
            Label(header, "CAMPUS // PULSE 2030", 25, cyan, 22, 14, 420, 32);
            Label(header, "DIGITAL TWIN OPERATIONS CENTER  •  URP REALTIME", 12, muted, 24, 48, 520, 22);
            summary = Label(header, "", 16, Color.white, 470, 15, 1040, 28);
            timeText = Label(header, "", 13, gold, 470, 48, 1040, 22);
            Button(header, "OVERVIEW", 1620, 16, 180, () => Session.Overview());

            // 2. Bottom Navigation & Speed Controls
            var footer = Panel(root.transform, "Navigation", Vector2.zero, new Vector2(1, 0), new Vector2(20, 20), new Vector2(-20, 80));
            Label(footer, "WASD  PAN    Q / E  ORBIT    SCROLL  ZOOM    F  FOCUS    HOME  RESET    ESC  PAUSE", 14, muted, 20, 18, 920, 26);
            feedbackText = Label(footer, "", 13, cyan, 960, 18, 540, 26);
            Button(footer, "PAUSE / PLAY", 1520, 8, 170, () => Session.TogglePause());
            Button(footer, "RESTART", 1705, 8, 155, () => Session.Restart());

            // 3. Left Telemetry & Discipline Panel
            var leftPanel = Panel(root.transform, "Telemetry Panel", new Vector2(0, 0), new Vector2(0, 1), new Vector2(20, 95), new Vector2(340, -130));
            Label(leftPanel, "DISCIPLINE FILTERS", 12, cyan, 16, 16, 280, 22);
            CreateCategoryTabs(leftPanel, 44);
            Label(leftPanel, "LIVE SENSOR ALERTS", 12, gold, 16, 110, 280, 22);
            alertsText = Label(leftPanel, "", 14, Color.white, 16, 138, 290, 140);
            Label(leftPanel, "ACTIVE OPERATIONS", 12, cyan, 16, 290, 280, 22);
            activeOrdersText = Label(leftPanel, "No active operations.", 13, muted, 16, 318, 290, 180);

            // 4. Right Facility Inspector & Action Drawer
            inspectorPanel = Panel(root.transform, "Facility Inspector", new Vector2(1, 0), new Vector2(1, 1), new Vector2(-460, 95), new Vector2(-20, -130)).gameObject;
            var ip = inspectorPanel.transform;
            Label(ip, "FACILITY / TELEMETRY & DECISIONS", 12, cyan, 20, 16, 380, 22);
            inspectorTitle = Label(ip, "Select a facility", 24, Color.white, 20, 48, 400, 36);
            inspectorDetail = Label(ip, "", 15, muted, 20, 90, 400, 190);
            Button(ip, "FOCUS CAMERA [F]", 20, 290, 400, () => Session.FocusSelected());

            Label(ip, "INTERVENTIONS & WORK ORDERS", 13, gold, 20, 350, 400, 24);
            actionDrawer = new GameObject("Action Drawer", typeof(RectTransform));
            actionDrawer.transform.SetParent(ip, false);
            var adr = (RectTransform)actionDrawer.transform;
            adr.anchorMin = new Vector2(0, 0); adr.anchorMax = new Vector2(1, 0);
            adr.pivot = new Vector2(0, 0);
            adr.offsetMin = new Vector2(20, 15); adr.offsetMax = new Vector2(-20, 340);
            actionButtonContainer = actionDrawer.transform;

            // 5. Welcome Modal (Start Screen)
            menu = Panel(root.transform, "Welcome", new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(-360, -240), new Vector2(360, 240)).gameObject;
            Label(menu.transform, "ONE CAMPUS.\nFOUR CRISES. EVERY DECISION COUNTS.", 28, Color.white, 35, 35, 650, 80);
            Label(menu.transform, "Manage water, power, waste, and student crowds in real-time.\n\n• Repair leaks and prevent reservoir depletion\n• Balance peak electrical demand with rooftop solar\n• Segregate waste and dispatch collection fleets\n• Keep student satisfaction and sustainability high", 17, muted, 35, 125, 650, 180);
            Button(menu.transform, "ENTER DIGITAL TWIN CAMPUS  →", 35, 370, 650, () => Session.Begin());

            RebuildActionButtons();
        }

        void CreateCategoryTabs(Transform parent, float y)
        {
            var categories = new[] { ResourceCategory.Water, ResourceCategory.Energy, ResourceCategory.Waste, ResourceCategory.Crowd };
            float bw = 70;
            for (int i = 0; i < categories.Length; i++)
            {
                var cat = categories[i];
                var go = new GameObject("Tab " + cat, typeof(RectTransform), typeof(Image), typeof(Button));
                go.transform.SetParent(parent, false);
                Place((RectTransform)go.transform, 16 + i * (bw + 5), y, bw, 32);
                go.GetComponent<Image>().color = new Color(.08f, .22f, .30f);
                var btn = go.GetComponent<Button>();
                btn.onClick.AddListener(() => { activeCategory = cat; RebuildActionButtons(); });
                var t = Label(go.transform, cat.ToString().ToUpperInvariant().Substring(0, 4), 12, cyan, 0, 0, bw, 32);
                t.alignment = TextAnchor.MiddleCenter;
            }
        }

        void Update()
        {
            if (!summary) return;
            var s = Session.State;

            // Header Telemetry
            summary.text = $"WATER  {s.WaterStoredLiters / 1000:0.0} kL / {s.WaterCapacityLiters / 1000:0} kL     ENERGY  {s.TotalPowerDemandKW:0} kW / {s.TotalPowerSupplyKW:0} kW     BUDGET  INR {s.BudgetINR:N0}     STAFF  {s.StaffAvailable}/{s.StaffTotal}";
            timeText.text = $"DAY {s.Minute / 1440 + 1:00}  •  {s.Minute / 60 % 24:00}:{s.Minute % 60:00}    •    SATISFACTION {s.Satisfaction}%    SUSTAINABILITY {s.Sustainability}%    •    MODE: {Session.Mode.ToString().ToUpperInvariant()}";

            menu.SetActive(Session.Mode == SessionMode.Menu);
            inspectorPanel.SetActive(Session.Mode != SessionMode.Menu);

            // Active Alerts
            string alerts = "";
            if (s.MainLeakActive) alerts += "• ⚠️ LEAK: 85 L/min pipe loss active at Water House!\n";
            if (s.PowerDeficitKW > 2f) alerts += $"• ⚡ CRITICAL: Power deficit ({s.PowerDeficitKW:0} kW)! Pumping halted.\n";
            else if (s.TotalPowerDemandKW > 320f) alerts += "• ⚡ HIGH LOAD: Peak grid consumption in progress.\n";
            if (s.TotalOverflowWasteKg > 5f) alerts += $"• 🗑️ OVERFLOW: Bins overflowing ({s.TotalOverflowWasteKg:0} kg)! Cleanliness drop.\n";
            if (s.Facilities.TryGetValue("canteen", out var cant) && cant.QueueCount > 20) alerts += $"• 👥 CONGESTION: Canteen queue at {cant.QueueCount} students.\n";
            if (string.IsNullOrEmpty(alerts)) alerts = "• All campus subsystems nominal.\n• No emergency conditions detected.";
            alertsText.text = alerts;

            // Active Operations List
            string orders = "";
            foreach (var t in s.Tickets)
            {
                if (t.Active)
                {
                    int rem = Math.Max(0, t.FinishMinute - s.Minute);
                    orders += $"• {ActionCatalog.Get(t.Kind).Name}\n  Remaining: {rem} min ({t.ReservedStaff} staff)\n";
                }
            }
            if (string.IsNullOrEmpty(orders)) orders = "No active work orders.\nStaff available for dispatch.";
            activeOrdersText.text = orders;

            // Feedback Ticker
            if (feedbackTimer > 0) feedbackTimer -= Time.deltaTime;
            else feedbackText.text = lastFeedback;

            // Inspector Details
            if (Session.Selected != null && s.Facilities.TryGetValue(Session.Selected.Id, out var f))
            {
                inspectorTitle.text = f.Name;
                inspectorDetail.text = $"{f.Purpose}\n\nWATER DEMAND   {f.BaseWaterLpm:0.0} L/min\nPOWER DRAW     {f.BasePowerKw:0.0} kW\nWASTE GENERATE {f.BaseWasteKgPerMin:0.0} kg/min\nBIN STATUS     {f.BinCurrentKg:0.0} / {f.BinCapacityKg:0} kg (Overflow: {f.BinOverflowKg:0.0} kg)\nOCCUPANTS      {f.Occupants} / {f.Capacity} (Queue: {f.QueueCount})\n\nFACILITY ID    {f.Id}";
            }
            else
            {
                inspectorTitle.text = $"CAMPUS ATLAS ({activeCategory.ToString().ToUpperInvariant()})";
                inspectorDetail.text = "Select any building in the 3D world to inspect specific infrastructure telemetry, or trigger campus-wide interventions below.\n\nWater storage: 36,000 L\nGrid Capacity: 380 kW\nSolar Array: 120 kW\nStaff Available: " + s.StaffAvailable;
            }
        }

        public void TriggerFeedback(string msg)
        {
            lastFeedback = msg;
            feedbackTimer = 4f;
            if (feedbackText) feedbackText.text = msg;
        }

        void RebuildActionButtons()
        {
            if (!actionButtonContainer) return;
            foreach (var b in spawnedButtons) Destroy(b);
            spawnedButtons.Clear();

            // Find actions relevant to current category or selected facility
            var defs = new List<ActionDefinition>();
            foreach (var d in ActionCatalog.All)
            {
                if (d.Category == activeCategory) defs.Add(d);
            }

            float y = 0;
            float btnH = 68;
            for (int i = 0; i < defs.Count && i < 4; i++)
            {
                var def = defs[i];
                var go = new GameObject("ActionBtn_" + def.Kind, typeof(RectTransform), typeof(Image), typeof(Button));
                go.transform.SetParent(actionButtonContainer, false);
                Place((RectTransform)go.transform, 0, y, 400, btnH);
                go.GetComponent<Image>().color = new Color(.07f, .18f, .26f);

                var btn = go.GetComponent<Button>();
                var targetKind = def.Kind;
                btn.onClick.AddListener(() =>
                {
                    string targetId = Session.Selected ? Session.Selected.Id : targetKind.ToString().ToLowerInvariant();
                    if (Session.State.TrySubmitAction(targetKind, targetId, out var ticket, out string reason))
                    {
                        TriggerFeedback($"✔ ACCEPTED: {def.Name} (INR {def.Cost:N0}, {def.Staff} staff)");
                    }
                    else
                    {
                        TriggerFeedback($"❌ REFUSED: {reason}");
                    }
                });

                Label(go.transform, def.Name, 15, Color.white, 12, 8, 250, 24);
                Label(go.transform, $"INR {def.Cost:N0}   •   {def.Staff} STAFF   •   {def.Duration} MIN", 12, gold, 12, 34, 250, 22);

                var orderBtn = Panel(go.transform, "OrderBadge", new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-110, -18), new Vector2(-10, 18));
                orderBtn.GetComponent<Image>().color = new Color(.12f, .42f, .50f);
                var orderLabel = Label(orderBtn, "EXECUTE", 13, cyan, 0, 0, 100, 36);
                orderLabel.alignment = TextAnchor.MiddleCenter;

                spawnedButtons.Add(go);
                y += btnH + 8;
            }
        }

        RectTransform Panel(Transform parent, string name, Vector2 min, Vector2 max, Vector2 lo, Vector2 hi)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var r = (RectTransform)go.transform;
            r.anchorMin = min; r.anchorMax = max;
            r.offsetMin = lo; r.offsetMax = hi;
            go.GetComponent<Image>().color = darkPanel;
            return r;
        }

        Text Label(Transform parent, string value, int size, Color color, float x, float y, float w, float h)
        {
            var go = new GameObject("Label", typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            Place((RectTransform)go.transform, x, y, w, h);
            var t = go.GetComponent<Text>();
            t.font = font;
            t.text = value;
            t.fontSize = size;
            t.color = color;
            t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            return t;
        }

        void Button(Transform parent, string caption, float x, float y, float w, UnityEngine.Events.UnityAction action)
        {
            var go = new GameObject(caption, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            Place((RectTransform)go.transform, x, y, w, 44);
            go.GetComponent<Image>().color = new Color(.08f, .25f, .34f);
            var button = go.GetComponent<Button>();
            button.onClick.AddListener(action);
            var t = Label(go.transform, caption, 14, cyan, 0, 0, w, 44);
            t.alignment = TextAnchor.MiddleCenter;
        }

        static void Place(RectTransform r, float x, float y, float w, float h)
        {
            r.anchorMin = r.anchorMax = new Vector2(0, 1);
            r.pivot = new Vector2(0, 1);
            r.anchoredPosition = new Vector2(x, -y);
            r.sizeDelta = new Vector2(w, h);
        }
    }
}
