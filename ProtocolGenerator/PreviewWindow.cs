using UnityEditor;
using UnityEngine;

namespace UnityPlugin.Protobuf
{
    public class PreviewWindow : EditorWindow
    {
        private static PreviewWindow window;
        private static string text;

        public static void ShowWindow(string text)
        {
            window = GetWindow<PreviewWindow>("PreviewWindow");
            window.titleContent = new GUIContent("Preview");
            PreviewWindow.text = text;
            window.Show();
        }

        private void OnGUI()
        {
             GUILayout.Label(text, GUILayout.Width(position.width));
        }
    }
}