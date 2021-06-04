using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEditor;
using System.IO;

public class DialogNode : GenericNode {
    public static string NEXT_NODE = "next_node";
    public static string PREVIOUS_NODE = "previous_node";
    [SerializeField]DialogEditorUIE dataEditorWindow = null;
    [SerializeField]public byte[] nodeData = null;

    //UIElementsTest editor = null;
    //public DialogFile dialogFile = null;

    public DialogNode() : base(new Rect(Vector2.one, Vector2.one)) { }
    public DialogNode(Texture2D backgroundTex, Texture2D vertTex, Rect box) : base(backgroundTex, vertTex, box) { }
    public DialogNode(bool serializing) : base(serializing) { }

    public DialogNode(NodeEditPrefs prefs) : base(prefs.dialogBackground, prefs.defaultVert, NodeEdit.GetMidScreenRect()) { }

    public override void ConstructVertex(string id) {
        if (id == PREVIOUS_NODE) {
            AddVertex(vertOptions.TOP_VERT, PREVIOUS_NODE, "Previous Event");
        } else if (id == NEXT_NODE) {
            AddVertex(vertOptions.BOTTOM_VERT, NEXT_NODE, "Next Event", new Type[] { typeof(DialogNode), typeof(DecisionNode) }, new string[] { PREVIOUS_NODE });
        }
    }

    protected override void RenderUIContent() {
        if (state == nodeStates.BLANK) {
            GUILayout.Space(16f);
            GUILayout.BeginHorizontal();

            if (GUILayout.Button("New Dialog File")) {
                ConstructVertex(PREVIOUS_NODE);
                ConstructVertex(NEXT_NODE);
                dataEditorWindow = EditorWindow.CreateInstance<DialogEditorUIE>();
                dataEditorWindow.nodeOwner = GetID();
                dataEditorWindow.Show();
                dataEditorWindow.Focus();
                state = nodeStates.IDLE;
            }

            if (GUILayout.Button("Open")) {
                EditorUtility.OpenFilePanel(null, "Data", ".evt");
            }

            GUILayout.EndHorizontal();
            GUILayout.Space(16f);
        } else if (state == nodeStates.IDLE) {
            GUILayout.BeginVertical();

            GUIStyle style = new GUIStyle(EditorStyles.helpBox);
            style.clipping = TextClipping.Overflow;
            style.fixedHeight = 0;

            GUILayout.Label(GetID().Substring(0, 5), EditorStyles.boldLabel);
            GUILayout.Label("There is a point where things get too far. We need to figure this out. Artie's sister has nothing to do with this.", style, new GUILayoutOption[] { GUILayout.Width(160f) });
            //if (GUILayout.Button("Fetch dialog content")) {
            GUILayout.EndVertical();
            GUILayout.BeginVertical();
            if (dataEditorWindow != null) {
                dataEditorWindow.GetNodePreview();
            } else {
                if (GUILayout.Button("Fetch content")) {
                    dataEditorWindow = EditorWindow.CreateInstance<DialogEditorUIE>();
                    dataEditorWindow.nodeOwner = GetID();
                    dataEditorWindow.Show();
                    dataEditorWindow.Focus();
                    dataEditorWindow.RestoreDialogFile(nodeData);
                }
            }

            GUILayout.EndVertical();
        }

        //if (editor != null) {
        //    dialogFile = editor.currDialogFile;

        //    if (dialogFile != null) {
        //        Debug.Log("DIALOGFILE: " + dialogFile.fileName);
        //    }
        //}
    }

    public override void DoubleClickBehaviour() {
        //editor = EditorWindow.GetWindow<UIElementsTest>();
    }
}
