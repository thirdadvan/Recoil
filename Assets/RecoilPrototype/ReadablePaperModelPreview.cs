using UnityEngine;

// Cache one render per document; the isolated temporary camera is never saved or updated.
public sealed class ReadablePaperModelPreview : System.IDisposable
{
    Mesh sourceMesh;
    Texture sourceImage;
    RenderTexture preview;
    Material front,back;
    public Texture Render(Mesh mesh,Texture image)
    {
        if(preview && sourceMesh==mesh && sourceImage==image && preview.IsCreated())return preview;
        Dispose();
        var shader=Resources.Load<Shader>("ReadablePhoto/PaperModel");
        if(!shader || !mesh || !image)return image;
        sourceMesh=mesh;sourceImage=image;
        front=new Material(shader){hideFlags=HideFlags.HideAndDontSave,mainTexture=image};
        back=new Material(shader){hideFlags=HideFlags.HideAndDontSave};back.SetColor("_Tint",new Color(.75f,.67f,.48f,1));
        preview=new RenderTexture(1536,1024,24,RenderTextureFormat.ARGB32){name="Curled document reader preview",hideFlags=HideFlags.HideAndDontSave,antiAliasing=2};preview.Create();
        // Camera.Render initializes all platform-specific projection/depth state,
        // including Metal. No scene geometry is admitted by this isolated pass.
        var previewObject=new GameObject("Temporary readable paper camera"){hideFlags=HideFlags.HideAndDontSave};
        var camera=previewObject.AddComponent<Camera>();
        camera.enabled=false;camera.cullingMask=0;camera.clearFlags=CameraClearFlags.SolidColor;
        camera.backgroundColor=Color.clear;camera.orthographic=true;camera.orthographicSize=.54f;
        camera.aspect=1.5f;camera.nearClipPlane=.1f;camera.farClipPlane=10;
        camera.transform.position=new Vector3(0,0,-2);camera.targetTexture=preview;
        var commands=new UnityEngine.Rendering.CommandBuffer{name="Render curled readable paper"};
        try
        {
            commands.DrawMesh(mesh,Matrix4x4.identity,front,0,0);
            if(mesh.subMeshCount>1)commands.DrawMesh(mesh,Matrix4x4.identity,back,1,0);
            camera.AddCommandBuffer(UnityEngine.Rendering.CameraEvent.AfterEverything,commands);
            camera.Render();
        }
        finally
        {
            camera.RemoveAllCommandBuffers();camera.targetTexture=null;
            commands.Release();Release(previewObject);
        }
        return preview;
    }
    static void Release(Object value){if(!value)return;if(Application.isPlaying)Object.Destroy(value);else Object.DestroyImmediate(value);}
    public void Dispose()
    {
        if(preview){preview.Release();Release(preview);}Release(front);Release(back);
        preview=null;front=null;back=null;sourceMesh=null;sourceImage=null;
    }
}
