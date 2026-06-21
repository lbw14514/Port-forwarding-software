using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using System.Diagnostics;

namespace PortForwarder
{
    public partial class MainForm : Form
    {
        private ComboBox cbKernel;
        private TextBox txtListenHost, txtListenPort, txtTargetHost, txtTargetPort;
        private Button btnTopology, btnGenerateBat, btnStart;
        private Label lblStatus;

        public MainForm()
        {
            ResourceManager.ExtractAll();

            this.Text = "端口转发管理器 - by wuyulbw";
            this.Size = new Size(460, 380);
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.StartPosition = FormStartPosition.CenterScreen;

            Label lblListenAddr = new Label
            {
                Text = "本机监听地址 (别人连你的哪台电脑? 例如: 0.0.0.0 表示所有网卡, 127.0.0.1 仅本机):",
                Location = new Point(20, 20),
                AutoSize = true
            };
            txtListenHost = new TextBox { Location = new Point(20, 45), Width = 280, Text = "0.0.0.0" };

            Label lblListenPort = new Label
            {
                Text = "本机监听端口 (别人访问你的哪个端口? 也就是「转发入口端口」, 例如: 8080):",
                Location = new Point(20, 80),
                AutoSize = true
            };
            txtListenPort = new TextBox { Location = new Point(20, 105), Width = 80, Text = "8080" };

            Label lblTargetAddr = new Label
            {
                Text = "目标服务器地址 (最终数据要发给谁? 例如: 192.168.1.100):",
                Location = new Point(20, 140),
                AutoSize = true
            };
            txtTargetHost = new TextBox { Location = new Point(20, 165), Width = 280, Text = "127.0.0.1" };

            Label lblTargetPort = new Label
            {
                Text = "目标服务器端口 (最终服务的端口, 也就是「原来服务的端口」, 例如网站是80):",
                Location = new Point(20, 200),
                AutoSize = true
            };
            txtTargetPort = new TextBox { Location = new Point(20, 225), Width = 80, Text = "80" };

            Label lblKernel = new Label
            {
                Text = "选择转发内核:",
                Location = new Point(20, 260),
                AutoSize = true
            };
            cbKernel = new ComboBox
            {
                Location = new Point(120, 258),
                Width = 150,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cbKernel.Items.AddRange(new string[] { "Node.js", "C++", "Python", "Go", "Rust", "Java" });
            cbKernel.SelectedIndex = 1;

            btnTopology = new Button { Text = "拓扑图", Location = new Point(20, 295), Width = 80 };
            btnTopology.Click += (s, e) => ShowTopology();

            btnGenerateBat = new Button { Text = "生成 .bat", Location = new Point(110, 295), Width = 80 };
            btnGenerateBat.Click += (s, e) => GenerateBat();

            btnStart = new Button { Text = "启动转发", Location = new Point(200, 295), Width = 80 };
            btnStart.Click += (s, e) => StartForward();

            lblStatus = new Label
            {
                Text = "状态：就绪 - by wuyulbw",
                Location = new Point(20, 340),
                AutoSize = true
            };

            this.Controls.AddRange(new Control[] {
                lblListenAddr, txtListenHost,
                lblListenPort, txtListenPort,
                lblTargetAddr, txtTargetHost,
                lblTargetPort, txtTargetPort,
                lblKernel, cbKernel,
                btnTopology, btnGenerateBat, btnStart,
                lblStatus
            });
        }

        private string GetKernelName()
        {
            string[] names = { "node", "cpp", "python", "go", "rust", "java" };
            return names[cbKernel.SelectedIndex];
        }

        private string GetExePath(string kernel)
        {
            string name = kernel switch
            {
                "node" => "node",
                "cpp" => "forward_kernel",
                "python" => "python_forward",
                "go" => "go_forward",
                "rust" => "rust_forward",
                "java" => "java_forward",
                _ => "forward_kernel"
            };
            return Path.Combine(ResourceManager.TempDir, name + ".exe");
        }

        private void ShowTopology()
        {
            string msg =
                $"外部用户 ──→ {txtListenHost.Text}:{txtListenPort.Text} (本机转发入口)\n" +
                $"        ↓\n" +
                $"   ┌──────────┐\n" +
                $"   │  本机转发  │\n" +
                $"   └──────────┘\n" +
                $"        ↓\n" +
                $"   转发到：{txtTargetHost.Text}:{txtTargetPort.Text} (目标服务)\n" +
                $"内核：{cbKernel.SelectedItem}";
            MessageBox.Show(msg, "转发拓扑图");
        }

        private void GenerateBat()
        {
            string kernel = GetKernelName();
            string exePath = GetExePath(kernel);
            string listenHost = txtListenHost.Text;
            string listenPort = txtListenPort.Text;
            string targetHost = txtTargetHost.Text;
            string targetPort = txtTargetPort.Text;

            string batContent =
                "@echo off\r\n" +
                "chcp 65001 >nul\r\n" +
                "echo ====================================\r\n" +
                "echo   端口转发已启动\r\n" +
                $"echo   监听地址: {listenHost}:{listenPort}\r\n" +
                $"echo   目标地址: {targetHost}:{targetPort}\r\n" +
                $"echo   转发方向: 从 {listenHost}:{listenPort} --> {targetHost}:{targetPort}\r\n" +
                "echo ====================================\r\n";

            if (kernel == "node")
                batContent +=
                    $"\"{exePath}\" \"{Path.Combine(ResourceManager.TempDir, "middle.js")}\" " +
                    $"--kernel node --listen-host {listenHost} --listen-port {listenPort} " +
                    $"--target-host {targetHost} --target-port {targetPort}\r\n";
            else
                batContent +=
                    $"\"{exePath}\" {listenHost} {listenPort} {targetHost} {targetPort}\r\n";

            batContent += "pause\r\n";

            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            string batFilePath = Path.Combine(desktopPath, "启动转发.bat");
            File.WriteAllText(batFilePath, batContent, System.Text.Encoding.UTF8);

            lblStatus.Text = $"已生成 启动转发.bat 到桌面 ({batFilePath})";
            MessageBox.Show($"已生成批处理文件：\n{batFilePath}\n\n双击即可启动转发。", "生成成功",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void StartForward()
        {
            string kernel = GetKernelName();
            string exePath = GetExePath(kernel);
            string listenHost = txtListenHost.Text;
            string listenPort = txtListenPort.Text;
            string targetHost = txtTargetHost.Text;
            string targetPort = txtTargetPort.Text;

            try
            {
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    WorkingDirectory = ResourceManager.TempDir,
                    UseShellExecute = false
                };

                if (kernel == "node")
                {
                    psi.FileName = exePath;
                    psi.Arguments =
                        $"\"{Path.Combine(ResourceManager.TempDir, "middle.js")}\" " +
                        $"--kernel node --listen-host {listenHost} --listen-port {listenPort} " +
                        $"--target-host {targetHost} --target-port {targetPort}";
                }
                else
                {
                    psi.FileName = exePath;
                    psi.Arguments = $"{listenHost} {listenPort} {targetHost} {targetPort}";
                }

                Process.Start(psi);
                lblStatus.Text = "转发已启动 - by wuyulbw";
            }
            catch (Exception ex)
            {
                MessageBox.Show("启动失败：" + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}