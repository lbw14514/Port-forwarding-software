use std::net::{TcpListener, TcpStream};
use std::thread;
use std::env;

fn forward(mut src: TcpStream, mut dst: TcpStream) {
    let _ = std::io::copy(&mut src, &mut dst);
}

fn main() {
    let args: Vec<String> = env::args().collect();
    if args.len() != 5 {
        eprintln!("Usage: {} <listen_host> <listen_port> <target_host> <target_port>", args[0]);
        return;
    }
    let listen_addr = format!("{}:{}", args[1], args[2]);
    let target_addr = format!("{}:{}", args[3], args[4]);

    let listener = match TcpListener::bind(&listen_addr) {
        Ok(l) => l,
        Err(e) => {
            eprintln!("listen {} failed (port already in use?): {}", listen_addr, e);
            return;
        }
    };
    println!("Rust kernel forwarding: {} -> {}", listen_addr, target_addr);
    for stream in listener.incoming() {
        if let Ok(client) = stream {
            let addr = target_addr.clone();
            thread::spawn(move || {
                if let Ok(target) = TcpStream::connect(&addr) {
                    let c = match client.try_clone() {
                        Ok(c) => c,
                        Err(_) => return,
                    };
                    let t = match target.try_clone() {
                        Ok(t) => t,
                        Err(_) => return,
                    };
                    thread::spawn(move || forward(c, t));
                    forward(client, target);
                }
            });
        }
    }
}
