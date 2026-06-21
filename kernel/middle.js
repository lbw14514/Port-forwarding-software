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
    server.listen(listenPort, listenHost, () => {
        console.log(`Node kernel forwarding: ${listenHost}:${listenPort} -> ${targetHost}:${targetPort}`);
    });
} else if (kernel === 'cpp') {
    // C++ 内核转发：启动 forward_kernel.exe 子进程
    const child = spawn('forward_kernel.exe', [listenHost, String(listenPort), targetHost, String(targetPort)], {
        stdio: 'inherit',
        shell: true
    });
    child.on('exit', (code) => {
        console.log(`C++ kernel exited with code ${code}`);
    });
} else {
    console.error('Unknown kernel type. Use --kernel node|cpp');
    process.exit(1);
}