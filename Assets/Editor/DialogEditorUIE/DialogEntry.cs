using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UIElements.Experimental;
using System.Collections.Generic;
using System;
using System.IO;

namespace DialogTool {
    public class DialogEntry : VisualElement {
        const ushort BAD_ANIMATOR = ushort.MaxValue;
        public enum SANITY_CHECK_FAIL {
            OKAY,
            BAD_ANIM_NO_IDLE,
            BAD_ANIM_CONTROLLER,
            BAD_NAME_LOGIC, //unimp
            BAD_TEXT_LOGIC  //unimp
        };

        const bool showCharacterNamesInNameMenu = false;
        public EditorWindow sourceWindow;
        public CharacterData character;
        public int id = 0;
        private string customLabel = "";
        private string animClip = "";
        private string idleClip = "";
        private bool supportsAnimations = false;
        public ScrollingLabel nameButton;
        public Button animButton;
        public Image portrait;
        public Image mainSpeakerIcon;
        private TextField dialogText;
        private VisualElement backgroundContainer;
        private TextElement animBadge;
        ContextualMenuManipulator nameButtonMenu;
        ContextualMenuManipulator animButtonMenu;
        ContextualMenuManipulator mainContextualMenu;
        private DateTime lastEnterPress;

        public DialogEntry() : this(0, "", DialogEditorUIE.characters[0]) { }

        public DialogEntry(int _id, string _dialog, CharacterData _character) {
            sourceWindow = EditorWindow.focusedWindow;
            character = _character;

            VisualTreeAsset tree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Editor/DialogEditorUIE/DialogEntry.uxml");
            StyleSheet style = AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/Editor/DialogEditorUIE/DialogEntry.uss");
            VisualElement ui = tree.CloneTree();
            ui.styleSheets.Add(style);
            lastEnterPress = DateTime.Now;

            id = _id;
            backgroundContainer = ui.Q<VisualElement>("dialog_entry_container");
            mainSpeakerIcon = ui.Q<Image>("main_speaker_icon");
            nameButton = ui.Q<ScrollingLabel>("name_button");
            animButton = ui.Q<Button>("animation_button");
            dialogText = ui.Q<TextField>("dialog_text");
            animBadge = ui.Q<TextElement>("animation_clip_badge");
            //ui.RegisterCallback<GeometryChangedEvent>(Geo);   //unnecessary with Unity update?
            Add(ui);


            //manipulators and events
            nameButtonMenu = new ContextualMenuManipulator(NameButtonMenu);
            nameButtonMenu.activators.Add(new ManipulatorActivationFilter() { button = MouseButton.LeftMouse });
            nameButton.AddManipulator(nameButtonMenu);

            animButtonMenu = new ContextualMenuManipulator(AnimButtonMenu);
            animButtonMenu.activators.Add(new ManipulatorActivationFilter() { button = MouseButton.LeftMouse });
            animButton.AddManipulator(animButtonMenu);
            this.AddManipulator(new DragManipulator());

            mainContextualMenu = new ContextualMenuManipulator(MainContextualMenu);
            mainContextualMenu.activators.Add(new ManipulatorActivationFilter() { button = MouseButton.RightMouse });
            this.AddManipulator(mainContextualMenu);

            //callback for keyboard input logic on the dialog input box
            dialogText.RegisterCallback<KeyUpEvent>(DialogInputEvents, TrickleDown.NoTrickleDown);

            Texture texMainSpeaker = (Texture)AssetDatabase.LoadAssetAtPath("Assets/Textures/Editor/main_speaker_icon.png", typeof(Texture));
            mainSpeakerIcon.image = texMainSpeaker;
            mainSpeakerIcon.style.display = DisplayStyle.None;

            SetDialogText(_dialog);
            CheckIfMainSpeaker();

            //load character customizations
            SetCharacter(character);
            UpdateAnimationUI();
        }

        public void CheckIfMainSpeaker() {
            if (DialogEditorUIE.mainSpeaker != null) {
                if (DialogEditorUIE.mainSpeaker.characterName == character.characterName) {
                    mainSpeakerIcon.style.display = DisplayStyle.Flex;
                } else {
                    mainSpeakerIcon.style.display = DisplayStyle.None;
                }
            } else {
                mainSpeakerIcon.style.display = DisplayStyle.None;
            }
        }

