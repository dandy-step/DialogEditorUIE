using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;


[System.Serializable]
public class NodeManager : ISerializationCallbackReceiver {
    [SerializeField] private NodeEditPrefs prefs;
    public List<GenericNode> nodes;
    [SerializeField] private List<GenericNode> currentNodeList;
    public static GenericNode activeNode = null;
    [SerializeField] private GenericNode previousActiveNode;
    [SerializeField] private Vector2 dragOffset;
    public string saveFilePath = "";
    public string tempFilePath = "";
    public float zoomLevel = 1.0f;
    public Rect viewRect;

    //nodemap save file constants
    string SAVE_NODEMAP_MAGICNUMBER = "NODE_MAP";
    int SAVE_NODEMAP_VERSION = 1;
    int SAVE_NODEMAP_VERSION_OFFSET = 8;
    int SAVE_NODEMAP_DATE_OFFSET = 12;
    int SAVE_NODEMAP_NODE_COUNT_OFFSET = 32;
    int SAVE_NODEMAP_DATA_START_OFFSET = 48;
    int SAVE_NODEMAP_NODE_BLOCK_SIZE = 1024 * 2;

    public NodeManager(NodeEditPrefs _prefs) {
        prefs = _prefs;
        nodes = new List<GenericNode>();
        currentNodeList = null;
        activeNode = null;
    }

    public void SaveNodeFile(string filePath, bool serialization) {
        Stream stream = null;

        if (serialization) {
            tempFilePath = Application.dataPath + "/" + "serializationData" + ".tempSerialized";
            stream = File.Create(tempFilePath);
        } else {
            stream = File.Create(filePath);
        }

        BinaryWriter data = new BinaryWriter(stream);
        data.Write(SAVE_NODEMAP_MAGICNUMBER.ToCharArray());
        data.Write(SAVE_NODEMAP_VERSION);
        data.Write(System.DateTime.Now.ToString());
        data.Write(nodes.Count);
        data.Write(SAVE_NODEMAP_DATA_START_OFFSET);
        data.Seek(SAVE_NODEMAP_DATA_START_OFFSET, SeekOrigin.Begin);

        for (int i = 0; i < nodes.Count; i++) {
            byte[] nodeData = nodes[i].GetSaveData();
            data.Write(nodeData);
            stream.Seek(SAVE_NODEMAP_NODE_BLOCK_SIZE - nodeData.Length, SeekOrigin.Current);
        }

        data.Close();
        stream.Close();

        if (serialization) {
            nodes.Clear();
        }
    }

    public void OpenNodeFile(Stream fileStream) {
        BinaryReader data = new BinaryReader(fileStream);

        fileStream.Seek(SAVE_NODEMAP_NODE_COUNT_OFFSET, SeekOrigin.Begin);
        int numNodes = data.ReadInt32();
        int dataStartOffset = data.ReadInt32();
        fileStream.Seek(dataStartOffset, SeekOrigin.Begin);

        for (int i = 0; i < numNodes; i++) {
            long currPos = fileStream.Position;
            string nodeType = data.ReadString().Trim();

            if (Type.GetType(nodeType).BaseType == typeof(GenericNode)) {
                nodes.Add((GenericNode)Activator.CreateInstance(Type.GetType(nodeType), new object[] { true }));
                nodes[nodes.Count - 1].RestoreSaveData(data);
            } else {
                Debug.Log("BAD NODE TYPE " + nodeType + "!");
            }

            fileStream.Seek(currPos + SAVE_NODEMAP_NODE_BLOCK_SIZE, SeekOrigin.Begin);
        }

        data.Close();
        fileStream.Close();
    }

    public void AddNode(Type nodeType) {
        nodes.Add((GenericNode)Activator.CreateInstance(nodeType, new object[] { prefs }));
    }

    public void SetActiveNode(GenericNode tgt) {
        previousActiveNode = activeNode;
        activeNode = tgt;
    }

    public void AddToNodeList(GenericNode tgt) {
        currentNodeList.Add(tgt);
    }

    public void RemoveNode(int index) {
        nodes.RemoveAt(index);
    }

    public void RemoveNode(string id) {
        nodes.Remove(nodes.Find(x => x.GetID() == id));
    }

    public GenericNode GetNodeByID(string id) {
        foreach (GenericNode node in nodes) {
            if (node.GetID() == id) {
                return node;
            }
        }

        throw new Exception("Node with ID " + id + " not found.");
    }

    public void DrawLine(Vector2 orig) {
        Handles.color = Color.magenta;
        Handles.DrawBezier(orig, Event.current.mousePosition, orig + new Vector2(0, 32f), Event.current.mousePosition + new Vector2(0, 32f), Color.magenta, null, 4f);
    }

    public void DrawLine(Vector2 orig, Vector2 target) {
        Handles.color = Color.magenta;
        Handles.DrawBezier(orig, target, orig + new Vector2(0, 32f), target - new Vector2(0, 32f), Color.magenta, null, 4f);
    }

    void RightClickMenu(object data) {
        //remove node
        if (data.GetType() == typeof(GUID)) {
            if (EditorUtility.DisplayDialog("Confirm", "Remove node with GUID " + (GUID)data + "?", "OK", "Cancel")) {
                RemoveNode((string)data);
            }
        ////add node
        //} else if (data.GetType() == typeof(String)) {
        //    switch ((string)data) {
        //        //case "generic":
        //        //    AddGenericNode();
        //        //    break;
        //        case "decision":
        //            AddDecisionNode();
        //            break;
        //        default:
        //            Debug.Log("Unrecognized right click menu node string. Nothing was added.");
        //            break;
        //    }
        }
    }

