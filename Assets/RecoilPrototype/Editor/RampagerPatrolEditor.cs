using System.Linq;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(RampagerPatrolPoint)),CanEditMultipleObjects]
public sealed class RampagerPatrolPointEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        EditorGUILayout.PropertyField(serializedObject.FindProperty("order"),new GUIContent("序号"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("duration"),new GUIContent("停留时间（秒）"));
        var action=serializedObject.FindProperty("action");
        EditorGUI.showMixedValue=action.hasMultipleDifferentValues;
        EditorGUI.BeginChangeCheck();
        int selected=EditorGUILayout.Popup("到达后的行为",action.enumValueIndex,new[]{"停留（保持朝向）","左右张望","向左看","向右看"});
        if(EditorGUI.EndChangeCheck())action.enumValueIndex=selected;
        EditorGUI.showMixedValue=false;
        if(action.hasMultipleDifferentValues || action.enumValueIndex==1)
            EditorGUILayout.PropertyField(serializedObject.FindProperty("lookInterval"),new GUIContent("张望间隔（秒）"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("customAction"),new GUIContent("扩展行为（可选）"));
        serializedObject.ApplyModifiedProperties();
        EditorGUILayout.HelpBox("拖动该子物体设置目标位置。序号从小到大执行；停留时间从到达后开始。当前角色只沿地面水平移动。",MessageType.Info);
        foreach(var value in targets)
        {
            var point=(RampagerPatrolPoint)value;var controller=point.GetComponentInParent<RampagerController>();
            if(!controller || !controller.GetComponent<RampagerPatrolRoute>())
                EditorGUILayout.HelpBox("请将行为点放在 Rampager 下，并给 Rampager 添加 Rampager Patrol Route。",MessageType.Warning);
            else if(Mathf.Abs(point.transform.position.y-controller.transform.position.y)>.1f)
                EditorGUILayout.HelpBox("该点高度与怪物不同。当前巡逻只使用 X 坐标，不会跳跃或爬升。",MessageType.Warning);
        }
    }
    [DrawGizmo(GizmoType.Selected | GizmoType.NonSelected)]
    static void DrawLabel(RampagerPatrolPoint point,GizmoType type)
    {Handles.Label(point.Destination+Vector3.up*.25f,$"{point.order} · {point.action} · {point.duration:0.##}s");}
}

[CustomEditor(typeof(RampagerPatrolRoute))]
public sealed class RampagerPatrolRouteEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        EditorGUILayout.PropertyField(serializedObject.FindProperty("loop"),new GUIContent("循环执行"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("arrivalDistance"),new GUIContent("到达距离"));
        serializedObject.ApplyModifiedProperties();
        var route=(RampagerPatrolRoute)target;var points=route.OrderedPoints();
        EditorGUILayout.HelpBox("只在 Roaming 时执行。其他姿态会中断巡逻；恢复后返回被中断的点并重新执行该点的停留行为。",MessageType.Info);
        if(!Application.isPlaying)
        {
            if(GUILayout.Button("添加行为点"))AddPoint(route);
            if(points.Length==0 && GUILayout.Button("创建四种行为的示例路线"))CreateExample(route);
        }
        EditorGUILayout.LabelField("执行顺序",EditorStyles.boldLabel);
        foreach(var point in points)
            if(GUILayout.Button($"{point.order}  {point.name}  ·  {point.duration:0.##} 秒",EditorStyles.miniButton))Selection.activeGameObject=point.gameObject;
        if(points.GroupBy(p=>p.order).Any(g=>g.Count()>1))EditorGUILayout.HelpBox("存在重复序号，将按 Hierarchy 顺序执行。可修改序号以明确顺序。",MessageType.Warning);
        if(Application.isPlaying && route.CurrentPoint)EditorGUILayout.LabelField("当前点",$"{route.CurrentPoint.order} · {route.Elapsed:0.0}s {(route.Complete?"（已完成）":"")}");
    }
    public static RampagerPatrolPoint AddPoint(RampagerPatrolRoute route)
    {
        var points=route.OrderedPoints();int order=points.Length>0?points.Max(p=>p.order)+1:0;
        var go=new GameObject($"Patrol Point {order:00}");Undo.RegisterCreatedObjectUndo(go,"Add Rampager patrol point");
        go.transform.SetParent(route.transform,false);go.transform.position=route.transform.position;
        var point=Undo.AddComponent<RampagerPatrolPoint>(go);point.order=order;
        Selection.activeGameObject=go;EditorUtility.SetDirty(point);return point;
    }
    public static void CreateExample(RampagerPatrolRoute route)
    {
        if(route.OrderedPoints().Length>0)return;
        var owner=route.GetComponent<RampagerController>();
        var offsets=new[]{owner.leftOffset,0,owner.rightOffset,0};
        var actions=new[]{RampagerPatrolPoint.Action.LookRight,RampagerPatrolPoint.Action.LookAround,RampagerPatrolPoint.Action.LookLeft,RampagerPatrolPoint.Action.Stay};
        for(int i=0;i<offsets.Length;i++)
        {var point=AddPoint(route);point.transform.position=route.transform.position+Vector3.right*offsets[i];point.action=actions[i];point.duration=i==1?3:1.5f;EditorUtility.SetDirty(point);}
        Selection.activeGameObject=route.gameObject;
    }
    [MenuItem("Recoil/Patrol/Add Example Routes to Scene Rampagers")]
    static void AddExamplesToScene()
    {
        foreach(var owner in Object.FindObjectsOfType<RampagerController>())
        {var route=owner.GetComponent<RampagerPatrolRoute>();if(!route)route=Undo.AddComponent<RampagerPatrolRoute>(owner.gameObject);CreateExample(route);}
    }
}
