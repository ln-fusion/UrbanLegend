using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UrbanLegend.Arg;

namespace UrbanLegend.Editor
{
    // One-time migration into normal scene objects / prefab assets. Never called by the player.
    [InitializeOnLoad]
    public static class ArgUiAuthoring
    {
        const string Folder = "Assets/Prefabs/Arg";
        static TMP_FontAsset font;
        static readonly Color Ink = new Color32(40,49,53,255);
        static readonly Color Paper = new Color32(241,238,228,255);
        static readonly Color Accent = new Color32(43,93,86,255);
        static ArgUiAuthoring()
        {
            EditorApplication.delayCall += () =>
            {
                if (!File.Exists("Temp/arg-author-ui.request")) return;
                File.Delete("Temp/arg-author-ui.request");
                Build();
            };
        }
        static RectTransform Box(string name, Transform parent, Color? color = null)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            if (color.HasValue) rect.gameObject.AddComponent<Image>().color = color.Value;
            return rect;
        }
        static void Stretch(RectTransform r, float x0, float y0, float x1, float y1, float inset = 0)
        {
            r.anchorMin = new Vector2(x0,y0); r.anchorMax = new Vector2(x1,y1);
            r.offsetMin = new Vector2(inset,inset); r.offsetMax = new Vector2(-inset,-inset);
        }
        static void Height(Component c, float h, float width = -1)
        {
            var le = c.GetComponent<LayoutElement>() ?? c.gameObject.AddComponent<LayoutElement>();
            le.minHeight = le.preferredHeight = h;
            if (width > 0) le.minWidth = le.preferredWidth = width;
        }
        static TMP_Text Text(Transform parent, string value, int size = 23, Color? color = null, string name = "Text")
        {
            var t = Box(name,parent).gameObject.AddComponent<TextMeshProUGUI>();
            t.font = font; t.text = value; t.fontSize = size; t.color = color ?? Ink;
            t.richText = false; t.raycastTarget = false; t.enableWordWrapping = true;
            return t;
        }
        static void Vertical(RectTransform r, int padding = 22, int spacing = 14)
        {
            var group = r.gameObject.AddComponent<VerticalLayoutGroup>();
            group.padding = new RectOffset(padding,padding,padding,padding); group.spacing = spacing;
            group.childControlWidth = group.childControlHeight = true;
            group.childForceExpandWidth = true; group.childForceExpandHeight = false;
        }
        static RectTransform Content(Transform parent, string name)
        {
            var r = Box(name,parent); r.anchorMin = new Vector2(0,1); r.anchorMax = Vector2.one;
            r.pivot = new Vector2(.5f,1); r.sizeDelta = Vector2.zero;
            Vertical(r);
            r.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            return r;
        }
        static Button Button(Transform parent, string label, float h = 46, float width = -1, string name = "Button")
        {
            var r = Box(name,parent,Accent); Height(r,h,width);
            var button = r.gameObject.AddComponent<Button>(); button.targetGraphic = r.GetComponent<Image>();
            var t = Text(r,label,19,Color.white,"Label"); Stretch(t.rectTransform,0,0,1,1,7);
            t.alignment = TextAlignmentOptions.Center;
            return button;
        }
        static TMP_InputField Input(Transform parent, string hint, string name, bool password = false)
        {
            var r = Box(name,parent,Color.white); Height(r,48);
            var input = r.gameObject.AddComponent<TMP_InputField>(); input.targetGraphic = r.GetComponent<Image>();
            var viewport = Box("Viewport",r); Stretch(viewport,0,0,1,1,10); viewport.gameObject.AddComponent<RectMask2D>();
            var label = Text(viewport,"",21,null,"Value"); Stretch(label.rectTransform,0,0,1,1);
            label.alignment = TextAlignmentOptions.MidlineLeft;
            var placeholder = Text(viewport,hint,19,new Color(.42f,.46f,.47f),"Placeholder");
            Stretch(placeholder.rectTransform,0,0,1,1); placeholder.alignment = TextAlignmentOptions.MidlineLeft;
            input.textViewport = viewport; input.textComponent = (TextMeshProUGUI)label; input.placeholder = placeholder;
            input.fontAsset = font; input.lineType = TMP_InputField.LineType.SingleLine;
            if (password) input.contentType = TMP_InputField.ContentType.Password;
            return input;
        }
        static ScrollRect Scroll(Transform parent, string name, out RectTransform viewport)
        {
            var r = Box(name,parent,Paper); var s = r.gameObject.AddComponent<ScrollRect>();
            viewport = Box("Viewport",r); Stretch(viewport,0,0,1,1); viewport.gameObject.AddComponent<RectMask2D>();
            s.viewport = viewport; s.horizontal = false; s.movementType = ScrollRect.MovementType.Clamped; s.scrollSensitivity = 35;
            return s;
        }
        static T SavePrefab<T>(T component, string path) where T : Component
        {
            var asset = PrefabUtility.SaveAsPrefabAsset(component.gameObject,path);
            var result = asset.GetComponent<T>();
            UnityEngine.Object.DestroyImmediate(component.gameObject);
            return result;
        }
        static T Instance<T>(T prefab, Transform parent) where T : Component =>
            ((GameObject)PrefabUtility.InstantiatePrefab(prefab.gameObject,parent)).GetComponent<T>();

