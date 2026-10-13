// Campus Pulse 2030 — Companion Mobile Controller Server
const http = require('http');
const fs = require('fs');
const path = require('path');
const os = require('os');
const url = require('url');
const crypto = require('crypto');
const { SimpleWebSocketServer } = require('./lib/ws.js');

const PORT = parseInt(process.env.PORT || '8090', 10);
const PUBLIC_DIR = path.join(__dirname, 'public');

function detectLanIp() {
    if (process.env.CAMPUS_LAN_IP) return process.env.CAMPUS_LAN_IP;
    const interfaces = os.networkInterfaces();
    const candidates = [];

    for (const name of Object.keys(interfaces)) {
        for (const iface of interfaces[name]) {
            if (iface.family === 'IPv4' && !iface.internal) {
                if (!iface.address.startsWith('169.254.')) {
                    candidates.push({ name, address: iface.address });
                }
            }
        }
    }

    // Prefer Wi-Fi or Ethernet starting with 192.168 or 10.
    const preferred = candidates.find(c => c.name.toLowerCase().includes('wi-fi') || c.address.startsWith('192.168.'))
        || candidates[0];

    return preferred ? preferred.address : '127.0.0.1';
}

class SessionManager {
    constructor() {
        this.rotate();
    }

    rotate() {
        this.pin = Math.floor(1000 + Math.random() * 9000).toString();
        this.token = 'tok_' + crypto.randomBytes(4).toString('hex');
        this.createdAt = Date.now();
        this.expiresAt = this.createdAt + 15 * 60 * 1000; // 15 mins
        this.phoneConnected = false;
        this.phoneDeviceName = 'None';
    }

    isValid(pin, token) {
        if (Date.now() > this.expiresAt) return false;
        return (pin === this.pin || token === this.token);
    }
}

const session = new SessionManager();
let unityClient = null;
let phoneClient = null;
let lastPhoneHeartbeat = 0;
let latestTelemetry = null;

const MIME_TYPES = {
    '.html': 'text/html; charset=utf-8',
    '.css': 'text/css; charset=utf-8',
    '.js': 'application/javascript; charset=utf-8',
    '.json': 'application/json; charset=utf-8',
    '.png': 'image/png',
    '.jpg': 'image/jpeg',
    '.svg': 'image/svg+xml',
    '.ico': 'image/x-icon'
};

const server = http.createServer((req, res) => {
    const parsedUrl = url.parse(req.url, true);
    const pathname = parsedUrl.pathname;

    // CORS for local development
    res.setHeader('Access-Control-Allow-Origin', '*');
    res.setHeader('Access-Control-Allow-Methods', 'GET, POST, OPTIONS');
    res.setHeader('Access-Control-Allow-Headers', 'Content-Type, Authorization');

    if (req.method === 'OPTIONS') {
        res.writeHead(204);
        res.end();
        return;
    }

    // API Routes
    if (pathname === '/api/session') {
        const lanIp = detectLanIp();
        const fullUrl = `http://${lanIp}:${PORT}/?token=${session.token}&pin=${session.pin}`;
        res.writeHead(200, { 'Content-Type': 'application/json' });
        res.end(JSON.stringify({
            success: true,
            lanIp,
            port: PORT,
            pin: session.pin,
            token: session.token,
            url: fullUrl,
            expiresAt: session.expiresAt,
            phoneConnected: phoneClient !== null,
            unityConnected: unityClient !== null
        }));
        return;
    }

    if (pathname === '/api/rotate-session' && req.method === 'POST') {
        session.rotate();
        if (phoneClient) {
            phoneClient.send({ type: 'session_expired', reason: 'Session rotated by host.' });
            phoneClient.close();
            phoneClient = null;
        }
        const lanIp = detectLanIp();
        res.writeHead(200, { 'Content-Type': 'application/json' });
        res.end(JSON.stringify({
            success: true,
            lanIp,
            port: PORT,
            pin: session.pin,
            token: session.token,
            url: `http://${lanIp}:${PORT}/?token=${session.token}&pin=${session.pin}`
        }));
        return;
    }

    if (pathname === '/api/health') {
        res.writeHead(200, { 'Content-Type': 'application/json' });
        res.end(JSON.stringify({ status: 'ok', uptime: process.uptime() }));
        return;
    }

    // Static Files
    let filePath = path.join(PUBLIC_DIR, pathname === '/' ? 'index.html' : pathname);
    filePath = path.normalize(filePath);

    if (!filePath.startsWith(PUBLIC_DIR)) {
        res.writeHead(403);
        res.end('Access Denied');
        return;
    }

    fs.stat(filePath, (err, stats) => {
        if (err || !stats.isFile()) {
            res.writeHead(404, { 'Content-Type': 'text/plain' });
            res.end('Not Found');
            return;
        }

        const ext = path.extname(filePath).toLowerCase();
        const contentType = MIME_TYPES[ext] || 'application/octet-stream';
        res.writeHead(200, { 'Content-Type': contentType });
        fs.createReadStream(filePath).pipe(res);
    });
});

const wss = new SimpleWebSocketServer(server);

