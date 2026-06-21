#include <winsock2.h>
#include <ws2tcpip.h>
#include <windows.h>
#include <iostream>
#include <string>
#include <thread>
#pragma comment(lib, "ws2_32.lib")

void forward(SOCKET src, SOCKET dst) {
    char buf[8192];
    while (true) {
        int len = recv(src, buf, sizeof(buf), 0);
        if (len <= 0) break;
        if (send(dst, buf, len, 0) == SOCKET_ERROR) break;
    }
}

void client_handler(SOCKET client, std::string target_host, u_short target_port) {
    SOCKET target = socket(AF_INET, SOCK_STREAM, IPPROTO_TCP);
    if (target == INVALID_SOCKET) { closesocket(client); return; }
    sockaddr_in addr{};
    addr.sin_family = AF_INET;
    addr.sin_port = htons(target_port);
    inet_pton(AF_INET, target_host.c_str(), &addr.sin_addr);
    if (connect(target, (sockaddr*)&addr, sizeof(addr)) == SOCKET_ERROR) {
        closesocket(client); closesocket(target); return;
    }
    std::thread t1(forward, client, target);
    std::thread t2(forward, target, client);
    t1.join(); t2.join();
    closesocket(client); closesocket(target);
}

int main(int argc, char* argv[]) {
    if (argc != 5) return 1;
    WSADATA wsa; WSAStartup(MAKEWORD(2, 2), &wsa);
    std::string listen_ip = argv[1];
    u_short listen_port = static_cast<u_short>(std::stoi(argv[2]));
    std::string target_ip = argv[3];
    u_short target_port = static_cast<u_short>(std::stoi(argv[4]));

    SOCKET listen_sock = socket(AF_INET, SOCK_STREAM, IPPROTO_TCP);
    sockaddr_in bind_addr{};
    bind_addr.sin_family = AF_INET;
    bind_addr.sin_port = htons(listen_port);
    inet_pton(AF_INET, listen_ip.c_str(), &bind_addr.sin_addr);
    bind(listen_sock, (sockaddr*)&bind_addr, sizeof(bind_addr));
    listen(listen_sock, SOMAXCONN);

    while (true) {
        sockaddr_in client_addr{}; int addr_len = sizeof(client_addr);
        SOCKET client = accept(listen_sock, (sockaddr*)&client_addr, &addr_len);
        if (client == INVALID_SOCKET) continue;
        std::thread(client_handler, client, target_ip, target_port).detach();
    }
    closesocket(listen_sock);
    WSACleanup();
    return 0;
}