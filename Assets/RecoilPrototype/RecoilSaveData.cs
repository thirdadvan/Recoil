using System;
using System.Collections.Generic;
using UnityEngine;

// Plain data only: never serialize scene objects, controller internals or transient physics.
[Serializable]
public sealed class RecoilSaveData
{
    public const int CurrentVersion=1;
    public int version=CurrentVersion;
    public string savedUtc;
    public RecoilWorldSave world;
    public RecoilMemorySave memory=new RecoilMemorySave();
    public RecoilChildWaitSave child=new RecoilChildWaitSave();
    public List<RecoilSaveSection> sections=new List<RecoilSaveSection>();
}
[Serializable]
public sealed class RecoilWorldSave
{
    public string scene;
    public Vector2 position,entrySpawn;
    public float depth;
    public bool checkpoint,hasClue;
    public int attempts=1;
    public float elapsed;
    public List<string> items=new List<string>(),entryItems=new List<string>();
    public RecoilWeaponSave weapon=new RecoilWeaponSave();
    public int health=3;
}
[Serializable]
public sealed class RecoilWeaponSave
{
    public string id="orange_water_gun";
    public int capacity=3,ammo=3;
    public bool automaticRecoil=true;
    public float refillElapsed,shotCooldown;
}
[Serializable]
public sealed class RecoilChildWaitSave
{
    public string scene;
    public Vector2 position;
    public float depth;
}
[Serializable]
public sealed class RecoilMemorySave
{
    public RecoilWeaponSave weapon;
    public int deaths,visits;
    public bool hasReturn,returning,checkpoint,hasEntrySpawn;
    public string scene;
    public Vector2 position,entrySpawn;
    public string[] entryItems,retryItems;
    public int attempts;
    public float elapsed;
}
[Serializable]
public sealed class RecoilSaveSection
{
    public string id,json;
}
// Optional extensions use a stable, globally unique ID, with their own versioned JSON payload.
public interface IRecoilSaveParticipant
{
    string SaveId {get;}
    string CaptureSaveJson();
    void RestoreSaveJson(string json);
}
