// Campus Pulse 2030 — Mobile Controller Client
(() => {
    'use strict';

    // State
    let ws = null;
    let isConnected = false;
    let isPaired = false;
    let currentPin = '';
    let currentToken = '';
    let selectedFacilityId = 'academic';
    let pendingAction = null;
    let heartbeatTimer = null;
    let panLoopTimer = null;

    // Joystick State
    let joyActive = false;
    let joyPanX = 0;
    let joyPanY = 0;
    const JOY_MAX_RADIUS = 45;

    // Button hold states
    let orbitState = 0;
    let zoomState = 0;

    // DOM Elements
    const connPill = document.getElementById('connPill');
    const connLabel = document.getElementById('connLabel');
    const chapterText = document.getElementById('chapterText');
    const statBudget = document.getElementById('statBudget');
    const statPower = document.getElementById('statPower');
    const statWater = document.getElementById('statWater');
    const statSat = document.getElementById('statSat');

    const joyBoundary = document.getElementById('joyBoundary');
    const joyKnob = document.getElementById('joyKnob');

    const btnOrbitL = document.getElementById('btnOrbitL');
    const btnOrbitR = document.getElementById('btnOrbitR');
    const btnZoomIn = document.getElementById('btnZoomIn');
    const btnZoomOut = document.getElementById('btnZoomOut');
    const btnOverview = document.getElementById('btnOverview');
    const btnFocus = document.getElementById('btnFocus');
    const btnPause = document.getElementById('btnPause');
    const btnNextCh = document.getElementById('btnNextCh');

    const facilityStrip = document.getElementById('facilityStrip');
    const actionSheet = document.getElementById('actionSheet');
    const facType = document.getElementById('facType');
    const facName = document.getElementById('facName');
    const facOcc = document.getElementById('facOcc');
    const facPower = document.getElementById('facPower');
    const facWater = document.getElementById('facWater');
    const facWaste = document.getElementById('facWaste');
    const actionBtnList = document.getElementById('actionBtnList');
    const btnDismissSheet = document.getElementById('btnDismissSheet');

    const confirmModal = document.getElementById('confirmModal');
    const confirmDesc = document.getElementById('confirmDesc');
    const confirmMeta = document.getElementById('confirmMeta');
    const btnCancelAction = document.getElementById('btnCancelAction');
    const btnConfirmAction = document.getElementById('btnConfirmAction');

    const pairingModal = document.getElementById('pairingModal');
    const pinInput = document.getElementById('pinInput');
    const btnSubmitPin = document.getElementById('btnSubmitPin');
    const pinError = document.getElementById('pinError');
    const feedbackToast = document.getElementById('feedbackToast');

    // Default facilities catalog
    const FACILITIES = [
        { id: 'academic', name: 'Academic Block', type: 'Education & Classrooms' },
        { id: 'admin', name: 'Admin HQ', type: 'Campus Operations' },
        { id: 'library', name: 'Central Library', type: 'Study Center' },
        { id: 'labs', name: 'Research Labs', type: 'High-Demand Facility' },
        { id: 'canteen', name: 'Student Canteen', type: 'Dining & Services' },
        { id: 'hostel', name: 'Hostel Cluster', type: 'Residential Block' },
        { id: 'sports', name: 'Sports Complex', type: 'Recreation' },
        { id: 'auditorium', name: 'Auditorium', type: 'Events & Assemblies' },
        { id: 'water', name: 'Water Towers', type: 'Utility Pumping' },
        { id: 'solar', name: 'Solar Farm', type: 'Clean Generation' },
        { id: 'waste', name: 'Recycling Hub', type: 'Waste Processing' },
        { id: 'transit', name: 'Bus Terminal', type: 'Transit Point' },
        { id: 'innovation', name: 'Innovation Hub', type: 'Incubation' }
    ];

    // Interventions Catalog
    const ACTIONS = [
        { kind: 'RepairLeak', name: 'Repair Main Pipe Leak', cost: '₹20,000', staff: '2 Slots', cat: 'water', desc: 'Seal damaged underground supply lines to prevent 85 L/min water loss.' },
        { kind: 'InstallLowFlow', name: 'Install Low-Flow Aerators', cost: '₹35,000', staff: '1 Slot', cat: 'water', desc: 'Retrofit restrooms with water-saving aerators reducing demand by 30%.' },
        { kind: 'RationWater', name: 'Emergency Water Rationing', cost: '₹0', staff: '1 Slot', cat: 'water', desc: 'Restrict non-essential facility supply during critical shortages.' },
        { kind: 'AdjustPumping', name: 'Optimize Pumping Schedule', cost: '₹5,000', staff: '1 Slot', cat: 'water', desc: 'Shift water pumping to match solar generation hours.' },

        { kind: 'ShiftLabLoads', name: 'Reschedule Lab Heavy Loads', cost: '₹5,000', staff: '1 Slot', cat: 'power', desc: 'Move autoclave and furnace operations to off-peak periods.' },
        { kind: 'InstallLed', name: 'LED Efficiency Retrofit', cost: '₹40,000', staff: '2 Slots', cat: 'power', desc: 'Upgrade building luminaires cutting base electrical load by 25%.' },
        { kind: 'ActivateBackupDiesel', name: 'Activate Diesel Generator', cost: '₹15,000', staff: '1 Slot', cat: 'power', desc: 'Provide 150 kW emergency backup to restore critical campus power.' },
        { kind: 'ShedUnnecessaryLoad', name: 'Campus Load Shedding', cost: '₹0', staff: '1 Slot', cat: 'power', desc: 'De-energize ornamental lighting and decorative cooling loads.' },

        { kind: 'DispatchCollection', name: 'Dispatch Waste Truck Route', cost: '₹15,000', staff: '2 Slots', cat: 'waste', desc: 'Send collection vehicle to empty full bins and eliminate campus litter.' },
        { kind: 'SegregationDrive', name: 'Enforce 3-Bin Segregation', cost: '₹20,000', staff: '1 Slot', cat: 'waste', desc: 'Deploy educational student ambassadors to raise sorting to 75%.' },
        { kind: 'ExpandBinCapacity', name: 'High-Capacity Bin Upgrade', cost: '₹25,000', staff: '2 Slots', cat: 'waste', desc: 'Replace standard receptacles with 100 kg compacting bins.' },

        { kind: 'StaggerClasses', name: 'Stagger Lecture Schedules', cost: '₹10,000', staff: '1 Slot', cat: 'crowd', desc: 'Desynchronize course dismissal times to eliminate corridor bottlenecks.' },
        { kind: 'AddCanteenCounter', name: 'Open 2nd Canteen Counter', cost: '₹30,000', staff: '2 Slots', cat: 'crowd', desc: 'Double food service speed during peak lunch rush.' },
        { kind: 'DivertToGarden', name: 'Open Outdoor Green Court', cost: '₹5,000', staff: '1 Slot', cat: 'crowd', desc: 'Redirect overflow queues to open-air shaded gazebos.' }
    ];

    function vibrate(ms = 15) {
        if ('vibrate' in navigator) {
            try { navigator.vibrate(ms); } catch (e) {}
        }
    }

    function showToast(msg) {
        feedbackToast.textContent = msg;
        feedbackToast.classList.add('show');
        setTimeout(() => feedbackToast.classList.remove('show'), 3000);
    }

    // 1. Initial Setup & Pairing
    function init() {
        const params = new URLSearchParams(window.location.search);
        currentToken = params.get('token') || '';
        currentPin = params.get('pin') || '';

        buildFacilityStrip();
        renderActionsForFacility(selectedFacilityId);
        setupJoystick();
        setupButtonListeners();

        if (currentToken || currentPin) {
            connectWebSocket();
        } else {
            pairingModal.classList.add('active');
        }
    }

    btnSubmitPin.addEventListener('click', () => {
        const val = pinInput.value.trim();
        if (val.length === 4) {
            currentPin = val;
            pairingModal.classList.remove('active');
            connectWebSocket();
        } else {
            pinError.textContent = 'Please enter a valid 4-digit PIN.';
        }
    });

    // 2. WebSocket Connection
    function connectWebSocket() {
        const protocol = window.location.protocol === 'https:' ? 'wss:' : 'ws:';
        const wsUrl = `${protocol}//${window.location.host}/ws/phone?token=${encodeURIComponent(currentToken)}&pin=${encodeURIComponent(currentPin)}&device=${encodeURIComponent(getDeviceName())}`;

        connLabel.textContent = 'CONNECTING...';
        connPill.className = 'connection-pill';

        try {
            ws = new WebSocket(wsUrl);
        } catch (e) {
            console.error('WS Error:', e);
            scheduleReconnect();
            return;
        }

        ws.onopen = () => {
            isConnected = true;
            connLabel.textContent = 'PAIRED';
            connPill.className = 'connection-pill connected';
            vibrate(30);
            showToast('✔ Connected to Campus Pulse 2030');

            // Start heartbeat
            clearInterval(heartbeatTimer);
            heartbeatTimer = setInterval(() => {
                if (ws && ws.readyState === WebSocket.OPEN) {
                    ws.send(JSON.stringify({ type: 'heartbeat', timestamp: Date.now() }));
                }
            }, 2000);
        };

        ws.onmessage = (evt) => {
            try {
                const msg = JSON.parse(evt.data);
                handleIncomingMessage(msg);
            } catch (e) {}
        };

        ws.onclose = () => {
            isConnected = false;
            isPaired = false;
            connLabel.textContent = 'DISCONNECTED';
            connPill.className = 'connection-pill';
            clearInterval(heartbeatTimer);
            scheduleReconnect();
        };

        ws.onerror = () => {
            ws.close();
        };
    }

    function scheduleReconnect() {
        setTimeout(() => {
            if (!isConnected && (currentToken || currentPin)) {
                connectWebSocket();
            }
        }, 3000);
    }

    function sendCommand(cmd) {
        if (ws && ws.readyState === WebSocket.OPEN) {
            ws.send(JSON.stringify(cmd));
        }
    }

    function getDeviceName() {
        const ua = navigator.userAgent;
        if (/iPhone/i.test(ua)) return 'iPhone';
        if (/iPad/i.test(ua)) return 'iPad';
        if (/Android/i.test(ua)) return 'Android Device';
        return 'Mobile Browser';
    }

    // 3. Incoming Telemetry Handling
    function handleIncomingMessage(msg) {
        if (msg.type === 'paired') {
            isPaired = true;
            connLabel.textContent = 'PAIRED';
            connPill.className = 'connection-pill connected';
        } else if (msg.type === 'pair_failed') {
            pairingModal.classList.add('active');
            pinError.textContent = msg.reason || 'Invalid PIN/Token.';
        } else if (msg.type === 'telemetry') {
            updateTelemetry(msg);
        } else if (msg.type === 'feedback') {
            showToast(msg.text || '');
        }
    }

    function updateTelemetry(data) {
        if (data.budget !== undefined) statBudget.textContent = `₹${data.budget.toLocaleString()}`;
        if (data.powerDemand !== undefined) statPower.textContent = `${Math.round(data.powerDemand)} kW`;
        if (data.waterStored !== undefined) statWater.textContent = `${(data.waterStored / 1000).toFixed(1)} kL`;
        if (data.satisfaction !== undefined) statSat.textContent = `${Math.round(data.satisfaction)}%`;
        if (data.chapterTitle) chapterText.textContent = data.chapterTitle;

        // If telemetry contains facility states, update metrics
        if (data.facilities && data.facilities[selectedFacilityId]) {
            const f = data.facilities[selectedFacilityId];
            facOcc.textContent = `${f.occupancy || 0}`;
            facPower.textContent = `${Math.round(f.power || 0)} kW`;
            facWater.textContent = `${Math.round(f.water || 0)} L/m`;
            facWaste.textContent = `${Math.round(f.waste || 0)} kg`;
        }
    }

    // 4. Joystick Control
    function setupJoystick() {
        let startX = 0, startY = 0;

        function onTouchStart(e) {
            e.preventDefault();
            const touch = e.touches ? e.touches[0] : e;
            const rect = joyBoundary.getBoundingClientRect();
            startX = rect.left + rect.width / 2;
            startY = rect.top + rect.height / 2;
            joyActive = true;
            updateKnob(touch.clientX, touch.clientY);
            startPanLoop();
            vibrate(10);
        }

        function onTouchMove(e) {
            if (!joyActive) return;
            e.preventDefault();
            const touch = e.touches ? e.touches[0] : e;
            updateKnob(touch.clientX, touch.clientY);
        }

        function onTouchEnd(e) {
            if (!joyActive) return;
            e.preventDefault();
            joyActive = false;
            joyPanX = 0;
            joyPanY = 0;
            joyKnob.style.transform = `translate(0px, 0px)`;
            stopPanLoop();
            sendCommand({ type: 'camera', pan: { x: 0, y: 0 }, orbit: 0, zoom: 0, reset: false });
        }

        function updateKnob(clientX, clientY) {
            const dx = clientX - startX;
            const dy = clientY - startY;
            const dist = Math.sqrt(dx * dx + dy * dy);
            const angle = Math.atan2(dy, dx);

            const clampedDist = Math.min(dist, JOY_MAX_RADIUS);
            const knobX = Math.cos(angle) * clampedDist;
            const knobY = Math.sin(angle) * clampedDist;

            joyKnob.style.transform = `translate(${knobX}px, ${knobY}px)`;

            // Normalized input (-1 to +1)
            joyPanX = knobX / JOY_MAX_RADIUS;
            joyPanY = -knobY / JOY_MAX_RADIUS; // Invert Y for camera pan forward
        }

        joyBoundary.addEventListener('touchstart', onTouchStart, { passive: false });
        window.addEventListener('touchmove', onTouchMove, { passive: false });
        window.addEventListener('touchend', onTouchEnd, { passive: false });
        window.addEventListener('touchcancel', onTouchEnd, { passive: false });

        // Mouse fallback for testing on laptop
        joyBoundary.addEventListener('mousedown', onTouchStart);
        window.addEventListener('mousemove', onTouchMove);
        window.addEventListener('mouseup', onTouchEnd);
    }

    function startPanLoop() {
        if (panLoopTimer) return;
        panLoopTimer = setInterval(() => {
            if (joyActive || orbitState !== 0 || zoomState !== 0) {
                sendCommand({
                    type: 'camera',
                    pan: { x: joyPanX, y: joyPanY },
                    orbit: orbitState,
                    zoom: zoomState,
                    reset: false
                });
            }
        }, 33); // ~30 fps
    }

    function stopPanLoop() {
        if (!joyActive && orbitState === 0 && zoomState === 0) {
            clearInterval(panLoopTimer);
            panLoopTimer = null;
        }
    }

    // 5. Button Listeners
    function setupButtonListeners() {
        function bindHold(btn, onStart, onEnd) {
            const start = (e) => {
                e.preventDefault();
                vibrate(15);
                onStart();
                startPanLoop();
            };
            const end = (e) => {
                e.preventDefault();
                onEnd();
                stopPanLoop();
                sendCommand({ type: 'camera', pan: { x: joyPanX, y: joyPanY }, orbit: orbitState, zoom: zoomState, reset: false });
            };

            btn.addEventListener('touchstart', start, { passive: false });
            btn.addEventListener('touchend', end, { passive: false });
            btn.addEventListener('touchcancel', end, { passive: false });
            btn.addEventListener('mousedown', start);
            btn.addEventListener('mouseup', end);
            btn.addEventListener('mouseleave', end);
        }

        bindHold(btnOrbitL, () => { orbitState = -1; }, () => { orbitState = 0; });
        bindHold(btnOrbitR, () => { orbitState = 1; }, () => { orbitState = 0; });
        bindHold(btnZoomIn, () => { zoomState = 1; }, () => { zoomState = 0; });
        bindHold(btnZoomOut, () => { zoomState = -1; }, () => { zoomState = 0; });

        btnOverview.addEventListener('click', () => {
            vibrate(15);
            sendCommand({ type: 'overview' });
            showToast('Camera reset to Overview');
        });

        btnFocus.addEventListener('click', () => {
            vibrate(15);
            sendCommand({ type: 'focus_selected' });
            showToast('Camera focused on ' + facName.textContent);
        });

        btnPause.addEventListener('click', () => {
            vibrate(20);
            sendCommand({ type: 'pause' });
        });

        btnNextCh.addEventListener('click', () => {
            vibrate(25);
            sendCommand({ type: 'advance_chapter' });
            showToast('Advancing Chapter...');
        });

        btnDismissSheet.addEventListener('click', () => {
            actionSheet.style.display = 'none';
        });

        btnCancelAction.addEventListener('click', () => {
            confirmModal.classList.remove('active');
            pendingAction = null;
        });

        btnConfirmAction.addEventListener('click', () => {
            if (pendingAction) {
                vibrate(35);
                sendCommand({
                    type: 'action',
                    actionKind: pendingAction.kind,
                    targetFacility: selectedFacilityId
                });
                showToast(`Executing: ${pendingAction.name}`);
                confirmModal.classList.remove('active');
                pendingAction = null;
            }
        });
    }

    // 6. Facility Strip and Actions
    function buildFacilityStrip() {
        facilityStrip.innerHTML = '';
        FACILITIES.forEach(f => {
            const pill = document.createElement('div');
            pill.className = `fac-pill ${f.id === selectedFacilityId ? 'active' : ''}`;
            pill.id = `pill_${f.id}`;
            pill.innerHTML = `<span class="fac-dot"></span><span>${f.name}</span>`;

            pill.addEventListener('click', () => {
                vibrate(15);
                selectFacility(f.id);
            });

            facilityStrip.appendChild(pill);
        });
    }

    function selectFacility(id) {
        selectedFacilityId = id;
        document.querySelectorAll('.fac-pill').forEach(p => p.classList.remove('active'));
        const activePill = document.getElementById(`pill_${id}`);
        if (activePill) activePill.classList.add('active');

        actionSheet.style.display = 'flex';
        renderActionsForFacility(id);

        sendCommand({ type: 'select_facility', facilityId: id });
    }

    function renderActionsForFacility(id) {
        const fac = FACILITIES.find(f => f.id === id) || FACILITIES[0];
        facName.textContent = fac.name;
        facType.textContent = fac.type.toUpperCase();

        actionBtnList.innerHTML = '';
        ACTIONS.forEach(a => {
            const btn = document.createElement('div');
            btn.className = 'intervention-row-btn';
            btn.innerHTML = `
                <span>${a.name}</span>
                <span class="int-cost">${a.cost}</span>
            `;

            btn.addEventListener('click', () => {
                vibrate(15);
                openConfirmModal(a);
            });

            actionBtnList.appendChild(btn);
        });
    }

    function openConfirmModal(action) {
        pendingAction = action;
        confirmDesc.textContent = action.desc;
        confirmMeta.textContent = `Cost: ${action.cost}  |  Staff: ${action.staff}`;
        confirmModal.classList.add('active');
    }

    // Initialize on load
    window.addEventListener('DOMContentLoaded', init);
})();