        //creates custom factory for traits
        public class Factory : UxmlFactory<DialogEntry, Traits> { }

        public class Traits : UxmlTraits {
            UxmlStringAttributeDescription text = new UxmlStringAttributeDescription() { name = "dialogText" } ;

            public override IEnumerable<UxmlChildElementDescription> uxmlChildElementsDescription {
                get { yield break; }
            }

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc) {
                base.Init(ve, bag, cc);
                ((DialogEntry)ve).dialogText.value = text.GetValueFromBag(bag, cc);
            }
        }

        public void SetBackgroundColor(StyleColor color) {
            backgroundContainer.style.backgroundColor = color;
        }

        public StyleColor GetBackgroundColor() {
            return backgroundContainer.style.backgroundColor;
        }

        public void SetDialogText(string text) {
            dialogText.SetValueWithoutNotify(text);
        }

        public string GetDialogText() {
            return dialogText.text;
        }

        public void SetSpeakerName(string name) {
            if (name != character.characterName) {
                customLabel = name;
            }

            nameButton.text.text = name;
        }

        public string GetSpeakerName() {
            return nameButton.text.text;
        }

        //updates animation UI, hiding elementes in case of changes
        public void UpdateAnimationUI() {
            if (string.IsNullOrEmpty(character.prefabPath)) {
                //hide animation UI elements for narrator character
                animButton.style.display = DisplayStyle.None;
                animBadge.style.display = DisplayStyle.None;
                supportsAnimations = false;
            } else {
                supportsAnimations = true;
                if (GetAnimationsList().Count == 0) {
                    animBadge.style.display = DisplayStyle.None;
                    animButton.style.display = DisplayStyle.Flex;
                    animButton.name = "animation_button_bad";
                    animButton.text = "<NO ANIMS!>";
                } else {
                    animButton.style.display = DisplayStyle.Flex;
                    animBadge.style.display = DisplayStyle.Flex;
                    animButton.text = "Animation";
                    if (string.IsNullOrEmpty(animClip)) {
                        idleClip = EditorWindow.GetWindow<DialogEditorUIE>("UPDATE ANIM UI").GetCurrentCharacterIdle(character);
                        animBadge.name = "animation_clip_badge_idle";
                        animBadge.text = "Idle";
                    } else {
                        animButton.name = "animation_button";
                    }
                }
            }
        }

        //set a clip as our animation
        public void SetAnimClip(string animName) {
            UpdateAnimationUI();
            animButton.style.display = DisplayStyle.Flex;
            animBadge.style.display = DisplayStyle.Flex;
            animButton.name = "animation_button";
            animClip = animName;
            animBadge.text = "Play: " + animClip;
            animBadge.name = "animation_clip_badge_animclip";
        }

        public string GetAnimClipName() {
            return animClip;
        }

        public string GetIdleClipName() {
            return idleClip;
        }

        //checks if we're main character, updates icon display if so
        public void UpdateMainCharacterIcon(CharacterData mainChar) {
            if (mainChar.characterName == character.characterName) {
                mainSpeakerIcon.style.display = DisplayStyle.Flex;
            } else {
                mainSpeakerIcon.style.display = DisplayStyle.None;
            }
        }

        //enables and disables styling depending on the type of character
        public void SetCharacter(CharacterData newChar) {
            if ((!string.IsNullOrEmpty(newChar.characterName) && (string.IsNullOrEmpty(newChar.prefabPath)))) {
                Debug.Log("Bad character prefab path... how did this get corrupted?");
            }

            SetBackgroundColor(newChar.editorColor);

            if (newChar.prefabPath == "") {
                this.EnableInClassList("EnvironmentalStyle", true);
                this.style.marginLeft = 0;
            } else {
                this.EnableInClassList("EnvironmentalStyle", false);
                this.style.marginLeft = newChar.editorAdvance;
            }

            if (newChar != character) {
                SetSpeakerName(newChar.characterName);
                customLabel = "";
            } else {
                if (customLabel != "") {
                    SetSpeakerName(customLabel);
                } else {
                    SetSpeakerName(newChar.characterName);
                }
            }

            if (animClip == DialogEditorUIE.ANIM_ERROR_VALUE) {
                animClip = "";
            }

            //idleClip = EditorWindow.GetWindow<DialogEditorUIE>().GetCurrentCharacterIdle(character);
            CheckIfMainSpeaker();
            UpdateAnimationUI();
        }

