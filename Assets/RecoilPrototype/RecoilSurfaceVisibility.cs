using UnityEngine;

// Collision remains solid; this flag controls only monster sight and player sight shadows.
[DisallowMultipleComponent,AddComponentMenu("Recoil/Surface Visibility")]
public sealed class RecoilSurfaceVisibility : MonoBehaviour
{
    [Tooltip("关闭后允许视线通过，但不改变人物和水弹碰撞。适用于硬屋顶和天花板。")]
    public bool blocksSight=false;
}
