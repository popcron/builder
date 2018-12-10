using UnityEditor;
using UnityEngine;

namespace Popcron.Builder
{
    internal class Window
    {
        internal static void DrawHeader(string barText, string infoText, float padding = 0f)
        {
            GUI.BeginGroup(new Rect(0, padding, Screen.width, 20));
            GUI.Box(new Rect(0, 0, Screen.width, 17), "", EditorStyles.toolbar);
            GUI.Label(new Rect(0, 0, Screen.width, 20), barText, EditorStyles.miniLabel);
            GUI.EndGroup();

            GUI.color = new Color(0.65f, 0.65f, 0.65f);
            GUI.Label(new Rect(0, padding + 17, Screen.width, 20), infoText);
            GUI.color = Color.white;
        }
    }
}