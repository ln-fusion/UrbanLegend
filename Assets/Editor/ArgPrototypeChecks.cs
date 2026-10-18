using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UrbanLegend.Arg;

namespace UrbanLegend.Editor
{
    [InitializeOnLoad]
    public static class ArgPrototypeChecks
    {
        static ArgPrototypeChecks()
        {
            EditorApplication.delayCall += () =>
            {
                if (!SessionState.GetBool("Arg.Checked.v1", false))
                { SessionState.SetBool("Arg.Checked.v1", true); Run(); }
            };
        }
        static void Require(bool condition, string message)
        { if (!condition) throw new Exception("ARG check failed: " + message); }

        [MenuItem("UrbanLegend/验证第一段逻辑")]
        public static void Run()
        {
            try
            {
                var data = JsonUtility.FromJson<ContentData>(File.ReadAllText("Assets/Resources/Arg/FirstInvestigation.json"));
                var app = UnityEngine.Object.FindObjectOfType<ArgDesktop>();
                if (app != null && app.pages != null && app.pages.Length > 0)
                {
                    Require(app.desktopRoot != null, "authored Canvas exists before Play");
                    Require(EditorUtility.IsPersistent(app.bodyPrefab), "body is a prefab asset");
                    Require(EditorUtility.IsPersistent(app.replyPrefab), "reply is a prefab asset");
                    Require(app.pages.All(p => p.titleText != null && p.bodyText != null), "authored page labels");
                    data.pages = app.pages.Select(p => p.Definition()).ToArray();
                }
                Require(data.pages.Select(p => p.id).Distinct().Count() == data.pages.Length, "unique page IDs");
                foreach (var p in data.pages)
                {
                    foreach (var link in p.links ?? Array.Empty<PageLink>()) Require(data.Page(link.target) != null, "link " + link.target);
                    if (!string.IsNullOrEmpty(p.image)) Require(data.art.Any(a => a.id == p.image), "art " + p.image);
                }
                Require(data.Search("沪上异闻录 回声").Any(p => p.id == "thread"), "initial search");
                Require(data.Search("回声17 编号").Any(p => p.id == "report"), "report alias");
                Require(data.Search("Q17").Any(p => p.id == "continuation"), "continuation search");
                Require(!data.Search("B06").Any(p => p.restricted), "private records not in public search");
                var s = new ArgSession();
                Require(!s.Login("回声17", "huisheng09"), "nickname is not login ID");
                Require(!s.Login("hx_0417", "wrong"), "wrong password");
                Require(!s.Visit(data.Page("record")), "locked page");
                Require(s.Login(" HX_0417 ", " HUISHENG09 "), "credentials and trimming");
                int messages = s.State.chat.Count;
                s.Login("hx_0417", "huisheng09");
                Require(s.State.chat.Count == messages, "arrival must not repeat");
                s.Visit(data.Page("metro")); s.Visit(data.Page("mall")); s.Visit(data.Page("record"));
                Require(s.Reply("先出来，这箱以前出过问题。"), "warning reply");
                s = new ArgSession(JsonUtility.FromJson<SaveData>(s.Serialize()));
                Require(s.State.conversation == 1, "resume before final photo");
                Require(s.Reply("没有，我一直在打字。"), "final photo reply");
                Require(s.State.chat.Count(c => c.image == "IMG03") == 1, "final photo once");
                Require(!s.Reply("没有，我一直在打字。"), "stale reply rejected");
                Require(s.Visit(data.Page("continuation")), "investigation continues after silence");
                var alternate = new ArgSession(); alternate.Login("hx_0417", "huisheng09");
                alternate.Visit(data.Page("mall")); alternate.Visit(data.Page("metro")); alternate.Visit(data.Page("record"));
                Require(alternate.Reply("你看一下刚才的登记表。"), "alternate reply");
                Require(alternate.Reply("没有，我一直在打字。"), "alternate reaches silence");
                Directory.CreateDirectory("Logs");
                File.WriteAllText("Logs/arg-logic-checks.txt", "PASS: authored scene and prefab references (when open), content references, search aliases, access control, credentials, both investigation orders, both replies, continuation, idempotence, save resume.\n");
                Debug.Log("ARG prototype logic checks PASS");
            }
            catch (Exception e)
            {
                Directory.CreateDirectory("Logs"); File.WriteAllText("Logs/arg-logic-checks.txt", e.ToString());
                Debug.LogException(e);
            }
        }

        [MenuItem("UrbanLegend/Check Prototype Logic")]
        public static void RunFromMcp() => Run();

        [MenuItem("UrbanLegend/打开第一段原型")]
        static void Open()
        {
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                EditorSceneManager.OpenScene("Assets/Scenes/ArgPrototype.unity");
        }
    }
}