        public void FocusOnDialogText() {
            dialogText.Q<VisualElement>("unity-text-input").Focus();
        }

        //logic for custom labels
        public void SetSpeakerNameFromContextMenu(DropdownMenuAction evt) {
            if (evt.name == "Custom Name") {
                UnityEngine.UIElements.PopupWindow window = new UnityEngine.UIElements.PopupWindow();
                window.style.position = Position.Absolute;
                //window.style.top = evt.eventInfo.mousePosition.y;
                //window.style.left = evt.eventInfo.mousePosition.x;
                window.StretchToParentWidth();
                parent.GetFirstAncestorOfType<VisualElement>().Add(window);
                Label nameLabel = new Label("Custom Name: ");
                TextField nameField = new TextField();
                Button okayButton;
                Button cancelButton;

                Action acceptCustomName = () => {
                    if (nameField.text != "") {
                        customLabel = nameField.text;
                        SetSpeakerName(nameField.text);
                        window.parent.Remove(window);
                    }
                };

                Action cancel = () => {
                    window.parent.Remove(window);
                };

                okayButton = new Button(acceptCustomName);
                okayButton.text = "Accept";
                cancelButton = new Button(cancel);
                cancelButton.text = "Cancel";
                nameField.RegisterCallback<KeyUpEvent>(keyEvent => {
                    if (keyEvent.keyCode == KeyCode.Return) {
                        acceptCustomName();
                    } else if (keyEvent.keyCode == KeyCode.Escape) {
                        window.parent.Remove(window);
                    }
                });

                window.Add(nameLabel);
                window.Add(nameField);
                window.Add(okayButton);
                window.Add(cancelButton);
                nameField.Q<VisualElement>("unity-text-input").Focus();
            } else {
                if (evt.name == character.characterName) {
                    customLabel = "";
                    SetSpeakerName(evt.name);
                } else {
                    customLabel = evt.name;
                    SetSpeakerName(customLabel);
                }
            }
        }

        public void SetAnimationFromContextMenu(DropdownMenuAction evt) {
            string animClipName = (string)evt.name;
            SetAnimClip(evt.name);
        }

        //general context menu
        public void MainContextualMenu(ContextualMenuPopulateEvent evt) {
            evt.menu.AppendAction("Set as Main Speaker", SetAsMainSpeaker);
            evt.menu.AppendAction("Delete entry", DeleteEntry);
            evt.StopPropagation();
        }

        //sets character as main speaker - you can only have one of these
        public void SetAsMainSpeaker(DropdownMenuAction evt) {
            EditorWindow.GetWindow<DialogEditorUIE>("SET MAIN SPEAKER").SetMainSpeaker(character);
        }

        public void DeleteEntry(DropdownMenuAction evt) {
            if (EditorUtility.DisplayDialog("", "Delete this entry?", "Ok", "Cancel")) {
                EditorWindow.GetWindow<DialogEditorUIE>("REMOVE ENTRY").RemoveEntry(animButton.GetFirstAncestorOfType<DialogEntry>());
            }
        }

        //grabs animation clip names, returns them in a list
        public List<string> GetAnimationsList() {
            List<string> animNames = new List<string>();

            if (!String.IsNullOrEmpty(character.prefabPath)) {
                GameObject prefab = PrefabUtility.LoadPrefabContents(character.prefabPath);
                if (prefab) {
                    RuntimeAnimatorController animator = ((prefab.GetComponent<Animator>() != null) ? prefab.GetComponent<Animator>().runtimeAnimatorController : null);
                    if (animator) {
                        for (int i = 0; i < animator.animationClips.Length; i++) {
                            animNames.Add(animator.animationClips[i].name);
                            Debug.Log(animNames[i]);
                        }
                    }

                    if (animNames.Count == 0) {
                        Animation animation = prefab.GetComponent<Animation>();
                        if (animation) {
                            foreach (AnimationState animState in animation) {
                                animNames.Add(animState.name);
                            }
                        }
                    }

                    PrefabUtility.UnloadPrefabContents(prefab);
                }
            }

            return animNames;
        }
        
