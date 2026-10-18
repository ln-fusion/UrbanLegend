using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UrbanLegend.Arg
{
    public sealed class ArgDesktop : MonoBehaviour
    {
        [Header("Editor-authored font (original assets remain unchanged)")]
        public TMP_FontAsset font;
        public Font fallbackSource;
        [Header("Authored scene layout")]
        public RectTransform desktopRoot;
        public ArgPageView[] pages;
        public RectTransform homeView, searchView, historyView;
        [SerializeField] RectTransform browserContent;
        public RectTransform chatContent, replies, modal;
        public TMP_InputField address;
        public TMP_Text location, chatStatus, saveStatus;
        public Button homeButton, historyButton, goButton, resetButton, indexButton;
        public ScrollRect browserScroll, chatScroll;
        [Header("Reusable prefab styles")]
        public TMP_Text headingPrefab, bodyPrefab, metaPrefab, chatNamePrefab, chatBodyPrefab;
        public Button linkPrefab, replyPrefab, photoButtonPrefab;
        [Header("Authored photo viewer and confirmation dialog")]
        public GameObject photoPanel, resetPanel;
        public TMP_Text photoTitle;
        public ScrollRect photoScroll;
        public ArgPhotoView[] photos;
        public Button closePhotoButton, zoomInButton, zoomOutButton, cancelResetButton, confirmResetButton;

        ContentData content;
        ArgSession session;
        ArgPhotoView activePhoto;
        string searchText = "", formNotice = "";
        bool showHistory;
        float photoZoom = 1;
        string SavePath
        {
            get
            {
#if UNITY_EDITOR
                if (UnityEditor.SessionState.GetBool("Arg.Smoke.Active", false))
                    return Path.GetFullPath("Logs/arg-smoke-save.json");
#endif
                return Path.Combine(Application.persistentDataPath, "arg-prototype-v1.json");
            }
        }

        void Awake()
        {
            if (desktopRoot == null || pages == null || pages.Length == 0)
            {
                Debug.LogError("ARG scene UI is missing. Use UrbanLegend/Build Authored UI in the editor.", this);
                enabled = false;
                return;
            }
            content = new ContentData { pages = pages.Select(p => p.Definition()).ToArray() };
            SaveData loaded = null;
            try { if (File.Exists(SavePath)) loaded = JsonUtility.FromJson<SaveData>(File.ReadAllText(SavePath)); }
            catch (Exception e) { Debug.LogWarning("Cannot load ARG save: " + e.Message); }
            session = new ArgSession(loaded);
            searchText = session.State.query;
            Bind();
            modal.gameObject.SetActive(false);
            RenderBrowser(); RenderChat();
        }
        void Bind()
        {
            homeButton.onClick.AddListener(() => Navigate("home"));
            indexButton.onClick.AddListener(() => Navigate(session.State.loggedIn ? "directory" : "login"));
            historyButton.onClick.AddListener(() => { showHistory = true; RenderBrowser(); });
            goButton.onClick.AddListener(() => Go(address.text));
            address.onSubmit.AddListener(Go);
            resetButton.onClick.AddListener(ConfirmReset);
            closePhotoButton.onClick.AddListener(CloseModal);
            zoomInButton.onClick.AddListener(() => ZoomPhoto(.25f));
            zoomOutButton.onClick.AddListener(() => ZoomPhoto(-.25f));
            cancelResetButton.onClick.AddListener(CloseModal);
            confirmResetButton.onClick.AddListener(() =>
            {
                session = new ArgSession(); searchText = ""; showHistory = false;
                foreach (var p in pages)
                {
                    if (p.userId != null) p.userId.text = "";
                    if (p.password != null) p.password.text = "";
                    if (p.recordNumber != null) p.recordNumber.text = "";
                    if (p.feedback != null) p.feedback.text = "";
                }
                CloseModal(); RenderBrowser(); RenderChat(); Save();
            });
            foreach (var page in pages)
            {
                foreach (var link in page.links)
                {
                    var target = link.target;
                    link.button.onClick.AddListener(() => Navigate(target));
                }
                if (page.photoButton != null)
                {
                    var imageId = page.imageId;
                    page.photoButton.onClick.AddListener(() => ShowPhoto(imageId));
                }
                if (page.userId != null)
                {
                    page.submit.onClick.AddListener(() => Login(page));
                    page.password.onSubmit.AddListener(_ => Login(page));
                }
                if (page.recordNumber != null)
                {
                    page.submit.onClick.AddListener(() => Lookup(page));
                    page.recordNumber.onSubmit.AddListener(_ => Lookup(page));
                }
            }
        }
        void OnApplicationPause(bool pause) { if (pause) Save(); }
        void OnApplicationQuit() => Save();
        void Save()
        {
            if (session == null) return;
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(SavePath));
                string temp = SavePath + ".tmp";
                File.WriteAllText(temp, session.Serialize());
                if (File.Exists(SavePath)) File.Replace(temp, SavePath, SavePath + ".bak");
                else File.Move(temp, SavePath);
                saveStatus.text = "进度已保存";
            }
            catch (Exception e) { Debug.LogWarning(e.Message); saveStatus.text = "保存失败，请保留当前窗口"; }
        }
        void Update() { if (UnityEngine.Input.GetKeyDown(KeyCode.Escape)) CloseModal(); }
        void Clear(Transform parent)
        {
            foreach (Transform child in parent) { child.gameObject.SetActive(false); Destroy(child.gameObject); }
        }
        TMP_Text AddText(TMP_Text prefab, Transform parent, string value)
        {
            var label = Instantiate(prefab, parent, false);
            label.text = value;
            return label;
        }
        Button AddButton(Button prefab, Transform parent, string value, Action action)
        {
            var button = Instantiate(prefab, parent, false);
            button.GetComponentInChildren<TMP_Text>(true).text = value;
            button.onClick.AddListener(() => action());
            return button;
        }
        void Go(string value)
        {
            value = value.Trim();
            string route = value.Replace("https://", "").Replace("http://", "").TrimEnd('/');
            if (route == "huisheng.invalid") { Navigate(session.State.loggedIn ? "directory" : "login"); return; }
            if (route == "xunye.invalid") { Navigate("home"); return; }
            var page = content.pages.FirstOrDefault(p => p.address == route);
            if (page != null) { Navigate(page.id); return; }
            searchText = value; session.State.query = value; session.State.page = "search";
            showHistory = false; formNotice = ""; RenderBrowser(); Save();
        }
        void Navigate(string id)
        {
            showHistory = false; formNotice = "";
            if (id == "home") session.State.page = id;
            else if (!session.Visit(content.Page(id)))
            { session.State.page = "login"; formNotice = "请先使用资料员编号登录。"; }
            RenderBrowser(); RenderChat(); Save();
        }
        void RenderBrowser()
        {
            // Only generated lists are rebuilt. Authored page objects are shown/hidden intact.
            foreach (var page in pages) page.gameObject.SetActive(false);
            homeView.gameObject.SetActive(false); searchView.gameObject.SetActive(false); historyView.gameObject.SetActive(false);
            if (showHistory)
            {
                browserContent = historyView; Clear(historyView);
                location.text = "浏览历史"; AddText(headingPrefab, historyView, "最近查看");
                foreach (var id in session.State.history.AsEnumerable().Reverse())
                {
                    var p = content.Page(id);
                    if (p != null) AddButton(linkPrefab, historyView, p.title, () => Navigate(p.id));
                }
            }
            else if (session.State.page == "home")
            {
                browserContent = homeView; location.text = "xunye.invalid"; address.text = "";
            }
            else if (session.State.page == "search")
            {
                browserContent = searchView; Clear(searchView);
                location.text = "xunye.invalid"; address.text = searchText;
                AddText(headingPrefab, searchView, "寻页"); AddText(bodyPrefab, searchView, "搜索：" + searchText);
                var results = content.Search(searchText);
                if (results.Length == 0) AddText(bodyPrefab, searchView, "没有找到相关页面。试试人物昵称、标题中的关键词，或回看聊天里的线索。");
                foreach (var p in results)
                {
                    AddButton(linkPrefab, searchView, p.title, () => Navigate(p.id));
                    AddText(metaPrefab, searchView, p.source + "\n" + p.body.Substring(0, Math.Min(90, p.body.Length)) + "……");
                }
            }
            else
            {
                var view = pages.FirstOrDefault(p => p.pageId == session.State.page) ?? pages.First(p => p.pageId == "login");
                var definition = content.Page(view.pageId);
                browserContent = (RectTransform)view.transform;
                address.text = definition.address; location.text = definition.source;
                if (view.pageId == "login") view.feedback.text = formNotice;
            }
            browserContent.gameObject.SetActive(true);
            browserScroll.content = browserContent;
            Canvas.ForceUpdateCanvases(); browserScroll.verticalNormalizedPosition = 1;
        }
        void Login(ArgPageView page)
        {
            if (session.Login(page.userId.text, page.password.text))
            { page.password.text = ""; page.feedback.text = ""; Navigate("directory"); }
            else page.feedback.text = "编号或阅览口令不匹配，请核对。可再次尝试。";
        }
        void Lookup(ArgPageView page)
        {
            switch (ContentData.Normalize(page.recordNumber.text))
            {
                case "b06": Navigate("box"); break;
                case "r02": Navigate("record"); break;
                case "m11": Navigate("mall"); break;
                case "t04": Navigate("metro"); break;
                case "q17": Navigate("continuation"); break;
                default: page.feedback.text = "未找到该编号。请核对箱签、记录说明或聊天图片。"; break;
            }
        }
        void RenderChat()
        {
            Clear(chatContent); Clear(replies);
            chatStatus.text = session.State.conversation >= 2 ? "周祁" : "周祁 · 聊天";
            foreach (var message in session.State.chat)
            {
                AddText(chatNamePrefab, chatContent, message.sender);
                if (!string.IsNullOrEmpty(message.text)) AddText(chatBodyPrefab, chatContent, message.text);
                if (!string.IsNullOrEmpty(message.image))
                {
                    string id = message.image;
                    AddButton(photoButtonPrefab, chatContent, "[图片] " + photos.First(p => p.imageId == id).caption + " · 点击放大", () => ShowPhoto(id));
                }
            }
            foreach (var reply in session.Replies())
                AddButton(replyPrefab, replies, reply, () => { if (session.Reply(reply)) { RenderChat(); Save(); } });
            if (session.State.conversation == 3) AddText(metaPrefab, replies, "消息已发送");
            Canvas.ForceUpdateCanvases(); chatScroll.verticalNormalizedPosition = 0;
        }
        void ShowPhoto(string id)
        {
            activePhoto = photos.First(p => p.imageId == id);
            foreach (var photo in photos) photo.gameObject.SetActive(photo == activePhoto);
            modal.gameObject.SetActive(true); photoPanel.SetActive(true); resetPanel.SetActive(false);
            photoTitle.text = activePhoto.title;
            photoScroll.content = (RectTransform)activePhoto.transform;
            photoZoom = 1; activePhoto.transform.localScale = Vector3.one;
            ((RectTransform)activePhoto.transform).anchoredPosition = Vector2.zero;
            Canvas.ForceUpdateCanvases(); photoScroll.verticalNormalizedPosition = 1;
        }
        void ZoomPhoto(float delta)
        {
            if (activePhoto == null) return;
            photoZoom = Mathf.Clamp(photoZoom + delta, .75f, 2.5f);
            activePhoto.transform.localScale = new Vector3(photoZoom, photoZoom, 1);
        }
        void CloseModal() => modal.gameObject.SetActive(false);
        void ConfirmReset()
        {
            modal.gameObject.SetActive(true); photoPanel.SetActive(false); resetPanel.SetActive(true);
        }
    }
}
