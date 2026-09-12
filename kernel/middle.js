// middle.js
const { spawn } = require('child_process');
const net = require('net');

// 解析命令行参数
const args = process.argv.slice(2);
const getArg = (name, def) => {
    const idx = args.indexOf(name);
    return idx !== -1 && idx + 1 < args.length ? args[idx + 1] : def;
};

const kernel      = getArg('--kernel', 'node');      // 'node' 或 'cpp'
const listenHost  = getArg('--listen-host', '0.0.0.0');
const listenPort  = parseInt(getArg('--listen-port', '8080'), 10);
const targetHost  = getArg('--target-host', '127.0.0.1');
const targetPort  = parseInt(getArg('--target-port', '80'), 10);

// 这些值会进入命令行，只允许格式正确的值，避免用户输入注入命令
const isValidHost = (h) => /^[\w.-]+$/.test(h);
const isValidPort = (p) => Number.isInteger(p) && p >= 1 && p <= 65535;
if (!isValidHost(listenHost) || !isValidHost(targetHost) ||
    !isValidPort(listenPort) || !isValidPort(targetPort)) {
    console.error('参数无效：地址只能包含字母/数字/点/横线，端口必须是 1-65535 的整数。');
    process.exit(1);
}

if (kernel === 'node') {
    // Node.js 内核转发
    const server = net.createServer((clientSocket) => {
        const targetSocket = net.createConnection({ host: targetHost, port: targetPort }, () => {
            clientSocket.pipe(targetSocket).pipe(clientSocket);
        });
        targetSocket.on('error', (err) => {
            console.error(`Target connection error: ${err.message}`);
            clientSocket.destroy();
        });
        clientSocket.on('error', (err) => {
            console.error(`Client socket error: ${err.message}`);
            targetSocket.destroy();
        });
    });
    server.on('error', (err) => {
        console.error(`Listen failed on ${listenHost}:${listenPort} (port already in use?): ${err.message}`);
        process.exit(1);
    });
    server.listen(listenPort, listenHost, () => {
        console.log(`Node kernel forwarding: ${listenHost}:${listenPort} -> ${targetHost}:${targetPort}`);
    });
} else if (kernel === 'cpp') {
    // C++ 内核转发：启动 forward_kernel.exe 子进程。
    // 不使用 shell，参数以 argv 数组传递，不存在拼接/注入问题。
    const child = spawn('forward_kernel.exe', [listenHost, String(listenPort), targetHost, String(targetPort)], {
        stdio: 'inherit'
    });
    child.on('error', (err) => {
        console.error(`Failed to start forward_kernel.exe: ${err.message}`);
    });
    child.on('exit', (code) => {
        console.log(`C++ kernel exited with code ${code}`);
    });
} else {
    console.error('Unknown kernel type. Use --kernel node|cpp');
    process.exit(1);
}
