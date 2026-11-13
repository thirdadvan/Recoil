using UnityEngine;
[ExecuteAlways,DisallowMultipleComponent]
public sealed class ScreenLightScattering : MonoBehaviour
{
    public Light screenLight;
    public Vector2 apertureHalfSize=new Vector2(.205f,.137f);
    public float length=2.4f,spread=.20f,density=.085f;
    MaterialPropertyBlock properties;
    void LateUpdate(){Sync();}
    public void Sync(){var r=GetComponent<Renderer>();if(!r)return;if(properties==null)properties=new MaterialPropertyBlock();properties.SetVector("_Shape",new Vector4(apertureHalfSize.x,apertureHalfSize.y,length,spread));properties.SetFloat("_Density",density*(screenLight&&screenLight.enabled?screenLight.intensity:0));properties.SetColor("_Color",screenLight?screenLight.color:new Color(.32f,.85f,.48f));r.SetPropertyBlock(properties);var camera=Camera.main;if(camera)camera.depthTextureMode|=DepthTextureMode.Depth;}
}