        //allows you to set a custom name for a specific dialog entry, overriding the CharacterData
        public void NameButtonMenu(ContextualMenuPopulateEvent evt) {
            if (showCharacterNamesInNameMenu) {
                if (!string.IsNullOrEmpty(character.characterName)) {
                    List<string> speakerNames = new List<string>();

                    foreach (VisualElement entry in parent.Children()) {
                        if (entry.GetType() == typeof(DialogEntry)) {
                            string name = ((DialogEntry)entry).GetSpeakerName();
                            if (!speakerNames.Contains(name)) {
                                speakerNames.Add(name);
                            }
                        }
                    }

                    for (int i = 0; i < speakerNames.Count; i++) {
                        evt.menu.AppendAction(speakerNames[i], SetSpeakerNameFromContextMenu, DropdownMenuAction.Status.Normal);
                    }

                    evt.menu.AppendSeparator();
                }
            }

            evt.menu.AppendAction("Custom Name", SetSpeakerNameFromContextMenu, DropdownMenuAction.Status.Normal);
            evt.StopImmediatePropagation();
        }

        //populate list of available animations to set in dialog for our characters in a context menu
        //animaitions will be on an Animation component, rather than an animator
        public void AnimButtonMenu(ContextualMenuPopulateEvent evt) {
            List<string> animNames = GetAnimationsList();

            if (supportsAnimations) {
                Debug.Log("IDLE: " + idleClip);
                string idleText = (string.IsNullOrEmpty(idleClip)) ? "Set Idle Clip/" : "Change Idle? (" + idleClip + ")/";

                for (int i = 0; i < animNames.Count; i++) {
                    evt.menu.AppendAction(idleText + animNames[i], SetIdleClipFromContextMenu, DropdownMenuAction.Status.Normal);
                }

                if (!string.IsNullOrEmpty(idleClip)) {
                    evt.menu.AppendAction("(Idle)", x => { animClip = ""; UpdateAnimationUI(); });
                }

                evt.menu.AppendSeparator();
                for (int i = 0; i < animNames.Count; i++) {
                    evt.menu.AppendAction(animNames[i], SetAnimationFromContextMenu, DropdownMenuAction.Status.Normal);
                }
            }

            evt.StopImmediatePropagation();
        }

        //sets idle clip for this character, calls out to EditorWindow to update all other entries with the same character
        public void SetIdleClipFromContextMenu(DropdownMenuAction evt) {
            idleClip = evt.name.Substring(evt.name.LastIndexOf("/") + 1);
            UpdateAnimationUI();
            EditorWindow.GetWindow<DialogEditorUIE>("SET IDLE FROM CONTEXT").UpdateIdles(idleClip, character);
        }

        //directly set the clip, useful for serialization, since Unity can break on all sorts of situations where it has to access components for GameObjects while serializing
        public void SetIdleClip(string clipName) {
            idleClip = clipName;
        }

        //cleanup previous to generating save data
        public SANITY_CHECK_FAIL CheckSanity() {

            //check animation state sanity
            if (supportsAnimations) {
                if ((string.IsNullOrEmpty(animClip) && (string.IsNullOrEmpty(idleClip)))) {
                    if (animButton.name != "animation_button_bad") {
                        return SANITY_CHECK_FAIL.BAD_ANIM_NO_IDLE;
                    } else {
                        return SANITY_CHECK_FAIL.BAD_ANIM_CONTROLLER;
                    }
                }
            }

            return SANITY_CHECK_FAIL.OKAY;
        }

