using System;
using System.IO;
using UnityEngine;

// A completed temporary write replaces the primary. Keep the preceding valid save as a backup.
public static class RecoilSaveStore
{
    public static bool Valid(RecoilSaveData data)
    {
        if(data==null || data.version!=RecoilSaveData.CurrentVersion || data.world==null)return false;
        var w=data.world;
        if(string.IsNullOrEmpty(w.scene)||!w.scene.StartsWith("Assets/Scenes/")||!w.scene.EndsWith(".unity"))return false;
        if(!Finite(w.depth)||!Finite(w.position.x)||!Finite(w.position.y)||!Finite(w.entrySpawn.x)||!Finite(w.entrySpawn.y)||!Finite(w.elapsed)||w.elapsed<0)return false;
        if(w.items==null||w.entryItems==null||w.weapon==null||w.weapon.capacity<1||w.weapon.capacity>100000||w.weapon.ammo<0||w.weapon.ammo>w.weapon.capacity||w.health<1)return false;
        if(!Finite(w.weapon.refillElapsed)||!Finite(w.weapon.shotCooldown))return false;
        foreach(var item in w.items)if(string.IsNullOrEmpty(item))return false;
        foreach(var item in w.entryItems)if(string.IsNullOrEmpty(item))return false;
        if(data.child!=null && (!Finite(data.child.position.x)||!Finite(data.child.position.y)||!Finite(data.child.depth)))return false;
        if(data.memory!=null && data.memory.hasReturn && (string.IsNullOrEmpty(data.memory.scene)||!Finite(data.memory.position.x)||!Finite(data.memory.position.y)))return false;
        if(data.sections==null)data.sections=new System.Collections.Generic.List<RecoilSaveSection>();
        if(data.child==null)data.child=new RecoilChildWaitSave();
        if(data.memory==null)data.memory=new RecoilMemorySave();
        return true;
    }
    static bool Finite(float f){return !float.IsNaN(f)&&!float.IsInfinity(f);}
    public static bool TryRead(string path,out RecoilSaveData data,out string error)
    {
        data=null;error=null;
        foreach(var candidate in new[]{path,path+".bak"})
        {
            if(!File.Exists(candidate))continue;
            try
            {
                var parsed=JsonUtility.FromJson<RecoilSaveData>(File.ReadAllText(candidate));
                if(!Valid(parsed))throw new InvalidDataException("Unsupported or invalid save data.");
                data=parsed;return true;
            }
            catch(Exception e){error=e.Message;}
        }
        return false;
    }
    public static void Write(string path,RecoilSaveData data)
    {
        if(!Valid(data))throw new InvalidDataException("Refusing to write invalid save data.");
        if(File.Exists(path))
        {
            RecoilSaveData existing=null;
            try{existing=JsonUtility.FromJson<RecoilSaveData>(File.ReadAllText(path));}catch{}
            if(existing!=null && existing.version>RecoilSaveData.CurrentVersion)
                throw new InvalidDataException("This save belongs to a newer game version; it has not been overwritten.");
        }
        data.savedUtc=DateTime.UtcNow.ToString("o");
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        string temp=path+".tmp";
        using(var stream=new FileStream(temp,FileMode.Create,FileAccess.Write,FileShare.None))
        using(var writer=new StreamWriter(stream))
        {writer.Write(JsonUtility.ToJson(data,true));writer.Flush();stream.Flush(true);}
        if(File.Exists(path))
        {
            // Never replace a healthy backup with a corrupted primary.
            bool primaryValid=false;
            try{primaryValid=Valid(JsonUtility.FromJson<RecoilSaveData>(File.ReadAllText(path)));}catch{}
            if(primaryValid)File.Copy(path,path+".bak",true);
            File.Replace(temp,path,null);
        }
        else File.Move(temp,path);
    }
}
