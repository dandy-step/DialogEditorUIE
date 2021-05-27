using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "characterData", menuName = "Custom Editors/Character Data")]
public class CharacterData : ScriptableObject
{
    [HideInInspector][SerializeField] public string prefabPath;
    [SerializeField] public string characterName = "";
    [SerializeField] public string characterLongName = "";
    [SerializeField] public string characterDescription = "";
    [SerializeField] public Color editorColor = Color.gray;
    [SerializeField] public float editorAdvance = 0f;
}
