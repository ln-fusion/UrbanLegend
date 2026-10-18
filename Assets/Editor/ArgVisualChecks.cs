using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UrbanLegend.Arg;

namespace UrbanLegend.Editor
{
    [InitializeOnLoad]
    public static class ArgVisualChecks
    {
        static double next;
        static readonly BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
        static ArgVisualChecks()
        {
            EditorApplication.update += Tick;
            EditorApplication.delayCall += () =>
            {
                if (File.Exists("Temp/arg-smoke.request"))
                { File.Delete("Temp/arg-smoke.request"); Start(); }
            };
        }
        [MenuItem("UrbanLegend/验证两种比例并截图")]
        public static void Start()
        {
            if (EditorApplication.isPlaying || EditorSceneManager.GetActiveScene().isDirty)
            { Debug.LogWarning("ARG visual check: save the scene and exit Play Mode first."); return; }
            SessionState.SetString("Arg.Smoke.Previous", EditorSceneManager.GetActiveScene().path);
            SessionState.SetInt("Arg.Smoke.Step", 0);
            SessionState.SetBool("Arg.Smoke.Active", true);
            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/arg-visual-checks.txt", "Running visual checks\n");
            if (File.Exists("Logs/arg-smoke-save.json")) File.Delete("Logs/arg-smoke-save.json");
            EditorSceneManager.OpenScene("Assets/Scenes/ArgPrototype.unity");
            SetSize(1920, 1080);
            EditorApplication.isPlaying = true;
        }
        static void SetSize(int width, int height)
        {
            var assembly = typeof(UnityEditor.Editor).Assembly;
            var sizesType = assembly.GetType("UnityEditor.GameViewSizes");
            object sizes = sizesType.BaseType.GetProperty("instance", BindingFlags.Public | BindingFlags.Static).GetValue(null);
            var groupMethod = sizesType.GetMethod("GetGroup");
            object group = groupMethod.Invoke(sizes, new[] {Enum.ToObject(groupMethod.GetParameters()[0].ParameterType, 0)});
            int index = (int)group.GetType().GetMethod("GetTotalCount").Invoke(group, null);
            var sizeType = assembly.GetType("UnityEditor.GameViewSize");
            var kind = assembly.GetType("UnityEditor.GameViewSizeType");
            object size = Activator.CreateInstance(sizeType, new[] {Enum.ToObject(kind, 1), (object)width, height, "ARG check " + width + "x" + height});
            group.GetType().GetMethod("AddCustomSize").Invoke(group, new[] {size});
            var view = EditorWindow.GetWindow(assembly.GetType("UnityEditor.GameView"));
            view.GetType().GetProperty("selectedSizeIndex", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).SetValue(view, index);
            view.Show();
        }
        static void Call(ArgDesktop app, string method, params object[] args) =>
            typeof(ArgDesktop).GetMethod(method, Hidden).Invoke(app, args);
        static void Tick()
        {
            if (!SessionState.GetBool("Arg.Smoke.Active", false) || EditorApplication.isCompiling) return;
            int step = SessionState.GetInt("Arg.Smoke.Step", 0);
            if (step == 9 && !EditorApplication.isPlaying && !EditorApplication.isPlayingOrWillChangePlaymode)
            {
                SessionState.SetBool("Arg.Smoke.Active", false);
                string previous = SessionState.GetString("Arg.Smoke.Previous", "");
                if (!string.IsNullOrEmpty(previous)) EditorSceneManager.OpenScene(previous);
                return;
            }
            if (!EditorApplication.isPlaying || EditorApplication.timeSinceStartup < next) return;
            var app = UnityEngine.Object.FindObjectOfType<ArgDesktop>();
            if (app == null) return;
            try
            {
                Directory.CreateDirectory("Logs");
                switch (step)
                {
                    case 0: Call(app, "Go", "回声17 编号"); break;
                    case 1: CheckBounds(app, 1920, 1080); ScreenCapture.CaptureScreenshot(Path.GetFullPath("Logs/arg-16x9-search.png")); break;
                    case 2: Call(app, "ShowPhoto", "IMG01"); break;
                    case 3: ScreenCapture.CaptureScreenshot(Path.GetFullPath("Logs/arg-16x9-photo.png")); break;
                    case 4:
                        SetSize(1920, 1200);
                        var modal = (RectTransform)typeof(ArgDesktop).GetField("modal", Hidden).GetValue(app);
                        modal.gameObject.SetActive(false);
                        Call(app, "Navigate", "login");
                        var browser = (RectTransform)typeof(ArgDesktop).GetField("browserContent", Hidden).GetValue(app);
                        var fields = browser.GetComponentsInChildren<TMP_InputField>();
                        fields[0].text = "hx_0417"; fields[1].text = "huisheng09";
                        fields[1].onSubmit.Invoke(fields[1].text);
                        var s = (ArgSession)typeof(ArgDesktop).GetField("session", Hidden).GetValue(app);
                        if (!s.State.loggedIn) throw new Exception("Login form submission failed");
                        Call(app, "Navigate", "record");
                        s.Reply("先出来，这箱以前出过问题。"); s.Reply("没有，我一直在打字。");
                        Call(app, "RenderChat"); break;
                    case 5: CheckBounds(app, 1920, 1200); ScreenCapture.CaptureScreenshot(Path.GetFullPath("Logs/arg-16x10-chat.png")); break;
                    case 6: Call(app, "ShowPhoto", "IMG03"); break;
                    case 7:
                        ScreenCapture.CaptureScreenshot(Path.GetFullPath("Logs/arg-16x10-photo.png")); break;
                    case 8:
                        File.AppendAllText("Logs/arg-visual-checks.txt", "PASS: both aspect ratios, browser/chat bounds, search, login, record, final photo.\n");
                        EditorApplication.isPlaying = false; break;
                }
                SessionState.SetInt("Arg.Smoke.Step", step + 1);
                next = EditorApplication.timeSinceStartup + 1.2;
            }
            catch (Exception e)
            {
                File.WriteAllText("Logs/arg-visual-checks.txt", e.ToString()); Debug.LogException(e);
                SessionState.SetInt("Arg.Smoke.Step", 9); EditorApplication.isPlaying = false;
            }
        }
        static void CheckBounds(ArgDesktop app, int width, int height)
        {
            Canvas.ForceUpdateCanvases();
            if (Screen.width != width || Screen.height != height) throw new Exception("Unexpected GameView resolution " + Screen.width + "x" + Screen.height);
            foreach (var name in new[] {"Browser", "Chat"})
            {
                var rect = (RectTransform)app.transform.Find("ARG Desktop/" + name);
                var corners = new Vector3[4]; rect.GetWorldCorners(corners);
                foreach (var c in corners)
                    if (c.x < -1 || c.y < -1 || c.x > width + 1 || c.y > height + 1) throw new Exception("Offscreen panel " + name);
            }
            File.AppendAllText("Logs/arg-visual-checks.txt", "Bounds PASS " + width + "x" + height + "\n");
        }
    }
}
