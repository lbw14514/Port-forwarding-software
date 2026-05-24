use std::net::{TcpListener, TcpStream};
use std::thread;
use std::env;

fn forward(mut src: TcpStream, mut dst: TcpStream) {
    let _ = std::io::copy(&mut src, &mut dst);
}

fn main() {
    let args: Vec<String> = env::args().collect();
    if args.len() != 5 { return; }
    let listen_addr = format!("{}:{}", args[1], args[2]);
    let target_addr = format!("{}:{}", args[3], args[4]);

    let listener = TcpListener::bind(&listen_addr).unwrap();
    for stream in listener.incoming() {
        if let Ok(client) = stream {
            let addr = target_addr.clone();
            thread::spawn(move || {
                if let Ok(target) = TcpStream::connect(&addr) {
                    let c = client.try_clone().unwrap();
                    let t = target.try_clone().unwrap();
                    thread::spawn(|| forward(c, t));
                    forward(client, target);
                }
            });
        }
    }
}