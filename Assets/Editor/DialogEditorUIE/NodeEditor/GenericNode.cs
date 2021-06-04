using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization;

[System.Serializable]
public class GenericNode {
    [SerializeField] private string id;
    [SerializeField] private Rect defaultRect;
    public Rect box;
    public bool dragging;
    public List<NodeVertex> nodeVertices = new List<NodeVertex>();
    public NodeVertex activeVertex;
    public Vector2 dragOffset;
    public string texBackgroundName = "";
    public string texLinkName = "";
    private Texture2D backgroundTex;
    private string backgroundTexPath;
    private Texture2D vertTex;
    private string vertTexPath;
    public nodeStates state;
    public float zoomLevel = 1.0f;
    private float lastZoomLevel = 1.0f;

    public enum nodeStates {
        BLANK,
        IDLE,
        SELECTED,
        DRAGGING,
        LINKING,
    }

    public enum vertOptions {
        TOP_VERT,
        BOTTOM_VERT,
        LEFT_VERT,
        RIGHT_VERT
    }

    #region Constructors

    public GenericNode(bool serializing) { }

    public GenericNode(Rect _box) : this(_box, EditorStyles.helpBox.normal.background, EditorStyles.radioButton.normal.background, GUID.Generate().ToString()) { }

    public GenericNode(Texture2D background, Texture2D vert, Rect box) : this(box, background, vert, GUID.Generate().ToString()) { }

    public GenericNode(Texture2D background, Texture2D vert, GUID id, Rect box) : this(box, background, vert, id.ToString()) { }

    public GenericNode(Rect _box, Texture2D background, Texture2D vert, string _id) {
        id = _id;
        state = nodeStates.BLANK;
        box = _box;
        backgroundTex = background;
        vertTex = vert;
    }

    #endregion

    public void AddVertex(vertOptions style, string vertID, string label, Type[] compatibleTypes, string[] compatibleIDs) {
        nodeVertices.Add(new NodeVertex(style, id, vertID, label, compatibleTypes, compatibleIDs));
    }

    public void AddVertex(vertOptions style, string vertID, string label) {
        nodeVertices.Add(new NodeVertex(style, id, vertID, label));
    }

    public virtual void ConstructVertex(string id) { return; }

    public string GetID() {
        return id;
    }

    public void SetActiveVertex(NodeVertex active) {
        activeVertex = active;
    }

    public NodeVertex GetVertexByID(string id) {
        foreach (NodeVertex vert in nodeVertices) {
            if (vert.id == id) {
                return vert;
            }
        }

        throw new Exception("Vertice with ID " + id + " not found.");
    }

    public bool IsBeingHovered() {
        return (box.Contains(Event.current.mousePosition) ? true : false);
    }

    virtual public void DoubleClickBehaviour() {
        Debug.Log("Got double click on " + id);
    }

    //virtual public void EventHandling() {
    //    if (Event.current.type == EventType.MouseDown) {
    //        if (Event.current.clickCount == 2) {
    //            Debug.Log("TELL ME ABOUT IT; BUDDY!");
    //        }
    //    }
    //}

    virtual protected void BeginRender() {
        //restore textures if serialized
        if ((backgroundTex == null) && (backgroundTexPath != null)) {
            backgroundTex = (Texture2D)AssetDatabase.LoadAssetAtPath(backgroundTexPath, typeof(Texture2D));
        }

        if ((vertTex == null) && (vertTexPath != null)) {
            vertTex = (Texture2D)AssetDatabase.LoadAssetAtPath(vertTexPath, typeof(Texture2D));
        }

        GUIStyle style = new GUIStyle(EditorStyles.helpBox);
        style.normal.background = backgroundTex;
        style.normal.textColor = Color.white;

        GUILayout.BeginArea(box);
        GUILayout.BeginVertical(style, new GUILayoutOption[] { GUILayout.Width(box.width), GUILayout.Height(box.height) });
    }

    virtual protected void RenderVerts(vertOptions sectionToRender, bool showLabels) {
        bool initiatedLayout = false;

        if ((nodeVertices != null) && (nodeVertices.Count > 0)) {
            foreach (NodeVertex vert in nodeVertices) {
                if ((vert.option == sectionToRender)) {

                    //initialization for layout according to option
                    if ((sectionToRender == vertOptions.BOTTOM_VERT) || (sectionToRender == vertOptions.TOP_VERT)) {
                        if (sectionToRender == vertOptions.BOTTOM_VERT) {
                            GUILayout.Space(8f);
                        }

                        if (!initiatedLayout) {
                            GUILayout.BeginHorizontal();
                            initiatedLayout = true;
                        }
                    } else if ((sectionToRender == vertOptions.RIGHT_VERT) || (sectionToRender == vertOptions.LEFT_VERT)) {
                        if (!initiatedLayout) {
                            GUILayout.BeginVertical();
                            initiatedLayout = true;
                        }
                    }

                    bool compatible = (NodeManager.activeNode == null ? true : vert.CheckCompatibilityWithVertex(GetType()));
                    vert.Render(vertTex, showLabels, compatible);
                }
            }

            if (initiatedLayout) {
                if ((sectionToRender == vertOptions.TOP_VERT) || (sectionToRender == vertOptions.BOTTOM_VERT)) {
                    GUILayout.EndHorizontal();

                    if (sectionToRender == vertOptions.TOP_VERT) {
                        GUILayout.Space(8f);
                    }
                } else {
                    GUILayout.EndVertical();
                }
            }
        }
    }

