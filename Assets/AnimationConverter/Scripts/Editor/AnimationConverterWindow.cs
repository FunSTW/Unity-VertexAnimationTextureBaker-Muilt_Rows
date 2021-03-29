using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;

namespace SoxwareInteractive.AnimationConversion
{
    internal class AnimationConverterWindow : EditorWindow
    {
        //********************************************************************************
        // Public Properties
        //********************************************************************************

        //********************************************************************************
        // Private Properties
        //********************************************************************************

        //----------------------
        // Inspector
        //----------------------

        //----------------------
        // Internal
        //----------------------
        private Vector2 inputScrollView = new Vector2();
        private bool clipsToConvertFoldout = true;
        private bool convertToFoldout = true;
        private bool settingsFoldout = true;
        private bool gameObjectsFoldout = true;

        private AnimationConverter.Configuration configuration = new AnimationConverter.Configuration();
        private List<AnimationClip> clipsToConvertList = new List<AnimationClip>();

        private GUIStyle h1Style = null;

        private GUIContent animationTypeContent = new GUIContent("Animation Type", "The animation type the animation clips should be converted to.");
        private GUIContent outputDirectoryContent = new GUIContent("Output Directory", "The directory in which the converted animations should be stored.");
        private GUIContent keyReductionContent = new GUIContent("Key Reduction", "The strategy used to remove redundant keys.");
        private GUIContent positionErrorContent = new GUIContent("Position Error", "Defines how much error (in the form of maximum delta deviation in percentage) should be tolerate when reducing position curves. A smaller value results in a better looking animation and a bigger file size.");
        private GUIContent rotationErrorContent = new GUIContent("Rotation Error", "Defines how much error (in the form of maximum delta deviation in percentage) should be tolerate when reducing rotation curves. A smaller value results in a better looking animation and a bigger file size.");
        private GUIContent scaleErrorContent = new GUIContent("Scale Error", "Defines how much error (in the form of delta deviation in percentage) should be tolerate when reducing scale curves. A smaller value results in a better looking animation and a bigger file size.");
        private GUIContent keepExtraBonesContent = new GUIContent("Keep Extra Bones", "When enabled animated transforms that are not defined in the Avatar will be included as generic animation curves.");
        private GUIContent humanoidHandIkContent = new GUIContent("Apply Humanoid Hand IK", "Defines if Mecanim's humanoid IK (Inverse Kinematics) should be enabled for the hands while sampling the humanoid animation. This is only relevant when converting a humanoid animation to generic or legacy.");
        private GUIContent humanoidFootIkContent = new GUIContent("Apply Humanoid Foot IK", "Defines if Mecanim's humanoid IK (Inverse Kinematics) should be enabled for the feet while sampling the humanoid animation. This is only relevant when converting a humanoid animation to generic or legacy.");
        private GUIContent constrainRootPositionContent = new GUIContent("Constrain Root Position", "Defines if the x, y or z value of the root motion translation should be constrained (i.e. kept at a constant value).");
        private GUIContent constrainRootRotationContent = new GUIContent("Constrain Root Rotation", "Defines if the x, y or z value of the root motion euler rotation should be constrained (i.e. kept at a constant value).");
        private GUIContent inputPrefabContent = new GUIContent("Input", "The model the animation have been created for. The model needs to be configured with the same animation type as the clips that should be converted.");
        private GUIContent outputPrefabContent = new GUIContent("Output", "The same model as above but configured with the animation type the clips should be converted to.");

        //********************************************************************************
        // Public Methods
        //********************************************************************************

        [MenuItem("Window/Animation Converter")]
        public static void ShowWindow()
        {
            AnimationConverterWindow window = (AnimationConverterWindow)EditorWindow.GetWindow(typeof(AnimationConverterWindow));
            window.titleContent = new GUIContent("Convert Anim");
            window.minSize = new Vector2(250, 500);
        }

        //********************************************************************************
        // Private Methods
        //********************************************************************************