        //handles keyboard input for dialog entry
        public void DialogInputEvents(KeyUpEvent evt) {
            if ((evt.keyCode == KeyCode.Return) || (evt.keyCode == KeyCode.KeypadEnter)) {
                DateTime now = DateTime.Now;

                if ((dialogText.cursorIndex == 1) || (evt.ctrlKey)) {
                    //cycle speakers by pressing enter at start or holding CTRL, eat paragraph
                    Event actionEvent = Event.KeyboardEvent("backspace");
                    KeyDownEvent backspaceEvent = KeyDownEvent.GetPooled(actionEvent);
                    dialogText.SendEvent(backspaceEvent);
                    //character = ((DialogEditorUIE)sourceWindow).CycleCharacter(character);
                    character = EditorWindow.GetWindow<DialogEditorUIE>("CYCLE CHARACTER").CycleCharacter(character);
                    SetCharacter(character);
                } else if ((dialogText.cursorIndex == dialogText.text.Length) && (dialogText.text.Length > 2)) {

                    //add new dialog entry if double pressed enter in quick succession
                    dialogText.value = dialogText.text.TrimEnd('\n');

                    if ((now - lastEnterPress).TotalMilliseconds < 300) {
                        evt.StopImmediatePropagation();
                        DialogEntry newEntry = new DialogEntry(id + 1, "", character);
                        newEntry.SetDialogText("<ENTER TEXT>");
                        parent.Insert(parent.IndexOf(this) + 1, newEntry);
                        ((ScrollView)parent).ScrollTo(parent.ElementAt(parent.IndexOf(this)));
                        ((ScrollView)parent).scrollOffset = new Vector2(((ScrollView)parent).scrollOffset.x, ((ScrollView)parent).scrollOffset.y + parent.ElementAt(parent.IndexOf(this)).layout.height);
                        ((DialogEntry)parent.ElementAt(parent.IndexOf(this) + 1)).FocusOnDialogText();
                    }
                }

                lastEnterPress = now;
            }
        }

        //data container class for writing to disk
        class DialogEntrySaveData {
            public char[] magicNumber = new char[4] { 'D', 'E', 'N', 'T' };
            public ushort characterIndex;       //which character                          
            public ushort customLabelLength;    //if there's a custom label, how long             
            public ushort animClipLength;       //if there's an animation, how long
            public ushort dialogLength;         //how long the text is
            public char[] customLabel;          //custom label text             
            public char[] animClipName;         //animation clip name                                 
            public char[] dialog;               //dialog text              
            public bool supportsAnimations;     //whether this character supports anims
            public bool animIsIdle;             //true if the anim clip is an idle
        }

        //generates contents of a text entry for a disk-writable DLG file, but can also be used for assembly serialization
        public byte[] GenerateSaveData(List<string> assetNames) {
            using (MemoryStream stream = new MemoryStream()) {
                using (BinaryWriter writer = new BinaryWriter(stream)) {
                    DialogEntrySaveData data = new DialogEntrySaveData();
                    data.dialogLength = (ushort)GetDialogText().Length;

                    if (data.dialogLength == 0) {
                        Debug.Log("Tried to save dialog entry with zero size dialog!");
                    }

                    if (character.prefabPath != "") {
                        int characterIndex = assetNames.IndexOf(character.prefabPath);

                        if (characterIndex >= 0) {
                            data.characterIndex = (ushort)characterIndex;
                        } else {
                            Action test = () => { for (int i = 0; i < assetNames.Count; i++) { Debug.Log(assetNames[i]); } };
                            throw new Exception("Couldn't find character in assetNames with index " + data.characterIndex.ToString() + "!\n Here were the assetnames:\n" + test);
                        }
                    } else {
                        data.characterIndex = 0;
                    }

                    //fill in data structure
                    data.customLabelLength = (ushort)customLabel.ToCharArray().Length;
                    data.supportsAnimations = supportsAnimations;
                    data.dialogLength = (ushort)GetDialogText().ToCharArray().Length;
                    data.customLabel = customLabel.ToCharArray();

                    //catch the animation cases: either there's an animation set, or it uses an idle, or it's a dialog character with no animation support (such as a narrator)
                    if (!string.IsNullOrEmpty(animClip)) {
                        data.animClipName = animClip.ToCharArray();
                        data.animClipLength = (ushort)animClip.Length;
                        data.animIsIdle = false;
                    } else if (!string.IsNullOrEmpty(idleClip)) {
                        data.animClipName = idleClip.ToCharArray();
                        data.animClipLength = (ushort)idleClip.Length;
                        data.animIsIdle = true;
                    } else {
                        if (data.supportsAnimations) {
                            Debug.Log("Tried to write an entry that supports anims with no animation or idle! This shoudn't happen unless you are saving a bad character! Here's the entry:");
                            Debug.Log("Character Index: " + data.characterIndex);
                            data.animClipName = DialogEditorUIE.ANIM_ERROR_VALUE.ToCharArray();
                            data.animClipLength = (ushort)data.animClipName.Length;
                        } else {
                            data.animClipName = animClip.ToCharArray();
                            data.animClipLength = (ushort)data.animClipName.Length;
                        }
                    }

                    data.dialog = GetDialogText().ToCharArray();

                    //write it
                    writer.Write(data.magicNumber);
                    writer.Write(data.characterIndex);
                    writer.Write(data.customLabelLength);
                    writer.Write(data.supportsAnimations);
                    writer.Write(data.animIsIdle);
                    writer.Write(data.animClipLength);
                    writer.Write(data.dialogLength);
                    writer.Seek(32, SeekOrigin.Begin);
                    writer.Write(data.customLabel);
                    writer.Write(data.animClipName);
                    writer.Write(data.dialog);
                    return stream.ToArray();    //send it back out to the DLG creator
                }
            }
        }

