using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using System.Runtime.Serialization;

[System.Serializable]
public class NodeVertex {
    public Rect box;
    public string ownerNodeID;
    public string id;
    public string label;
    public bool dragging;
    public string targetNodeID;
    public string targetVertID;

    public Type[] compatibleTypes { get; }
    public string[] compatibleIDs { get; }

    public GenericNode.vertOptions option;
    public float zoomLevel = 1.0f;
    private float lastZoomLevel = 1.0f;

    #region Constructors
    public NodeVertex(GenericNode.vertOptions vertOption, string _owner, string _id, string _label) : this(vertOption, _owner, _id, _label, new Type[0], new string[0]) { }

    public NodeVertex(GenericNode.vertOptions vertOption, string _owner, string _id, string _label, Type[] _compatibleTypes, string[] _compatibleIDs) {
        option = vertOption;
        ownerNodeID = _owner;
        id = _id;
        label = _label;
        targetVertID = "";
        compatibleTypes = _compatibleTypes;
        compatibleIDs = _compatibleIDs;
    }

    #endregion

    public void SetTarget(string _targetNodeID, string _targetVertID) {
        targetNodeID = _targetNodeID;
        targetVertID = _targetVertID;
    }

    public bool CheckCompatibilityWithVertex(Type type) {
        if (NodeManager.activeNode.activeVertex == null) {
            return true;
        }

        if (NodeManager.activeNode.activeVertex == this) {
            return true;
        }

        List<Type> compatibleTypes = new List<Type>(NodeManager.activeNode.activeVertex.compatibleTypes);
        List<string> compatibleIDs = new List<string>(NodeManager.activeNode.activeVertex.compatibleIDs);

        if (compatibleTypes.Contains(type)) {
            if (compatibleIDs.Contains(id)) {
                return true;
            }
        }

        return false;
    }

    private void RenderLabel(GenericNode.vertOptions options) {
        GUIStyle style;
        style = new GUIStyle(EditorStyles.helpBox);

        switch (options) {
            case GenericNode.vertOptions.LEFT_VERT:
                break;

            case GenericNode.vertOptions.RIGHT_VERT:
                style.alignment = TextAnchor.MiddleRight;
                break;
        }

        GUILayout.BeginVertical();
        GUILayout.FlexibleSpace();
        GUILayout.Label(label.ToUpper(), style, new GUILayoutOption[] { GUILayout.Width(style.CalcSize(new GUIContent(label)).x * 1.35f), GUILayout.ExpandWidth(true) });
        GUILayout.FlexibleSpace();
        GUILayout.EndVertical();
    }

    public void Render(Texture2D tex, bool withLabel, bool compatible) {
        box = new Rect(new Vector2(box.position.x * zoomLevel, box.position.y * zoomLevel), new Vector2(box.size.x * zoomLevel, box.size.y * zoomLevel));
        lastZoomLevel = zoomLevel;

        if (!compatible) {
            tex = EditorStyles.foldout.normal.background;
        }

        switch (option) {
            case GenericNode.vertOptions.TOP_VERT:
            case GenericNode.vertOptions.BOTTOM_VERT:
                GUILayout.FlexibleSpace();
                GUILayout.Label(tex, new GUILayoutOption[] { GUILayout.Width(18f), GUILayout.Height(18f) });

                if (Event.current.type == EventType.Repaint) {
                    box = GUILayoutUtility.GetLastRect();
                }

                GUILayout.FlexibleSpace();
                break;

            case GenericNode.vertOptions.RIGHT_VERT:
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();

                if (withLabel) { RenderLabel(option); }

                GUILayout.BeginVertical();
                GUILayout.FlexibleSpace();
                GUILayout.Label(tex, new GUILayoutOption[] { GUILayout.Height(18f), GUILayout.Width(18f) });
                GUILayout.FlexibleSpace();
                GUILayout.EndVertical();

                if (Event.current.type == EventType.Repaint) {
                    box = GUILayoutUtility.GetLastRect();
                }

                GUILayout.EndHorizontal();
                break;

            case GenericNode.vertOptions.LEFT_VERT:
                GUILayout.BeginHorizontal();

                GUILayout.BeginVertical();
                GUILayout.FlexibleSpace();
                if (compatible) {
                    GUILayout.Label(tex, new GUILayoutOption[] { GUILayout.Height(18f), GUILayout.Width(18f) });
                }
                GUILayout.FlexibleSpace();
                GUILayout.EndVertical();

                if (Event.current.type == EventType.Repaint) {
                    box = GUILayoutUtility.GetLastRect();
                }

                if (withLabel) { RenderLabel(option); }

                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
                break;
        }
    }
}

//public class MultipleNodeVertex : NodeVertex {
//    private List<string> targetsNodeID;
//    private List<string> targetsVertID;

//    public MultipleNodeVertex(string _parent, string _id, string _label) : base(_parent, _id, _label) {
//        targetsNodeID = new List<string>();
//        targetsVertID = new List<string>();
//    }

//    public void AddTarget(string _targetNodeID, string _targetVertID) {
//        if (!targetsNodeID.Contains(_targetNodeID)) {
//            targetsNodeID.Add(_targetNodeID);
//            targetsVertID.Add(_targetVertID);
//        }
//    }
//}
    