        private void OnGUI()
        {
            bool hasAnActiveError = false;

            if (h1Style == null)
            {
                h1Style = new GUIStyle(GUI.skin.label);
                h1Style.alignment = TextAnchor.MiddleCenter;
                h1Style.fontStyle = FontStyle.Bold;
                h1Style.fontSize = 14;
            }

            GUILayout.Space(5);

            GUILayout.Label("Animation Converter", h1Style);

            GUILayout.BeginHorizontal();
            {
                GUILayout.FlexibleSpace();
                GUILayout.Label("V" + AnimationConverter.GetVersion());
                GUILayout.FlexibleSpace();
            }
            GUILayout.EndHorizontal();
            GUILayout.Space(8);

            GUILayout.BeginHorizontal();
            {
                if (GUILayout.Button("Reset Configuration"))
                {
                    if (EditorUtility.DisplayDialog("Animation Converter - Reset Configuration", "Do you really want to reset the current configuration?\r\n\r\nAll changes will be lost!", "Yes", "No"))
                    {
                        configuration = new AnimationConverter.Configuration();
                        clipsToConvertList.Clear();
                    }
                }
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Manual"))
                {
                    AnimationConverter.BrowseManual();
                }
                if (GUILayout.Button("Support"))
                {
                    AnimationConverter.BrowseSupport();
                }
            }
            GUILayout.EndHorizontal();

            if (configuration.Prefabs == null)
            {
                configuration.Prefabs = new AnimationConverter.PrefabPair[1];
            }

            GUILayout.Space(4);

            DrawLine(1000, 2, 0, 0, Color.black);

            GUILayout.Space(10);

            AnimationConverter.AnimationType inputAnimationType = AnimationConverter.AnimationType.Legacy;
            bool inputAnimationTypeValid = false;
            bool multipleDifferentAnimationTypes = false;

            // ---------------------
            // Input
            // ---------------------
            clipsToConvertFoldout = EditorGUILayout.Foldout(clipsToConvertFoldout, "Input");
            if (clipsToConvertFoldout)
            {
                EditorGUI.indentLevel++;

                int indexToDelete = -1;
                bool anyClipNull = false;
                GUILayout.BeginHorizontal();
                {
                    GUILayout.Space(20);
                    inputScrollView = EditorGUILayout.BeginScrollView(inputScrollView, GUI.skin.box, GUILayout.Height(150));
                    {
                        for (int index = 0; index < clipsToConvertList.Count; index++)
                        {
                            GUILayout.BeginHorizontal();
                            {
                                GUILayout.BeginVertical(GUILayout.Width(20));
                                {
                                    if (GUILayout.Button("-"))
                                    {
                                        indexToDelete = index;
                                    }
                                }
                                GUILayout.EndVertical();

                                AnimationClip clip = clipsToConvertList[index];
                                if (clip != null)
                                {
                                    AnimationConverter.AnimationType animType = AnimationConverter.GetAnimationType(clip);
                                    if (!inputAnimationTypeValid)
                                    {
                                        inputAnimationType = animType;
                                        inputAnimationTypeValid = true;
                                    }
                                    else
                                    {
                                        if (animType != inputAnimationType)
                                        {
                                            multipleDifferentAnimationTypes = true;
                                        }
                                    }

                                    GUILayout.Label(animType.ToString(), GUILayout.Width(70));
                                }
                                else
                                {
                                    GUILayout.Label("", GUILayout.Width(70));

                                    anyClipNull = true;
                                }

                                clipsToConvertList[index] = (AnimationClip)EditorGUILayout.ObjectField(clip, typeof(AnimationClip), false);
                            }
                            GUILayout.EndHorizontal();
                        }
                    }
                    EditorGUILayout.EndScrollView();
                }
                GUILayout.EndHorizontal();

                if (indexToDelete >= 0)
                {
                    clipsToConvertList.RemoveAt(indexToDelete);
                }

                GUILayout.BeginHorizontal();
                {
                    GUILayout.Space(20);
                    GUILayout.BeginHorizontal(GUI.skin.box, GUILayout.Height(40));
                    {
                        GUILayout.FlexibleSpace();
                        GUILayout.BeginVertical();
                        {
                            GUILayout.FlexibleSpace();
                            GUILayout.Label("Drag & Drop Animation Clips Here");
                            GUILayout.FlexibleSpace();
                        }
                        GUILayout.EndVertical();
                        GUILayout.FlexibleSpace();
                    }
                    GUILayout.EndHorizontal();

                    Rect lastRect = GUILayoutUtility.GetLastRect();
                    Object[] droppedObjects = DragAndDropReceiver(lastRect, new System.Type[] { typeof(AnimationClip), typeof(GameObject) }, true);
                    if (droppedObjects != null)
                    {
                        foreach (Object droppedObj in droppedObjects)
                        {
                            AnimationClip animationClip = droppedObj as AnimationClip;

                            if (animationClip != null)
                            {
                                clipsToConvertList.Add(animationClip);
                            }
                            else
                            {
                                string assetPath = AssetDatabase.GetAssetPath(droppedObj);

                                Object[] allAssets = AssetDatabase.LoadAllAssetsAtPath(assetPath);

                                foreach (Object asset in allAssets)
                                {
                                    animationClip = asset as AnimationClip;
                                    if ((animationClip != null) && !animationClip.name.StartsWith("__preview__"))
                                    {
                                        clipsToConvertList.Add(animationClip);
                                    }
                                }
                            }
                        }
                    }
                }
                GUILayout.EndHorizontal();

                if (anyClipNull)
                {
                    EditorGUILayout.HelpBox("Entries with no clip assigned need to be removed.", MessageType.Error);

                    hasAnActiveError = true;
                }

                List<string> clipsWithDuplicateNames = GetClipsWithDuplicateNames(clipsToConvertList);
                if (clipsWithDuplicateNames.Count > 0)
                {
                    string helpText = "The following clip names are not unique:";
                    foreach (string clipName in clipsWithDuplicateNames)
                    {
                        helpText += string.Format("\r\n\"{0}\"", clipName);
                    }
                    EditorGUILayout.HelpBox(helpText, MessageType.Error);

                    hasAnActiveError = true;
                }

                if (multipleDifferentAnimationTypes)
                {
                    EditorGUILayout.HelpBox("All clips need to be of the same animation type.", MessageType.Error);

                    hasAnActiveError = true;
                }

                EditorGUI.indentLevel--;
            }

            // ---------------------
            // Output
            // ---------------------
            GUILayout.Space(10);
            convertToFoldout = EditorGUILayout.Foldout(convertToFoldout, "Output");
            if (convertToFoldout)
            {
                EditorGUI.indentLevel++;
                float originalLabelWidth = EditorGUIUtility.labelWidth;
                EditorGUIUtility.labelWidth = 125;

                configuration.DestinationAnimationType = (AnimationConverter.AnimationType)EditorGUILayout.EnumPopup(animationTypeContent, configuration.DestinationAnimationType);

                if (inputAnimationTypeValid && (configuration.DestinationAnimationType == inputAnimationType))
                {
                    EditorGUILayout.HelpBox("The output anmiation type must be different then the input animation type.", MessageType.Error);

                    hasAnActiveError = true;
                }

                GUILayout.BeginHorizontal();
                {
                    configuration.OutputDirectory = EditorGUILayout.TextField(outputDirectoryContent, configuration.OutputDirectory);

                    GUILayout.BeginVertical(GUILayout.Width(40));
                    {
                        if (GUILayout.Button("..."))
                        {
                            configuration.OutputDirectory = BrowseDirectory("Open Folder", configuration.OutputDirectory);
                        }
                    }
                    GUILayout.EndVertical();
                }
                GUILayout.EndHorizontal();

                if (!AssetDatabase.IsValidFolder(configuration.OutputDirectory))
                {
                    EditorGUILayout.HelpBox("Invalid output directory. The directory needs to be a child of the \"Assets\" folder.", MessageType.Error);

                    hasAnActiveError = true;
                }

                EditorGUIUtility.labelWidth = originalLabelWidth;
                EditorGUI.indentLevel--;
            }

            // ---------------------
            // Settings
            // ---------------------

            GUILayout.Space(10);
            settingsFoldout = EditorGUILayout.Foldout(settingsFoldout, "Settings");
            if (settingsFoldout)
            {
                EditorGUI.indentLevel++;

                if (inputAnimationTypeValid)
                {
                    float originalLabelWidth = EditorGUIUtility.labelWidth;
                    EditorGUIUtility.labelWidth = 170;

                    bool anySettingsShow = false;
                    if ((inputAnimationType == AnimationConverter.AnimationType.Humanoid) ||
                        (configuration.DestinationAnimationType == AnimationConverter.AnimationType.Humanoid))
                    {
                        configuration.KeyReduction = (AnimationConverter.KeyReductionMode)EditorGUILayout.EnumPopup(keyReductionContent, configuration.KeyReduction);
                        if (configuration.KeyReduction == AnimationConverter.KeyReductionMode.Lossy)
                        {
                            configuration.KeyReductionPositionError = EditorGUILayout.FloatField(positionErrorContent, configuration.KeyReductionPositionError);
                            configuration.KeyReductionRotationError = EditorGUILayout.FloatField(rotationErrorContent, configuration.KeyReductionRotationError);
                            configuration.KeyReductionScaleError = EditorGUILayout.FloatField(scaleErrorContent, configuration.KeyReductionScaleError);
                        }

                        anySettingsShow = true;
                    }

                    if (configuration.DestinationAnimationType == AnimationConverter.AnimationType.Humanoid)
                    {
                        configuration.HumanoidKeepExtraGenericBones = EditorGUILayout.Toggle(keepExtraBonesContent, configuration.HumanoidKeepExtraGenericBones);

                        anySettingsShow = true;
                    }
                    else if (inputAnimationType == AnimationConverter.AnimationType.Humanoid)
                    {
                        configuration.SampleHumanoidHandIK = EditorGUILayout.Toggle(humanoidHandIkContent, configuration.SampleHumanoidHandIK);
                        configuration.SampleHumanoidFootIK = EditorGUILayout.Toggle(humanoidFootIkContent, configuration.SampleHumanoidFootIK);

                        GUILayout.BeginHorizontal();
                        {
                            GUILayout.Space(20);
                            GUILayout.Label(constrainRootPositionContent, GUILayout.Width(150));
                            configuration.ConstrainRootMotionPosition.ConstrainX = GUILayout.Toggle(configuration.ConstrainRootMotionPosition.ConstrainX, "X");
                            configuration.ConstrainRootMotionPosition.ConstrainY = GUILayout.Toggle(configuration.ConstrainRootMotionPosition.ConstrainY, "Y");
                            configuration.ConstrainRootMotionPosition.ConstrainZ = GUILayout.Toggle(configuration.ConstrainRootMotionPosition.ConstrainZ, "Z");
                            GUILayout.FlexibleSpace();
                        }
                        GUILayout.EndHorizontal();

                        GUILayout.BeginHorizontal();
                        {
                            GUILayout.Space(20);
                            GUILayout.Label(constrainRootRotationContent, GUILayout.Width(150));
                            configuration.ConstrainRootMotionRotation.ConstrainX = GUILayout.Toggle(configuration.ConstrainRootMotionRotation.ConstrainX, "X");
                            configuration.ConstrainRootMotionRotation.ConstrainY = GUILayout.Toggle(configuration.ConstrainRootMotionRotation.ConstrainY, "Y");
                            configuration.ConstrainRootMotionRotation.ConstrainZ = GUILayout.Toggle(configuration.ConstrainRootMotionRotation.ConstrainZ, "Z");
                            GUILayout.FlexibleSpace();
                        }
                        GUILayout.EndHorizontal();

                        anySettingsShow = true;
                    }

                    EditorGUIUtility.labelWidth = originalLabelWidth;

                    if (!anySettingsShow)
                    {
                        EditorGUILayout.LabelField("No settings require");
                    }
                }
                else
                {
                    EditorGUILayout.HelpBox("Please assign the input clip(s) first.", MessageType.Warning);
                }
                EditorGUI.indentLevel--;
            }

            // ---------------------
            // Prefabs
            // ---------------------
            GUILayout.Space(10);
            gameObjectsFoldout = EditorGUILayout.Foldout(gameObjectsFoldout, "Prefabs");
            if (gameObjectsFoldout)
            {
                EditorGUI.indentLevel++;
                string helpText = string.Format("The model related to the animation clips needs to be assigned once using the animation type of the source clips and once using the destination animation type.\r\n\r\n1) Duplicate the original model in Unity's Project Window.\r\n\r\n2) Change the animation type of the duplicated model to the desired animation type and click on Apply.\r\n\r\n3) Drag & Drop the duplicated model to the appropriate field:", configuration.DestinationAnimationType);
                EditorGUILayout.HelpBox(helpText, MessageType.Info);
                EditorGUI.indentLevel--;

                GUILayout.BeginHorizontal();
                {
                    GUILayout.Space(20);

                    int removePrefabIndex = -1;
                    if (configuration.Prefabs.Length > 1)
                    {
                        GUILayout.BeginVertical(GUILayout.Width(20));
                        {
                            GUILayout.Space(38);
                            for (int prefabIndex = 1; prefabIndex < configuration.Prefabs.Length; prefabIndex++)
                            {
                                if (GUILayout.Button("-", GUILayout.Height(15)))
                                {
                                    removePrefabIndex = prefabIndex;
                                }
                            }
                        }
                        GUILayout.EndVertical();
                    }

                    GUILayout.BeginVertical();
                    {
                        if (inputAnimationTypeValid)
                        {
                            inputPrefabContent.text = string.Format("Input ({0})", inputAnimationType);
                        }
                        else
                        {
                            inputPrefabContent.text = "Input";
                        }
                        GUILayout.Label(inputPrefabContent);

                        for (int prefabIndex = 0; prefabIndex < configuration.Prefabs.Length; prefabIndex++)
                        {
                            AnimationConverter.PrefabPair prefabPair = configuration.Prefabs[prefabIndex];

                            prefabPair.SourcePrefab = (GameObject)EditorGUILayout.ObjectField("", prefabPair.SourcePrefab, typeof(GameObject), false, GUILayout.MinWidth(100), GUILayout.ExpandWidth(true));

                            if (inputAnimationTypeValid && (prefabPair.SourcePrefab != null))
                            {
                                string errorMessage;
                                AnimationConverter.AnimationType sourceAnimationType = DetermineAnimationType(prefabPair.SourcePrefab, out errorMessage);

                                if (!string.IsNullOrEmpty(errorMessage))
                                {
                                    EditorGUILayout.HelpBox(errorMessage, MessageType.Error);
                                    hasAnActiveError = true;
                                }
                                else if (sourceAnimationType != inputAnimationType)
                                {
                                    EditorGUILayout.HelpBox(string.Format("The prefab's animation type is {0} but should be {1}", sourceAnimationType, inputAnimationType), MessageType.Error);
                                    hasAnActiveError = true;
                                }
                            }

                            configuration.Prefabs[prefabIndex] = prefabPair;
                        }
                    }
                    GUILayout.EndVertical();
                
                    GUILayout.BeginVertical();
                    {
                        outputPrefabContent.text = string.Format("Output ({0})", configuration.DestinationAnimationType);
                        GUILayout.Label(outputPrefabContent);

                        for (int prefabIndex = 0; prefabIndex < configuration.Prefabs.Length; prefabIndex++)
                        {
                            AnimationConverter.PrefabPair prefabPair = configuration.Prefabs[prefabIndex];

                            prefabPair.DestinationPrefab = (GameObject)EditorGUILayout.ObjectField("", prefabPair.DestinationPrefab, typeof(GameObject), false, GUILayout.MinWidth(100), GUILayout.ExpandWidth(true));

                            if (prefabPair.DestinationPrefab != null)
                            {
                                string errorMessage;
                                AnimationConverter.AnimationType animationType = DetermineAnimationType(prefabPair.DestinationPrefab, out errorMessage);

                                if (!string.IsNullOrEmpty(errorMessage))
                                {
                                    EditorGUILayout.HelpBox(errorMessage, MessageType.Error);
                                    hasAnActiveError = true;
                                }
                                else if (animationType != configuration.DestinationAnimationType)
                                {
                                    EditorGUILayout.HelpBox(string.Format("The prefab's animation type is {0} but should be {1}", animationType, configuration.DestinationAnimationType), MessageType.Error);
                                    hasAnActiveError = true;
                                }
                            }

                            configuration.Prefabs[prefabIndex] = prefabPair;
                        }
                    }
                    GUILayout.EndVertical();

                    if (removePrefabIndex >= 0)
                    {
                        List<AnimationConverter.PrefabPair> prefabsList = configuration.Prefabs.ToList();
                        prefabsList.RemoveAt(removePrefabIndex);
                        configuration.Prefabs = prefabsList.ToArray();
                    }
                }
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                {
                    GUILayout.Space(20);

                    if (GUILayout.Button("Add Row"))
                    {
                        System.Array.Resize(ref configuration.Prefabs, configuration.Prefabs.Length + 1);
                    }
                }
                GUILayout.EndHorizontal();
            }

            GUILayout.Space(10);

            // ---------------------
            // Convert Button
            // ---------------------

            if (GUILayout.Button("Convert", GUILayout.Height(30)))
            {
                if (clipsToConvertList.Count <= 0)
                {
                    EditorUtility.DisplayDialog("Animation Converter - Couldn't start Conversion", "No clips to convert.", "OK");
                }
                else if (hasAnActiveError)
                {
                    EditorUtility.DisplayDialog("Animation Converter - Couldn't start Conversion", "Please fix all errors first.", "OK");
                }
                else if ((configuration.Prefabs[0].SourcePrefab == null) ||
                         (configuration.Prefabs[0].DestinationPrefab == null))
                {
                    EditorUtility.DisplayDialog("Animation Converter - Couldn't start Conversion", "Please assign the correct prefabs first.", "OK");
                }
                else
                {
                    string errorMessage = "This action would overwrite the following input clips:";
                    bool wouldOverwriteInputClips = false;
                    foreach (AnimationClip clip in clipsToConvertList)
                    {
                        string path = AssetDatabase.GetAssetPath(clip);

                        if (System.IO.Path.GetDirectoryName(path) == configuration.OutputDirectory)
                        {
                            errorMessage += string.Format("\r\n\"{0}.anim\"", clip.name);
                            wouldOverwriteInputClips = true;
                        }
                    }

                    if (wouldOverwriteInputClips)
                    {
                        errorMessage += "\r\n\r\nPlease choose a different output directory.";
                        EditorUtility.DisplayDialog("Animation Converter - Couldn't start Conversion", errorMessage, "OK");
                    }
                    else
                    {
                        try
                        {
                            string logMessages;
                            AnimationConverter.Convert(clipsToConvertList.ToArray(), configuration, out logMessages);

                            AnimationConverterLogWindow.ShowDialog(logMessages);
                        }
                        catch (System.Exception ex)
                        {
                            Debug.LogException(ex);

                            EditorUtility.DisplayDialog("Animation Converter - An Error Occured", ex.Message + "\r\n\r\nA detailed error message is printed to the console.", "OK");
                        }
                    }
                }
            }
        }