        //restores data previously generated in GenerateSaveData()
        public void RestoreSaveData(BinaryReader reader, List<CharacterData> characterList, List<string> idles) {
            //serialization state debugging
            //Debug.Log("Restore save data, here's the characterList we were fed:\n Count: " + characterList.Count);
            //for (int i = 0; i < characterList.Count; i++) {
            //    Debug.Log(characterList[i].characterName + " " + characterList[i].prefabPath);
            //}

            long originalPosition = reader.BaseStream.Position - 4;

            //uses same type of container as GenerateSaveData()
            DialogEntrySaveData data = new DialogEntrySaveData();
            data.characterIndex = reader.ReadUInt16();
            data.customLabelLength = reader.ReadUInt16();
            data.supportsAnimations = reader.ReadBoolean();
            data.animIsIdle = reader.ReadBoolean();
            data.animClipLength = reader.ReadUInt16();
            data.dialogLength = reader.ReadUInt16();
            reader.BaseStream.Seek(originalPosition + 32, SeekOrigin.Begin);
            data.customLabel = reader.ReadChars(data.customLabelLength);

            if (data.animClipLength != BAD_ANIMATOR) {
                data.animClipName = reader.ReadChars(data.animClipLength);
            }

            idleClip = idles[data.characterIndex];
            data.dialog = reader.ReadChars(data.dialogLength);
            if (!data.animIsIdle) {
                SetAnimClip(new string(data.animClipName));
            } else {
                SetAnimClip("");
            }

            SetDialogText(new string(data.dialog));
            supportsAnimations = data.supportsAnimations;

            if (data.characterIndex >= 0) {
                try {
                    character = characterList[data.characterIndex];
                } catch (ArgumentOutOfRangeException e) {
                    Debug.Log("Actual value: " + e.ActualValue);
                }

                SetCharacter(character);
            }

            if (data.customLabelLength > 0) {
                SetSpeakerName(new string(data.customLabel));
            }

            CheckIfMainSpeaker();
        }
    }

    //unused, see MultipleChoiceContainer instead
    //public class MultipleChoiceEntry : VisualElement {
    //    private Label choiceText;
    //    public List<string> choices;
    //    public MultipleChoiceContainer choiceContainer;

    //    public MultipleChoiceEntry(MultipleChoiceContainer container) {
    //        VisualTreeAsset tree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Editor/DialogEditorUIE/MultipleChoiceEntry.uxml");
    //        StyleSheet style = AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/Editor/DialogEditorUIE/DialogEntry.uss");

    //        VisualElement element = tree.CloneTree();
    //        Button addDialogButton = new Button(AddDialog);
    //        addDialogButton.text = "ADD DLG";
    //        element.Add(addDialogButton);
    //        choiceContainer = element.Q<MultipleChoiceContainer>("choice_container");
    //        choiceText = element.Q<Label>("choice_text");
    //        Add(element);
    //    }

    //    public void SetEntryText(string text) {
    //        choiceText.text = text;
    //    }

    //    public void AddDialog() {
    //        DialogEntry entry = new DialogEntry(0, "TEST TEST TEST", DialogEditorUIE.characters[0]);
    //        this.Add(entry);
    //    }

    //    class MultipleChoiceEntrySaveData {
    //        public char[] magicNumber = new char[4] { 'C', 'H', 'O', 'O' };         //0
    //        public ushort entryLength = 0;
    //        public ushort choiceCount = 0;
    //        public List<string> choices = new List<string>();
    //    }

    //    public byte[] GenerateSaveData(List<string> assetNames) {
    //        using (MemoryStream stream = new MemoryStream()) {
    //            using (BinaryWriter writer = new BinaryWriter(stream)) {
    //                MultipleChoiceEntrySaveData data = new MultipleChoiceEntrySaveData();
    //                data.choiceCount = (ushort)choices.Count;
    //                data.choices = choices;
    //                return new byte[1];
    //            }
    //        }
    //    }
    //}
}

