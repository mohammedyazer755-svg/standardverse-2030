// Zero-dependency RFC 6455 WebSocket Server Implementation for Node.js
const http = require('http');
const crypto = require('crypto');
const EventEmitter = require('events');

const WS_GUID = '258EAFA5-E914-47DA-95CA-C5AB0DC85B11';

class SimpleWebSocket extends EventEmitter {
    constructor(socket) {
        super();
        this.socket = socket;
        this.isAlive = true;
        this.buffer = Buffer.alloc(0);

        socket.on('data', (chunk) => this._onData(chunk));
        socket.on('end', () => {
            if (this.isAlive) {
                this.isAlive = false;
                this.emit('close');
            }
        });
        socket.on('close', () => {
            if (this.isAlive) {
                this.isAlive = false;
                this.emit('close');
            }
        });
        socket.on('error', (err) => {
            this.emit('error', err);
        });
    }

    _onData(chunk) {
        this.buffer = Buffer.concat([this.buffer, chunk]);
        while (this.buffer.length >= 2) {
            const byte1 = this.buffer[0];
            const byte2 = this.buffer[1];
            const fin = (byte1 & 0x80) !== 0;
            const opcode = byte1 & 0x0F;
            const masked = (byte2 & 0x80) !== 0;
            let payloadLen = byte2 & 0x7F;

            let offset = 2;
            if (payloadLen === 126) {
                if (this.buffer.length < offset + 2) return;
                payloadLen = this.buffer.readUInt16BE(offset);
                offset += 2;
            } else if (payloadLen === 127) {
                if (this.buffer.length < offset + 8) return;
                // Read high 32 and low 32
                const high = this.buffer.readUInt32BE(offset);
                const low = this.buffer.readUInt32BE(offset + 4);
                payloadLen = high * 2 ** 32 + low;
                offset += 8;
            }

            let maskKey = null;
            if (masked) {
                if (this.buffer.length < offset + 4) return;
                maskKey = this.buffer.subarray(offset, offset + 4);
                offset += 4;
            }

            if (this.buffer.length < offset + payloadLen) return;

            let payload = this.buffer.subarray(offset, offset + payloadLen);
            this.buffer = this.buffer.subarray(offset + payloadLen);

            if (masked && maskKey) {
                const unmasked = Buffer.alloc(payload.length);
                for (let i = 0; i < payload.length; i++) {
                    unmasked[i] = payload[i] ^ maskKey[i % 4];
                }
                payload = unmasked;
            }

            if (opcode === 0x8) { // Close
                this.close();
                return;
            } else if (opcode === 0x9) { // Ping
                this._sendFrame(0xA, payload); // Pong
            } else if (opcode === 0xA) { // Pong
                this.isAlive = true;
                this.emit('pong');
            } else if (opcode === 0x1) { // Text
                const text = payload.toString('utf8');
                this.emit('message', text);
            }
        }
    }

    send(data) {
        if (!this.socket.writable) return;
        const payload = Buffer.from(typeof data === 'string' ? data : JSON.stringify(data), 'utf8');
        this._sendFrame(0x1, payload);
    }

    ping() {
        if (!this.socket.writable) return;
        this._sendFrame(0x9, Buffer.alloc(0));
    }

    _sendFrame(opcode, payload) {
        const len = payload.length;
        let header;
        if (len < 126) {
            header = Buffer.alloc(2);
            header[0] = 0x80 | (opcode & 0x0F);
            header[1] = len;
        } else if (len <= 0xFFFF) {
            header = Buffer.alloc(4);
            header[0] = 0x80 | (opcode & 0x0F);
            header[1] = 126;
            header.writeUInt16BE(len, 2);
        } else {
            header = Buffer.alloc(10);
            header[0] = 0x80 | (opcode & 0x0F);
            header[1] = 127;
            header.writeBigUInt64BE(BigInt(len), 2);
        }
        try {
            this.socket.write(Buffer.concat([header, payload]));
        } catch (e) {
            // Ignore socket write errors
        }
    }

    close() {
        try {
            this._sendFrame(0x8, Buffer.alloc(0));
            this.socket.end();
        } catch (e) {}
    }
}

class SimpleWebSocketServer extends EventEmitter {
    constructor(server) {
        super();
        this.server = server;
        server.on('upgrade', (req, socket, head) => this._onUpgrade(req, socket, head));
    }

    _onUpgrade(req, socket, head) {
        const key = req.headers['sec-websocket-key'];
        if (!key) {
            socket.destroy();
            return;
        }

        const digest = crypto.createHash('sha1')
            .update(key + WS_GUID)
            .digest('base64');

        const headers = [
            'HTTP/1.1 101 Switching Protocols',
            'Upgrade: websocket',
            'Connection: Upgrade',
            `Sec-WebSocket-Accept: ${digest}`
        ];

        socket.write(headers.join('\r\n') + '\r\n\r\n');
        const ws = new SimpleWebSocket(socket);
        this.emit('connection', ws, req);
    }
}

module.exports = { SimpleWebSocket, SimpleWebSocketServer };
