using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using CampusPulse.Simulation;

namespace CampusPulse
{
    /// <summary>
    /// Bridges the Windows Unity game with the local Node.js mobile controller relay.
    /// Handles pairing state, incoming phone commands, QR texture generation, and live telemetry streaming.
    /// </summary>
    public sealed class CampusControllerClient : MonoBehaviour
    {
        public CampusSession Session;
        public int ServerPort = 8090;
        public string ServerHost = "127.0.0.1";

        public string LanIp { get; private set; } = "127.0.0.1";
        public string Pin { get; private set; } = "----";
        public string Token { get; private set; } = "";
        public string ControllerUrl { get; private set; } = "";
        public bool IsPhoneConnected { get; private set; } = false;
        public string ConnectedDeviceName { get; private set; } = "None";
        public Texture2D QrTexture { get; private set; }

        public Vector2 MobilePan { get; private set; }
        public float MobileOrbit { get; private set; }
        public float MobileZoom { get; private set; }
        public bool MobileReset { get; private set; }

        public event Action<bool> ConnectionStateChanged;
        public event Action<string> SessionInfoUpdated;

        ClientWebSocket ws;
        CancellationTokenSource cts;
        bool isConnecting = false;
        float telemetryTimer = 0f;

        void Start()
        {
            if (!Session) Session = FindFirstObjectByType<CampusSession>();
            StartCoroutine(PollSessionInfoRoutine());
            StartCoroutine(ConnectWebSocketRoutine());
        }

        void OnDestroy()
        {
            cts?.Cancel();
            ws?.Dispose();
        }

        public void RotateSession()
        {
            StartCoroutine(PostRotateSessionRoutine());
        }

        IEnumerator PollSessionInfoRoutine()
        {
            while (true)
            {
                using (var req = UnityWebRequest.Get($"http://{ServerHost}:{ServerPort}/api/session"))
                {
                    req.timeout = 2;
                    yield return req.SendWebRequest();

                    if (req.result == UnityWebRequest.Result.Success)
                    {
                        try
                        {
                            var json = req.downloadHandler.text;
                            ParseSessionJson(json);
                        }
                        catch (Exception ex)
                        {
                            Debug.LogWarning("[Controller] Parse session error: " + ex.Message);
                        }
                    }
                }
                yield return new WaitForSeconds(4f);
            }
        }

        IEnumerator PostRotateSessionRoutine()
        {
            using (var req = UnityWebRequest.Post($"http://{ServerHost}:{ServerPort}/api/rotate-session", "", "application/json"))
            {
                req.timeout = 2;
                yield return req.SendWebRequest();
                if (req.result == UnityWebRequest.Result.Success)
                {
                    ParseSessionJson(req.downloadHandler.text);
                }
            }
        }

        void ParseSessionJson(string json)
        {
            int ipIdx = json.IndexOf("\"lanIp\":\"", StringComparison.Ordinal);
            int pinIdx = json.IndexOf("\"pin\":\"", StringComparison.Ordinal);
            int tokIdx = json.IndexOf("\"token\":\"", StringComparison.Ordinal);
            int urlIdx = json.IndexOf("\"url\":\"", StringComparison.Ordinal);

            if (ipIdx >= 0 && pinIdx >= 0 && tokIdx >= 0 && urlIdx >= 0)
            {
                string ip = ExtractJsonString(json, ipIdx + 9);
                string pin = ExtractJsonString(json, pinIdx + 7);
                string tok = ExtractJsonString(json, tokIdx + 9);
                string url = ExtractJsonString(json, urlIdx + 7);

                if (url != ControllerUrl || pin != Pin)
                {
                    LanIp = ip;
                    Pin = pin;
                    Token = tok;
                    ControllerUrl = url;

                    // Generate clean scannable QR texture
                    try
                    {
                        QrTexture = QrCodeGenerator.GenerateTexture(ControllerUrl, scale: 8, quietZone: 4);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning("[Controller] QR generation failed: " + ex.Message);
                    }

                    SessionInfoUpdated?.Invoke(ControllerUrl);
                }
            }
        }

        string ExtractJsonString(string src, int start)
        {
            int end = src.IndexOf('"', start);
            return end > start ? src.Substring(start, end - start) : "";
        }