//custom manipulator to enable dragging and reordering the elements in the list, which wasn't a built-in manipulator for UIElements as of Unity 2019.1 - handles highlighting the item being dragged over and repositioning of items
public class DragManipulator : MouseManipulator {
    public VisualElement draggedElement = null;
    public Image floatingElement = null;
    public VisualElement hoveredElement = null;
    private VisualElement lastHoveredElement = null;
    public Tuple<StyleLength, StyleLength, StyleColor, StyleFloat> originalHoverElementMargins;
    private Vector2 dragOrigin = Vector2.zero;
    public bool enabled, distanceTriggered;

    public DragManipulator() {
        activators.Add(new ManipulatorActivationFilter { button = MouseButton.LeftMouse });
    }

    protected override void RegisterCallbacksOnTarget() {
        target.RegisterCallback<MouseDownEvent>(OnMouseDown);
        target.RegisterCallback<MouseMoveEvent>(OnMouseMove);
        target.RegisterCallback<MouseUpEvent>(OnMouseUp);
        target.RegisterCallback<KeyDownEvent>(EscapeBailout, TrickleDown.NoTrickleDown);
    }

    protected override void UnregisterCallbacksFromTarget() {
        target.UnregisterCallback<MouseDownEvent>(OnMouseDown);
        target.UnregisterCallback<MouseMoveEvent>(OnMouseMove);
        target.UnregisterCallback<MouseUpEvent>(OnMouseUp);
        target.UnregisterCallback<KeyDownEvent>(EscapeBailout, TrickleDown.NoTrickleDown);
    }

    public void EscapeBailout(KeyDownEvent evt) {
        if ((enabled) && (evt.keyCode == KeyCode.Escape)) {
            draggedElement.ReleaseMouse();
            if (floatingElement != null) {
                floatingElement.parent.Remove(floatingElement);
                floatingElement = null;
            }

            if (lastHoveredElement != null) {
                lastHoveredElement.EnableInClassList("HoverStyle", false);
                lastHoveredElement = null;
            }

            if (hoveredElement != null) {
                hoveredElement.EnableInClassList("HoverStyle", false);
                hoveredElement = null;
            }

            enabled = false;
        }
    }

    public void OnMouseDown(MouseDownEvent evt) {
        if (CanStartManipulation(evt)) {
            enabled = true;
            dragOrigin = evt.mousePosition;
            draggedElement = target;
            evt.StopImmediatePropagation();
        }
    }

    public void OnMouseMove(MouseMoveEvent evt) {
        if (enabled) {
            draggedElement.CaptureMouse();
            if (!distanceTriggered) {
                if (Vector2.Distance(dragOrigin, evt.mousePosition) > 15f) {
                    distanceTriggered = true;
                } else {
                    return;
                }
            }

            if (floatingElement == null) {
                floatingElement = new Image();

                Vector2 pos;
                int width = (int)draggedElement.contentRect.width;
                int height = (int)draggedElement.contentRect.height;
                Texture2D capture = new Texture2D(width, height);

                pos = EditorWindow.focusedWindow.position.position + draggedElement.worldBound.position;
                Color[] pixels = UnityEditorInternal.InternalEditorUtility.ReadScreenPixel(pos, width, height);
                capture.SetPixels(pixels);
                capture.Apply();

                floatingElement.image = capture;
                floatingElement.style.opacity = 0.25f;
                floatingElement.style.position = Position.Absolute;
                EditorWindow.focusedWindow.rootVisualElement.Add(floatingElement);
            }

            floatingElement.style.left = evt.mousePosition.x;
            floatingElement.style.top = evt.mousePosition.y;

            foreach (VisualElement child in draggedElement.parent.Children()) {
                if (child.ContainsPoint(child.WorldToLocal(new Vector2(evt.mousePosition.x, evt.mousePosition.y)))) {
                    hoveredElement = child;
                    if (originalHoverElementMargins == null) {
                        originalHoverElementMargins = new Tuple<StyleLength, StyleLength, StyleColor, StyleFloat>(hoveredElement.style.paddingTop, hoveredElement.style.paddingBottom, hoveredElement.style.backgroundColor, hoveredElement.style.opacity);
                    }
                    break;
                }
            }

            if ((lastHoveredElement != null) && (hoveredElement != lastHoveredElement)) {
                lastHoveredElement.EnableInClassList("HoverStyle", false);
                hoveredElement.AddToClassList("HoverStyle");
                hoveredElement.EnableInClassList("HoverStyle", true);
            }

            lastHoveredElement = hoveredElement;
        }
    }

