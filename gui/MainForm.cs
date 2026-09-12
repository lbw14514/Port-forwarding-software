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
            var missing = ResourceManager.ExtractAll();
            if (missing.Count > 0)
            {
                MessageBox.Show(
                    "以下内核文件未部署，启动对应内核时会失败：\n" + string.Join("\n", missing) +
                    "\n\n请阅读 README.md 的「内核部署」章节。",
                    "内核未部署", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }

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
            txtListenHost = new TextBox { Location = new Point(20, 45), Width = 280, Text = KernelMap.DefaultListenHost };

            Label lblListenPort = new Label
            {
                Text = "本机监听端口 (别人访问你的哪个端口? 也就是「转发入口端口」, 例如: 8080):",
                Location = new Point(20, 80),
                AutoSize = true
            };
            txtListenPort = new TextBox { Location = new Point(20, 105), Width = 80, Text = KernelMap.DefaultListenPort };

            Label lblTargetAddr = new Label
            {
                Text = "目标服务器地址 (最终数据要发给谁? 例如: 192.168.1.100):",
                Location = new Point(20, 140),
                AutoSize = true
            };
            txtTargetHost = new TextBox { Location = new Point(20, 165), Width = 280, Text = KernelMap.DefaultTargetHost };

            Label lblTargetPort = new Label
            {
                Text = "目标服务器端口 (最终服务的端口, 也就是「原来服务的端口」, 例如网站是80):",
                Location = new Point(20, 200),
                AutoSize = true
            };
            txtTargetPort = new TextBox { Location = new Point(20, 225), Width = 80, Text = KernelMap.DefaultTargetPort };

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
            cbKernel.Items.AddRange(KernelMap.DisplayNames);
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
            return KernelMap.KernelNames()[cbKernel.SelectedIndex];
        }

        // Reads and validates the four text fields. Hosts and ports end up in
        // command lines and .bat files, so only well-formed values are accepted.
        private bool TryGetInputs(out string listenHost, out string listenPort,
                                  out string targetHost, out string targetPort)
        {
            listenHost = txtListenHost.Text.Trim();
            listenPort = txtListenPort.Text.Trim();
            targetHost = txtTargetHost.Text.Trim();
            targetPort = txtTargetPort.Text.Trim();

            if (!KernelMap.IsValidHost(listenHost) || !KernelMap.IsValidHost(targetHost))
            {
                MessageBox.Show("地址只能包含字母、数字、点和横线（例如 0.0.0.0 或 192.168.1.100）。",
                    "输入无效", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
            if (!KernelMap.IsValidPort(listenPort) || !KernelMap.IsValidPort(targetPort))
            {
                MessageBox.Show("端口必须是 1-65535 的整数。", "输入无效",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
            return true;
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
            if (!TryGetInputs(out string listenHost, out string listenPort,
                              out string targetHost, out string targetPort))
                return;

            string kernel = GetKernelName();
            string exePath = KernelMap.ExePath(kernel);

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
            if (!TryGetInputs(out string listenHost, out string listenPort,
                              out string targetHost, out string targetPort))
                return;

            string kernel = GetKernelName();
            string exePath = KernelMap.ExePath(kernel);
            if (!File.Exists(exePath))
            {
                MessageBox.Show($"内核文件不存在：\n{exePath}\n\n请阅读 README.md 的「内核部署」章节。",
                    "内核未部署", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

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

                using (Process? process = Process.Start(psi))
                {
                    if (process == null)
                    {
                        MessageBox.Show("启动失败：进程未能创建。", "错误",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                }
                lblStatus.Text = "转发已启动 - by wuyulbw";
            }
            catch (Exception ex)
            {
                MessageBox.Show("启动失败：" + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
