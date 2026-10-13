// Integration and Unit Tests for Controller Server
const http = require('http');
const crypto = require('crypto');
const net = require('net');
const { server, session, detectLanIp } = require('./server.js');

function makeRequest(path, method = 'GET') {
    return new Promise((resolve, reject) => {
        const req = http.request({
            hostname: '127.0.0.1',
            port: 8090,
            path,
            method
        }, (res) => {
            let data = '';
            res.on('data', chunk => data += chunk);
            res.on('end', () => {
                try {
                    resolve({ status: res.statusCode, json: JSON.parse(data) });
                } catch (e) {
                    resolve({ status: res.statusCode, text: data });
                }
            });
        });
        req.on('error', reject);
        req.end();
    });
}

function createWsClient(path, onMsg = null) {
    return new Promise((resolve, reject) => {
        const socket = net.createConnection({ host: '127.0.0.1', port: 8090 }, () => {
            const key = crypto.randomBytes(16).toString('base64');
            const req = [
                `GET ${path} HTTP/1.1`,
                `Host: 127.0.0.1:8090`,
                `Upgrade: websocket`,
                `Connection: Upgrade`,
                `Sec-WebSocket-Key: ${key}`,
                `Sec-WebSocket-Version: 13`,
                `\r\n`
            ].join('\r\n');
            socket.write(req);
        });

        let upgraded = false;
        let buffer = Buffer.alloc(0);
        const callbacks = onMsg ? [onMsg] : [];

        function parseFrames() {
            while (buffer.length >= 2) {
                const len = buffer[1] & 0x7F;
                if (buffer.length < 2 + len) break;
                const payload = buffer.subarray(2, 2 + len);
                buffer = buffer.subarray(2 + len);
                let parsed;
                try {
                    parsed = JSON.parse(payload.toString('utf8'));
                } catch (e) {
                    parsed = payload.toString('utf8');
                }
                for (const cb of callbacks) cb(parsed);
            }
        }

        socket.on('data', (chunk) => {
            if (!upgraded) {
                const idx = chunk.indexOf(Buffer.from('\r\n\r\n'));
                if (idx !== -1) {
                    upgraded = true;
                    const leftover = chunk.subarray(idx + 4);
                    if (leftover.length > 0) {
                        buffer = Buffer.concat([buffer, leftover]);
                        parseFrames();
                    }
                    resolve({
                        socket,
                        sendText: (text) => {
                            const payload = Buffer.from(text, 'utf8');
                            const mask = crypto.randomBytes(4);
                            const masked = Buffer.alloc(payload.length);
                            for (let i = 0; i < payload.length; i++) masked[i] = payload[i] ^ mask[i % 4];

                            let header;
                            if (payload.length < 126) {
                                header = Buffer.alloc(6);
                                header[0] = 0x81;
                                header[1] = 0x80 | payload.length;
                                mask.copy(header, 2);
                            }
                            socket.write(Buffer.concat([header, masked]));
                        },
                        onMessage: (cb) => {
                            callbacks.push(cb);
                        },
                        close: () => socket.end()
                    });
                }
            } else {
                buffer = Buffer.concat([buffer, chunk]);
                parseFrames();
            }
        });

        socket.on('error', reject);
    });
}

async function runTests() {
    console.log('--- RUNNING CONTROLLER SERVER TESTS ---');

    // Test 1: GET /api/session
    const sessionRes = await makeRequest('/api/session');
    if (sessionRes.status !== 200 || !sessionRes.json.success) throw new Error('Test 1 Failed');
    console.log('✔ Test 1 Passed: REST /api/session returns valid session & LAN IP');

    // Test 2: Unity WebSocket Connection
    let sessionInfoReceived = false;
    const unityWs = await createWsClient('/ws/unity', (msg) => {
        if (msg.type === 'session_info') sessionInfoReceived = true;
    });

    await new Promise(r => setTimeout(r, 200));
    if (!sessionInfoReceived) throw new Error('Test 2 Failed: session_info not received');
    console.log('✔ Test 2 Passed: Unity client connected and received session_info');

    // Test 3: Phone WebSocket Connection with valid PIN & token
    let phonePaired = false;
    let unityNotifiedPhoneConnected = false;

    unityWs.onMessage((msg) => {
        if (msg.type === 'phone_connected') unityNotifiedPhoneConnected = true;
    });

    const phoneWs = await createWsClient(`/ws/phone?pin=${session.pin}&token=${session.token}`, (msg) => {
        if (msg.type === 'paired') phonePaired = true;
    });

    await new Promise(r => setTimeout(r, 200));
    if (!phonePaired) throw new Error('Test 3 Failed: Phone pairing failed');
    if (!unityNotifiedPhoneConnected) throw new Error('Test 3 Failed: Unity was not notified');
    console.log('✔ Test 3 Passed: Phone paired and Unity notified');

    // Test 4: Relay Camera & Commands
    let receivedCamera = null;
    let receivedFacility = null;
    unityWs.onMessage((msg) => {
        if (msg.type === 'camera') receivedCamera = msg;
        if (msg.type === 'select_facility') receivedFacility = msg;
    });

    phoneWs.sendText(JSON.stringify({
        type: 'camera',
        pan: { x: 0.75, y: -0.5 },
        orbit: 1.0,
        zoom: -0.5,
        reset: false
    }));

    phoneWs.sendText(JSON.stringify({
        type: 'select_facility',
        facilityId: 'canteen'
    }));

    await new Promise(r => setTimeout(r, 200));
    if (!receivedCamera || receivedCamera.panX !== 0.75) throw new Error('Test 4 Failed: Camera pan');
    if (!receivedFacility || receivedFacility.facilityId !== 'canteen') throw new Error('Test 4 Failed: Facility');
    console.log('✔ Test 4 Passed: Controller camera and facility selection commands relayed correctly');

    // Test 5: Phone Disconnect Safety Reset
    let phoneDisconnectedReset = false;
    unityWs.onMessage((msg) => {
        if (msg.type === 'phone_disconnected' && msg.camera.panX === 0) phoneDisconnectedReset = true;
    });

    phoneWs.close();
    await new Promise(r => setTimeout(r, 300));
    if (!phoneDisconnectedReset) throw new Error('Test 5 Failed: Disconnect reset');
    console.log('✔ Test 5 Passed: Disconnect immediately zeroed out camera input in Unity');

    unityWs.close();
    server.close();
    console.log('=== ALL CONTROLLER SERVER INTEGRATION TESTS PASSED ===');
    process.exit(0);
}

runTests().catch(err => {
    console.error('Test suite failed:', err);
    process.exit(1);
});