    public void OnMouseUp(MouseUpEvent evt) {
        if ((enabled) && (lastHoveredElement != null)) {
            lastHoveredElement.EnableInClassList("HoverStyle", false);

            if (lastHoveredElement != draggedElement) {
                int originalIndex = draggedElement.parent.IndexOf(draggedElement);
                int newIndex = draggedElement.parent.IndexOf(lastHoveredElement);
                VisualElement draggedParent = draggedElement.parent;

                draggedParent.Remove(draggedElement);
                draggedParent.Insert(newIndex, draggedElement);
            }

            floatingElement.parent.Remove(floatingElement);
        }

        dragOrigin = Vector2.zero;
        draggedElement.ReleaseMouse();
        floatingElement = null;
        draggedElement = null;
        hoveredElement = null;
        lastHoveredElement = null;
        enabled = false;
        distanceTriggered = false;
    }
}

namespace DialogTool {
    public class ScrollingLabel : VisualElement {
        Button btn;
        ScrollView scrollView;
        public TextElement text;
        public float startWaitValue = 1f;
        public float endWaitValue = 1.25f;
        double stopTimer = 0;
        bool resetTimer = false;
        ValueAnimation<float> scrollAnim;

        public class Factory : UxmlFactory<ScrollingLabel, Traits> { }

        public class Traits : UxmlTraits {
            UxmlStringAttributeDescription text = new UxmlStringAttributeDescription() { name = "scrollingLabel" };

            public override IEnumerable<UxmlChildElementDescription> uxmlChildElementsDescription {
                get { yield break; }
            }

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc) {
                base.Init(ve, bag, cc);
                ((ScrollingLabel)ve).text.text = text.GetValueFromBag(bag, cc);
            }
        }

        public ScrollingLabel() : this("Default Label") { }

        public ScrollingLabel(string labelText) {
            scrollView = new ScrollView(ScrollViewMode.Horizontal);
            scrollView.showHorizontal = false;
            btn = new Button();
            //btn.style.width = 100;
            btn.Add(scrollView);
            text = new TextElement() { text = labelText };
            scrollView.Add(text);
            Add(btn);

            scrollAnim = text.experimental.animation.Start(scrollView.horizontalScroller.lowValue, scrollView.horizontalScroller.highValue, 10000, (x, y) => { UpdateTextScroll(x, y); });
            scrollAnim.easingCurve = Easing.InOutBack;
            scrollAnim.onAnimationCompleted = ResetTextScroll;
            scrollAnim.autoRecycle = true;
            btn.RegisterCallback<TooltipEvent>((x) => x.tooltip = labelText, TrickleDown.NoTrickleDown);
        }

        public void ResetTextScroll() {
            scrollAnim = text.experimental.animation.Start(scrollView.horizontalScroller.lowValue, scrollView.horizontalScroller.highValue, 10000, (x, y) => { UpdateTextScroll(x, y); });
            scrollAnim.easingCurve = Easing.InOutBack;
            scrollAnim.onAnimationCompleted = ResetTextScroll;
            scrollAnim.autoRecycle = true;
        }

        public void UpdateTextScroll(VisualElement ui, float value) {
            scrollAnim.to = scrollView.horizontalScroller.highValue;
            scrollAnim.durationMs = 1000 * ((((int)scrollAnim.to + (int)btn.style.width.value.value)) / 25);
            scrollView.horizontalScroller.style.display = DisplayStyle.None;
            scrollView.horizontalScroller.value = value;
        }
    }
}