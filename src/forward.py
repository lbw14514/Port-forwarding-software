import sys
import socket
import threading

BUF_SIZE = 4096
BACKLOG = 128


def forward(src, dst):
    while True:
        data = src.recv(BUF_SIZE)
        if not data:
            break
        dst.sendall(data)


def handle(client, target_host, target_port):
    target = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    try:
        target.connect((target_host, target_port))
    except OSError as e:
        print(f"无法连接目标 {target_host}:{target_port}: {e}", file=sys.stderr)
        client.close()
        return
    t1 = threading.Thread(target=forward, args=(client, target))
    t2 = threading.Thread(target=forward, args=(target, client))
    t1.start()
    t2.start()
    t1.join()
    t2.join()
    client.close()
    target.close()


def main():
    if len(sys.argv) != 5:
        print(f"用法: {sys.argv[0]} <监听地址> <监听端口> <目标地址> <目标端口>", file=sys.stderr)
        sys.exit(1)
    listen_host = sys.argv[1]
    target_host = sys.argv[3]
    try:
        listen_port = int(sys.argv[2])
        target_port = int(sys.argv[4])
    except ValueError:
        print("端口必须是 1-65535 的整数", file=sys.stderr)
        sys.exit(1)
    if not (1 <= listen_port <= 65535 and 1 <= target_port <= 65535):
        print("端口必须是 1-65535 的整数", file=sys.stderr)
        sys.exit(1)

    server = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    server.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
    try:
        server.bind((listen_host, listen_port))
    except OSError as e:
        print(f"监听 {listen_host}:{listen_port} 失败（端口被占用？）: {e}", file=sys.stderr)
        sys.exit(1)
    server.listen(BACKLOG)
    print(f"Python 内核转发: {listen_host}:{listen_port} -> {target_host}:{target_port}")
    while True:
        try:
            client, _ = server.accept()
        except OSError:
            continue
        threading.Thread(target=handle, args=(client, target_host, target_port), daemon=True).start()


if __name__ == '__main__':
    main()