    virtual protected void RenderUIContent() {
        GUILayout.Label(id.ToString(), EditorStyles.whiteLabel);
        GUILayout.Label(GetType().ToString());
    }

    virtual protected void EndRender() {
        GUILayout.EndVertical();
        if (Event.current.type == EventType.Repaint) {
            box.size = GUILayoutUtility.GetLastRect().size;
        }
        GUILayout.EndArea();
    }

    virtual public void Render() {
        nodeStates originalState = state;
        box = new Rect(new Vector2(box.position.x * zoomLevel, box.position.y * zoomLevel), new Vector2(box.size.x * zoomLevel, box.size.y * zoomLevel));
        lastZoomLevel = zoomLevel;

        BeginRender();
        RenderVerts(vertOptions.TOP_VERT, false);
        RenderUIContent();
        RenderVerts(vertOptions.BOTTOM_VERT, false);
        EndRender();
        //EventHandling();  not sure we should have this, there are events that it makes sense to handle in the node, privately and specifically, but there are events that we're going to have to handle in the manager (like checking vertices, drawing lines, etc)

        if (state != originalState) {
            NodeEdit.UpdateLayoutAndRepaint();
        }
    }

    [System.Serializable]
    public struct SaveDataFormat {
        public string nodeType;
        public string id;
        public int state;
        public float boxPosX;
        public float boxPosY;
        public string texBackground;
        public string texVert;
        public int numVerts;
        public string[] vertIDs;
        //public int[] vertOptions;
        //public string[] vertLabels;
        public string[] vertTargetNodeIDs;
        public string[] vertTargetVertIDs;
        //public int[] vertCompatibleTypesLen;
        //public int[] vertCompatibleIDsLen;
        //public Type[][] vertCompatibleTypes;
        //public string[][] vertCompatibleIDs;
    }

    virtual public byte[] GetSaveData() {
        //this way is better because we get to use the struct, but it's basically doing the same thing. try deserializing to see if it actually works. also uses more storage. also hard to account how much to move

        MemoryStream stream = new MemoryStream();
        BinaryWriter data = new BinaryWriter(stream);

        SaveDataFormat structData = new SaveDataFormat() {
            nodeType = GetType().ToString(),
            state = (int)state,
            id = id.ToString(),
            boxPosX = box.position.x,
            boxPosY = box.position.y,
            texBackground = AssetDatabase.GetAssetPath(backgroundTex),
            texVert = AssetDatabase.GetAssetPath(vertTex),
            numVerts = nodeVertices.Count

        };

        if (structData.numVerts > 0) {
            structData.vertIDs = new string[structData.numVerts];
            //structData.vertOptions = new int[structData.numVerts];
            //structData.vertLabels = new string[structData.numVerts];
            structData.vertTargetNodeIDs = new string[structData.numVerts];
            structData.vertTargetVertIDs = new string[structData.numVerts];
            //structData.vertCompatibleTypesLen = new int[structData.numVerts];
            //structData.vertCompatibleIDsLen = new int[structData.numVerts];
            //structData.vertCompatibleTypes = new Type[structData.numVerts][];
            //structData.vertCompatibleIDs = new string[structData.numVerts][];

            for (int i = 0; i < nodeVertices.Count; i++) {
                structData.vertIDs[i] = nodeVertices[i].id;
                //structData.vertOptions[i] = (int) nodeVertices[i].option;
                //structData.vertLabels[i] = nodeVertices[i].label;
                structData.vertTargetNodeIDs[i] = nodeVertices[i].targetNodeID;
                structData.vertTargetVertIDs[i] = nodeVertices[i].targetVertID;
                //structData.vertCompatibleTypes[i] = nodeVertices[i].compatibleTypes;
                //structData.vertCompatibleIDs[i] = nodeVertices[i].compatibleIDs;

                //structData.vertCompatibleTypesLen[i] = nodeVertices[i].compatibleTypes.Length;
                //structData.vertCompatibleIDsLen[i] = nodeVertices[i].compatibleIDs.Length;
            }
        }

        string nodeType = structData.nodeType.PadRight(31);
        data.Write(nodeType);
        BinaryFormatter formatter = new BinaryFormatter();
        formatter.Serialize(stream, structData);
        byte[] returnData = stream.ToArray();
        data.Close();
        stream.Close();
        return returnData;
    }

    public virtual void RestoreSaveData(BinaryReader data) {
        BinaryFormatter formatter = new BinaryFormatter();
        SaveDataFormat restore = (SaveDataFormat)formatter.Deserialize(data.BaseStream);

        id = restore.id;
        state = (nodeStates)restore.state;
        box.position = new Vector2(restore.boxPosX, restore.boxPosY);
        box.size = new Vector2(50, 50);
        backgroundTexPath = restore.texBackground;
        vertTexPath = restore.texVert;

        nodeVertices.Clear();
        
        for (int i = 0; i < restore.numVerts; i++) {
            //Type[] compatibleTypes = restore.vertCompatibleTypes[i];
            //string[] compatibleIDs = restore.vertCompatibleIDs[i];

            ConstructVertex(restore.vertIDs[i]);
            GetVertexByID(restore.vertIDs[i]).SetTarget(restore.vertTargetNodeIDs[i], restore.vertTargetVertIDs[i]);
        }
    }
}