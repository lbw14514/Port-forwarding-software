using System;
using System.Diagnostics;
using System.IO;

namespace PortForwarder
{
    public static class CommandLineMode
    {
        public static void Run(string[] args)
        {
            var missing = ResourceManager.ExtractAll();
            foreach (var file in missing)
                Console.WriteLine("警告: 内核文件未部署: " + file);

            string kernel = "cpp";
            string listenHost = KernelMap.DefaultListenHost;
            string listenPort = KernelMap.DefaultListenPort;
            string targetHost = KernelMap.DefaultTargetHost;
            string targetPort = KernelMap.DefaultTargetPort;

            try
            {
                for (int i = 0; i < args.Length; i++)
                {
                    switch (args[i])
                    {
                        case "--kernel": kernel = NextValue(args, ref i); break;
                        case "--listen-host": listenHost = NextValue(args, ref i); break;
                        case "--listen-port": listenPort = NextValue(args, ref i); break;
                        case "--target-host": targetHost = NextValue(args, ref i); break;
                        case "--target-port": targetPort = NextValue(args, ref i); break;
                        default:
                            Console.WriteLine("未知参数: " + args[i]);
                            PrintUsage();
                            Environment.ExitCode = 2;
                            return;
                    }
                }
            }
            catch (ArgumentException ex)
            {
                Console.WriteLine(ex.Message);
                PrintUsage();
                Environment.ExitCode = 2;
                return;
            }

            string[] knownKernels = KernelMap.KernelNames();
            if (Array.IndexOf(knownKernels, kernel) < 0)
            {
                Console.WriteLine("未知内核: " + kernel + "（可选: " + string.Join(", ", knownKernels) + "）");
                Environment.ExitCode = 2;
                return;
            }
            if (!KernelMap.IsValidHost(listenHost) || !KernelMap.IsValidHost(targetHost) ||
                !KernelMap.IsValidPort(listenPort) || !KernelMap.IsValidPort(targetPort))
            {
                Console.WriteLine("参数无效：地址只能包含字母/数字/点/横线，端口必须是 1-65535 的整数。");
                Environment.ExitCode = 2;
                return;
            }

            string exePath = KernelMap.ExePath(kernel);
            if (!File.Exists(exePath))
            {
                Console.WriteLine("内核文件不存在: " + exePath + "（请阅读 README.md 的「内核部署」章节）");
                Environment.ExitCode = 2;
                return;
            }

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
                using Process? process = Process.Start(psi);
                if (process == null)
                {
                    Console.WriteLine("启动失败：进程未能创建。");
                    Environment.ExitCode = 1;
                    return;
                }
                process.WaitForExit();
                // Propagate the kernel exit code to our own exit code.
                Environment.ExitCode = process.ExitCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine("启动失败：" + ex.Message);
                Environment.ExitCode = 1;
            }
        }

        private static string NextValue(string[] args, ref int i)
        {
            if (i + 1 >= args.Length)
                throw new ArgumentException("缺少参数值: " + args[i]);
            return args[++i];
        }

        private static void PrintUsage()
        {
            Console.WriteLine("用法: PortForwarder --kernel <node|cpp|python|go|rust|java>" +
                " [--listen-host <地址>] [--listen-port <端口>] [--target-host <地址>] [--target-port <端口>]");
        }
    }
}
