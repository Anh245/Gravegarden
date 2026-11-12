using System;
using UnityEditor;
using UnityEngine;

internal static class FinalUISkin
{
    private const string Folder="Assets/Art/UI/FinalSkin";
    public static Sprite UI(string name)=>Get("ui",name);
    public static Sprite Icon(string name)=>Get("icon",name);
    private static Sprite Get(string type,string name)
    {
        if(!AssetDatabase.IsValidFolder(Folder))AssetDatabase.CreateFolder("Assets/Art/UI","FinalSkin");
        string key=type+"_"+name;
        string path=Folder+"/"+key+".asset";
        var existing=AssetDatabase.LoadAssetAtPath<Sprite>(path);if(existing!=null)return existing;
        string source="Assets/Art/UI/"+(type=="ui"?"ui":"Icon")+"/"+name+".png";
        if(AssetDatabase.LoadAssetAtPath<Texture2D>(source)==null)throw new InvalidOperationException("Thiếu ảnh "+source);
        string copy=Folder+"/"+key+".png";
        if(AssetDatabase.LoadAssetAtPath<Texture2D>(copy)==null && !AssetDatabase.CopyAsset(source,copy))throw new InvalidOperationException("Không sao chép được "+source);
        var importer=(TextureImporter)AssetImporter.GetAtPath(copy);
        importer.textureType=TextureImporterType.Default;importer.npotScale=TextureImporterNPOTScale.None;
        importer.alphaIsTransparency=true;importer.mipmapEnabled=false;importer.filterMode=FilterMode.Point;
        importer.textureCompression=TextureImporterCompression.Uncompressed;importer.maxTextureSize=4096;importer.SaveAndReimport();
        var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(copy);Rect rect=Region(key);
        Vector4 border=Vector4.zero;
        if(name.StartsWith("primary_"))border=new Vector4(70,60,70,60);
        else if(name.StartsWith("icon_"))border=new Vector4(40,40,40,40);
        else if(name.StartsWith("shop_"))border=new Vector4(24,24,24,24);
        else if(name=="modal_panel")border=new Vector4(80,80,80,160);
        else if(name=="building_panel")border=new Vector4(40,40,40,40);
        else if(name=="mara_speech_bubble")border=new Vector4(200,80,80,80);
        else if(name=="heart_track"||name=="heart_fill")border=new Vector4(8,4,8,4);
        border.x=Mathf.Min(border.x,rect.width*.4f);border.z=Mathf.Min(border.z,rect.width*.4f);
        border.y=Mathf.Min(border.y,rect.height*.4f);border.w=Mathf.Min(border.w,rect.height*.4f);
        var sprite=Sprite.Create(texture,rect,new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect,border);
        sprite.name=key;AssetDatabase.CreateAsset(sprite,path);
        if(!AssetDatabase.Contains(sprite))throw new InvalidOperationException("Không lưu được sprite "+path);
        return sprite;
    }
    private static Rect Region(string key)
    {
        switch(key)
        {
case "ui_bone_divider":return new Rect(0,0,690,114);
case "ui_boss_fill":return new Rect(0,0,789,71);
case "ui_building_panel":return new Rect(0,0,450,166);
case "ui_cancel_button":return new Rect(0,0,296,278);
case "ui_collect_button":return new Rect(0,0,362,332);
case "ui_confirm_button":return new Rect(0,0,302,277);
case "ui_defeat_banner":return new Rect(0,0,1134,384);
case "ui_heart_fill":return new Rect(0,0,917,130);
case "ui_heart_track":return new Rect(0,0,923,153);
case "ui_icon_disabled":return new Rect(0,0,268,253);
case "ui_icon_normal":return new Rect(0,0,268,253);
case "ui_icon_pressed":return new Rect(0,0,235,230);
case "ui_icon_selected":return new Rect(0,0,266,253);
case "ui_mara_speech_bubble":return new Rect(0,693,1955,571);
case "ui_modal_panel":return new Rect(0,0,1689,949);
case "ui_move_button":return new Rect(0,0,364,332);
case "ui_music_off":return new Rect(0,0,232,223);
case "ui_music_on":return new Rect(0,0,232,223);
case "ui_pause_button":return new Rect(0,0,296,277);
case "ui_primary_disabled":return new Rect(0,0,505,240);
case "ui_primary_normal":return new Rect(0,0,587,240);
case "ui_primary_pressed":return new Rect(0,0,586,245);
case "ui_primary_selected":return new Rect(0,0,585,240);
case "ui_sell_button":return new Rect(0,0,366,332);
case "ui_shop_affordable":return new Rect(0,0,400,566);
case "ui_shop_locked":return new Rect(0,0,373,564);
case "ui_shop_placed":return new Rect(0,0,371,564);
case "ui_shop_selected":return new Rect(0,0,389,566);
case "ui_shop_tray":return new Rect(0,0,2736,279);
case "ui_shop_unaffordable":return new Rect(0,0,395,566);
case "ui_sound_off":return new Rect(0,0,232,225);
case "ui_sound_on":return new Rect(0,0,232,226);
case "ui_speed_one":return new Rect(0,0,299,277);
case "ui_speed_two":return new Rect(0,0,290,277);
case "ui_upgrade_button":return new Rect(0,0,369,332);
case "ui_victory_banner":return new Rect(0,0,1033,421);
case "ui_wallet_capsule":return new Rect(0,0,741,243);
case "icon_collect":return new Rect(3,1,44,49);
case "icon_damage":return new Rect(3,1,43,49);
case "icon_gravecoin":return new Rect(0,0,42,42);
case "icon_integrity":return new Rect(0,2,55,53);
case "icon_move":return new Rect(0,1,51,50);
case "icon_production":return new Rect(6,1,39,51);
case "icon_range":return new Rect(0,1,73,72);
case "icon_sell":return new Rect(0,2,38,35);
case "icon_speed":return new Rect(16,0,56,88);
case "icon_storage":return new Rect(0,5,52,43);
case "icon_upgrade":return new Rect(45,1,137,227);
case "icon_wave":return new Rect(5,0,44,54);
            default:throw new InvalidOperationException("Chưa có vùng sprite cho "+key);
        }
    }
}