    public void HandleEvents(EditorWindow window) {
        Vector2 mousePos = Event.current.mousePosition;

        if (Event.current.type == EventType.MouseDown) {

            //right click
            if (Event.current.button == 1) {
                GenericMenu menu = new GenericMenu();

                //on node
                foreach (GenericNode node in nodes) {
                    if (node.IsBeingHovered()) {
                        menu.AddItem(new GUIContent("Remove Node"), false, RightClickMenu, node.GetID());
                        menu.ShowAsContext();
                        return;
                    }
                }

                //blank space
                menu.AddItem(new GUIContent("Add Node/Generic Node"), false, RightClickMenu, "generic");
                menu.AddItem(new GUIContent("Add Node/Decision Node"), false, RightClickMenu, "decision");
                menu.ShowAsContext();

            } else {
                foreach (GenericNode node in nodes) {
                    if (node.box.Contains(mousePos)) {
                        if (Event.current.clickCount == 2) {
                            node.DoubleClickBehaviour();
                        } else {
                            foreach (NodeVertex vert in node.nodeVertices) {
                                if (new Rect(node.box.position + vert.box.position, vert.box.size).Contains(mousePos)) {
                                    node.SetActiveVertex(vert);
                                    break;
                                }
                            }
                        }

                        SetActiveNode(node);
                    }
                }
            }
        } else if (Event.current.type == EventType.MouseDrag) {
            if (activeNode != null) {
                if (!activeNode.dragging && (activeNode.activeVertex == null)) {
                    if (activeNode.box.Contains(mousePos)) {
                        activeNode.dragging = true;
                        dragOffset = activeNode.box.center - mousePos;
                    } else {
                        activeNode = null;
                    }
                }

                if (activeNode.dragging) {
                    activeNode.box.center = mousePos + dragOffset;
                    window.Repaint();
                } else if (activeNode.activeVertex != null) {
                    foreach (GenericNode node in nodes) {
                        if (node.box.Contains(mousePos) && (node != activeNode)) {
                            foreach (NodeVertex vert in node.nodeVertices) {
                                if (new Rect(node.box.position + vert.box.position, vert.box.size).Contains(mousePos)) {
                                    if (vert.CheckCompatibilityWithVertex(node.GetType())) {
                                        activeNode.activeVertex.SetTarget(vert.ownerNodeID, vert.id);
                                        window.SendEvent(new Event() { type = EventType.MouseUp });
                                        break;
                                    }
                                }
                            }
                        }
                    }
                    window.Repaint();
                }
            }
        } else if (Event.current.type == EventType.MouseUp) {
            if (activeNode != null) {
                activeNode.dragging = false;
                activeNode.SetActiveVertex(null);
                dragOffset = Vector2.zero;
                activeNode = null;
            }
        } else if (Event.current.type == EventType.Repaint) {
            //we need to catch the repaint here and do the drawing directly because there's no permanence
            if ((activeNode != null) && (activeNode.activeVertex != null)) {
                DrawLine(activeNode.box.position + activeNode.activeVertex.box.center);
                window.Repaint();
            }

            foreach (GenericNode node in nodes) {
                foreach (NodeVertex vert in node.nodeVertices) {
                    if (vert.targetVertID != "") {
                        DrawLine(node.box.position + vert.box.center, GetNodeByID(vert.targetNodeID).box.position + GetNodeByID(vert.targetNodeID).GetVertexByID(vert.targetVertID).box.center);
                    }
                }
            }
        } else if (Event.current.type == EventType.MouseLeaveWindow) {
            window.SendEvent(new Event() { type = EventType.MouseUp });
        }
        //} else if (Event.current.type == EventType.ScrollWheel) {
        //    Debug.Log(Event.current.delta.y);
        //    zoomLevel += (Event.current.delta.y * 0.1f);
        //    Mathf.Clamp(zoomLevel, 0f, 1.0f);
        //    window.Repaint();
        //}
    }

    public void SortNodes() {
        List<GenericNode> rootNodes = new List<GenericNode>();
        List<string> targets = new List<string>();

        foreach (GenericNode node in nodes) {
            foreach (NodeVertex vert in node.nodeVertices) {
                if ((vert.targetNodeID != null) && (vert.targetNodeID != "") && (!targets.Contains(vert.targetNodeID))) {
                    targets.Add(vert.targetNodeID);
                }
            }
        }

        foreach (GenericNode node in nodes) {
            if (!targets.Contains(node.GetID())) {
                rootNodes.Add(node);
            }
        }

        foreach (GenericNode node in rootNodes) {
            Debug.Log("GOT " + node.GetID().Substring(0, 5));
        }
    }

    public void RenderNodes() {
        viewRect = new Rect(Vector2.zero, Vector2.zero);

        foreach (GenericNode node in nodes) {
            viewRect.size = new Vector2(Math.Max(viewRect.size.x, node.box.xMax), Math.Max(viewRect.size.y, node.box.yMax));
            viewRect.size += new Vector2(20f, 20f);
            node.zoomLevel = zoomLevel;
            node.Render();
        }
    }

    public void OnBeforeSerialize() {
        SaveNodeFile("", true);
    }

    public void OnAfterDeserialize() {
        FileStream tempFile = File.OpenRead(tempFilePath);

        if (tempFile != null) {
            OpenNodeFile(tempFile);
            tempFile.Close();
            File.Delete(tempFilePath);
        }
    }
}
