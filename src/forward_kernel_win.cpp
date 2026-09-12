#include <winsock2.h>
#include <ws2tcpip.h>
#include <windows.h>
#include <iostream>
#include <string>
#include <thread>
#pragma comment(lib, "ws2_32.lib")

static const int BUF_SIZE = 8192;

// send() may accept only part of the buffer; a plain send() silently
// corrupts the stream when that happens.
static bool send_all(SOCKET sock, const char* buf, int len) {
    while (len > 0) {
        int sent = send(sock, buf, len, 0);
        if (sent == SOCKET_ERROR) return false;
        buf += sent;
        len -= sent;
    }
    return true;
}

static void forward(SOCKET src, SOCKET dst) {
    char buf[BUF_SIZE];
    while (true) {
        int len = recv(src, buf, sizeof(buf), 0);
        if (len <= 0) break;
        if (!send_all(dst, buf, len)) break;
    }
}

static bool parse_port(const char* text, u_short* out) {
    try {
        long value = std::stol(text);
        if (value < 1 || value > 65535) return false;
        *out = static_cast<u_short>(value);
        return true;
    } catch (...) {
        return false;
    }
}

static bool parse_ipv4(const std::string& host, in_addr* out) {
    return inet_pton(AF_INET, host.c_str(), out) == 1;
}

static void client_handler(SOCKET client, std::string target_host, u_short target_port) {
    SOCKET target = socket(AF_INET, SOCK_STREAM, IPPROTO_TCP);
    if (target == INVALID_SOCKET) { closesocket(client); return; }
    sockaddr_in addr{};
    addr.sin_family = AF_INET;
    addr.sin_port = htons(target_port);
    if (!parse_ipv4(target_host, &addr.sin_addr)) {
        closesocket(client); closesocket(target); return;
    }
    if (connect(target, (sockaddr*)&addr, sizeof(addr)) == SOCKET_ERROR) {
        closesocket(client); closesocket(target); return;
    }
    std::thread t1(forward, client, target);
    std::thread t2(forward, target, client);
    t1.join(); t2.join();
    closesocket(client); closesocket(target);
}

int main(int argc, char* argv[]) {
    if (argc != 5) {
        std::cerr << "Usage: " << argv[0] << " <listen_ipv4> <listen_port> <target_ipv4> <target_port>" << std::endl;
        return 1;
    }
    WSADATA wsa;
    if (WSAStartup(MAKEWORD(2, 2), &wsa) != 0) {
        std::cerr << "WSAStartup failed" << std::endl;
        return 1;
    }
    u_short listen_port;
    u_short target_port;
    if (!parse_port(argv[2], &listen_port) || !parse_port(argv[4], &target_port)) {
        std::cerr << "Invalid port: ports must be in 1-65535" << std::endl;
        return 1;
    }
    in_addr listen_ip;
    in_addr target_ip;
    if (!parse_ipv4(argv[1], &listen_ip) || !parse_ipv4(argv[3], &target_ip)) {
        std::cerr << "Invalid IPv4 address (this kernel does not resolve hostnames)" << std::endl;
        return 1;
    }

    SOCKET listen_sock = socket(AF_INET, SOCK_STREAM, IPPROTO_TCP);
    if (listen_sock == INVALID_SOCKET) {
        std::cerr << "socket() failed: " << WSAGetLastError() << std::endl;
        WSACleanup();
        return 1;
    }
    sockaddr_in bind_addr{};
    bind_addr.sin_family = AF_INET;
    bind_addr.sin_port = htons(listen_port);
    bind_addr.sin_addr = listen_ip;
    if (bind(listen_sock, (sockaddr*)&bind_addr, sizeof(bind_addr)) == SOCKET_ERROR) {
        std::cerr << "bind() failed on " << argv[1] << ":" << argv[2] << " (port already in use?): "
                  << WSAGetLastError() << std::endl;
        closesocket(listen_sock);
        WSACleanup();
        return 1;
    }
    if (listen(listen_sock, SOMAXCONN) == SOCKET_ERROR) {
        std::cerr << "listen() failed: " << WSAGetLastError() << std::endl;
        closesocket(listen_sock);
        WSACleanup();
        return 1;
    }

    std::cout << "C++ kernel forwarding: " << argv[1] << ":" << argv[2]
              << " -> " << argv[3] << ":" << argv[4] << std::endl;
    while (true) {
        sockaddr_in client_addr{}; int addr_len = sizeof(client_addr);
        SOCKET client = accept(listen_sock, (sockaddr*)&client_addr, &addr_len);
        if (client == INVALID_SOCKET) continue;
        std::thread(client_handler, client, argv[3], target_port).detach();
    }
    closesocket(listen_sock);
    WSACleanup();
    return 0;
}
