using UnityEngine;

public partial class RecoilPrototype
{
    Material readablePhotoMaterial;
    ReadablePaperModelPreview readablePaperPreview;

    void PrepareReadableModel(ReadableProp prop)
    {
        if(!prop || !prop.pageMesh || !prop.pageImage)return;
        if(readablePaperPreview==null)readablePaperPreview=new ReadablePaperModelPreview();
        readablePaperPreview.Render(prop.pageMesh,prop.pageImage);
    }

    void DrawReadableImage(Rect bounds,Texture image,float displayScale=1f,Mesh paperMesh=null)
    {
        if(Event.current.type!=EventType.Repaint||!image)return;
        if(paperMesh)
        {
            if(readablePaperPreview==null)readablePaperPreview=new ReadablePaperModelPreview();
            image=readablePaperPreview.Render(paperMesh,image);
        }
        var centre=bounds.center;
        float pageScale=Mathf.Clamp(displayScale,.25f,1f);
        bounds.size*=pageScale;
        bounds.center=centre;
        if(!readablePhotoMaterial)
        {
            var shader=Resources.Load<Shader>("ReadablePhoto/ColourCorrect");
            if(shader)readablePhotoMaterial=new Material(shader){hideFlags=HideFlags.HideAndDontSave};
        }
        if(!readablePhotoMaterial){GUI.DrawTexture(bounds,image,ScaleMode.ScaleToFit,true);return;}
        float scale=Mathf.Min(bounds.width/image.width,bounds.height/image.height);
        var rect=new Rect(bounds.center.x-image.width*scale*.5f,bounds.center.y-image.height*scale*.5f,image.width*scale,image.height*scale);
        // Graphics.DrawTexture uses screen pixels, while the reader uses a centred GUI matrix.
        var topLeft=GUI.matrix.MultiplyPoint3x4(new Vector3(rect.xMin,rect.yMin,0));
        var bottomRight=GUI.matrix.MultiplyPoint3x4(new Vector3(rect.xMax,rect.yMax,0));
        var matrix=GUI.matrix;
        try
        {
            GUI.matrix=Matrix4x4.identity;
            Graphics.DrawTexture(Rect.MinMaxRect(topLeft.x,topLeft.y,bottomRight.x,bottomRight.y),image,new Rect(0,0,1,1),0,0,0,0,Color.white,readablePhotoMaterial);
        }
        finally{GUI.matrix=matrix;}
    }

    void DisposeReadablePhoto()
    {
        if(readablePhotoMaterial)Destroy(readablePhotoMaterial);
        if(readablePaperPreview!=null){readablePaperPreview.Dispose();readablePaperPreview=null;}
    }
}
