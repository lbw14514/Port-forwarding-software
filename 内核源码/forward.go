package main

import (
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
        os.Exit(1)
    }
    listenAddr := net.JoinHostPort(os.Args[1], os.Args[2])
    targetAddr := net.JoinHostPort(os.Args[3], os.Args[4])

    ln, err := net.Listen("tcp", listenAddr)
    if err != nil {
        os.Exit(1)
    }
    for {
        client, err := ln.Accept()
        if err != nil {
            continue
        }
        target, err := net.Dial("tcp", targetAddr)
        if err != nil {
            client.Close()
            continue
        }
        go forward(client, target)
        go forward(target, client)
    }
}