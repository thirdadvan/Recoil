using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(PlayerLightExposure))]
public sealed class PlayerLightExposureEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        EditorGUILayout.PropertyField(serializedObject.FindProperty("fullyLitBrightness"),new GUIContent("完全照亮的参考亮度"));
        serializedObject.ApplyModifiedProperties();
        var exposure=(PlayerLightExposure)target;
        using(new EditorGUI.DisabledScope(true))
            EditorGUILayout.Slider("被照亮系数",Application.isPlaying?exposure.Coefficient:serializedObject.FindProperty("illumination").floatValue,PlayerLightExposure.MinimumCoefficient,1);
        EditorGUILayout.HelpBox("运行时自动计算：头、躯干、脚三点取平均。完全黑暗为 0.4，充分照亮为 1。主角自带视觉光不计入；环境灯及声波红色警灯计入。",MessageType.Info);
        if(Application.isPlaying)Repaint();
    }
}
