import sys
import socket
import threading

def forward(src, dst):
    while True:
        data = src.recv(4096)
        if not data:
            break
        dst.sendall(data)

def handle(client, target_host, target_port):
    target = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    try:
        target.connect((target_host, target_port))
    except:
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

if __name__ == '__main__':
    if len(sys.argv) != 5:
        sys.exit(1)
    listen_host = sys.argv[1]
    listen_port = int(sys.argv[2])
    target_host = sys.argv[3]
    target_port = int(sys.argv[4])

    server = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    server.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
    server.bind((listen_host, listen_port))
    server.listen(128)
    print(f"Python 内核转发: {listen_host}:{listen_port} -> {target_host}:{target_port}")
    while True:
        client, _ = server.accept()
        threading.Thread(target=handle, args=(client, target_host, target_port)).start()