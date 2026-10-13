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
        Text chapterTitleText, objectivesText;
        Text advisorPriorityText, advisorHeadlineText, advisorDetailText;
        Button advisorActionButton, advisorBisButton;

        // Modals
        GameObject menu, inspectorPanel, actionDrawer;
        GameObject chapterModal, endingModal, bisModal, qrModal;
        RawImage qrImage;
        Text qrStatusText, qrUrlText, qrPinText;
        Text chapterModalTitle, chapterModalDetail;
        Text endingModalTitle, endingModalSummary, endingScoresText;
        Text bisModalTitle, bisModalDetail;

        Transform actionButtonContainer;
        readonly List<GameObject> spawnedButtons = new List<GameObject>();

        readonly Color ink = new Color(.02f, .05f, .08f, .95f);
        readonly Color darkPanel = new Color(.04f, .09f, .14f, .92f);
        readonly Color muted = new Color(.56f, .70f, .78f);
        readonly Color cyan = new Color(.16f, .84f, .92f);
        readonly Color gold = new Color(1f, .78f, .22f);
        readonly Color alertRed = new Color(.94f, .22f, .18f);
        readonly Color green = new Color(.18f, .82f, .45f);

        ResourceCategory activeCategory = ResourceCategory.Water;
        string lastFeedback = "Nominal operations. Select a facility or discipline to execute interventions.";
        float feedbackTimer = 0f;
        ActionKind currentAdvisorAction = ActionKind.RepairLeak;
        BisStandardInfo currentBisInfo = null;

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
            Label(header, "CAMPUS // PULSE 2030", 24, cyan, 22, 12, 380, 28);
            chapterTitleText = Label(header, "CHAPTER 1: THE NORMAL DAY", 12, gold, 24, 44, 420, 20);
            summary = Label(header, "", 15, Color.white, 440, 14, 980, 26);
            timeText = Label(header, "", 13, gold, 440, 44, 980, 22);
            Button(header, "OVERVIEW", 1370, 14, 105, () => Session.Overview());
            Button(header, "SAVE", 1485, 14, 75, () => Session.SaveGame());
            Button(header, "LOAD", 1570, 14, 75, () => Session.LoadGame());
            Button(header, "PHONE [QR]", 1655, 14, 185, () => ToggleQrModal());

            Button(header, "FUTURE LENS [L]", 1370, 48, 140, () => FutureLensManager.Instance?.ToggleFutureLens());
            Button(header, "AUDIO 🔊", 1520, 48, 95, () => ToggleAudio());
            Button(header, "QUALITY ⚡", 1625, 48, 215, () => CycleQuality());

            // 2. Bottom Navigation Bar
            var footer = Panel(root.transform, "Navigation", Vector2.zero, new Vector2(1, 0), new Vector2(20, 20), new Vector2(-20, 75));
            Label(footer, "WASD PAN   Q/E ORBIT   SCROLL ZOOM   F FOCUS   ESC PAUSE", 13, muted, 20, 16, 520, 24);
            feedbackText = Label(footer, "", 13, cyan, 550, 16, 680, 24);

            Button(footer, "CH 1", 1250, 10, 65, () => Session.PlayChapter(CampaignChapter.Chapter1_NormalDay));
            Button(footer, "CH 2", 1325, 10, 65, () => Session.PlayChapter(CampaignChapter.Chapter2_PressureBuilds));
            Button(footer, "CH 3", 1400, 10, 65, () => Session.PlayChapter(CampaignChapter.Chapter3_CampusCrisis));
            Button(footer, "PAUSE / PLAY", 1475, 10, 135, () => Session.TogglePause());
            Button(footer, "RESTART", 1620, 10, 110, () => Session.Restart());

            // 3. Left Telemetry & Campaign Objectives Panel
            var leftPanel = Panel(root.transform, "Telemetry Panel", new Vector2(0, 0), new Vector2(0, 1), new Vector2(20, 90), new Vector2(360, -125));
            Label(leftPanel, "DISCIPLINE FILTERS", 12, cyan, 16, 14, 320, 20);
            CreateCategoryTabs(leftPanel, 38);

            Label(leftPanel, "CHAPTER OBJECTIVES", 12, gold, 16, 95, 320, 20);
            objectivesText = Label(leftPanel, "", 13, Color.white, 16, 120, 320, 115);

            Label(leftPanel, "CRISIS INCIDENTS & ALERTS", 12, alertRed, 16, 245, 320, 20);
            alertsText = Label(leftPanel, "", 13, Color.white, 16, 270, 320, 110);

            Label(leftPanel, "ACTIVE WORK ORDERS", 12, cyan, 16, 390, 320, 20);
            activeOrdersText = Label(leftPanel, "No active operations.", 13, muted, 16, 415, 320, 110);

            // 4. Center-Bottom Campus Pulse Advisor Card
            var advisorPanel = Panel(root.transform, "Advisor Box", new Vector2(0, 0), new Vector2(0, 0), new Vector2(385, 90), new Vector2(980, 240));
            advisorPriorityText = Label(advisorPanel, "CAMPUS PULSE ADVISOR // TELEMETRY DIAGNOSTIC", 11, cyan, 16, 10, 560, 20);
            advisorHeadlineText = Label(advisorPanel, "Nominal Operations", 16, Color.white, 16, 32, 560, 24);
            advisorDetailText = Label(advisorPanel, "Campus subsystems within normal parameters.", 13, muted, 16, 58, 560, 52);

            advisorActionButton = MakeButton(advisorPanel, "APPLY ADVICE", 16, 112, 190, 30, () =>
            {
                if (Session.State.TrySubmitAction(currentAdvisorAction, "advisor", out var ticket, out string reason))
                {
                    TriggerFeedback($"✔ ACCEPTED: {ActionCatalog.Get(currentAdvisorAction).Name}");
                }
                else
                {
                    TriggerFeedback($"❌ {reason}");
                }
            });

            advisorBisButton = MakeButton(advisorPanel, "VIEW BIS STANDARD [IS]", 220, 112, 220, 30, () =>
            {
                if (currentBisInfo != null) ShowBisModal(currentBisInfo);
            });

            // 5. Right Facility Inspector & Action Drawer
            inspectorPanel = Panel(root.transform, "Facility Inspector", new Vector2(1, 0), new Vector2(1, 1), new Vector2(-460, 90), new Vector2(-20, -125)).gameObject;
            var ip = inspectorPanel.transform;
            Label(ip, "FACILITY / TELEMETRY & DECISIONS", 12, cyan, 20, 14, 380, 20);
            inspectorTitle = Label(ip, "Select a facility", 23, Color.white, 20, 42, 400, 32);
            inspectorDetail = Label(ip, "", 14, muted, 20, 78, 400, 185);
            Button(ip, "FOCUS CAMERA [F]", 20, 270, 400, () => Session.FocusSelected());

            Label(ip, "AVAILABLE INTERVENTIONS", 13, gold, 20, 326, 400, 22);
            actionDrawer = new GameObject("Action Drawer", typeof(RectTransform));
            actionDrawer.transform.SetParent(ip, false);
            var adr = (RectTransform)actionDrawer.transform;
            adr.anchorMin = new Vector2(0, 0); adr.anchorMax = new Vector2(1, 0);
            adr.pivot = new Vector2(0, 0);
            adr.offsetMin = new Vector2(20, 15); adr.offsetMax = new Vector2(-20, 360);
            actionButtonContainer = actionDrawer.transform;

            // 6. Modals
            BuildWelcomeModal(root.transform);
            BuildChapterModal(root.transform);
            BuildEndingModal(root.transform);
            BuildBisModal(root.transform);
            BuildQrModal(root.transform);

            RebuildActionButtons();
        }

        void BuildWelcomeModal(Transform parent)
        {
            menu = Panel(parent, "Welcome", new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(-360, -230), new Vector2(360, 230)).gameObject;
            Label(menu.transform, "ONE CAMPUS.\nFOUR CRISES. EVERY DECISION COUNTS.", 26, Color.white, 35, 30, 650, 70);
            Label(menu.transform, "Manage water, power, waste, and student crowds in real-time.\n\n• Chapter 1: The Normal Day — Discover baseline leaks, idle loads, and routines.\n• Chapter 2: The Pressure Builds — Navigate major symposium crowds and peak grid strain.\n• Chapter 3: The Campus Crisis — Resolve critical pipeline bursts and substation cascades.\n• Campus Pulse Advisor highlights live hazards with Indian Standards (BIS) insights.", 15, muted, 35, 110, 650, 190);
            Button(menu.transform, "ENTER DIGITAL TWIN CAMPUS  →", 35, 345, 650, () => Session.Begin());
        }

        void BuildChapterModal(Transform parent)
        {
            chapterModal = Panel(parent, "ChapterModal", new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(-340, -180), new Vector2(340, 180)).gameObject;
            chapterModal.SetActive(false);
            chapterModalTitle = Label(chapterModal.transform, "CHAPTER COMPLETE", 24, gold, 30, 25, 620, 32);
            chapterModalDetail = Label(chapterModal.transform, "Objectives fulfilled successfully.", 15, Color.white, 30, 68, 620, 160);
            Button(chapterModal.transform, "PROCEED TO NEXT CHAPTER  →", 30, 250, 620, () =>
            {
                chapterModal.SetActive(false);
                Session.AdvanceChapter();
            });
        }

        void BuildEndingModal(Transform parent)
        {
            endingModal = Panel(parent, "EndingModal", new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(-420, -280), new Vector2(420, 280)).gameObject;
            endingModal.SetActive(false);
            endingModalTitle = Label(endingModal.transform, "CAMPAIGN COMPLETED", 26, gold, 35, 25, 770, 34);
            endingModalSummary = Label(endingModal.transform, "", 15, Color.white, 35, 68, 770, 95);
            endingScoresText = Label(endingModal.transform, "", 14, cyan, 35, 175, 770, 180);
            Button(endingModal.transform, "REPLAY CAMPAIGN FROM CHAPTER 1  ↺", 35, 410, 770, () =>
            {
                endingModal.SetActive(false);
                Session.PlayChapter(CampaignChapter.Chapter1_NormalDay);
            });
        }

        void BuildBisModal(Transform parent)
        {
            bisModal = Panel(parent, "BisModal", new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(-350, -200), new Vector2(350, 200)).gameObject;
            bisModal.SetActive(false);
            Label(bisModal.transform, "BUREAU OF INDIAN STANDARDS (BIS) // EDUCATIONAL REFERENCE", 12, gold, 30, 20, 640, 22);
            bisModalTitle = Label(bisModal.transform, "IS STANDARD", 22, cyan, 30, 48, 640, 30);
            bisModalDetail = Label(bisModal.transform, "", 15, Color.white, 30, 90, 640, 210);
            Button(bisModal.transform, "DISMISS REFERENCE", 30, 310, 640, () => bisModal.SetActive(false));
        }

        void ShowBisModal(BisStandardInfo info)
        {
            if (info == null) return;
            bisModalTitle.text = info.StandardCode;
            bisModalDetail.text = $"<b>{info.Title}</b>\n\n<b>Institutional Benchmark:</b>\n{info.BenchmarkRule}\n\n<b>Direct Campus Application:</b>\n{info.DirectRelevance}\n\n<i>Note: Indian Standards are presented as educational infrastructure references.</i>";
            bisModal.SetActive(true);
        }

        public void ToggleQrModal()
        {
            if (qrModal != null) qrModal.SetActive(!qrModal.activeSelf);
        }

        void ToggleAudio()
        {
            if (CampusAudioManager.Instance != null)
            {
                CampusAudioManager.Instance.ToggleMute();
                TriggerFeedback(CampusAudioManager.Instance.IsMuted ? "Audio muted" : "Audio unmuted");
            }
        }

        void CycleQuality()
        {
            int levels = QualitySettings.names.Length;
            if (levels > 1)
            {
                int next = (QualitySettings.GetQualityLevel() + 1) % levels;
                QualitySettings.SetQualityLevel(next, true);
                TriggerFeedback($"Quality preset: {QualitySettings.names[next]}");
            }
            else
            {
                TriggerFeedback("Quality: High");
            }
        }

        void BuildQrModal(Transform parent)
        {
            qrModal = Panel(parent, "QrModal", new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(-420, -250), new Vector2(420, 250)).gameObject;
            qrModal.SetActive(false);
            var tr = qrModal.transform;

            Label(tr, "CAMPUS PULSE 2030 // MOBILE QR CONTROLLER", 12, gold, 30, 20, 780, 20);
            Label(tr, "WIRELESS CONTROLLER PAIRING", 22, cyan, 30, 44, 780, 30);

            // Left: QR Code Box
            var qrBox = new GameObject("QR Container", typeof(RectTransform), typeof(RawImage));
            qrBox.transform.SetParent(tr, false);
            var rt = (RectTransform)qrBox.transform;
            Place(rt, 30, 85, 260, 260);
            qrImage = qrBox.GetComponent<RawImage>();

            // Right: Connection Info & Instructions
            qrStatusText = Label(tr, "STATUS: WAITING FOR PHONE SCAN...", 14, alertRed, 315, 85, 480, 24);
            qrUrlText = Label(tr, "URL: Initializing LAN listener...", 12, Color.white, 315, 115, 480, 38);
            qrPinText = Label(tr, "PAIRING PIN: [ ---- ]", 20, gold, 315, 158, 480, 30);

            Label(tr, "<b>PAIRING & CONNECTIVITY GUIDE:</b>\n• Ensure smartphone is on the <b>same Wi-Fi</b> or Laptop Mobile Hotspot.\n• Open standard camera on Android / iOS and scan the QR code above.\n• Controller runs entirely in mobile browser (no app install needed).\n• If needed, allow incoming LAN connection on Windows Firewall (Port 8090).", 12, muted, 315, 198, 480, 95);

            MakeButton(tr, "ROTATE PIN / TOKEN", 315, 305, 220, 34, () =>
            {
                Session.ControllerClient?.RotateSession();
            });

            Button(tr, "DISMISS OVERLAY", 30, 375, 780, () => qrModal.SetActive(false));
        }

        void CreateCategoryTabs(Transform parent, float y)
        {
            var categories = new[] { ResourceCategory.Water, ResourceCategory.Energy, ResourceCategory.Waste, ResourceCategory.Crowd };
            float bw = 75;
            for (int i = 0; i < categories.Length; i++)
            {
                var cat = categories[i];
                var go = new GameObject("Tab " + cat, typeof(RectTransform), typeof(Image), typeof(Button));
                go.transform.SetParent(parent, false);
                Place((RectTransform)go.transform, 16 + i * (bw + 6), y, bw, 30);
                go.GetComponent<Image>().color = new Color(.08f, .22f, .30f);
                var btn = go.GetComponent<Button>();
                btn.onClick.AddListener(() => { activeCategory = cat; RebuildActionButtons(); });
                var t = Label(go.transform, cat.ToString().ToUpperInvariant().Substring(0, 4), 12, cyan, 0, 0, bw, 30);
                t.alignment = TextAnchor.MiddleCenter;
            }
        }

        void Update()
        {
            if (!summary) return;
            var s = Session.State;
            var camp = Session.Campaign;
            var adv = Session.Advisor;

            // Header Telemetry
            summary.text = $"WATER  {s.WaterStoredLiters / 1000:0.0} kL / {s.WaterCapacityLiters / 1000:0} kL     ENERGY  {s.TotalPowerDemandKW:0} kW / {s.TotalPowerSupplyKW:0} kW     BUDGET  INR {s.BudgetINR:N0}     STAFF  {s.StaffAvailable}/{s.StaffTotal}";
            timeText.text = $"DAY {s.Minute / 1440 + 1:00}  •  {s.Minute / 60 % 24:00}:{s.Minute % 60:00}    •    SATISFACTION {s.Satisfaction}%    SUSTAINABILITY {s.Sustainability}%    •    {Session.Mode.ToString().ToUpperInvariant()}";

            if (Session.ControllerClient != null)
            {
                var cc = Session.ControllerClient;
                timeText.text += cc.IsPhoneConnected
                    ? $"    •    <color=#2ecc71>PHONE: CONNECTED ({cc.ConnectedDeviceName})</color>"
                    : "    •    <color=#94a3b8>PHONE: STANDBY [QR]</color>";

                if (qrModal != null && qrModal.activeSelf)
                {
                    if (qrImage != null && cc.QrTexture != null) qrImage.texture = cc.QrTexture;
                    if (qrStatusText != null)
                    {
                        qrStatusText.text = cc.IsPhoneConnected
                            ? $"STATUS: <color=#2ecc71>CONNECTED ({cc.ConnectedDeviceName})</color>"
                            : "STATUS: <color=#e67e22>WAITING FOR PHONE SCAN...</color>";
                    }
                    if (qrUrlText != null) qrUrlText.text = $"LAN URL: {cc.ControllerUrl}";
                    if (qrPinText != null) qrPinText.text = $"PAIRING PIN: [ <color=#f1c40f>{cc.Pin}</color> ]";
                }
            }

            if (camp != null)
            {
                string chName = camp.CurrentChapter switch
                {
                    CampaignChapter.Chapter1_NormalDay => "CH 1: THE NORMAL DAY (DISCOVERY)",
                    CampaignChapter.Chapter2_PressureBuilds => "CH 2: THE PRESSURE BUILDS (SYMPOSIUM)",
                    CampaignChapter.Chapter3_CampusCrisis => "CH 3: THE CAMPUS CRISIS (CASCADING)",
                    _ => "CAMPAIGN COMPLETE"
                };
                chapterTitleText.text = chName;

                // Objectives Tracker
                string objStr = "";
                foreach (var obj in camp.Objectives)
                {
                    string check = obj.IsCompleted ? "<color=#2ecc71>[✔]</color>" : "<color=#e67e22>[ ]</color>";
                    objStr += $"{check} {obj.Title}\n";
                }
                objectivesText.text = objStr;

                // Chapter & Ending Modals
                if (camp.ChapterCompleted && !chapterModal.activeSelf && !camp.CampaignCompleted)
                {
                    chapterModalTitle.text = $"{chName} COMPLETED!";
                    chapterModalDetail.text = $"All core operational milestones achieved.\n\nCurrent Metrics:\n• Budget: INR {s.BudgetINR:N0}\n• Satisfaction: {s.Satisfaction}%\n• Sustainability: {s.Sustainability}%\n• Power Grid Deficit: {s.PowerDeficitKW:0} kW";
                    chapterModal.SetActive(true);
                }

                if (camp.CampaignCompleted && !endingModal.activeSelf && camp.FinalReport != null)
                {
                    var rep = camp.FinalReport;
                    endingModalTitle.text = rep.EndingTitle;
                    endingModalSummary.text = rep.EndingSummary;
                    endingScoresText.text = $"COMPOSITE PERFORMANCE SCORE: <b>{rep.CompositeScore} / 100</b>\n\n• Resource Efficiency: {rep.ResourceEfficiencyScore}%\n• Budget Preservation: {rep.BudgetManagementScore}% (Remaining INR {rep.RemainingBudgetINR:N0})\n• Student Satisfaction: {rep.StudentSatisfactionScore}%\n• Campus Sustainability: {rep.CampusSustainabilityScore}%\n• Operational Resilience: {rep.CampusResilienceScore}%\n• Water Conservation: {rep.WaterSavedPercent:0.0}%\n• Waste Recycled: {rep.WasteRecycledPercent:0.0}%";
                    endingModal.SetActive(true);
                }
            }

            menu.SetActive(Session.Mode == SessionMode.Menu);
            inspectorPanel.SetActive(Session.Mode != SessionMode.Menu);

            // Active Crisis Incidents & Sensor Alerts
            string alerts = "";
            if (camp != null)
            {
                foreach (var inc in camp.Incidents)
                {
                    if (inc.IsActive && !inc.IsResolved)
                        alerts += $"• 🚨 {inc.Title.ToUpperInvariant()}\n  {inc.Description}\n";
                }
            }
            if (s.MainLeakActive) alerts += "• ⚠️ PIPE LEAK: 85 L/min loss active at Water House!\n";
            if (s.PowerDeficitKW > 2f) alerts += $"• ⚡ GRID DEFICIT: {s.PowerDeficitKW:0} kW short! Pumps halted.\n";
            if (s.TotalOverflowWasteKg > 5f) alerts += $"• 🗑️ BIN OVERFLOW: {s.TotalOverflowWasteKg:0} kg uncollected!\n";
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

            // Campus Pulse Advisor Evaluation
            if (adv != null)
            {
                var advice = adv.Evaluate(s, camp);
                if (advice != null)
                {
                    currentAdvisorAction = advice.SuggestedAction;
                    currentBisInfo = advice.BisReference;

                    Color pColor = advice.Priority switch
                    {
                        AdvisorPriority.Critical => alertRed,
                        AdvisorPriority.Warning => gold,
                        AdvisorPriority.Recommendation => cyan,
                        _ => muted
                    };

                    advisorPriorityText.color = pColor;
                    advisorPriorityText.text = $"CAMPUS PULSE ADVISOR // [{advice.Priority.ToString().ToUpperInvariant()}]";
                    advisorHeadlineText.text = advice.Headline;
                    advisorDetailText.text = $"{advice.Explanation}\n<color=#f39c12>Recommended Action: {advice.SuggestedActionText}</color>";

                    advisorBisButton.gameObject.SetActive(currentBisInfo != null);
                }
            }

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

        Button MakeButton(Transform parent, string caption, float x, float y, float w, float h, UnityEngine.Events.UnityAction action)
        {
            var go = new GameObject(caption, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            Place((RectTransform)go.transform, x, y, w, h);
            go.GetComponent<Image>().color = new Color(.08f, .25f, .34f);
            var button = go.GetComponent<Button>();
            button.onClick.AddListener(action);
            var t = Label(go.transform, caption, 13, cyan, 0, 0, w, h);
            t.alignment = TextAnchor.MiddleCenter;
            return button;
        }

        void Button(Transform parent, string caption, float x, float y, float w, UnityEngine.Events.UnityAction action)
        {
            MakeButton(parent, caption, x, y, w, 40, action);
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
