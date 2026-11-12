using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class GraveGardenChapterOneBuilder
{
    private const string DataFolder="Assets/Data/Enemies/GodotChapter1";
    private const string PrefabFolder="Assets/Prefabs/Enemies/GodotChapter1";
    private const string WavePath="Assets/Data/Waves/GodotChapter1.asset";
    [MenuItem("GraveGarden/Content/Create Godot Chapter 1")]
    public static void Create()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode){Debug.LogError("Thoát Play Mode trước khi tạo dữ liệu.");return;}
        if(AssetDatabase.LoadMainAssetAtPath(WavePath)!=null)
        {Selection.activeObject=AssetDatabase.LoadMainAssetAtPath(WavePath);Debug.LogWarning("GodotChapter1 đã có; không ghi đè dữ liệu đã chỉnh.");return;}
        foreach(var entry in GodotChapterOne.Enemies())
        {
            string path="Assets/Prefabs/Enemies/"+entry.PrefabName+".prefab";
            var original=AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if(original==null || original.GetComponent<EnemyHealth>()==null || original.GetComponent<EnemyHealth>().Definition==null
                || original.GetComponent<EnemyRouteFollower>()==null || original.GetComponent<EnemySiegeAttack>()==null)
            {Debug.LogError("Thiếu prefab gốc/Definition: "+path);return;}
        }
        Folder("Assets/Data/Enemies","GodotChapter1");Folder("Assets/Prefabs/Enemies","GodotChapter1");
        Folder("Assets/Data","Waves");
        var catalog=new Dictionary<string,GameObject>();
        foreach(var entry in GodotChapterOne.Enemies())
        {
            string originalPath="Assets/Prefabs/Enemies/"+entry.PrefabName+".prefab";
            string dataPath=DataFolder+"/"+entry.PrefabName+"_Chapter1.asset";
            var data=AssetDatabase.LoadAssetAtPath<EnemyDefinition>(dataPath);
            if(data==null)
            {
                var original=AssetDatabase.LoadAssetAtPath<GameObject>(originalPath).GetComponent<EnemyHealth>().Definition;
                data=UnityEngine.Object.Instantiate(original);data.name=entry.PrefabName+"_Chapter1";
                var serialized=new SerializedObject(data);
                serialized.FindProperty("maxHealth").floatValue=entry.Health;
                serialized.FindProperty("bounty").intValue=entry.Bounty;
                serialized.FindProperty("moveSpeed").floatValue=entry.Speed;
                serialized.FindProperty("siegeDamage").intValue=entry.HeartDamage;
                serialized.FindProperty("firstHitDelay").floatValue=0.65f;
                serialized.FindProperty("attackInterval").floatValue=1.6f;
                serialized.FindProperty("slowEffectMultiplier").floatValue=entry.SlowMultiplier;
                serialized.ApplyModifiedPropertiesWithoutUndo();AssetDatabase.CreateAsset(data,dataPath);
            }
            string prefabPath=PrefabFolder+"/"+entry.PrefabName+"_Chapter1.prefab";
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if(prefab==null)
            {
                GameObject root=PrefabUtility.LoadPrefabContents(originalPath);
                try
                {
                    root.name=entry.PrefabName+"_Chapter1";
                    var health=new SerializedObject(root.GetComponent<EnemyHealth>());
                    health.FindProperty("definition").objectReferenceValue=data;health.ApplyModifiedPropertiesWithoutUndo();
                    var follower=new SerializedObject(root.GetComponent<EnemyRouteFollower>());
                    follower.FindProperty("startingDistance").floatValue=0f;follower.ApplyModifiedPropertiesWithoutUndo();
                    prefab=PrefabUtility.SaveAsPrefabAsset(root,prefabPath);
                }
                finally{PrefabUtility.UnloadPrefabContents(root);}
            }
            if(prefab==null || prefab.GetComponent<EnemyHealth>().Definition!=data)
                throw new InvalidOperationException("Prefab chapter không khớp dữ liệu: "+prefabPath);
            catalog.Add(entry.Id,prefab);
        }
        var waves=ScriptableObject.CreateInstance<MixedWaveSetDefinition>();waves.name="GodotChapter1";
        var serializedWaves=new SerializedObject(waves);var array=serializedWaves.FindProperty("waves");array.arraySize=5;
        for(int i=0;i<5;i++)
        {
            var source=GodotChapterOne.Wave(i+1);var wave=array.GetArrayElementAtIndex(i);
            wave.FindPropertyRelative("displayName").stringValue="Đợt "+(i+1)+(i==4?" - Người Khổng Lồ Xương":"");
            wave.FindPropertyRelative("completionReward").intValue=source.Reward;
            var groups=wave.FindPropertyRelative("groups");string[] order=source.SpawnOrder();groups.arraySize=order.Length;
            for(int j=0;j<order.Length;j++)
            {
                var group=groups.GetArrayElementAtIndex(j);
                group.FindPropertyRelative("enemyPrefab").objectReferenceValue=catalog[order[j]];
                group.FindPropertyRelative("count").intValue=1;
                group.FindPropertyRelative("spawnInterval").floatValue=source.Interval;
                group.FindPropertyRelative("startDelay").floatValue=j==0?0f:source.Interval;
            }
        }
        serializedWaves.ApplyModifiedPropertiesWithoutUndo();
        if(!waves.TryCreatePlans(out _,out string error)){UnityEngine.Object.DestroyImmediate(waves);throw new InvalidOperationException(error);}
        AssetDatabase.CreateAsset(waves,WavePath);AssetDatabase.SaveAssets();Selection.activeObject=waves;EditorGUIUtility.PingObject(waves);
        Debug.Log("Đã tạo GodotChapter1: 5 đợt, 218 quái, 2 boss. Gán asset mới vào WaveDirector và thiết lập hai tuyến trong Garden. Prefab/data thử nghiệm cũ giữ nguyên.");
    }
    private static void Folder(string parent,string name)
    {if(!AssetDatabase.IsValidFolder(parent+"/"+name))AssetDatabase.CreateFolder(parent,name);}
}
