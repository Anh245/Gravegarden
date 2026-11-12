using System;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class VietnameseHelpSetup
{
    private const string Skin="Assets/Art/UI/UnitySkin/";
    [MenuItem("GraveGarden/UI/Setup Vietnamese Help (Current Scene)")]
    private static void Setup()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode){Debug.LogError("Thoát Play trước khi tạo hướng dẫn.");return;}
        Scene scene=SceneManager.GetActiveScene();int undo=-1;
        try
        {
            if(scene.name!="MainMenu" && scene.name!="Garden")throw new InvalidOperationException("Mở MainMenu hoặc Garden.");
            Undo.IncrementCurrentGroup();undo=Undo.GetCurrentGroup();Undo.SetCurrentGroupName("Thêm hướng dẫn tiếng Việt");
            if(scene.name=="MainMenu")SetupMenu(scene);else SetupGarden(scene);
            EditorSceneManager.MarkSceneDirty(scene);Undo.CollapseUndoOperations(undo);
            Debug.Log("Đã chuẩn bị hướng dẫn cho "+scene.name+". Ctrl+S lưu scene, kiểm tra chữ và bố cục trong Game View.");
        }
        catch(Exception e){if(undo>=0)Undo.RevertAllDownToGroup(undo);Debug.LogError("Tạo hướng dẫn thất bại: "+e.Message);}
    }
    private static T Single<T>(Scene scene) where T:Component
    {
        T found=null;
        foreach(var item in UnityEngine.Object.FindObjectsByType<T>(FindObjectsInactive.Include))
            if(item.gameObject.scene==scene){if(found!=null)throw new InvalidOperationException("Có nhiều "+typeof(T).Name);found=item;}
        if(found==null)throw new InvalidOperationException("Thiếu "+typeof(T).Name);return found;
    }
    private static void SetupMenu(Scene scene)
    {
        var menu=Single<MainMenuController>(scene);var data=new SerializedObject(menu);
        var root=data.FindProperty("helpPanel").objectReferenceValue as GameObject;
        if(root==null)throw new InvalidOperationException("MainMenuController chưa gán Help Panel.");
        Canvas canvas=root.GetComponentInParent<Canvas>();
        if(canvas==null)throw new InvalidOperationException("HelpPanel phải nằm trong Canvas.");
        TMP_Text status=data.FindProperty("statusText").objectReferenceValue as TMP_Text;
        var font=status!=null?status.font:TMP_Settings.defaultFontAsset;
        Button close;
        var guide=Build(root,canvas,font,out close);
        Undo.RecordObject(menu,"Gán nút đóng hướng dẫn");data.Update();
        data.FindProperty("closeHelpButton").objectReferenceValue=close;data.ApplyModifiedProperties();
        Label(data,"newGameButton","Chơi mới");Label(data,"continueButton","Tiếp tục");Label(data,"helpButton","Hướng dẫn");Label(data,"quitButton","Thoát");
        Selection.activeGameObject=guide.gameObject;
    }
    private static void SetupGarden(Scene scene)
    {
        var pause=Single<GardenPauseController>(scene);var data=new SerializedObject(pause);
        var panel=data.FindProperty("pausePanel").objectReferenceValue as GameObject;
        if(panel==null)throw new InvalidOperationException("GardenPauseController thiếu Pause Panel.");
        Canvas canvas=panel.GetComponentInParent<Canvas>();
        if(canvas==null)throw new InvalidOperationException("PausePanel phải nằm trong Canvas.");
        TMP_Text status=data.FindProperty("statusText").objectReferenceValue as TMP_Text;
        var font=status!=null?status.font:TMP_Settings.defaultFontAsset;
        HelpGuidePanel guide=data.FindProperty("helpGuide").objectReferenceValue as HelpGuidePanel;
        GameObject root;
        if(guide!=null)root=guide.gameObject;
        else
        {
            root=new GameObject("GardenHelpPanel",typeof(RectTransform),typeof(CanvasRenderer),typeof(Image));
            Undo.RegisterCreatedObjectUndo(root,"Tạo bảng hướng dẫn");root.transform.SetParent(canvas.transform,false);
        }
        Button close;guide=Build(root,canvas,font,out close);
        Button help=data.FindProperty("helpButton").objectReferenceValue as Button;
        if(help==null)
        {
            help=Button(panel.transform,"HelpGuideButton","Hướng dẫn",font);
            var rect=help.GetComponent<RectTransform>();rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(1,0);
            rect.anchoredPosition=new Vector2(-55,55);rect.sizeDelta=new Vector2(240,58);
        }
        Undo.RecordObject(pause,"Gán hướng dẫn trong Pause");data.Update();
        data.FindProperty("helpGuide").objectReferenceValue=guide;data.FindProperty("helpButton").objectReferenceValue=help;data.ApplyModifiedProperties();
        Selection.activeGameObject=guide.gameObject;
    }
    private static HelpGuidePanel Build(GameObject root,Canvas canvas,TMP_FontAsset font,out Button close)
    {
        var existing=root.GetComponent<HelpGuidePanel>();
        if(existing!=null)
        {
            var current=new SerializedObject(existing);close=current.FindProperty("closeButton").objectReferenceValue as Button;
            if(close==null)throw new InvalidOperationException("Hướng dẫn đã có nhưng mất Close Button. Khôi phục tham chiếu trước.");
            Undo.RecordObject(root,"Ẩn bảng mặc định");root.SetActive(false);
            Debug.Log("Bảng hướng dẫn đã có; giữ bố cục đã chỉnh.",root);return existing;
        }
        if(root.GetComponent<RectTransform>()==null)throw new InvalidOperationException("HelpPanel phải là UI RectTransform.");
        foreach(Transform child in root.transform){Undo.RecordObject(child.gameObject,"Giữ giao diện cũ ở trạng thái ẩn");child.gameObject.SetActive(false);}
        Undo.RecordObject(root,"Ẩn hướng dẫn mặc định");root.SetActive(false);
        Undo.SetTransformParent(root.transform,canvas.transform,"Đặt hướng dẫn trực tiếp trong Canvas");
        var rt=root.GetComponent<RectTransform>();Undo.RecordObject(rt,"Căn nền phủ màn hình");Stretch(rt,Vector2.zero,Vector2.zero);
        rt.localScale=Vector3.one;rt.localRotation=Quaternion.identity;
        var shade=root.GetComponent<Image>();if(shade==null)shade=Undo.AddComponent<Image>(root);else Undo.RecordObject(shade,"Đổi nền phủ");
        shade.sprite=null;shade.color=new Color(0,0,0,.85f);shade.raycastTarget=true;
        var frame=Image(root.transform,"VietnameseGuideFrame");
        frame.sprite=AssetDatabase.LoadAssetAtPath<Sprite>(Skin+"modal_panel.png");
        frame.type=UnityEngine.UI.Image.Type.Sliced;frame.color=frame.sprite!=null?Color.white:new Color(.08f,.04f,.12f,1);
        var frameRect=frame.GetComponent<RectTransform>();frameRect.anchorMin=new Vector2(.06f,.06f);frameRect.anchorMax=new Vector2(.94f,.94f);frameRect.offsetMin=frameRect.offsetMax=Vector2.zero;
        var title=Text(frame.transform,"GuideTitle",font,38);title.alignment=TextAlignmentOptions.Center;
        var tr=title.rectTransform;tr.anchorMin=new Vector2(0,1);tr.anchorMax=new Vector2(1,1);tr.pivot=new Vector2(.5f,1);tr.offsetMin=new Vector2(55,-105);tr.offsetMax=new Vector2(-55,-35);
        var body=Text(frame.transform,"GuideBody",font,28);body.alignment=TextAlignmentOptions.TopLeft;body.enableAutoSizing=true;body.fontSizeMin=18;body.fontSizeMax=28;
        body.textWrappingMode=TextWrappingModes.Normal;body.overflowMode=TextOverflowModes.Ellipsis;
        Stretch(body.rectTransform,new Vector2(60,160),new Vector2(-60,-125));
        var page=Text(frame.transform,"GuidePage",font,24);page.alignment=TextAlignmentOptions.Center;
        var pr=page.rectTransform;pr.anchorMin=pr.anchorMax=pr.pivot=new Vector2(.5f,0);pr.anchoredPosition=new Vector2(0,110);pr.sizeDelta=new Vector2(200,38);
        var previous=Button(frame.transform,"PreviousGuideButton","Trang trước",font);Footer(previous,.2f);
        var next=Button(frame.transform,"NextGuideButton","Trang sau",font);Footer(next,.5f);
        close=Button(frame.transform,"CloseGuideButton","Đóng",font);Footer(close,.8f);
        var component=Undo.AddComponent<HelpGuidePanel>(root);var data=new SerializedObject(component);
        Assign(data,"titleText",title);Assign(data,"bodyText",body);Assign(data,"pageText",page);Assign(data,"previousButton",previous);Assign(data,"nextButton",next);Assign(data,"closeButton",close);data.ApplyModifiedProperties();
        var first=GraveGardenHelpContent.Get(0);title.text=first.Title;body.text=first.Body;page.text="1 / "+GraveGardenHelpContent.Count;
        return component;
    }
    private static void Stretch(RectTransform rt,Vector2 minimum,Vector2 maximum)
    {rt.anchorMin=Vector2.zero;rt.anchorMax=Vector2.one;rt.pivot=new Vector2(.5f,.5f);rt.offsetMin=minimum;rt.offsetMax=maximum;}
    private static Image Image(Transform parent,string name)
    {
        var obj=new GameObject(name,typeof(RectTransform),typeof(CanvasRenderer),typeof(Image));
        Undo.RegisterCreatedObjectUndo(obj,"Tạo ảnh hướng dẫn");obj.transform.SetParent(parent,false);return obj.GetComponent<Image>();
    }
    private static TextMeshProUGUI Text(Transform parent,string name,TMP_FontAsset font,float size)
    {
        var obj=new GameObject(name,typeof(RectTransform),typeof(TextMeshProUGUI));
        Undo.RegisterCreatedObjectUndo(obj,"Tạo chữ hướng dẫn");obj.transform.SetParent(parent,false);
        var text=obj.GetComponent<TextMeshProUGUI>();text.font=font;text.fontSize=size;text.color=new Color(1,.96f,.86f,1);text.raycastTarget=false;return text;
    }
    private static Button Button(Transform parent,string name,string label,TMP_FontAsset font)
    {
        var image=Image(parent,name);image.sprite=AssetDatabase.LoadAssetAtPath<Sprite>(Skin+"primary_normal.png");image.type=UnityEngine.UI.Image.Type.Sliced;
        image.color=image.sprite!=null?Color.white:new Color(.22f,.12f,.3f,1);var button=Undo.AddComponent<Button>(image.gameObject);button.targetGraphic=image;
        var nav=button.navigation;nav.mode=Navigation.Mode.None;button.navigation=nav;
        var text=Text(image.transform,"Label",font,25);text.text=label;text.alignment=TextAlignmentOptions.Center;Stretch(text.rectTransform,new Vector2(15,8),new Vector2(-15,-8));return button;
    }
    private static void Footer(Button button,float x)
    {var rt=button.GetComponent<RectTransform>();rt.anchorMin=rt.anchorMax=new Vector2(x,0);rt.pivot=new Vector2(.5f,0);rt.anchoredPosition=new Vector2(0,45);rt.sizeDelta=new Vector2(240,58);}
    private static void Assign(SerializedObject data,string name,UnityEngine.Object value)=>data.FindProperty(name).objectReferenceValue=value;
    private static void Label(SerializedObject data,string field,string label)
    {
        var button=data.FindProperty(field).objectReferenceValue as Button;if(button==null)return;
        var text=button.GetComponentInChildren<TMP_Text>(true);if(text==null)return;Undo.RecordObject(text,"Việt hóa nhãn menu");text.text=label;EditorUtility.SetDirty(text);
    }
}
