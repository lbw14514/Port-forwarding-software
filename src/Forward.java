import java.io.InputStream;
import java.io.OutputStream;
import java.net.ServerSocket;
import java.net.Socket;

public class Forward {
    public static void main(String[] args) throws Exception {
        if (args.length != 4) return;
        String listenHost = args[0];
        int listenPort = Integer.parseInt(args[1]);
        String targetHost = args[2];
        int targetPort = Integer.parseInt(args[3]);

        ServerSocket server = new ServerSocket(listenPort, 128, java.net.InetAddress.getByName(listenHost));
        while (true) {
            Socket client = server.accept();
            new Thread(() -> {
                try {
                    Socket target = new Socket(targetHost, targetPort);
                    InputStream cIn = client.getInputStream();
                    OutputStream cOut = client.getOutputStream();
                    InputStream tIn = target.getInputStream();
                    OutputStream tOut = target.getOutputStream();
                    new Thread(() -> { try { cIn.transferTo(tOut); } catch (Exception e) {} }).start();
                    tIn.transferTo(cOut);
                    target.close();
                } catch (Exception e) {}
                try { client.close(); } catch (Exception e) {}
            }).start();
        }
    }
}