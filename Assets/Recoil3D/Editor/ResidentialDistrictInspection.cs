using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
public static class ResidentialDistrictInspection
{
 public static void Run()
 {
  Directory.CreateDirectory("output/residential");var b=new StringBuilder();
  foreach(var path in new[]{"Assets/Scenes/3D/RecoilPrototype.unity","Assets/Scenes/3D/FactoryRooms/FactoryWashroomF2.unity","Assets/Scenes/3D/RecoilKitchenMemory.unity","Assets/Scenes/3D/RecoilFactoryInterior.unity"})
  {
   var s=EditorSceneManager.OpenScene(path);b.AppendLine("SCENE "+path);
   foreach(var root in s.GetRootGameObjects()){
    b.AppendLine("ROOT "+root.name+" "+string.Join(",",root.GetComponents<Component>().Where(c=>c).Select(c=>c.GetType().Name)));
    foreach(Transform t in root.transform)b.AppendLine(" CHILD "+t.name+" p="+t.position+" scale="+t.lossyScale+" components="+string.Join(",",t.GetComponents<Component>().Where(c=>c).Select(c=>c.GetType().Name)));
   }
   var w=s.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<RecoilPrototype>(true)).First();
   b.AppendLine("WORLD "+JsonUtility.ToJson(w));
   foreach(var c in s.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<RecoilCameraController>(true)))b.AppendLine("CAM "+JsonUtility.ToJson(c));
   foreach(var c in w.GetComponentsInChildren<BoxCollider2D>(true).Where(c=>!c.isTrigger&&(path.Contains("Kitchen")||path.Contains("Washroom")||c.bounds.max.x>95)))b.AppendLine("COL "+c.name+" "+c.bounds+" parent="+c.transform.parent.name);
   foreach(var c in w.GetComponentsInChildren<RecoilDoor>(true))b.AppendLine("DOOR "+c.name+" "+c.transform.position+" "+JsonUtility.ToJson(c));
   foreach(var c in w.GetComponentsInChildren<WalkableStairway>(true))b.AppendLine("STAIR "+c.name+" "+string.Join(" -> ",c.routePoints.Select(t=>t.position.ToString()))+" "+JsonUtility.ToJson(c));
  }
  File.WriteAllText("output/residential/scene-inspection.txt",b.ToString());
 }
}
