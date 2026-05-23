using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(RoomTransitionScreen))]
public class RoomTransitionScreenEditor : Editor
{
    private static readonly Keyframe[] DEFAULT_CURVE = new Keyframe[]
    {
        new Keyframe(0f,    0f,    0f,   2.5f),
        new Keyframe(0.35f, 0.75f, 3f,   1.5f),
        new Keyframe(0.72f, 0.97f, 0.4f, 0f),
        new Keyframe(1f,    1f,    0f,   0f),
    };

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space(4);

        var target = (RoomTransitionScreen)this.target;
        if (GUILayout.Button("↺  Reset Animation Curve về mặc định (horror)"))
        {
            Undo.RecordObject(target, "Reset RollCurve");
            target.rollCurve = new AnimationCurve(DEFAULT_CURVE);
            EditorUtility.SetDirty(target);
        }
    }
}
