using System;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class MaraTutorialSetup
{
    [MenuItem("GraveGarden/UI/Setup Mara Interactive Tutorial")]
    private static void Setup()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode){Debug.LogError("Thoát Play trước.");return;}
        Scene scene=SceneManager.GetActiveScene();int undo=-1;
        try
        {
            if(scene.name!="Garden")throw new InvalidOperationException("Mở và chọn scene Garden.");
            foreach(var item in UnityEngine.Object.FindObjectsByType<GardenTutorialController>(FindObjectsInactive.Include))
                if(item.gameObject.scene==scene){Selection.activeGameObject=item.gameObject;Debug.LogWarning("Đã có hướng dẫn Mara; không tạo trùng.");return;}
            var manager=Single<BuildManager>(scene);var mode=Single<BuildModeController>(scene);
            var waves=Single<WaveDirector>(scene);var save=Single<GardenSaveController>(scene);
            BuildingOption orchard=null,lily=null;
            foreach(var option in manager.CheckpointOptions)
            {if(option.SaveId=="bone_orchard")orchard=option;if(option.SaveId=="bone_lily")lily=option;}
            if(orchard==null || lily==null || !orchard.IsValid || !lily.IsValid || !orchard.IsProducer || lily.IsProducer || lily.IsSupport
                || orchard.UnlockWave>1 || lily.UnlockWave>1)
                throw new InvalidOperationException("BuildingOptions cần bone_orchard và bone_lily hợp lệ, mở từ đợt1.");
            var modeData=new SerializedObject(mode);
            var buildButton=modeData.FindProperty("toggleBuildButton").objectReferenceValue as Button;
            var label=modeData.FindProperty("buttonLabel").objectReferenceValue as TMP_Text;
            Canvas canvas=buildButton!=null?buildButton.GetComponentInParent<Canvas>():null;
            if(canvas==null)throw new InvalidOperationException("BuildModeController chưa có nút xây trong Canvas.");
            TMP_FontAsset font=label!=null?label.font:TMP_Settings.defaultFontAsset;
            Undo.IncrementCurrentGroup();undo=Undo.GetCurrentGroup();Undo.SetCurrentGroupName("Thêm hướng dẫn Mara tương tác");
            var obj=new GameObject("GardenTutorialController");SceneManager.MoveGameObjectToScene(obj,scene);
            Undo.RegisterCreatedObjectUndo(obj,"Tạo controller Mara");obj.transform.SetParent(waves.transform.parent,false);
            var controller=Undo.AddComponent<GardenTutorialController>(obj);
            var image=Image(canvas.transform,"MaraTutorialPanel");
            image.sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/UnitySkin/building_panel.png");
            image.type=UnityEngine.UI.Image.Type.Sliced;image.color=image.sprite!=null?Color.white:new Color(.08f,.04f,.12f,.98f);
            var rt=image.rectTransform;rt.anchorMin=rt.anchorMax=rt.pivot=new Vector2(0,.5f);rt.anchoredPosition=new Vector2(24,0);rt.sizeDelta=new Vector2(600,310);
            var title=Text(image.transform,"MaraTitle",font,27);title.alignment=TextAlignmentOptions.TopLeft;
            title.rectTransform.anchorMin=new Vector2(0,1);title.rectTransform.anchorMax=new Vector2(1,1);title.rectTransform.pivot=new Vector2(.5f,1);
            title.rectTransform.offsetMin=new Vector2(24,-65);title.rectTransform.offsetMax=new Vector2(-24,-20);
            var body=Text(image.transform,"MaraBody",font,23);body.alignment=TextAlignmentOptions.TopLeft;body.enableAutoSizing=true;body.fontSizeMin=18;body.fontSizeMax=23;body.textWrappingMode=TextWrappingModes.Normal;
            body.rectTransform.anchorMin=Vector2.zero;body.rectTransform.anchorMax=Vector2.one;body.rectTransform.offsetMin=new Vector2(24,90);body.rectTransform.offsetMax=new Vector2(-24,-75);
            TMP_Text actionText,skipText;
            Button action=Button(image.transform,"MaraAction","Bắt đầu hướng dẫn",font,out actionText);
            var ar=action.GetComponent<RectTransform>();ar.anchorMin=ar.anchorMax=ar.pivot=new Vector2(0,0);ar.anchoredPosition=new Vector2(24,22);ar.sizeDelta=new Vector2(320,52);
            Button skip=Button(image.transform,"MaraSkip","Bỏ qua",font,out skipText);
            var sr=skip.GetComponent<RectTransform>();sr.anchorMin=sr.anchorMax=sr.pivot=new Vector2(1,0);sr.anchoredPosition=new Vector2(-24,22);sr.sizeDelta=new Vector2(180,52);
            var data=new SerializedObject(controller);
            Assign(data,"buildManager",manager);Assign(data,"buildMode",mode);Assign(data,"waves",waves);Assign(data,"save",save);
            data.FindProperty("orchardId").stringValue=orchard.SaveId;data.FindProperty("lilyId").stringValue=lily.SaveId;
            Assign(data,"panel",image.gameObject);Assign(data,"title",title);Assign(data,"body",body);Assign(data,"actionButton",action);Assign(data,"actionText",actionText);Assign(data,"skipButton",skip);data.ApplyModifiedProperties();
            Undo.RecordObject(waves,"Gán điều kiện hướng dẫn");var waveData=new SerializedObject(waves);Assign(waveData,"tutorial",controller);waveData.ApplyModifiedProperties();
            title.text="Mara • Chào mừng đến khu vườn";body.text="Hướng dẫn tương tác sẽ hiện khi Chơi mới. Tiếp tục bản lưu sẽ không bắt học lại.";
            image.gameObject.SetActive(false);
            EditorSceneManager.MarkSceneDirty(scene);Undo.CollapseUndoOperations(undo);Selection.activeGameObject=obj;
            Debug.Log("Đã gán tutorial Mara. Ctrl+S lưu Garden, chạy Chơi mới để kiểm tra.",controller);
        }
        catch(Exception error){if(undo>=0)Undo.RevertAllDownToGroup(undo);Debug.LogError("Tạo tutorial thất bại: "+error.Message);}
    }
    private static T Single<T>(Scene scene) where T:Component
    {
        T found=null;foreach(var item in UnityEngine.Object.FindObjectsByType<T>(FindObjectsInactive.Include))
            if(item.gameObject.scene==scene){if(found!=null)throw new InvalidOperationException("Có nhiều "+typeof(T).Name);found=item;}
        if(found==null)throw new InvalidOperationException("Thiếu "+typeof(T).Name);return found;
    }
    private static Image Image(Transform parent,string name)
    {
        var obj=new GameObject(name,typeof(RectTransform),typeof(CanvasRenderer),typeof(Image));
        Undo.RegisterCreatedObjectUndo(obj,"Tạo giao diện Mara");obj.transform.SetParent(parent,false);return obj.GetComponent<Image>();
    }
    private static TextMeshProUGUI Text(Transform parent,string name,TMP_FontAsset font,float size)
    {
        var obj=new GameObject(name,typeof(RectTransform),typeof(TextMeshProUGUI));Undo.RegisterCreatedObjectUndo(obj,"Tạo chữ Mara");obj.transform.SetParent(parent,false);
        var text=obj.GetComponent<TextMeshProUGUI>();text.font=font;text.fontSize=size;text.raycastTarget=false;text.color=new Color(1,.96f,.86f,1);return text;
    }
    private static Button Button(Transform parent,string name,string label,TMP_FontAsset font,out TMP_Text text)
    {
        var image=Image(parent,name);image.sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/UnitySkin/primary_normal.png");image.type=UnityEngine.UI.Image.Type.Sliced;
        image.color=image.sprite!=null?Color.white:new Color(.2f,.1f,.3f,1);
        var button=Undo.AddComponent<Button>(image.gameObject);button.targetGraphic=image;
        var caption=Text(image.transform,"Label",font,22);caption.text=label;caption.alignment=TextAlignmentOptions.Center;
        caption.rectTransform.anchorMin=Vector2.zero;caption.rectTransform.anchorMax=Vector2.one;caption.rectTransform.offsetMin=new Vector2(10,6);caption.rectTransform.offsetMax=new Vector2(-10,-6);text=caption;return button;
    }
    private static void Assign(SerializedObject data,string name,UnityEngine.Object value)=>data.FindProperty(name).objectReferenceValue=value;
}
