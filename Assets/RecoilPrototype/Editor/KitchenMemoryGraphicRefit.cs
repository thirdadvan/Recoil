using System;
using System.Linq;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;

// Authored rectangles and silhouettes, using the same square sprite as the prototype.
public static class KitchenMemoryGraphicRefit
{
 static Sprite square;static Material mat;
 static Transform G(string n,Transform parent,float x=0,float y=0){var t=new GameObject(n).transform;t.SetParent(parent,false);t.position=new Vector2(x,y);return t;}
 static SpriteRenderer B(string n,Transform p,float x,float y,float w,float h,int color,int order=0){var t=G(n,p,x,y);t.localScale=new Vector3(w,h,1);var r=t.gameObject.AddComponent<SpriteRenderer>();r.sprite=square;r.sharedMaterial=mat;r.color=RecoilPrototype.Hex(color);r.sortingOrder=order;return r;}
 static Transform Person(string n,Transform parent,float x,bool mother,out SpriteRenderer body){var root=G(n,parent,x,0);float h=mother?1.65f:1.05f;
  body=B("Body",root,x,h*.47f,mother?.48f:.34f,h*.61f,mother?0x666553:0x647b7e,8);
  B("Head",root,x,h*.86f,mother?.3f:.26f,h*.2f,0xb4a58e,9);B("Hair - back",root,x,h*.94f,mother?.34f:.26f,h*.13f,0x34342e,10);
  for(int i=-1;i<=1;i+=2){B("Leg",root,x+i*.1f,h*.13f,.12f,h*.26f,0x393b36,8);B("Shoe",root,x+i*.1f,.05f,.17f,.1f,0x242a27,9);B("Arm",root,x+i*(mother?.28f:.21f),h*.5f,.09f,h*.43f,mother?0x666553:0x647b7e,9);}
  if(mother){B("Apron tie - back",root,x,h*.4f,.52f,.07f,0xa7a389,10);B("Apron tie tail",root,x+.04f,h*.31f,.07f,.28f,0xa7a389,10);B("Hair bun",root,x+.07f,h*.96f,.18f,.14f,0x34342e,11);}
  return root;
 }
 [MenuItem("Recoil/Story/Refit Kitchen Memory as 2D Graphics")]
 public static void Apply(){
  if(EditorApplication.isPlaying)throw new Exception("Exit Play first.");var active=SceneManager.GetActiveScene();var scene=SceneManager.GetSceneByPath(RecoilPrototype.KitchenMemoryScene);bool added=!scene.isLoaded;if(added)scene=EditorSceneManager.OpenScene(RecoilPrototype.KitchenMemoryScene,OpenSceneMode.Additive);
  string backup="Backups/KitchenGraphicRefit/"+DateTime.Now.ToString("yyyyMMdd_HHmmss");Directory.CreateDirectory(backup);EditorSceneManager.SaveScene(scene,backup+"/Kitchen.unity",true);
  var c=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<KitchenMemoryController>()).First();var root=c.transform;
  foreach(var t in root.Cast<Transform>().ToArray())UnityEngine.Object.DestroyImmediate(t.gameObject);
  square=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/RecoilPrototype/Art/PrototypeSquare.png");mat=AssetDatabase.LoadAssetAtPath<Material>("Assets/RecoilPrototype/Art/PrototypeSprite.mat");
  var env=G("Apartment - assembled 2D shapes",root);
  B("Continuous floor",env,12,-.15f,24,.3f,0x353e3b,-1);B("Ceiling",env,12,4.45f,24,.2f,0x29312f,0);
  var bedroom=G("Bedroom - extended 10 metres",env);
  B("Bedroom wall",bedroom,5,2.2f,10,4.4f,0x35433f,-10);B("Bedroom lower wall",bedroom,5,.65f,10,1.3f,0x293b35,-9);
  var window=G("Large window - distant factory",bedroom,4.7f,2.6f);
  B("Window recess",window,4.7f,2.6f,6.3f,3.15f,0x161f22,-8);B("Night sky",window,4.7f,2.6f,5.9f,2.85f,0x263943,-7);
  var factory=G("Factory skyline - window child",window);
  B("Distant four-storey factory",factory,4.8f,2.05f,3.2f,1.45f,0x34434a,-6);B("Distant warehouse",factory,6.45f,1.6f,1.15f,.55f,0x334249,-6);
  for(int row=0;row<4;row++)for(int col=0;col<5;col++)B("Dim factory window",factory,3.6f+col*.58f,1.55f+row*.3f,.22f,.13f,(row+col)%3==0?0x738174:0x233840,-5);
  B("Factory chimney",factory,6.12f,2.8f,.2f,1.65f,0x30434a,-6);B("Chimney cap",factory,6.12f,3.63f,.27f,.08f,0x45555b,-5);
  B("Distant warning tower mast",factory,2.65f,2.3f,.07f,1.9f,0x3a5058,-5);B("Tower alarm matrix",factory,2.65f,3.12f,.45f,.22f,0x435b62,-5);B("Tower red light",factory,2.65f,3.3f,.065f,.065f,0xa16458,-4);
  B("Window sill",window,4.7f,1.08f,6.4f,.13f,0x7e8270,-3);foreach(float x in new[]{1.72f,4.7f,7.68f})B("Window upright",window,x,2.6f,.09f,2.98f,0x677a78,-3);B("Window transom",window,4.7f,2.7f,6,.07f,0x677a78,-3);
  var bed=G("Bed - foreground",bedroom);B("Bed frame",bed,3,.36f,3.4f,.23f,0x665c49,1);B("Mattress",bed,3,.59f,3.3f,.25f,0x888477,2);B("Blanket",bed,3.35f,.75f,2.6f,.13f,0x68776b,3);B("Pillow",bed,1.76f,.8f,.55f,.18f,0xa29f86,3);for(int i=-1;i<=1;i+=2)B("Bed leg",bed,3+i*1.4f,.17f,.13f,.34f,0x4c4d40,1);
  B("Wardrobe",bedroom,8.75f,1.2f,1.2f,2.4f,0x514d3e,0);B("Wardrobe seam",bedroom,8.75f,1.2f,.025f,2.3f,0x2d352f,1);B("Wardrobe handle",bedroom,8.58f,1.1f,.045f,.23f,0x9b9581,2);
  B("Bedside cabinet",bedroom,.75f,.45f,.75f,.9f,0x5b5745,1);B("Bedside lamp stem",bedroom,.75f,1.06f,.07f,.35f,0x898170,2);B("Bedside shade",bedroom,.75f,1.28f,.5f,.22f,0xb6a477,3);
  var hall=G("Hall - central exit",env);B("Hall wall",hall,12.6f,2.2f,5.2f,4.4f,0x54584a,-10);B("Hall skirting",hall,12.6f,.07f,5.2f,.13f,0x323e36,0);
  var exitMarker=G("Middle exit - checkpoint return",hall,12.1f,0);B("Exit door recess",exitMarker,12.1f,1.3f,1.6f,2.7f,0x202a27,0);B("Exit wooden leaf",exitMarker,12.1f,1.25f,1.35f,2.5f,0x66624d,1);B("Exit inset panel",exitMarker,12.1f,1.4f,1.05f,1.5f,0x585943,2);B("Exit handle",exitMarker,12.62f,1.12f,.15f,.055f,0xb2a989,3);
  var kitchen=G("Kitchen - mother cooking",env);B("Kitchen upper wall",kitchen,19.6f,2.2f,8.8f,4.4f,0x646b59,-10);B("Kitchen lower wall",kitchen,19.6f,.65f,8.8f,1.3f,0x425c4c,-9);
  for(int n=0;n<4;n++){float x=17.25f+n*1.45f;B("Lower cabinet",kitchen,x,.45f,1.38f,.9f,0x9c9c81,-1);B("Cabinet seam",kitchen,x,.47f,.026f,.8f,0x5d6b56,0);B("Cabinet handle",kitchen,x+.42f,.75f,.19f,.05f,0x525c50,1);B("Upper cabinet",kitchen,x,2.42f,1.35f,.9f,0x9b9d86,-1);B("Upper cabinet seam",kitchen,x,2.42f,.025f,.88f,0x6a7663,0);}
  B("Countertop",kitchen,19.4f,.98f,6.2f,.12f,0xb3b09b,2);B("Stove",kitchen,19.5f,.53f,1.3f,1.05f,0x8c9787,3);B("Oven window",kitchen,19.5f,.43f,.95f,.5f,0x273e37,4);for(int i=0;i<4;i++)B("Stove control",kitchen,19.12f+i*.25f,.86f,.1f,.085f,0x404b40,4);
  B("Cooking pot",kitchen,19.6f,1.2f,.55f,.33f,0x536d67,5);B("Pot lid",kitchen,19.6f,1.38f,.65f,.07f,0x84958a,6);B("Sink",kitchen,17.6f,1.01f,1,.06f,0x576e69,4);B("Tap upright",kitchen,17.4f,1.2f,.06f,.4f,0x8b9a8c,4);B("Tap spout",kitchen,17.55f,1.38f,.35f,.05f,0x8b9a8c,4);
  var pendant=G("Pendant - kitchen",kitchen);B("Pendant cable",pendant,19.5f,3.85f,.035f,.9f,0x414b40,1);B("Pendant shade",pendant,19.5f,3.4f,.8f,.18f,0xa9b096,2);B("Pendant bulb",pendant,19.5f,3.3f,.45f,.09f,0xd4c594,3);
  var bedroomDoor=G("Bedroom door - closes after mother enters",env,10.15f,0);B("Bedroom door frame",bedroomDoor,10.15f,3.65f,.17f,1.5f,0x25372e,4);
  var opened=G("Open bedroom leaf",bedroomDoor);B("Open door edge",opened,10.15f,1.45f,.11f,2.9f,0x756e56,5);c.openBedroomDoor=opened.gameObject;
  var closed=G("Closed bedroom leaf",bedroomDoor);B("Closed door - side view",closed,10.15f,1.45f,.24f,2.9f,0x817b62,21);B("Bedroom latch",closed,10.3f,1.2f,.12f,.06f,0xb4ac8e,22);c.closedBedroomDoor=closed.gameObject;closed.gameObject.SetActive(false);
  c.bedroomOcclusion=B("Bedroom - sight lost when door closes",bedroom,5,2.2f,10,4.4f,0x111b1a,20).gameObject;c.bedroomOcclusion.SetActive(false);
  c.bedroomRestPoint=G("Mother destination - inside bedroom",bedroom,8.6f,0);
  SpriteRenderer body;c.mother=Person("Mother - geometric back view",root,19.4f,true,out body);
  c.motherCookingPoint=G("Mother cooking position",root,19.4f,0);
  KitchenMemorySharedSystems.Attach(scene,c,exitMarker,bedroomDoor);
  EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);SceneManager.SetActiveScene(active);if(added)EditorSceneManager.CloseScene(scene,true);Debug.Log("KITCHEN_GRAPHIC_REFIT_COMPLETE");
 }
}