        private string BrowseDirectory(string title, string directory)
        {
            if (string.IsNullOrEmpty(directory))
            {
                directory = "Assets";
            }
            string selectedDirectory = EditorUtility.OpenFolderPanel(title, directory, "");

            if (!string.IsNullOrEmpty(selectedDirectory))
            {
                if (selectedDirectory.IndexOf(Application.dataPath) == 0)
                {
                    selectedDirectory = selectedDirectory.Substring(Application.dataPath.Length - 6);
                }
                else
                {
                    EditorUtility.DisplayDialog("Invalid Path", "The selected directory must be a child of the projects \"Assets\" folder.", "OK");
                    selectedDirectory = directory;
                }

                if (!AssetDatabase.IsValidFolder(selectedDirectory))
                {
                    EditorUtility.DisplayDialog("Directory Not Found", "The selected directory is not part of this Unity Project. Make sure to create folders inside Unity's Project Window.", "OK");
                    selectedDirectory = directory;
                }
            }
            else
            {
                selectedDirectory = directory;
            }

            return selectedDirectory;
        }

        private static UnityEngine.Object[] DragAndDropReceiver(Rect position, System.Type[] types, bool isPresistent)
        {
            UnityEngine.Object[] droppedObjects = null;

            if (GUI.enabled)
            {
                switch (Event.current.type)
                {
                    case EventType.DragUpdated:
                    case EventType.DragPerform:
                        if (position.Contains(Event.current.mousePosition))
                        {
                            List<UnityEngine.Object> acceptedDraggedObjectsList = null;
                            bool atLeastOneAcceptedDraggedObj = false;

                            foreach (UnityEngine.Object objectRef in DragAndDrop.objectReferences)
                            {
                                System.Type type = objectRef.GetType();

                                if ((System.Array.IndexOf(types, type) >= 0) && (EditorUtility.IsPersistent(objectRef) == isPresistent))
                                {
                                    if (Event.current.type == EventType.DragPerform)
                                    {
                                        if (acceptedDraggedObjectsList == null)
                                        {
                                            acceptedDraggedObjectsList = new List<UnityEngine.Object>();
                                        }
                                        acceptedDraggedObjectsList.Add(objectRef);
                                    }

                                    atLeastOneAcceptedDraggedObj = true;
                                }
                            }

                            if (atLeastOneAcceptedDraggedObj)
                            {
                                DragAndDrop.visualMode = DragAndDropVisualMode.Generic;

                                if (Event.current.type == EventType.DragPerform)
                                {
                                    droppedObjects = acceptedDraggedObjectsList.ToArray();

                                    DragAndDrop.AcceptDrag();
                                    DragAndDrop.activeControlID = 0;
                                }

                                Event.current.Use();
                            }
                        }
                        break;
                }
            }

            return droppedObjects;
        }

