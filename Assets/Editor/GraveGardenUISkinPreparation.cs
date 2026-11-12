using System;
using UnityEditor;
using UnityEngine;

public static class GraveGardenUISkinPreparation
{
    private const string Source="Assets/Art/UI/ui";
    private const string Target="Assets/Art/UI/UnitySkin";
    private static readonly string[] Names={"building_panel","modal_panel","wallet_capsule",
        "primary_normal","primary_selected","primary_pressed","primary_disabled",
        "heart_track","heart_fill","upgrade_button","move_button","sell_button",
        "pause_button","confirm_button","cancel_button","victory_banner","defeat_banner","bone_divider"};

    [MenuItem("GraveGarden/UI/Prepare UI Skin Copies")]
    public static void Prepare()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)
        {Debug.LogWarning("Thoát Play Mode trước khi chuẩn bị UI Skin.");return;}
        foreach(string name in Names)
            if(AssetDatabase.LoadAssetAtPath<Texture2D>(Source+"/"+name+".png")==null)
            {Debug.LogError("Thiếu ảnh gốc: "+Source+"/"+name+".png");return;}
        if(!AssetDatabase.IsValidFolder(Target)) AssetDatabase.CreateFolder("Assets/Art/UI","UnitySkin");
        int count=0;
        foreach(string name in Names)
        {
            string path=Target+"/"+name+".png";
            if(AssetDatabase.LoadMainAssetAtPath(path)==null && !AssetDatabase.CopyAsset(Source+"/"+name+".png",path))
                throw new InvalidOperationException("Không copy được "+path);
            var importer=AssetImporter.GetAtPath(path) as TextureImporter;
            if(importer==null) throw new InvalidOperationException("Không đọc được importer: "+path);
            importer.textureType=TextureImporterType.Sprite;
            importer.spriteImportMode=SpriteImportMode.Single;
            importer.spritePixelsPerUnit=100;
            importer.mipmapEnabled=false;importer.alphaIsTransparency=true;
            importer.wrapMode=TextureWrapMode.Clamp;importer.filterMode=FilterMode.Point;
            importer.textureCompression=TextureImporterCompression.Uncompressed;
            importer.npotScale=TextureImporterNPOTScale.None;
            var settings=new TextureImporterSettings();importer.ReadTextureSettings(settings);
            settings.spriteMeshType=SpriteMeshType.FullRect;importer.SetTextureSettings(settings);
            importer.spriteBorder=name=="building_panel"?new Vector4(48,32,48,32)
                :name.StartsWith("primary_",StringComparison.Ordinal)?new Vector4(48,48,48,48):Vector4.zero;
            importer.SaveAndReimport();count++;
        }
        Selection.activeObject=AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(Target);
        EditorGUIUtility.PingObject(Selection.activeObject);
        Debug.Log($"Đã chuẩn bị {count} sprite UI trong {Target}. Ảnh gốc và scene giữ nguyên. Gán các sprite mới vào Image theo hướng dẫn.");
    }
}
