using System;
using System.Text.RegularExpressions;

namespace PortForwarder
{
    // Single source of truth for kernel names, kernel executable names,
    // the shared defaults and input validation. Used by both the GUI and
    // command line mode so the two cannot drift apart.
    public static class KernelMap
    {
        public static readonly string[] DisplayNames = { "Node.js", "C++", "Python", "Go", "Rust", "Java" };

        public static readonly string DefaultListenHost = "0.0.0.0";
        public static readonly string DefaultListenPort = "8080";
        public static readonly string DefaultTargetHost = "127.0.0.1";
        public static readonly string DefaultTargetPort = "80";

        // Hosts end up in command lines and .bat files; only allow values
        // made of letters/digits/dots/dashes so user input can never inject
        // commands. Hostnames and IPv4 addresses both match.
        private static readonly Regex HostRegex = new Regex(@"^[A-Za-z0-9](?:[A-Za-z0-9.-]*[A-Za-z0-9])?$");

        public static bool IsValidHost(string host)
        {
            return !string.IsNullOrEmpty(host) && host.Length <= 253 && HostRegex.IsMatch(host);
        }

        public static bool IsValidPort(string port)
        {
            return ushort.TryParse(port, out ushort value) && value > 0;
        }

        public static string[] KernelNames()
        {
            return new string[] { "node", "cpp", "python", "go", "rust", "java" };
        }

        public static string ExeName(string kernel)
        {
            switch (kernel)
            {
                case "node": return "node";
                case "cpp": return "forward_kernel";
                case "python": return "python_forward";
                case "go": return "go_forward";
                case "rust": return "rust_forward";
                case "java": return "java_forward";
                default: return "forward_kernel";
            }
        }

        public static string ExePath(string kernel)
        {
            return System.IO.Path.Combine(ResourceManager.TempDir, ExeName(kernel) + ".exe");
        }
    }
}
