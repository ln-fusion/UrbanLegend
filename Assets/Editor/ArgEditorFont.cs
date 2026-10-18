using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;
using UrbanLegend.Arg;

namespace UrbanLegend.Editor
{
    public static class ArgEditorFont
    {
        [MenuItem("UrbanLegend/Create Editor UI Font")]
        public static void Create()
        {
            if (EditorApplication.isPlaying) return;
            var app = Object.FindObjectOfType<ArgDesktop>();
            string folder = "Assets/Prefabs/Arg/Styles/";
            var uiFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(folder + "ArgUIFont.asset");
            var original = app.font;
            if (uiFont == null)
            {
                var fallback = TMP_FontAsset.CreateFontAsset(app.fallbackSource,32,9,GlyphRenderMode.SDFAA,1024,1024,AtlasPopulationMode.Dynamic,true);
                fallback.name = "Arg UI Fallback";
                var text = File.ReadAllText("Assets/Resources/Arg/FirstInvestigation.json") +
                    File.ReadAllText("Assets/Scripts/Arg/ArgSession.cs") + File.ReadAllText("Assets/Scripts/Arg/ArgDesktop.cs");
                text += string.Concat(app.GetComponentsInChildren<TMP_Text>(true).Select(t => t.text));
                fallback.TryAddCharacters(new string(text.Where(c => !char.IsControl(c) && !char.IsSurrogate(c)).Distinct().ToArray()));
                AssetDatabase.CreateAsset(fallback,folder + "ArgUIFallback.asset");
                AssetDatabase.AddObjectToAsset(fallback.material,fallback);
                foreach (var texture in fallback.atlasTextures) if (texture != null) AssetDatabase.AddObjectToAsset(texture,fallback);
                uiFont = Object.Instantiate(original); uiFont.name = "Arg UI Font";
                uiFont.fallbackFontAssetTable = new System.Collections.Generic.List<TMP_FontAsset> {fallback};
                AssetDatabase.CreateAsset(uiFont,folder + "ArgUIFont.asset");
            }
            var paths = AssetDatabase.FindAssets("t:Prefab",new[] {"Assets/Prefabs/Arg"})
                .Select(AssetDatabase.GUIDToAssetPath).OrderBy(p => p.Contains("/Styles/") ? 0 : 1).ToArray();
            foreach (var path in paths)
            {
                var root = PrefabUtility.LoadPrefabContents(path);
                Replace(root,original,uiFont);
                PrefabUtility.SaveAsPrefabAsset(root,path);
                PrefabUtility.UnloadPrefabContents(root);
            }
            Replace(app.gameObject,original,uiFont); app.font = uiFont;
            EditorUtility.SetDirty(app); AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(app.gameObject.scene); EditorSceneManager.SaveScene(app.gameObject.scene);
            Debug.Log("ARG font is now an editable, persistent asset with missing glyphs available in Edit Mode.");
        }
        static void Replace(GameObject root, TMP_FontAsset original, TMP_FontAsset replacement)
        {
            foreach (var text in root.GetComponentsInChildren<TMP_Text>(true))
                if (text.font == original) text.font = replacement;
            foreach (var input in root.GetComponentsInChildren<TMP_InputField>(true))
                if (input.fontAsset == original) input.fontAsset = replacement;
        }
    }
}
