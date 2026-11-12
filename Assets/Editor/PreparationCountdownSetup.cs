using System;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class PreparationCountdownSetup
{
    [MenuItem("GraveGarden/UI/Setup Preparation Countdown")]
    private static void Setup()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode){Debug.LogError("Thoát Play trước.");return;}
        var scene=SceneManager.GetActiveScene();int undo=-1;
        try
        {
            if(scene.name!="Garden")throw new InvalidOperationException("Mở và chọn scene Garden.");
            var waves=Single<WaveDirector>(scene);var save=Single<GardenSaveController>(scene);
            var data=new SerializedObject(waves);
            var start=data.FindProperty("startWaveButton").objectReferenceValue as Button;
            if(start==null)throw new InvalidOperationException("WaveDirector chưa có Start Wave Button.");
            TMP_Text label=data.FindProperty("preparationText").objectReferenceValue as TMP_Text;
            Undo.IncrementCurrentGroup();undo=Undo.GetCurrentGroup();Undo.SetCurrentGroupName("Thêm đồng hồ chuẩn bị");
            if(label==null)
            {
                var obj=new GameObject("PreparationCountdownText",typeof(RectTransform),typeof(TextMeshProUGUI));
                Undo.RegisterCreatedObjectUndo(obj,"Tạo chữ đếm ngược");obj.transform.SetParent(start.transform,false);
                var text=obj.GetComponent<TextMeshProUGUI>();var original=start.GetComponentInChildren<TMP_Text>(true);
                text.font=original!=null?original.font:TMP_Settings.defaultFontAsset;
                text.fontSize=24;text.enableAutoSizing=true;text.fontSizeMin=18;text.fontSizeMax=24;
                text.alignment=TextAlignmentOptions.Center;text.color=new Color(1,.92f,.67f,1);text.raycastTarget=false;
                text.textWrappingMode=TextWrappingModes.Normal;
                var rect=text.rectTransform;rect.anchorMin=rect.anchorMax=new Vector2(.5f,1);rect.pivot=new Vector2(.5f,0);
                rect.anchoredPosition=new Vector2(0,12);rect.sizeDelta=new Vector2(720,60);
                text.text="Chuẩn bị: 20s • Bắt đầu ngay: +80 xu";label=text;
            }
            Undo.RecordObject(waves,"Gán UI và bộ chặn lưu/tải");data.Update();
            data.FindProperty("preparationText").objectReferenceValue=label;
            data.FindProperty("preparationSaveController").objectReferenceValue=save;
            data.ApplyModifiedProperties();EditorSceneManager.MarkSceneDirty(scene);Undo.CollapseUndoOperations(undo);
            Selection.activeGameObject=label.gameObject;Debug.Log("Đã gán đồng hồ chuẩn bị. Ctrl+S để lưu Garden.",waves);
        }
        catch(Exception error){if(undo>=0)Undo.RevertAllDownToGroup(undo);Debug.LogError("Tạo đồng hồ thất bại: "+error.Message);}
    }
    private static T Single<T>(Scene scene) where T:Component
    {
        T found=null;foreach(var item in UnityEngine.Object.FindObjectsByType<T>(FindObjectsInactive.Include))
            if(item.gameObject.scene==scene){if(found!=null)throw new InvalidOperationException("Có nhiều "+typeof(T).Name);found=item;}
        if(found==null)throw new InvalidOperationException("Thiếu "+typeof(T).Name);return found;
    }
}