        IEnumerator ConnectWebSocketRoutine()
        {
            while (true)
            {
                if ((ws == null || ws.State != WebSocketState.Open) && !isConnecting)
                {
                    isConnecting = true;
                    _ = ConnectWebSocketAsync();
                }
                yield return new WaitForSeconds(3f);
            }
        }

        async Task ConnectWebSocketAsync()
        {
            cts = new CancellationTokenSource();
            ws = new ClientWebSocket();
            var uri = new Uri($"ws://{ServerHost}:{ServerPort}/ws/unity");

            try
            {
                await ws.ConnectAsync(uri, cts.Token);
                isConnecting = false;
                Debug.Log("[Controller] Unity connected to Relay WebSocket.");
                _ = ReceiveLoopAsync();
            }
            catch
            {
                isConnecting = false;
            }
        }

        async Task ReceiveLoopAsync()
        {
            var buffer = new byte[8192];
            while (ws != null && ws.State == WebSocketState.Open && !cts.IsCancellationRequested)
            {
                try
                {
                    var result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), cts.Token);
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", cts.Token);
                        break;
                    }
                    else if (result.MessageType == WebSocketMessageType.Text)
                    {
                        var text = Encoding.UTF8.GetString(buffer, 0, result.Count);
                        ProcessIncomingMessage(text);
                    }
                }
                catch
                {
                    break;
                }
            }
            ResetControls();
        }

        void ProcessIncomingMessage(string json)
        {
            try
            {
                if (json.Contains("\"type\":\"session_info\""))
                {
                    ParseSessionJson(json);
                }
                else if (json.Contains("\"type\":\"phone_connected\""))
                {
                    IsPhoneConnected = true;
                    int dIdx = json.IndexOf("\"device\":\"", StringComparison.Ordinal);
                    ConnectedDeviceName = dIdx >= 0 ? ExtractJsonString(json, dIdx + 10) : "Mobile Browser";
                    ConnectionStateChanged?.Invoke(true);
                }
                else if (json.Contains("\"type\":\"phone_disconnected\""))
                {
                    IsPhoneConnected = false;
                    ConnectedDeviceName = "None";
                    ResetControls();
                    ConnectionStateChanged?.Invoke(false);
                }
                else if (json.Contains("\"type\":\"camera\""))
                {
                    MobilePan = new Vector2(
                        ExtractFloat(json, "\"panX\":", 0f),
                        ExtractFloat(json, "\"panY\":", 0f)
                    );
                    MobileOrbit = ExtractFloat(json, "\"orbit\":", 0f);
                    MobileZoom = ExtractFloat(json, "\"zoom\":", 0f);
                    MobileReset = json.Contains("\"reset\":true");
                }
                else if (json.Contains("\"type\":\"select_facility\""))
                {
                    string facId = ExtractString(json, "\"facilityId\":\"", "\"");
                    SelectFacilityById(facId);
                }
                else if (json.Contains("\"type\":\"focus_selected\""))
                {
                    Session?.FocusSelected();
                }
                else if (json.Contains("\"type\":\"overview\""))
                {
                    Session?.Overview();
                }
                else if (json.Contains("\"type\":\"pause\""))
                {
                    Session?.TogglePause();
                }
                else if (json.Contains("\"type\":\"advance_chapter\""))
                {
                    Session?.AdvanceChapter();
                }
                else if (json.Contains("\"type\":\"restart\""))
                {
                    Session?.Restart();
                }
                else if (json.Contains("\"type\":\"action\""))
                {
                    string kindStr = ExtractString(json, "\"actionKind\":\"", "\"");
                    string target = ExtractString(json, "\"targetFacility\":\"", "\"");
                    ExecuteMobileAction(kindStr, target);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[Controller] Process message failed: " + ex.Message);
            }
        }

        void SelectFacilityById(string id)
        {
            if (Session == null || Session.Facilities == null) return;
            foreach (var f in Session.Facilities)
            {
                if (f.Id.Equals(id, StringComparison.OrdinalIgnoreCase))
                {
                    Session.Select(f);
                    return;
                }
            }
        }

        void ExecuteMobileAction(string kindStr, string target)
        {
            if (Session == null || Session.State == null) return;
            ActionKind kind;
            if (!Enum.TryParse<ActionKind>(kindStr, true, out kind))
            {
                kind = kindStr.ToLowerInvariant() switch
                {
                    "installled" or "led" => ActionKind.LEDs,
                    "installlowflow" => ActionKind.LowFlow,
                    "rationwater" => ActionKind.Ration,
                    "adjustpumping" => ActionKind.SchedulePump,
                    "shiftlabloads" => ActionKind.RescheduleLabs,
                    "activatebackupdiesel" => ActionKind.Backup,
                    "shedunnecessaryload" => ActionKind.ReduceLoad,
                    "dispatchcollection" => ActionKind.CollectWaste,
                    "segregationdrive" => ActionKind.Segregation,
                    "expandbincapacity" => ActionKind.MoreBins,
                    "staggerclasses" => ActionKind.Stagger,
                    "addcanteencounter" => ActionKind.ExtraCounter,
                    "diverttogarden" => ActionKind.Reroute,
                    _ => (ActionKind)(-1)
                };
            }

            if ((int)kind >= 0)
            {
                if (Session.State.TrySubmitAction(kind, target, out var ticket, out string reason))
                {
                    SendFeedbackToPhone($"✔ ORDER ACCEPTED: {ActionCatalog.Get(kind).Name}");
                    Session.Hud?.TriggerFeedback($"✔ PHONE DISPATCH: {ActionCatalog.Get(kind).Name}");
                }
                else
                {
                    SendFeedbackToPhone($"❌ REJECTED: {reason}");
                    Session.Hud?.TriggerFeedback($"❌ PHONE DISPATCH: {reason}");
                }
            }
        }

        public void SendFeedbackToPhone(string text)
        {
            if (ws != null && ws.State == WebSocketState.Open)
            {
                string msg = $"{{\"type\":\"feedback\",\"text\":\"{EscapeJson(text)}\"}}";
                var bytes = Encoding.UTF8.GetBytes(msg);
                _ = ws.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);
            }
        }

        public CameraCommand ReadMobileCommand()
        {
            return new CameraCommand
            {
                Pan = MobilePan,
                Orbit = MobileOrbit,
                Zoom = MobileZoom,
                Reset = MobileReset
            };
        }

        void ResetControls()
        {
            MobilePan = Vector2.zero;
            MobileOrbit = 0f;
            MobileZoom = 0f;
            MobileReset = false;
        }

        void Update()
        {
            // Stream telemetry to phone
            if (IsPhoneConnected && ws != null && ws.State == WebSocketState.Open && Session != null && Session.State != null)
            {
                telemetryTimer += Time.unscaledDeltaTime;
                if (telemetryTimer >= 0.2f) // 5 Hz
                {
                    telemetryTimer = 0f;
                    StreamTelemetry();
                }
            }
        }

        void StreamTelemetry()
        {
            var s = Session.State;
            var camp = Session.Campaign;
            var sb = new StringBuilder();
            sb.Append("{");
            sb.Append("\"type\":\"telemetry\",");
            sb.Append($"\"budget\":{s.BudgetINR},");
            sb.Append($"\"powerDemand\":{s.TotalPowerDemandKW:0.0},");
            sb.Append($"\"powerSupply\":{s.TotalPowerSupplyKW:0.0},");
            sb.Append($"\"waterStored\":{s.WaterStoredLiters:0.0},");
            sb.Append($"\"satisfaction\":{s.Satisfaction},");
            sb.Append($"\"sustainability\":{s.Sustainability},");
            sb.Append($"\"chapterTitle\":\"{EscapeJson(camp?.CurrentChapter.ToString() ?? "Campaign")}\"");
            sb.Append("}");

            var bytes = Encoding.UTF8.GetBytes(sb.ToString());
            _ = ws.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);
        }

        float ExtractFloat(string json, string key, float def)
        {
            int idx = json.IndexOf(key, StringComparison.Ordinal);
            if (idx < 0) return def;
            idx += key.Length;
            int end = idx;
            while (end < json.Length && (char.IsDigit(json[end]) || json[end] == '-' || json[end] == '.')) end++;
            string sub = json.Substring(idx, end - idx);
            return float.TryParse(sub, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float v) ? v : def;
        }

        string ExtractString(string json, string prefix, string suffix)
        {
            int idx = json.IndexOf(prefix, StringComparison.Ordinal);
            if (idx < 0) return "";
            idx += prefix.Length;
            int end = json.IndexOf(suffix, idx, StringComparison.Ordinal);
            return end > idx ? json.Substring(idx, end - idx) : "";
        }

        string EscapeJson(string s) => s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n");
    }
}
