using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections;
using System.Collections.Generic;

namespace SoxwareInteractive.AnimationConversion
{
    internal class AnimationConverterLogWindow : EditorWindow
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
        private static Vector2 windowSize = new Vector2(400, 240);
        private static Vector2 inputScrollView = new Vector2();

        //----------------------
        // Internal
        //----------------------
        private string logMessages = "";

        //********************************************************************************
        // Public Methods
        //********************************************************************************

        public static AnimationConverterLogWindow ShowDialog(string logMessages)
        {
            AnimationConverterLogWindow window = ScriptableObject.CreateInstance<AnimationConverterLogWindow>();
            window.titleContent = new GUIContent("Animation Converter - Report");
            window.logMessages = logMessages;
            window.minSize = windowSize;
            window.ShowUtility();

            return window;
        }

        //********************************************************************************
        // Private Methods
        //********************************************************************************

        private void OnGUI()
        {
            inputScrollView = EditorGUILayout.BeginScrollView(inputScrollView, GUI.skin.box);
            {
                EditorGUILayout.TextArea(logMessages, GUI.skin.label);
            }
            EditorGUILayout.EndScrollView();

            GUILayout.Space(20);

            GUILayout.BeginHorizontal();
            {
                GUILayout.FlexibleSpace();

                bool returnKeyPressed = false;
                if (GUI.enabled && (Event.current.type == EventType.KeyDown) && (Event.current.keyCode == KeyCode.Return))
                {
                    returnKeyPressed = true;
                    Event.current.Use();
                }

                if (GUILayout.Button("OK", GUILayout.Height(25), GUILayout.Width(120)) || returnKeyPressed)
                {
                    Close();
                }

                GUILayout.FlexibleSpace();
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(5);
        }
    }
}
