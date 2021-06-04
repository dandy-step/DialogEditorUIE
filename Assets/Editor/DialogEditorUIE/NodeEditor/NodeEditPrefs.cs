using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NodeEdit", menuName = "Custom Editors/NodeEditPrefs")]
[System.Serializable]
public class NodeEditPrefs : ScriptableObject
{
    public Texture2D saveIcon;
    public Texture2D addNodeIcon;
    public Texture2D sortIcon;

    public Texture2D defaultBackground;
    public Texture2D defaultVert;
    public Texture2D debugRed;

    public Texture2D dialogBackground;
    public Texture2D decisionBackground;
}
