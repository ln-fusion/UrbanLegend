# Unity MCP

- 插件：CoplayDev MCP for Unity 10.3.0（兼容本项目 Unity 2022.3）。
- 安装：`Packages/com.coplaydev.unity-mcp-10.3.0.tgz`，由官方 v10.3.0 源码包制作，包含上游许可证；由 manifest 引用并随项目保存。
- Codex 已有连接配置：`http://127.0.0.1:8080/mcp`。
- Unity 菜单：Window > MCP for Unity。使用 HTTP、本地地址 `http://127.0.0.1:8080`，启动服务并 Start Session。Connected 表示编辑器已连接。
- 如需手动启动服务，在终端运行：

```powershell
uvx --from mcpforunityserver==10.3.0 mcp-for-unity --transport http --http-url http://127.0.0.1:8080
```

Codex 若未加载 Unity 工具，在服务和 Unity Session 启动后重启 Codex。

字体放入 `Assets/Fonts/`，支持 `.ttf`、`.otf`。项目已安装 TextMeshPro；后续使用 TMP 时再根据界面文本制作字体资源，中文字体需要包含对应字形。

官方安装说明：https://coplaydev.github.io/unity-mcp/getting-started/install

源码版本：https://github.com/CoplayDev/unity-mcp/tree/v10.3.0
