using System;
using System.Diagnostics;
using System.IO;

namespace PortForwarder
{
    public static class CommandLineMode
    {
        public static void Run(string[] args)
        {
            ResourceManager.ExtractAll();

            string kernel = "cpp";
            string listenHost = "0.0.0.0";
            string listenPort = "8080";
            string targetHost = "127.0.0.1";
            string targetPort = "80";

            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "--kernel": kernel = args[++i]; break;
                    case "--listen-host": listenHost = args[++i]; break;
                    case "--listen-port": listenPort = args[++i]; break;
                    case "--target-host": targetHost = args[++i]; break;
                    case "--target-port": targetPort = args[++i]; break;
                }
            }

            string exe = kernel switch
            {
                "node" => "node.exe",
                "python" => "python_forward.exe",
                "go" => "go_forward.exe",
                "rust" => "rust_forward.exe",
                "java" => "java_forward.exe",
                _ => "forward_kernel.exe"
            };

            string exePath = Path.Combine(ResourceManager.TempDir, exe);
            ProcessStartInfo psi = new ProcessStartInfo
            {
                WorkingDirectory = ResourceManager.TempDir,
                UseShellExecute = false
            };

            if (kernel == "node")
            {
                psi.FileName = exePath;
                psi.Arguments = "\"middle.js\" --kernel node --listen-host " + listenHost +
                                " --listen-port " + listenPort + " --target-host " + targetHost +
                                " --target-port " + targetPort;
            }
            else
            {
                psi.FileName = exePath;
                psi.Arguments = listenHost + " " + listenPort + " " + targetHost + " " + targetPort;
            }

            try
            {
                Process.Start(psi)?.WaitForExit();
            }
            catch (Exception ex)
            {
                Console.WriteLine("启动失败：" + ex.Message);
            }
        }
    }
}