using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using UnityEditor.UIElements;
using System.IO;
using System.Linq;

public class NodeEdit : EditorWindow {
    static string NODEMAP_SAVE_DIRECTORY_PATH = "/Editor/NodeEdit/Nodemaps/";
    static string DIALOG_SAVE_DIRECTORY_PATH = "/Data/Dialog/";
    [SerializeField] public NodeManager man;
    [SerializeField] public NodeEditPrefs prefs;
    Vector2 scrollPos;
    bool sortNodes = false;
    GUIStyle toolbarStyle;

    [MenuItem("Custom Editors/NodeEdit")]
    static void Init() {
        NodeEdit window = GetWindow<NodeEdit>();
        window.Show();
    }

    private void OnEnable() {
        this.wantsMouseEnterLeaveWindow = true;

        string[] pathsToCheck = new string[] { NODEMAP_SAVE_DIRECTORY_PATH, DIALOG_SAVE_DIRECTORY_PATH };

        for (int i = 0; i < pathsToCheck.Length; i++) {
            if (!Directory.Exists(Application.dataPath + pathsToCheck[i])) {
                Debug.Log("Creating missing folder at " + Application.dataPath + pathsToCheck[i]);
                Directory.CreateDirectory(Application.dataPath + pathsToCheck[i]);
            }
        }
    }

    public static Rect GetMidScreenRect() {
        Rect position = GetWindow<NodeEdit>().position;
        return new Rect(new Vector2(position.width / 2, position.height / 2), new Vector2(1f, 1f));
    }

    private void RenderToolbar() {
        toolbarStyle = new GUIStyle(EditorStyles.miniButton);
        toolbarStyle.clipping = TextClipping.Clip;
        toolbarStyle.fixedWidth = 22f;
        toolbarStyle.fixedHeight = 22f;
        toolbarStyle.imagePosition = ImagePosition.ImageOnly;
        toolbarStyle.padding = new RectOffset(1, 1, 1, 1);

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Restart Editor")) {
            man = null;
            return;
        }

        if (GUILayout.Button(prefs.saveIcon, toolbarStyle)) {
            if (man.saveFilePath == "") {
                string savePath = EditorUtility.SaveFilePanel("Save Nodemap", Application.dataPath + NODEMAP_SAVE_DIRECTORY_PATH, "default_" + DateTime.Now.Ticks.ToString(), "nodemap");
                man.saveFilePath = savePath;
            }

            man.SaveNodeFile(man.saveFilePath, false);
        }

        if (GUILayout.Button(prefs.addNodeIcon, toolbarStyle)) {
            GenericMenu menu = new GenericMenu();
            Type[] nodeTypes = Type.GetType("GenericNode").Assembly.GetTypes();

            for (int i = 0; i < nodeTypes.Count(); i++) {
                if (nodeTypes[i].BaseType == typeof(GenericNode)) {
                    menu.AddItem(new GUIContent() { text = nodeTypes[i].ToString() }, false, NodeAddMenu, nodeTypes.ElementAt(i));
                    Repaint();
                }
            }

            menu.ShowAsContext();
        }

        if (GUILayout.Button(prefs.sortIcon, toolbarStyle)) {
            man.SortNodes();
        }
    }

    void NodeAddMenu(object data) {
        Type selectedType = (Type)data;
        man.AddNode(selectedType);
        UpdateLayoutAndRepaint();
    }

    public static void UpdateLayoutAndRepaint() {
        NodeEdit window = GetWindow<NodeEdit>();
        window.Repaint();
        window.SendEvent(new Event() { type = EventType.Layout });
        window.SendEvent(new Event() { type = EventType.Repaint });
    }

    private void OnGUI() {
        if (man == null) {
            GUILayout.BeginVertical();
            if (GUILayout.Button("New Node Event Map")) {
                man = new NodeManager(prefs);
            }

            if (GUILayout.Button("Open Node Event Map")) {
                string filePath = EditorUtility.OpenFilePanel("Select Nodemap file", Application.dataPath + NODEMAP_SAVE_DIRECTORY_PATH, "nodemap");
                Debug.Log(filePath);

                if (filePath != "") {
                    man = new NodeManager(prefs);

                    if (File.Exists(filePath)) {
                        man.saveFilePath = filePath;
                        FileStream stream = File.OpenRead(filePath);
                        man.OpenNodeFile(stream);
                    }
                }
            }
            GUILayout.EndVertical();
        } else {
            GUILayout.BeginVertical();
            GUILayout.BeginHorizontal();
            RenderToolbar();
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            //manager can get killed by toolbar, so check here again
            if (man != null) {
                GUILayout.BeginVertical();
                scrollPos = GUI.BeginScrollView(new Rect(Vector2.zero, new Vector2(Screen.width, Screen.height)), scrollPos, new Rect(Vector2.zero, new Vector2(man.viewRect.xMax, man.viewRect.yMax)), true, true);
                man.RenderNodes();
                man.HandleEvents(this);
                GUI.EndScrollView();
                GUILayout.EndVertical();
            }
        }
    }

    private void OnDestroy() {
        if (EditorUtility.DisplayDialog("NodeEdit", "Save Nodemap?", "Ok", "Nope")) {
            string save = EditorUtility.SaveFilePanel("Save Nodemap", Application.dataPath + NODEMAP_SAVE_DIRECTORY_PATH, "default_" + DateTime.Now.Ticks.ToString(), "nodemap");
            //if (save) {
            //    man.saveFilePath = save;
            //}

            //man.SaveNodeFile(save, true);
            Debug.Log(save);
        }
    }
}
