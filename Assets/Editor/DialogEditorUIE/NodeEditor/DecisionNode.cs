using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using System.IO;

public class DecisionNode : GenericNode {
    static public string PREVIOUS_NODE = "previous_node";
    static public string TEXTURE_NODE = "texture_node";
    static public string PROPERTY_NODE = "property_node";
    static public string CAP_NODE = "cap_node";
    static public string NEXT_NODE = "next_node";

    public DecisionNode(bool serializing) : base(serializing) { }

    public DecisionNode(NodeEditPrefs prefs) : base(prefs.decisionBackground, prefs.defaultVert, NodeEdit.GetMidScreenRect()) { }

    public override void ConstructVertex(string id) {
        if (id == PREVIOUS_NODE) {
            AddVertex(vertOptions.TOP_VERT, PREVIOUS_NODE, "Previous Node", new Type[] { typeof(DialogNode) }, new string[] { DialogNode.NEXT_NODE });
        } else if (id == TEXTURE_NODE) {
            AddVertex(vertOptions.RIGHT_VERT, TEXTURE_NODE, "Texture");
        } else if (id == PROPERTY_NODE) {
            AddVertex(vertOptions.LEFT_VERT, PROPERTY_NODE, "Property");
        } else if (id == CAP_NODE) {
            AddVertex(vertOptions.RIGHT_VERT, CAP_NODE, "Captain Crunch");
        } else if (id.Contains(NEXT_NODE)) {
            int currentCount = 0;
            for (int i = 0; i < nodeVertices.Count; i++) {
                if (nodeVertices[i].id.Contains(NEXT_NODE)) {
                    currentCount++;
                }
            }

            AddVertex(vertOptions.BOTTOM_VERT, NEXT_NODE + currentCount, "Path " + currentCount, new Type[] { typeof(DialogNode) }, new string[] { DialogNode.PREVIOUS_NODE });
        }
    }

    protected override void RenderUIContent() {
        if (state == nodeStates.BLANK) {
            GUILayout.Space(16f);
            GUILayout.BeginHorizontal();

            if (GUILayout.Button("Set up")) {
                state = nodeStates.IDLE;
            }

            GUILayout.EndHorizontal();
            GUILayout.Space(16f);

        } else if (state == nodeStates.IDLE) {
            GUILayout.BeginVertical();

            GUILayout.Label("Set a condition to determine where \n the nodes should go");

            RenderVerts(vertOptions.LEFT_VERT, true);
            RenderVerts(vertOptions.RIGHT_VERT, true);

            GUILayout.Button("Click me, dummy");

            if (nodeVertices.Count == 0) {
                ConstructVertex(PREVIOUS_NODE);
                ConstructVertex(TEXTURE_NODE);
                ConstructVertex(CAP_NODE);
                ConstructVertex(PROPERTY_NODE);
                ConstructVertex(NEXT_NODE);
                ConstructVertex(NEXT_NODE);
                ConstructVertex(NEXT_NODE);
            }

            GUILayout.EndVertical();
        }
    }

    public override void RestoreSaveData(BinaryReader data) {
        base.RestoreSaveData(data);
    }

}
