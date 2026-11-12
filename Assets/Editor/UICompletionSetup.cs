using System;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static partial class UICompletionSetup
{
    private static Scene activeScene;
    [MenuItem("GraveGarden/UI/Complete Existing UI (Current Scene)")]
    private static void Apply()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode){Debug.LogError("Thoát Play trước khi áp dụng UI.");return;}
        activeScene=SceneManager.GetActiveScene();int undo=-1;
        try
        {
            if(activeScene.name!="Garden" && activeScene.name!="MainMenu")throw new InvalidOperationException("Mở Garden hoặc MainMenu.");
            // Chuẩn bị tài nguyên trước khi sửa scene. Không sửa import của ảnh gốc.
            FinalUISkin.UI("primary_normal");FinalUISkin.UI("modal_panel");
            Undo.IncrementCurrentGroup();undo=Undo.GetCurrentGroup();Undo.SetCurrentGroupName("Hoàn thiện giao diện GraveGarden");
            foreach(var audio in All<AudioSettingsUI>())AudioPanel(audio);
            foreach(var guide in All<HelpGuidePanel>())
            {
                var data=new SerializedObject(guide);Primary(Ref<Button>(data,"previousButton"));Primary(Ref<Button>(data,"nextButton"));Primary(Ref<Button>(data,"closeButton"),FinalUISkin.UI("cancel_button"));
                var frame=guide.transform.Find("VietnameseGuideFrame");if(frame!=null)Frame(frame.gameObject,"modal_panel");
            }
            if(activeScene.name=="MainMenu")Menu();else Garden();
            AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(activeScene);Undo.CollapseUndoOperations(undo);
            Debug.Log("Đã hoàn thiện UI cho "+activeScene.name+". Ctrl+S lưu scene. Kiểm tra bảng công trình, nút âm thanh, Mara và boss trong Play. Tài nguyên mới nằm ở Art/UI/FinalSkin.");
        }
        catch(Exception error)
        {
            if(undo>=0)Undo.RevertAllDownToGroup(undo);
            Debug.LogError("Áp dụng UI thất bại, đã hoàn tác thay đổi scene: "+error.Message);
        }
    }
    private static List<T> All<T>() where T:Component
    {
        var list=new List<T>();foreach(var item in UnityEngine.Object.FindObjectsByType<T>(FindObjectsInactive.Include))
            if(item.gameObject.scene==activeScene)list.Add(item);return list;
    }
    private static T Need<T>() where T:Component
    {
        var list=All<T>();if(list.Count!=1)throw new InvalidOperationException("Cần đúng một "+typeof(T).Name+" trong scene.");return list[0];
    }
    private static void Menu()
    {
        var menu=Need<MainMenuController>();var data=new SerializedObject(menu);
        Primary(Ref<Button>(data,"newGameButton"),null,"Chơi mới");Primary(Ref<Button>(data,"continueButton"),null,"Tiếp tục");
        Primary(Ref<Button>(data,"helpButton"),null,"Hướng dẫn");Primary(Ref<Button>(data,"quitButton"),null,"Thoát");
        Primary(Ref<Button>(data,"closeHelpButton"),FinalUISkin.UI("cancel_button"),"Đóng");
        foreach(var image in All<Image>())if(image.gameObject.name=="MenuFrame")Frame(image.gameObject,"modal_panel");
    }
    private static void Garden()
    {
        var waves=Need<WaveDirector>();var manager=Need<BuildManager>();
        Details(Need<TowerUpgradeUI>(),0);Details(Need<ProducerUpgradeUI>(),1);Details(Need<SupportUpgradeUI>(),2);BuildPanel(manager);
        var waveData=new SerializedObject(waves);
        Primary(Ref<Button>(waveData,"startWaveButton"),FinalUISkin.Icon("wave"),"Bắt đầu đợt");
        Inline(Ref<TMP_Text>(waveData,"waveText"),FinalUISkin.Icon("wave"),"FinalUI_WaveIcon",30);
        var start=Ref<Button>(waveData,"startWaveButton");Canvas canvas=start!=null?start.GetComponentInParent<Canvas>():null;
        if(canvas==null)throw new InvalidOperationException("StartWaveButton phải thuộc Canvas.");
        var original=start.GetComponentInChildren<TMP_Text>(true);TMP_FontAsset font=original!=null?original.font:TMP_Settings.defaultFontAsset;
        Boss(waves,canvas,font);
        foreach(var health in All<TownHallHealthView>())Health(health);
        foreach(var wallet in All<WalletView>())Wallet(wallet);
        foreach(var mara in All<GardenTutorialController>())Mara(mara);
        foreach(var speed in All<GameSpeedController>())
        {
            var data=new SerializedObject(speed);Square(Ref<Button>(data,"normalSpeedButton"),FinalUISkin.UI("speed_one"));Square(Ref<Button>(data,"doubleSpeedButton"),FinalUISkin.UI("speed_two"));
        }
        foreach(var pause in All<GardenPauseController>())
        {
            var data=new SerializedObject(pause);Primary(Ref<Button>(data,"pauseButton"),FinalUISkin.UI("pause_button"),"Tạm dừng");
            SkinFields(pause,new[]{"resumeButton","saveAndMenuButton","menuWithoutSaveButton","helpButton"});
            var panel=Ref<GameObject>(data,"pausePanel");if(panel!=null)Frame(panel,"modal_panel");
        }
        foreach(var save in All<GardenSaveController>())
        {
            var data=new SerializedObject(save);Primary(Ref<Button>(data,"saveButton"));Primary(Ref<Button>(data,"loadButton"));Primary(Ref<Button>(data,"offlineCloseButton"),FinalUISkin.UI("confirm_button"));
            var panel=Ref<GameObject>(data,"offlinePanel");if(panel!=null)Frame(panel,"modal_panel");
        }
        foreach(var collect in All<CollectAllController>())
        {var data=new SerializedObject(collect);Primary(Ref<Button>(data,"collectAllButton"),FinalUISkin.UI("collect_button"));}
        foreach(var move in All<BuildingMoveController>())
        {
            var data=new SerializedObject(move);Primary(Ref<Button>(data,"cancelButton"),FinalUISkin.UI("cancel_button"));
            Inline(Ref<TMP_Text>(data,"statusText"),FinalUISkin.Icon("move"),"FinalUI_MoveIcon",36);
            var panel=Ref<GameObject>(data,"movePanel");if(panel!=null)Frame(panel,"building_panel");
        }
        foreach(var result in All<GameResultController>())
        {
            var data=new SerializedObject(result);Result(Ref<GameObject>(data,"victoryPanel"),true,font);Result(Ref<GameObject>(data,"defeatPanel"),false,font);
            SkinFields(result,new[]{"victoryRetryButton","defeatRetryButton","victoryMenuButton","defeatMenuButton"});
        }
        foreach(var shop in All<BuildingShopController>())
        {
            Record(shop);var data=new SerializedObject(shop);var skins=data.FindProperty("skins");
            foreach(string state in new[]{"affordable","unaffordable","locked","selected","placed","tray"})
                skins.FindPropertyRelative(state).objectReferenceValue=FinalUISkin.UI("shop_"+state);
            data.ApplyModifiedProperties();Primary(Ref<Button>(data,"closeButton"));
        }

    }
}