        private static void DrawLine(float maxWidth, float thikness, float marginLeft, float marginRight, Color color)
        {
            Rect lastRect = GUILayoutUtility.GetRect(maxWidth, thikness);
            lastRect.x += marginLeft;
            lastRect.width -= (marginLeft + marginRight);
            lastRect.height = thikness;

            EditorGUI.DrawRect(lastRect, color);
        }

        private static List<string> GetClipsWithDuplicateNames(List<AnimationClip> clipsList)
        {
            return clipsList.Where(x => (x != null)).GroupBy(x => x.name).Where(g => g.Count() > 1).Select(y => y.Key).ToList();
        }

        private AnimationConverter.AnimationType DetermineAnimationType(GameObject gameObject, out string errorMessage)
        {
            Animator animator = gameObject.GetComponent<Animator>();
            Animation animation = gameObject.GetComponent<Animation>();

            AnimationConverter.AnimationType animationType = AnimationConverter.AnimationType.Legacy;
            errorMessage = "";

            if ((animator == null) && (animation == null))
            {
                errorMessage = "No \"Animator\" or \"Animation\" component found.";
            }
            else if ((animator != null) && (animation != null))
            {
                errorMessage = "Only an \"Animator\" or an \"Animation\" component must be attached to the Prefab.";
            }
            else if (animator != null)
            {
                if (animator.isHuman)
                {
                    animationType = AnimationConverter.AnimationType.Humanoid;
                }
                else
                {
                    animationType = AnimationConverter.AnimationType.Generic;
                }
            }
            else if (animation != null)
            {
                animationType = AnimationConverter.AnimationType.Legacy;
            }

            return animationType;
        }
    }
}
