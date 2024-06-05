using Ntreev.Library.Psd;
using PSDUnity.Data;
using PSDUnity.UGUI;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace PSDUnity.Analysis
{
    [CustomEditor(typeof(Data.Exporter), true)]
    public class ExporterDrawer : Editor
    {
        private SerializedProperty scriptProp;
        private Exporter exporter;
        private const string Prefs_LastPsdsDir = "lastPsdFileDir";
        private ExporterTreeView m_TreeView;
        private GroupNodeItem rootNode;
        [SerializeField]
        private TreeViewState m_TreeViewState = new TreeViewState();
        private void OnEnable()
        {
            exporter = target as Exporter;
            scriptProp = serializedObject.FindProperty("m_Script");
            AutoChargeRule();
            InitTreeView();
        }

        [UnityEditor.Callbacks.OnOpenAsset()]
        public static bool OnOpenAsset(int instanceID, int line)
        {
            var exporter = EditorUtility.InstanceIDToObject(instanceID) as Exporter;
            if (exporter != null)
            {
                var window = EditorWindow.GetWindow<Analysis.PsdPreviewWindow>();
                window.OpenGraph(exporter);
                return true;
            }
            return false;
        }
        private void AutoChargeRule()
        {
            if (exporter.ruleObj == null)
            {
                LoadRuleObject();
            }
        }

        private void LoadRuleObject()
        {
            var ruleObj = RuleHelper.GetRuleObj();
            var path = AssetDatabase.GetAssetPath(ruleObj);
            if (string.IsNullOrEmpty(path))
            {
                ruleObj.name = "内嵌规则";
                var assetPath = AssetDatabase.GetAssetPath(target);
                if (!string.IsNullOrEmpty(assetPath))
                {
                    var oldItem = AssetDatabase.LoadAssetAtPath<RuleObject>(assetPath);
                    if (oldItem != null)
                    {
                        //Debug.Log("use old RuleObj:" + target);
                        exporter.ruleObj = oldItem;
                    }
                    else
                    {
                        Debug.Log("Add new rule Object To:" + target);
                        exporter.ruleObj = ruleObj;
                        AssetDatabase.AddObjectToAsset(ruleObj, target);
                        AssetDatabase.Refresh();
                        AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
                        Selection.activeObject = ruleObj;
                    }
                }
                else
                {
                    //exporter.ruleObj = ruleObj;
                }
            }
            else
            {
                exporter.ruleObj = ruleObj;
            }
        }


        private void InitTreeView()
        {
            if (exporter.groups != null && exporter.groups.Count > 0)
            {
                if (exporter.groups.Count > 0)
                {
                    var list = exporter.groups.ConvertAll(x => new GroupNodeItem(x));
                    rootNode = TreeViewUtility.ListToTree<GroupNodeItem>(list);
                    m_TreeView = new ExporterTreeView(m_TreeViewState);
                    m_TreeView.root = rootNode;
                    m_TreeView.ruleObj = (target as Exporter).ruleObj;
                }
            }
        }

        protected override void OnHeaderGUI()
        {
            base.OnHeaderGUI();
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.ObjectField(exporter, typeof(Data.Exporter), false);
            EditorGUILayout.PropertyField(scriptProp);
            EditorGUI.EndDisabledGroup();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawPathOption();
            DrawConfigs();
            if (m_TreeView != null && rootNode != null)
            {
                DrawUICreateOption();
                DrawGroupNode();
            }
            serializedObject.ApplyModifiedProperties();
        }

        private void DrawUICreateOption()
        {
            using (var hor = new EditorGUILayout.HorizontalScope())
            {
                var style = "miniButton";
                var layout = GUILayout.Width(60);
                if (GUILayout.Button("Build-All", style, layout))
                {
                    var canvasObj = Array.Find(Selection.objects, x => x is GameObject && (x as GameObject).GetComponent<Canvas>() != null);
                    var ctrl = PSDImporterUtility.CreatePsdImportCtrlSafty(exporter.ruleObj, exporter.ruleObj.defultUISize, canvasObj == null ? FindObjectOfType<Canvas>() : (canvasObj as GameObject).GetComponent<Canvas>());
                    ctrl.Import(rootNode.data);
                    AssetDatabase.Refresh();
                }

                if (GUILayout.Button("Build-Sel", style, layout))
                {
                    var canvasObj = Array.Find(Selection.objects, x => x is GameObject && (x as GameObject).GetComponent<Canvas>() != null);
                    var ctrl = PSDImporterUtility.CreatePsdImportCtrlSafty(exporter.ruleObj, exporter.ruleObj.defultUISize, canvasObj == null ? FindObjectOfType<Canvas>() : (canvasObj as GameObject).GetComponent<Canvas>());
                    var root = new GroupNodeItem(new Rect(Vector2.zero, rootNode.rect.size), 0, -1);
                    root.displayName = "partial build";
                    foreach (var node in m_TreeView.selected)
                    {
                        root.AddChild(node);
                    }
                    ctrl.Import(root.data);
                    AssetDatabase.Refresh();
                }

                if (GUILayout.Button("Expland", style, layout))
                {
                    m_TreeView.ExpandAll();
                }

                if (GUILayout.Button("Clear", style, layout))
                {
                    exporter.groups.Clear();
                    m_TreeView.root = null;
                    m_TreeView.Reload();
                }

            }

        }

        private void DrawGroupNode()
        {
            var rect = GUILayoutUtility.GetRect(EditorGUIUtility.currentViewWidth, m_TreeView.totalHeight);
            m_TreeView.OnGUI(rect);
        }

        private void RecordAllPsdInformation()
        {
            if (!string.IsNullOrEmpty(exporter.psdFile))
            {
                var psd = PsdDocument.Create(exporter.psdFile);

                if (psd != null)
                {
                    try
                    {
                        var rootSize = new Vector2(psd.Width, psd.Height);
                        ExportUtility.InitPsdExportEnvrioment(exporter, rootSize);
                        rootNode = new GroupNodeItem(new Rect(Vector2.zero, rootSize), 0, -1);
                        rootNode.displayName = exporter.name;
                        var groupDatas = ExportUtility.CreatePictures(psd.Childs, rootSize, exporter.ruleObj.defultUISize, exporter.ruleObj.forceSprite);
                        if (groupDatas != null)
                        {
                            foreach (var groupData in groupDatas)
                            {
                                rootNode.AddChild(groupData);
                                ExportUtility.ChargeTextures(exporter, groupData);
                            }
                        }
                        var list = new List<GroupNodeItem>();
                        TreeViewUtility.TreeToList<GroupNodeItem>(rootNode, list, true);
                        exporter.groups = list.ConvertAll(x => x.data);
                        EditorUtility.SetDirty(exporter);
                    }
                    catch (Exception e)
                    {
                        throw e;
                    }
                    finally
                    {
                        psd.Dispose();
                    }
                }
            }
        }

        private void DrawPathOption()
        {
            using (var hor = new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("PSD path:", GUILayout.Width(60));
                exporter.psdFile = EditorGUILayout.TextField(exporter.psdFile);

                if (GUILayout.Button("Select", GUILayout.Width(60)))
                {
                    var dir = PlayerPrefs.GetString(Prefs_LastPsdsDir);

                    if (string.IsNullOrEmpty(dir) && !string.IsNullOrEmpty(exporter.psdFile))
                    {
                        dir = System.IO.Path.GetDirectoryName(exporter.psdFile);
                    }

                    if (string.IsNullOrEmpty(dir))//|| !System.IO.Directory.Exists(dir)
                    {
                        dir = Application.dataPath;
                    }

                    var path = EditorUtility.OpenFilePanel("Select a psd file", dir, "psd");

                    if (!string.IsNullOrEmpty(path))
                    {
                        if (path.Contains(Application.dataPath))
                        {
                            path = path.Replace("\\", "/").Replace(Application.dataPath, "Assets");
                        }
                        exporter.psdFile = path;
                        PlayerPrefs.SetString(Prefs_LastPsdsDir, System.IO.Path.GetDirectoryName(path));
                    }

                }
            }
        }

        private void DrawConfigs()
        {
            using (var hor = new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label("Rules to use: " + "  [PSD file export + UI interface generation]");
                exporter.ruleObj = EditorGUILayout.ObjectField(exporter.ruleObj, typeof(RuleObject), false) as RuleObject;
                if (GUILayout.Button("Create", GUILayout.Width(60)))
                {
                    if (EditorUtility.DisplayDialog("Create new rule", "A new rule file will be generated after confirmation!", "Confirm", "Cancel"))
                    {
                        exporter.ruleObj = RuleHelper.CreateRuleObject();
                    }
                }
            }

            if (GUILayout.Button("Convert layers to image and record index", EditorStyles.toolbarButton))
            {
                if (EditorUtility.DisplayDialog("Warning", "Recording will overwrite the current configuration, please press Confirm to continue!", "Confirm"))
                {
                    RecordAllPsdInformation();
                    m_TreeView = new ExporterTreeView(m_TreeViewState);
                    m_TreeView.root = rootNode;
                    m_TreeView.ruleObj = (target as Exporter).ruleObj;
                }

            }
        }

    }

}