wss.on('connection', (ws, req) => {
    const parsed = url.parse(req.url, true);
    const pathname = parsed.pathname;

    if (pathname === '/ws/unity') {
        console.log('[WS] Unity client connected.');
        unityClient = ws;
        ws.on('error', (err) => console.warn('[WS] Unity socket error:', err.message));

        // Inform Unity about current session info
        ws.send({
            type: 'session_info',
            lanIp: detectLanIp(),
            port: PORT,
            pin: session.pin,
            token: session.token,
            phoneConnected: phoneClient !== null
        });

        ws.on('message', (msg) => {
            try {
                const data = JSON.parse(msg);
                if (data.type === 'telemetry') {
                    latestTelemetry = data;
                    if (phoneClient) phoneClient.send(data);
                } else if (data.type === 'feedback') {
                    if (phoneClient) phoneClient.send(data);
                }
            } catch (e) {}
        });

        ws.on('close', () => {
            console.log('[WS] Unity client disconnected.');
            if (unityClient === ws) unityClient = null;
        });

        return;
    }

    if (pathname === '/ws/phone') {
        console.log('[WS] Phone connection attempt...');
        ws.on('error', (err) => console.warn('[WS] Phone socket error:', err.message));
        let isPaired = false;
        const queryToken = parsed.query.token;
        const queryPin = parsed.query.pin;

        if (session.isValid(queryPin, queryToken)) {
            isPaired = true;
            setupPhoneConnection(ws, parsed.query.device || 'Mobile Browser');
        }

        ws.on('message', (msg) => {
            try {
                const data = JSON.parse(msg);

                if (!isPaired) {
                    if (data.type === 'pair') {
                        if (session.isValid(data.pin, data.token)) {
                            isPaired = true;
                            setupPhoneConnection(ws, data.device || 'Mobile Device');
                        } else {
                            ws.send({ type: 'pair_failed', reason: 'Invalid or expired PIN / Token.' });
                        }
                    }
                    return;
                }

                // Phone is paired, handle commands
                handlePhoneCommand(data);

            } catch (e) {
                console.warn('[WS] Malformed phone message:', e.message);
            }
        });

        ws.on('close', () => {
            if (phoneClient === ws) {
                console.log('[WS] Active phone disconnected.');
                phoneClient = null;
                session.phoneConnected = false;

                // Send safety reset to Unity
                if (unityClient) {
                    unityClient.send({
                        type: 'phone_disconnected',
                        camera: { panX: 0, panY: 0, orbit: 0, zoom: 0, reset: false }
                    });
                }
            }
        });
    }
});

function setupPhoneConnection(ws, deviceName) {
    if (phoneClient && phoneClient !== ws) {
        phoneClient.send({ type: 'displaced', reason: 'Another controller connected.' });
        phoneClient.close();
    }

    phoneClient = ws;
    session.phoneConnected = true;
    session.phoneDeviceName = deviceName;
    lastPhoneHeartbeat = Date.now();

    console.log(`[WS] Phone paired successfully (${deviceName})!`);
    ws.send({
        type: 'paired',
        pin: session.pin,
        device: deviceName
    });

    if (latestTelemetry) {
        ws.send(latestTelemetry);
    }

    if (unityClient) {
        unityClient.send({
            type: 'phone_connected',
            device: deviceName
        });
    }
}

function handlePhoneCommand(data) {
    lastPhoneHeartbeat = Date.now();

    if (data.type === 'heartbeat') {
        if (phoneClient) phoneClient.send({ type: 'heartbeat_ack', timestamp: Date.now() });
        return;
    }

    if (!unityClient) return;

    if (data.type === 'camera') {
        // Sanitize and bound camera input
        const panX = Math.max(-1, Math.min(1, parseFloat(data.pan?.x || 0)));
        const panY = Math.max(-1, Math.min(1, parseFloat(data.pan?.y || 0)));
        const orbit = Math.max(-1, Math.min(1, parseFloat(data.orbit || 0)));
        const zoom = Math.max(-2, Math.min(2, parseFloat(data.zoom || 0)));
        const reset = Boolean(data.reset);

        unityClient.send({
            type: 'camera',
            panX, panY, orbit, zoom, reset
        });
    } else if (data.type === 'select_facility') {
        unityClient.send({
            type: 'select_facility',
            facilityId: String(data.facilityId || '')
        });
    } else if (data.type === 'focus_selected') {
        unityClient.send({ type: 'focus_selected' });
    } else if (data.type === 'overview') {
        unityClient.send({ type: 'overview' });
    } else if (data.type === 'action') {
        unityClient.send({
            type: 'action',
            actionKind: String(data.actionKind || ''),
            targetFacility: String(data.targetFacility || '')
        });
    } else if (data.type === 'pause') {
        unityClient.send({ type: 'pause' });
    } else if (data.type === 'advance_chapter') {
        unityClient.send({ type: 'advance_chapter' });
    } else if (data.type === 'restart') {
        unityClient.send({ type: 'restart' });
    }
}

// Watchdog timer: check heartbeat loss (> 3s without message zeroes camera)
setInterval(() => {
    if (phoneClient && Date.now() - lastPhoneHeartbeat > 3000) {
        if (unityClient) {
            unityClient.send({
                type: 'camera',
                panX: 0, panY: 0, orbit: 0, zoom: 0, reset: false
            });
        }
    }
}, 1000);

process.on('uncaughtException', (err) => {
    console.warn('[Server] Uncaught exception caught gracefully:', err.message);
});
process.on('unhandledRejection', (reason) => {
    console.warn('[Server] Unhandled rejection caught gracefully:', reason);
});

server.listen(PORT, '0.0.0.0', () => {
    const lanIp = detectLanIp();
    console.log(`=======================================================`);
    console.log(`CAMPUS PULSE 2030 — CONTROLLER SERVER ACTIVE`);
    console.log(`Port: ${PORT}`);
    console.log(`LAN IP: ${lanIp}`);
    console.log(`Controller URL: http://${lanIp}:${PORT}/?token=${session.token}&pin=${session.pin}`);
    console.log(`Pairing PIN: ${session.pin}`);
    console.log(`=======================================================`);
});

module.exports = { server, session, detectLanIp };
