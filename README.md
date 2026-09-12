# Port-forwarding-software

端口转发管理器：GUI（C# WinForms）+ 六种语言的转发内核（Node.js / C++ / Python / Go / Rust / Java）。

## 目录结构

```
gui/      C# WinForms 界面 + 命令行模式（生成 .bat / 直接启动内核）
kernel/   Node.js 转发内核 middle.js
src/      其余五种语言的转发内核（同一逻辑的独立实现）
```

## 参数契约

所有内核接受同一套参数（GUI 与 middle.js 都按此调用）：

- 四参数模式：`<内核> <监听地址> <监听端口> <目标地址> <目标端口>`
- middle.js 额外支持：`--kernel node|cpp --listen-host <地址> --listen-port <端口> --target-host <地址> --target-port <端口>`
- 地址：主机名或 IPv4（C++ 内核仅支持 IPv4 字面量）
- 端口：1-65535
- 出错时内核向 stderr 输出原因并返回非 0 退出码

## 构建各内核（在仓库根目录执行）

| 内核 | 命令 | 产物 |
|---|---|---|
| C++ | `cmake -B build && cmake --build build --config Release` | `build/forward_kernel.exe` |
| Go | `cd src && go build -o go_forward.exe forward.go` | `src/go_forward.exe` |
| Rust | `cd src && cargo build --release` | `src/target/release/rust_forward.exe` |
| Java | `cd src && javac Forward.java && jar cfe java_forward.jar Forward Forward*.class` | `src/java_forward.jar` |
| Python | 无需构建，直接使用 `src/forward.py` | `src/forward.py` |
| Node.js | 无需构建，直接使用 `kernel/middle.js` | `kernel/middle.js` |

> GUI按 `.exe` 名称查找内核。Python/Java 内核是脚本/jar，若要嵌入 GUI 需自行打包成
> exe（如 pyinstaller / launch4j），或直接使用 C++/Go/Rust/Node 内核。

## 内核部署（GUI 使用前提）

GUI 启动时会把嵌入的资源解压到 `%TEMP%\PortForwardTool`。嵌入资源是可选的：

1. 把下面这些文件放进 `gui/Resources/` 目录（目录不存在就新建，已加入 .gitignore）：
   - `node.exe` — 从 https://nodejs.org/downloads 下载 win-x64 版
   - `middle.js` — 复制自 `kernel/middle.js`
   - `forward_kernel.exe` — CMake 构建产物
   - `python_forward.exe` / `go_forward.exe` / `rust_forward.exe` / `java_forward.exe` — 各自构建产物
2. `dotnet publish gui/PortForwarder.csproj -c Release -r win-x64` 会把它们全部嵌入单个 exe。

不部署资源时项目同样可以构建，只是启动对应内核会提示「内核文件不存在」。

## 构建与使用 GUI

```
dotnet build gui/PortForwarder.csproj -c Release
dotnet run --project gui -c Release              # 图形界面
dotnet run --project gui -c Release -- --kernel node --listen-port 8080 --target-host 127.0.0.1 --target-port 80
```

命令行模式支持 `--kernel <node|cpp|python|go|rust|java>` 与四个参数选项，未知参数会
报错并显示用法，退出码与内核退出码一致。

## node.exe 下载

https://nodejs.org/downloads （选择 win-x64 版本）