        [MenuItem("UrbanLegend/Build Authored UI")]
        public static void Build()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play Mode before authoring UI.");
            if (AssetDatabase.LoadAssetAtPath<GameObject>(Folder + "/Pages/Page_thread.prefab") != null)
            { Debug.Log("Authored UI already exists. Edit the scene and prefabs directly; automatic rebuild is disabled to preserve your edits."); return; }
            if (EditorSceneManager.GetActiveScene().isDirty)
                throw new InvalidOperationException("Save the current scene before migrating the UI.");
            Directory.CreateDirectory(Folder + "/Styles"); Directory.CreateDirectory(Folder + "/Pages"); Directory.CreateDirectory(Folder + "/Photos");
            Directory.CreateDirectory("Temp");
            File.Copy("Assets/Scenes/ArgPrototype.unity","Temp/ArgPrototype.before-authored-ui.unity",true);
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/ArgPrototype.unity");
            var app = UnityEngine.Object.FindObjectOfType<ArgDesktop>();
            font = app.font;
            var data = JsonUtility.FromJson<ContentData>(File.ReadAllText("Assets/Resources/Arg/FirstInvestigation.json"));
            var root = Box("ARG Desktop",app.transform,new Color32(26,40,44,255)); app.desktopRoot = root;
            root.gameObject.AddComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = root.gameObject.AddComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1600,900); scaler.matchWidthOrHeight = 0;
            root.gameObject.AddComponent<GraphicRaycaster>();
            var camera = new GameObject("DesktopCamera",typeof(Camera)).GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = Ink; camera.orthographic = true;
            camera.transform.position = new Vector3(0,0,-10); camera.gameObject.tag = "MainCamera";
            new GameObject("EventSystem",typeof(EventSystem),typeof(StandaloneInputModule));

            // Reusable styles: no runtime code sets their font size, color or padding.
            app.headingPrefab = SavePrefab(Text(null,"标题",32,Accent),Folder + "/Styles/Heading.prefab");
            app.bodyPrefab = SavePrefab(Text(null,"正文",23),Folder + "/Styles/Body.prefab");
            app.metaPrefab = SavePrefab(Text(null,"来源与说明",19),Folder + "/Styles/Meta.prefab");
            app.chatNamePrefab = SavePrefab(Text(null,"周祁",17,Accent),Folder + "/Styles/ChatName.prefab");
            app.chatBodyPrefab = SavePrefab(Text(null,"聊天内容",21),Folder + "/Styles/ChatBody.prefab");
            app.linkPrefab = SavePrefab(Button(null,"页面链接"),Folder + "/Styles/LinkButton.prefab");
            app.replyPrefab = SavePrefab(Button(null,"回复内容",43),Folder + "/Styles/ReplyButton.prefab");
            app.photoButtonPrefab = SavePrefab(Button(null,"[图片] 点击放大",64),Folder + "/Styles/PhotoButton.prefab");
            var inputPrefab = SavePrefab(Input(null,"输入内容","Input"),Folder + "/Styles/Input.prefab");
            var pageTemplate = Content(null,"Page");
            pageTemplate.gameObject.AddComponent<ArgPageView>();
            SavePrefab(pageTemplate,Folder + "/Pages/PageLayout.prefab");

            var header = Text(root,"沪上无此人  /  同行者",25,Color.white,"DesktopTitle"); Stretch(header.rectTransform,.02f,.94f,.66f,.99f);
            app.resetButton = Instance(app.linkPrefab,root); app.resetButton.name = "RestartButton";
            app.resetButton.GetComponentInChildren<TMP_Text>().text = "重新开始";
            Stretch((RectTransform)app.resetButton.transform,.86f,.945f,.98f,.99f);
            var browser = Box("Browser",root,Paper); Stretch(browser,.018f,.055f,.69f,.93f);
            var toolbar = Box("Toolbar",browser); Stretch(toolbar,0,1,1,1); toolbar.pivot = new Vector2(.5f,1);
            toolbar.sizeDelta = new Vector2(0,54); toolbar.anchoredPosition = Vector2.zero;
            var horizontal = toolbar.gameObject.AddComponent<HorizontalLayoutGroup>();
            horizontal.spacing = 8; horizontal.childControlHeight = horizontal.childControlWidth = true;
            horizontal.childForceExpandHeight = true; horizontal.childForceExpandWidth = false;
            app.homeButton = Button(toolbar,"首页",46,70,"HomeButton");
            app.historyButton = Button(toolbar,"历史",46,70,"HistoryButton");
            app.address = Instance(inputPrefab,toolbar); app.address.name = "AddressInput";
            ((TMP_Text)app.address.placeholder).text = "搜索关键词 / 输入站内地址";
            app.address.GetComponent<LayoutElement>().flexibleWidth = 1;
            app.goButton = Button(toolbar,"前往",46,72,"GoButton");
            app.location = Text(browser,"xunye.invalid",17,null,"Location"); Stretch(app.location.rectTransform,.025f,.84f,.98f,.90f);
            app.browserScroll = Scroll(browser,"Pages",out var pageViewport); Stretch((RectTransform)app.browserScroll.transform,0,0,1,.84f);
            app.homeView = Content(pageViewport,"Home");
            var heading = Instance(app.headingPrefab,app.homeView); heading.text = "寻页";
            var intro = Instance(app.bodyPrefab,app.homeView); intro.text = "查找留在旧网页里的声音。\n\n在上方输入关键词或站内地址。周祁正在等你核对资料目录。";
            app.indexButton = Instance(app.linkPrefab,app.homeView); app.indexButton.name = "OpenIndexButton";
            app.indexButton.GetComponentInChildren<TMP_Text>().text = "打开回声资料索引";
            app.searchView = Content(pageViewport,"SearchResults"); app.searchView.gameObject.SetActive(false);
            app.historyView = Content(pageViewport,"HistoryResults"); app.historyView.gameObject.SetActive(false);
            var views = new List<ArgPageView>();
            foreach (var p in data.pages)
            {
                var layoutPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(Folder + "/Pages/PageLayout.prefab").GetComponent<ArgPageView>();
                var view = Instance(layoutPrefab,pageViewport); view.name = "Page_" + p.id;
                view.pageId = p.id; view.address = p.address; view.source = p.source; view.restricted = p.restricted; view.searchTerms = p.terms;
                view.titleText = Instance(app.headingPrefab,view.transform); view.titleText.name = "Title"; view.titleText.text = p.title;
                view.bodyText = Instance(app.bodyPrefab,view.transform); view.bodyText.name = "Body"; view.bodyText.text = p.body;
                if (!string.IsNullOrEmpty(p.image))
                {
                    view.photoButton = Instance(app.photoButtonPrefab,view.transform); view.photoButton.name = "PhotoLink";
                    view.photoButton.GetComponentInChildren<TMP_Text>().text = "[图片] " + data.Art(p.image).caption + " · 点击放大";
                    view.imageId = p.image;
                }
                var links = new List<ArgPageLink>();
                foreach (var link in p.links ?? Array.Empty<PageLink>())
                {
                    var button = Instance(app.linkPrefab,view.transform); button.name = "Link_" + link.target;
                    button.GetComponentInChildren<TMP_Text>().text = link.label;
                    links.Add(new ArgPageLink {button = button,target = link.target});
                }
                view.links = links.ToArray();
                if (p.id == "login")
                {
                    view.userId = Instance(inputPrefab,view.transform); view.userId.name = "UserId";
                    ((TMP_Text)view.userId.placeholder).text = "用户 ID（不区分大小写）";
                    view.password = Instance(inputPrefab,view.transform); view.password.name = "Password";
                    view.password.contentType = TMP_InputField.ContentType.Password;
                    ((TMP_Text)view.password.placeholder).text = "阅览口令（不区分大小写）";
                    view.feedback = Instance(app.metaPrefab,view.transform); view.feedback.name = "Feedback"; view.feedback.text = "";
                    view.submit = Instance(app.linkPrefab,view.transform); view.submit.name = "LoginButton";
                    view.submit.GetComponentInChildren<TMP_Text>().text = "进入目录";
                }
                if (p.id == "directory")
                {
                    view.recordNumber = Instance(inputPrefab,view.transform); view.recordNumber.name = "RecordNumber";
                    ((TMP_Text)view.recordNumber.placeholder).text = "输入箱号或记录号";
                    view.feedback = Instance(app.metaPrefab,view.transform); view.feedback.name = "Feedback"; view.feedback.text = "";
                    view.submit = Instance(app.linkPrefab,view.transform); view.submit.name = "LookupButton";
                    view.submit.GetComponentInChildren<TMP_Text>().text = "检索";
                }
                var pageAsset = PrefabUtility.SaveAsPrefabAsset(view.gameObject,Folder + "/Pages/Page_" + p.id + ".prefab");
                UnityEngine.Object.DestroyImmediate(view.gameObject);
                var instance = ((GameObject)PrefabUtility.InstantiatePrefab(pageAsset,pageViewport)).GetComponent<ArgPageView>();
                instance.gameObject.SetActive(false); views.Add(instance);
            }
            app.pages = views.ToArray(); app.browserScroll.content = app.homeView;
            var chat = Box("Chat",root,Paper); Stretch(chat,.705f,.055f,.982f,.93f);
            app.chatStatus = Text(chat,"周祁 · 聊天",25,null,"ContactName"); Stretch(app.chatStatus.rectTransform,.05f,.93f,.95f,.99f);
            app.chatScroll = Scroll(chat,"Messages",out var chatViewport); Stretch((RectTransform)app.chatScroll.transform,0,.23f,1,.92f);
            app.chatContent = Content(chatViewport,"MessageList"); app.chatScroll.content = app.chatContent;
            // Edit-mode examples are regular prefab instances; runtime fills this list from the save.
            foreach (var message in new ArgSession().State.chat)
            {
                var sender = Instance(app.chatNamePrefab,app.chatContent); sender.name = "Preview_Sender"; sender.text = message.sender;
                var body = Instance(app.chatBodyPrefab,app.chatContent); body.name = "Preview_Message"; body.text = message.text;
            }
            app.replies = Box("Replies",chat); Stretch(app.replies,0,0,1,.23f); Vertical(app.replies,10,5);
            foreach (var reply in new ArgSession().Replies())
            { var b = Instance(app.replyPrefab,app.replies); b.name = "Preview_Reply"; b.GetComponentInChildren<TMP_Text>().text = reply; }
            app.saveStatus = Text(root,"进度会自动保存",16,Color.white,"SaveStatus"); Stretch(app.saveStatus.rectTransform,.02f,.008f,.67f,.045f);
            var help = Text(root,"滚轮翻页 · 点击图片放大 · Esc 关闭图片",16,Color.white,"Help"); Stretch(help.rectTransform,.7f,.008f,.99f,.045f);
            app.modal = Box("Modal",root,new Color(0,0,0,.9f)); Stretch(app.modal,0,0,1,1);
            var photoPanel = Box("PhotoViewer",app.modal); Stretch(photoPanel,0,0,1,1); app.photoPanel = photoPanel.gameObject;
            app.photoTitle = Text(photoPanel,"图片查看器",25,Color.white,"PhotoTitle"); Stretch(app.photoTitle.rectTransform,.08f,.92f,.8f,.98f);
            app.closePhotoButton = Button(photoPanel,"关闭 / Esc",name:"CloseButton"); Stretch((RectTransform)app.closePhotoButton.transform,.82f,.92f,.94f,.98f);
            var photoViewport = Box("PhotoViewport",photoPanel,Ink); Stretch(photoViewport,.08f,.10f,.94f,.90f);
            app.photoScroll = photoViewport.gameObject.AddComponent<ScrollRect>(); photoViewport.gameObject.AddComponent<RectMask2D>();
            app.photoScroll.viewport = photoViewport; app.photoScroll.horizontal = app.photoScroll.vertical = true;
            app.photoScroll.movementType = ScrollRect.MovementType.Clamped; app.photoScroll.scrollSensitivity = 35;
            var photoViews = new List<ArgPhotoView>();
            foreach (var art in data.art)
            {
                var picture = Box("Photo_" + art.id,photoViewport);
                picture.anchorMin = picture.anchorMax = new Vector2(.5f,1); picture.pivot = new Vector2(.5f,1); picture.sizeDelta = new Vector2(1000,750);
                var view = picture.gameObject.AddComponent<ArgPhotoView>(); view.imageId = art.id; view.title = art.title; view.caption = art.caption;
                view.photograph = picture.gameObject.AddComponent<RawImage>(); view.photograph.color = Color.white;
                var overlay = Box("ClueOverlay",picture,new Color32(245,239,217,248)); Stretch(overlay,.12f,.15f,.88f,.72f);
                var clue = Text(overlay,art.overlay,28,null,"ClueText"); Stretch(clue.rectTransform,0,0,1,1,20); view.clueOverlay = overlay.gameObject;
                var asset = PrefabUtility.SaveAsPrefabAsset(picture.gameObject,Folder + "/Photos/Photo_" + art.id + ".prefab");
                UnityEngine.Object.DestroyImmediate(picture.gameObject);
                var instance = ((GameObject)PrefabUtility.InstantiatePrefab(asset,photoViewport)).GetComponent<ArgPhotoView>();
                instance.gameObject.SetActive(false); photoViews.Add(instance);
            }
            app.photos = photoViews.ToArray();
            var controls = Box("Controls",photoPanel); Stretch(controls,.08f,.025f,.94f,.085f);
            var controlsLayout = controls.gameObject.AddComponent<HorizontalLayoutGroup>(); controlsLayout.spacing = 8;
            controlsLayout.childControlHeight = controlsLayout.childControlWidth = true; controlsLayout.childForceExpandWidth = false;
            app.zoomOutButton = Button(controls,"缩小",46,100,"ZoomOutButton"); app.zoomInButton = Button(controls,"放大",46,100,"ZoomInButton");
            Text(controls,"滚动或拖动查看图片；关闭后继续调查",19,Color.white);
            var resetPanel = Box("RestartConfirmation",app.modal,Paper); Stretch(resetPanel,.28f,.32f,.72f,.68f); Vertical(resetPanel,18,12);
            app.resetPanel = resetPanel.gameObject; Text(resetPanel,"重新开始会清除本原型的调查与聊天进度。",25);
            app.cancelResetButton = Button(resetPanel,"取消，继续调查",name:"CancelButton");
            app.confirmResetButton = Button(resetPanel,"确认重新开始",name:"ConfirmButton");
            resetPanel.gameObject.SetActive(false); app.modal.gameObject.SetActive(false);
            EditorUtility.SetDirty(app); EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            File.WriteAllText("Logs/arg-authored-ui.txt","PASS: authored scene, page prefabs, photo prefabs and style prefabs saved.\n");
            Debug.Log("ARG authored UI saved. Edit scene objects and prefab assets directly; no runtime layout generation.");
            if (File.Exists("Temp/arg-smoke.request"))
            { File.Delete("Temp/arg-smoke.request"); EditorApplication.delayCall += ArgVisualChecks.Start; }
        }
    }
}

