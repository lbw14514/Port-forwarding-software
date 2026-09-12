package main

import (
    "fmt"
    "io"
    "net"
    "os"
)

func forward(src, dst net.Conn) {
    io.Copy(dst, src)
    src.Close()
    dst.Close()
}

func main() {
    if len(os.Args) != 5 {
        fmt.Fprintf(os.Stderr, "Usage: %s <listen_host> <listen_port> <target_host> <target_port>\n", os.Args[0])
        os.Exit(1)
    }
    listenAddr := net.JoinHostPort(os.Args[1], os.Args[2])
    targetAddr := net.JoinHostPort(os.Args[3], os.Args[4])

    ln, err := net.Listen("tcp", listenAddr)
    if err != nil {
        fmt.Fprintf(os.Stderr, "listen %s failed (port already in use?): %v\n", listenAddr, err)
        os.Exit(1)
    }
    fmt.Printf("Go kernel forwarding: %s -> %s\n", listenAddr, targetAddr)
    for {
        client, err := ln.Accept()
        if err != nil {
            fmt.Fprintf(os.Stderr, "accept failed: %v\n", err)
            continue
        }
        target, err := net.Dial("tcp", targetAddr)
        if err != nil {
            fmt.Fprintf(os.Stderr, "connect target %s failed: %v\n", targetAddr, err)
            client.Close()
            continue
        }
        go forward(client, target)
        go forward(target, client)
    }
}
