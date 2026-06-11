using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Psd2Prefab
{
    /// <summary>PSD → UGUI Prefab 转换窗口。菜单:Tools/PSD to Prefab</summary>
    public class PsdToPrefabWindow : EditorWindow
    {
        private string _psdPath = "";
        private string _outputFolder = "Assets/PSD2Prefab_Output";
        private string _lastResult = "";
#if PSD2PREFAB_TMP
        private bool _useTmp = true;
#else
        private const bool _useTmp = false;
#endif

        [MenuItem("Tools/PSD to Prefab")]
        public static void Open()
        {
            var win = GetWindow<PsdToPrefabWindow>("PSD to Prefab");
            win.minSize = new Vector2(420, 200);
        }

        private void OnGUI()
        {
            GUILayout.Space(8);
            EditorGUILayout.LabelField("PSD → UGUI Prefab", EditorStyles.boldLabel);
            GUILayout.Space(4);

            // PSD 路径
            using (new EditorGUILayout.HorizontalScope())
            {
                _psdPath = EditorGUILayout.TextField("PSD 文件", _psdPath);
                if (GUILayout.Button("浏览...", GUILayout.Width(64)))
                {
                    string p = EditorUtility.OpenFilePanel("选择 PSD 文件", "", "psd");
                    if (!string.IsNullOrEmpty(p)) _psdPath = p;
                }
            }

            // 支持把项目内的 .psd 资源拖到窗口
            HandleDragAndDrop();

            _outputFolder = EditorGUILayout.TextField("输出目录", _outputFolder);
#if PSD2PREFAB_TMP
            _useTmp = EditorGUILayout.Toggle(new GUIContent("使用 TextMeshPro", "文本图层生成 TextMeshProUGUI 而非 Legacy Text"), _useTmp);
#endif

            GUILayout.Space(8);
            EditorGUILayout.HelpBox(
                "命名规则:psd 图层名以 btn_ 开头 → 生成 Button 节点(Btn_ 前缀);\n" +
                "文本图层 → Label_ 前缀;普通图片 → Image_ 前缀;分组/未知 → Node_ 前缀。\n" +
                "隐藏图层与完全透明图层会被忽略。", MessageType.Info);

            GUILayout.Space(4);
            using (new EditorGUI.DisabledScope(string.IsNullOrEmpty(_psdPath)))
            {
                if (GUILayout.Button("生成 Prefab", GUILayout.Height(32)))
                    Generate();
            }

            if (!string.IsNullOrEmpty(_lastResult))
                EditorGUILayout.HelpBox(_lastResult, MessageType.None);
        }

        private void HandleDragAndDrop()
        {
            var evt = Event.current;
            if (evt.type != EventType.DragUpdated && evt.type != EventType.DragPerform) return;
            bool hasPsd = false;
            foreach (var path in DragAndDrop.paths)
                if (path.EndsWith(".psd", StringComparison.OrdinalIgnoreCase)) { hasPsd = true; break; }
            if (!hasPsd) return;

            DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
            if (evt.type == EventType.DragPerform)
            {
                DragAndDrop.AcceptDrag();
                foreach (var path in DragAndDrop.paths)
                    if (path.EndsWith(".psd", StringComparison.OrdinalIgnoreCase)) { _psdPath = path; break; }
            }
            evt.Use();
        }

        private void Generate()
        {
            try
            {
                string fullPath = _psdPath;
                if (!Path.IsPathRooted(fullPath))
                    fullPath = Path.GetFullPath(fullPath); // 项目内相对路径(如 Assets/xxx.psd)

                if (!File.Exists(fullPath))
                {
                    EditorUtility.DisplayDialog("PSD2Prefab", "找不到文件:\n" + fullPath, "确定");
                    return;
                }

                string prefabPath = PsdPrefabBuilder.Build(fullPath, _outputFolder, _useTmp);
                _lastResult = "生成成功: " + prefabPath;

                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                if (prefab != null)
                {
                    Selection.activeObject = prefab;
                    EditorGUIUtility.PingObject(prefab);
                }
            }
            catch (Exception e)
            {
                _lastResult = "生成失败: " + e.Message;
                Debug.LogException(e);
                EditorUtility.DisplayDialog("PSD2Prefab 生成失败", e.Message, "确定");
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }
    }
}
