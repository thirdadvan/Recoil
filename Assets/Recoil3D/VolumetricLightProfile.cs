using UnityEngine;

// Optional per-fixture controls. Real surface illumination and detection keep using the Light.
[DisallowMultipleComponent, AddComponentMenu("Recoil/Effects/Volumetric Light Profile")]
public sealed class VolumetricLightProfile : MonoBehaviour
{
    [Min(0), Tooltip("只影响空气中的光柱，不改变桥面亮度或发现范围。")]
    public float scatteringMultiplier = 1;
    [Tooltip("按世界高度减弱上半段光柱，避免跟随镜头时光雾铺满上方画面。")]
    public bool fadeAboveHeight;
    public float maximumVisibleHeight = 5;
    [Min(.05f)] public float heightFeather = 2;
    [Tooltip("最多两个实体遮挡物；遮住灯到采样点之间的光，形成真实空间中的雾影。")]
    public BoxCollider[] shelters = new BoxCollider[0];
}
