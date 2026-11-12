using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class GodotEndlessSetup
{
    [MenuItem("GraveGarden/Content/Setup Godot Endless Waves")]
    private static void Setup()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode){Debug.LogError("Thoát Play trước.");return;}
        Scene scene=SceneManager.GetActiveScene();
        if(scene.name!="Garden"){Debug.LogError("Mở và chọn scene Garden.");return;}
        WaveDirector director=null;
        foreach(var item in Object.FindObjectsByType<WaveDirector>(FindObjectsInactive.Include))
            if(item.gameObject.scene==scene)
            {if(director!=null){Debug.LogError("Có nhiều WaveDirector trong Garden.");return;}director=item;}
        if(director==null){Debug.LogError("Thiếu WaveDirector.");return;}
        var waveData=new SerializedObject(director);
        if(waveData.FindProperty("expansion").objectReferenceValue==null)
        {Debug.LogError("Hoàn thành gán Expansion Controller trước.");return;}
        const string path="Assets/Data/Waves/GodotEndless.asset";
        var asset=AssetDatabase.LoadAssetAtPath<GodotEndlessWaves>(path);
        if(asset==null)
        {
            string[] fields={"rotling","boneHound","bogBrute","wraith","ossuaryGiant"};
            string[] names={"Rotling","BoneHound","BogBrute","Wraith","OssuaryGiant"};
            var prefabs=new GameObject[5];
            for(int i=0;i<5;i++)
            {
                string file="Assets/Prefabs/Enemies/GodotChapter1/"+names[i]+"_Chapter1.prefab";
                prefabs[i]=AssetDatabase.LoadAssetAtPath<GameObject>(file);
                if(prefabs[i]==null){Debug.LogError("Thiếu prefab: "+file);return;}
            }
            asset=ScriptableObject.CreateInstance<GodotEndlessWaves>();
            var data=new SerializedObject(asset);
            for(int i=0;i<5;i++)data.FindProperty(fields[i]).objectReferenceValue=prefabs[i];
            data.ApplyModifiedPropertiesWithoutUndo();
            if(!asset.TryValidate(out string error)){Object.DestroyImmediate(asset);Debug.LogError(error);return;}
            if(!AssetDatabase.IsValidFolder("Assets/Data/Waves")){Object.DestroyImmediate(asset);Debug.LogError("Thiếu Assets/Data/Waves.");return;}
            AssetDatabase.CreateAsset(asset,path);
            AssetDatabase.SaveAssets();
        }
        if(!asset.TryValidate(out string validation)){Debug.LogError(validation);return;}
        Undo.RecordObject(director,"Gán dữ liệu wave vô tận");
        waveData.FindProperty("endlessWaves").objectReferenceValue=asset;waveData.ApplyModifiedProperties();
        EditorSceneManager.MarkSceneDirty(scene);Selection.activeObject=asset;
        Debug.Log("Đã gán GodotEndless. Dữ liệu năm đợt đầu giữ nguyên. Ctrl+S lưu Garden.",director);
    }
}
