import java.io.InputStream;
import java.io.OutputStream;
import java.net.ServerSocket;
import java.net.Socket;

public class Forward {
    public static void main(String[] args) throws Exception {
        if (args.length != 4) {
            System.err.println("Usage: java Forward <listen_host> <listen_port> <target_host> <target_port>");
            return;
        }
        String listenHost = args[0];
        int listenPort = Integer.parseInt(args[1]);
        String targetHost = args[2];
        int targetPort = Integer.parseInt(args[3]);

        ServerSocket server = new ServerSocket(listenPort, 128, java.net.InetAddress.getByName(listenHost));
        System.out.println("Java kernel forwarding: " + listenHost + ":" + listenPort + " -> " + targetHost + ":" + targetPort);
        while (true) {
            Socket client = server.accept();
            new Thread(() -> {
                Socket target = null;
                try {
                    target = new Socket(targetHost, targetPort);
                    InputStream cIn = client.getInputStream();
                    OutputStream cOut = client.getOutputStream();
                    InputStream tIn = target.getInputStream();
                    OutputStream tOut = target.getOutputStream();
                    new Thread(() -> { try { cIn.transferTo(tOut); } catch (Exception e) {} }).start();
                    tIn.transferTo(cOut);
                } catch (Exception e) {
                    System.err.println("connection error: " + e);
                } finally {
                    // Close both ends even when either direction throws, so the
                    // target socket no longer leaks on the error path.
                    try { client.close(); } catch (Exception e) {}
                    if (target != null) { try { target.close(); } catch (Exception e) {} }
                }
            }).start();
        }
    }
}
