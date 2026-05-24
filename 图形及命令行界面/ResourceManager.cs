using System.IO;
using System.Reflection;

namespace PortForwarder
{
    public static class ResourceManager
    {
        public static readonly string TempDir = Path.Combine(Path.GetTempPath(), "PortForwardTool");

        public static void ExtractAll()
        {
            Directory.CreateDirectory(TempDir);
            string[] resources = {
                "node.exe",
                "middle.js",
                "forward_kernel.exe",
                "python_forward.exe",
                "go_forward.exe",
                "rust_forward.exe",
                "java_forward.exe"
            };

            foreach (var res in resources)
            {
                string dest = Path.Combine(TempDir, res);
                if (!File.Exists(dest))
                {
                    using (var stream = Assembly.GetExecutingAssembly()
                        .GetManifestResourceStream("PortForwarder.Resources." + res))
                    {
                        if (stream != null)
                        {
                            using (var fs = new FileStream(dest, FileMode.Create))
                                stream.CopyTo(fs);
                        }
                    }
                }
            }
        }
    }
}