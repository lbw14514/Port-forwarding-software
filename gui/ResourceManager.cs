using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace PortForwarder
{
    public static class ResourceManager
    {
        public static readonly string TempDir = Path.Combine(Path.GetTempPath(), "PortForwardTool");

        // Extracts the embedded kernels into TempDir. Files already present are
        // left alone. Returns the names of kernels that are neither embedded
        // nor deployed, so callers can tell the user what will not work.
        public static List<string> ExtractAll()
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

            var missing = new List<string>();
            foreach (var res in resources)
            {
                string dest = Path.Combine(TempDir, res);
                if (File.Exists(dest))
                    continue;
                using (var stream = Assembly.GetExecutingAssembly()
                    .GetManifestResourceStream("PortForwarder.Resources." + res))
                {
                    if (stream != null)
                    {
                        using (var fs = new FileStream(dest, FileMode.Create))
                            stream.CopyTo(fs);
                    }
                    else
                    {
                        missing.Add(res);
                    }
                }
            }
            return missing;
        }
    }
}
