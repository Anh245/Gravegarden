using System;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class GraveGardenShopBuilder
{
    private static TMP_FontAsset font;
    private const string SkinFolder="Assets/Art/UI/ShopUnity";
    private static readonly string[] SkinNames={"shop_affordable","shop_unaffordable","shop_locked","shop_selected","shop_placed","shop_tray"};

    [MenuItem("GraveGarden/UI/Create Building Shop")]
    public static void Create()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode || SceneManager.GetActiveScene().name!="Garden")
        {Debug.LogError("Thoát Play Mode và mở scene Garden trước khi tạo shop.");return;}
        var existing=UnityEngine.Object.FindObjectsByType<BuildingShopController>(FindObjectsInactive.Include)
            .FirstOrDefault(x=>x.gameObject.scene==SceneManager.GetActiveScene());
        if(existing!=null){Selection.activeGameObject=existing.gameObject;Debug.LogWarning("Shop đã có, giữ UI hiện tại thay vì tạo trùng.");return;}
        var manager=Find<BuildManager>();var mode=Find<BuildModeController>();var save=Find<GardenSaveController>();
        var canvas=Selection.activeGameObject!=null?Selection.activeGameObject.GetComponentInParent<Canvas>():null;
        if(canvas!=null)canvas=canvas.rootCanvas;
        if(canvas!=null && canvas.gameObject.scene!=SceneManager.GetActiveScene())canvas=null;
        var canvases=UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include)
            .Where(x=>x.isRootCanvas && x.gameObject.scene==SceneManager.GetActiveScene() && x.renderMode==RenderMode.ScreenSpaceOverlay).ToArray();
        if(canvas==null && canvases.Length==1)canvas=canvases[0];
        font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/LilitaOne-Regular SDF.asset");
        if(manager==null || mode==null || save==null || canvas==null || font==null)
        {Debug.LogError("Cần Canvas, BuildManager, BuildModeController, GardenSaveController và font LilitaOne-Regular SDF. Chọn đúng Canvas rồi thử lại.");return;}
        foreach(string name in SkinNames)if(AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/UI/ui/"+name+".png")==null)
        {Debug.LogError("Thiếu ảnh UI: "+name);return;}
        if(!AssetDatabase.IsValidFolder(SkinFolder))AssetDatabase.CreateFolder("Assets/Art/UI","ShopUnity");
        var sprites=SkinNames.Select(PrepareSprite).ToArray();
        Undo.IncrementCurrentGroup();int group=Undo.GetCurrentGroup();Undo.SetCurrentGroupName("Create GraveGarden Building Shop");

        RectTransform panel=Rect("BuildingShopPanel",canvas.transform);
        panel.anchorMin=new Vector2(0,0);panel.anchorMax=new Vector2(1,0);panel.pivot=new Vector2(0.5f,0);
        panel.anchoredPosition=new Vector2(0,16);panel.sizeDelta=new Vector2(-32,320);
        Image tray=panel.gameObject.AddComponent<Image>();tray.sprite=sprites[5];tray.type=Image.Type.Sliced;tray.pixelsPerUnitMultiplier=2;
        TMP_Text title=Text("ShopTitle",panel,"Công trình",24);At(title.rectTransform,new Vector2(0,1),new Vector2(0,1),new Vector2(24,-10),new Vector2(220,32));
        TMP_Text status=Text("ShopStatus",panel,"Chọn công trình rồi chọn ô trống.",18);
        At(status.rectTransform,new Vector2(0.5f,1),new Vector2(0.5f,1),new Vector2(0,-12),new Vector2(1180,30));
        Button close=Button("ShopCloseButton",panel,"Xong",18);
        At((RectTransform)close.transform,new Vector2(1,1),new Vector2(1,1),new Vector2(-20,-10),new Vector2(100,34));

        RectTransform scrollRoot=Rect("ShopScroll",panel);Stretch(scrollRoot,48,48,54,32);
        ScrollRect scroll=scrollRoot.gameObject.AddComponent<ScrollRect>();scroll.horizontal=true;scroll.vertical=false;
        scroll.movementType=ScrollRect.MovementType.Clamped;scroll.scrollSensitivity=40;
        RectTransform viewport=Rect("Viewport",scrollRoot);Stretch(viewport,0,0,0,0);
        var catchDrag=viewport.gameObject.AddComponent<Image>();catchDrag.color=Color.clear;catchDrag.raycastTarget=true;
        viewport.gameObject.AddComponent<RectMask2D>();
        RectTransform content=Rect("Content",viewport);content.anchorMin=new Vector2(0,0);content.anchorMax=new Vector2(0,1);
        content.pivot=new Vector2(0,0.5f);content.anchoredPosition=Vector2.zero;content.sizeDelta=Vector2.zero;
        var layout=content.gameObject.AddComponent<HorizontalLayoutGroup>();layout.spacing=12;
        layout.padding=new RectOffset(8,8,0,0);layout.childAlignment=TextAnchor.MiddleLeft;
        layout.childControlWidth=false;layout.childControlHeight=false;layout.childForceExpandWidth=false;layout.childForceExpandHeight=false;
        var fitter=content.gameObject.AddComponent<ContentSizeFitter>();fitter.horizontalFit=ContentSizeFitter.FitMode.PreferredSize;
        fitter.verticalFit=ContentSizeFitter.FitMode.Unconstrained;scroll.viewport=viewport;scroll.content=content;

        RectTransform card=Rect("CardTemplate",content);card.sizeDelta=new Vector2(156,220);
        var background=card.gameObject.AddComponent<Image>();background.sprite=sprites[0];background.preserveAspect=true;
        var cardButton=card.gameObject.AddComponent<Button>();cardButton.targetGraphic=background;cardButton.transition=Selectable.Transition.None;
        var cardView=card.gameObject.AddComponent<BuildingShopCard>();
        TMP_Text cardTitle=Text("Title",card,"Công trình",18);At(cardTitle.rectTransform,new Vector2(0.5f,1),new Vector2(0.5f,1),new Vector2(0,-8),new Vector2(136,40));
        TMP_Text role=Text("Role",card,"PHÒNG THỦ | Có: 0",12);At(role.rectTransform,new Vector2(0.5f,1),new Vector2(0.5f,1),new Vector2(0,-50),new Vector2(140,20));
        RectTransform iconRect=Rect("Icon",card);At(iconRect,new Vector2(0.5f,0.5f),new Vector2(0.5f,0.5f),Vector2.zero,new Vector2(100,70));
        var icon=iconRect.gameObject.AddComponent<Image>();icon.preserveAspect=true;icon.raycastTarget=false;
        TMP_Text state=Text("State",card,"Chọn công trình",14);At(state.rectTransform,new Vector2(0.5f,0),new Vector2(0.5f,0.5f),new Vector2(0,58),new Vector2(140,22));
        TMP_Text price=Text("Price",card,"150",22);At(price.rectTransform,new Vector2(0.5f,0),new Vector2(0.5f,0.5f),new Vector2(0,26),new Vector2(140,30));
        Set(cardView,("button",cardButton),("background",background),("icon",icon),("titleText",cardTitle),("roleText",role),("priceText",price),("stateText",state));
        card.gameObject.SetActive(false);

        var controllerRoot=new GameObject("BuildingShopController");Undo.RegisterCreatedObjectUndo(controllerRoot,"Create shop controller");
        controllerRoot.transform.SetParent(manager.transform.parent,false);
        var controller=controllerRoot.AddComponent<BuildingShopController>();
        Set(controller,("buildManager",manager),("buildMode",mode),("saveController",save),("shopPanel",panel.gameObject),
            ("content",content),("cardTemplate",cardView),("closeButton",close),("statusText",status),
            ("skins.affordable",sprites[0]),("skins.unaffordable",sprites[1]),("skins.locked",sprites[2]),
            ("skins.selected",sprites[3]),("skins.placed",sprites[4]),("skins.tray",sprites[5]));
        panel.gameObject.SetActive(false);
        Undo.CollapseUndoOperations(group);EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Selection.activeGameObject=controllerRoot;
        Debug.Log("Đã tạo shop và nối tham chiếu. Lưu Garden. Play rồi bấm Xây dựng. Giữ dropdown cũ, code sẽ ẩn nó khi shop khởi tạo thành công.");
    }
    private static T Find<T>() where T:Component=>UnityEngine.Object.FindObjectsByType<T>(FindObjectsInactive.Include)
        .FirstOrDefault(x=>x.gameObject.scene==SceneManager.GetActiveScene());
    private static Sprite PrepareSprite(string name)
    {
        string path=SkinFolder+"/"+name+".png";
        if(AssetDatabase.LoadMainAssetAtPath(path)==null && !AssetDatabase.CopyAsset("Assets/Art/UI/ui/"+name+".png",path))
            throw new InvalidOperationException("Không copy được "+name);
        var importer=(TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;
        importer.spritePixelsPerUnit=100;importer.mipmapEnabled=false;importer.alphaIsTransparency=true;
        importer.filterMode=FilterMode.Point;importer.textureCompression=TextureImporterCompression.Uncompressed;
        importer.wrapMode=TextureWrapMode.Clamp;importer.npotScale=TextureImporterNPOTScale.None;
        var settings=new TextureImporterSettings();importer.ReadTextureSettings(settings);settings.spriteMeshType=SpriteMeshType.FullRect;
        importer.SetTextureSettings(settings);importer.spriteBorder=name=="shop_tray"?new Vector4(100,64,100,64):Vector4.zero;
        importer.SaveAndReimport();return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
    private static RectTransform Rect(string name,Transform parent)
    {
        var root=new GameObject(name,typeof(RectTransform));Undo.RegisterCreatedObjectUndo(root,"Create shop UI");
        root.transform.SetParent(parent,false);return (RectTransform)root.transform;
    }
    private static void At(RectTransform rect,Vector2 anchor,Vector2 pivot,Vector2 position,Vector2 size)
    {rect.anchorMin=rect.anchorMax=anchor;rect.pivot=pivot;rect.anchoredPosition=position;rect.sizeDelta=size;}
    private static void Stretch(RectTransform rect,float left,float right,float top,float bottom)
    {rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=new Vector2(left,bottom);rect.offsetMax=new Vector2(-right,-top);}
    private static TMP_Text Text(string name,Transform parent,string value,float size)
    {
        var rect=Rect(name,parent);var text=rect.gameObject.AddComponent<TextMeshProUGUI>();
        text.font=font;text.text=value;text.fontSize=size;text.alignment=TextAlignmentOptions.Center;
        text.color=new Color(0.95f,0.91f,0.84f);text.raycastTarget=false;return text;
    }
    private static Button Button(string name,Transform parent,string label,float size)
    {
        var rect=Rect(name,parent);var image=rect.gameObject.AddComponent<Image>();image.color=new Color(0.23f,0.17f,0.3f,1f);
        Sprite primary=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/UnitySkin/primary_normal.png");
        if(primary!=null){image.sprite=primary;image.type=Image.Type.Sliced;image.pixelsPerUnitMultiplier=2;image.color=Color.white;}
        var button=rect.gameObject.AddComponent<Button>();button.targetGraphic=image;
        TMP_Text text=Text("Text",rect,label,size);Stretch(text.rectTransform,6,6,0,0);return button;
    }
    private static void Set(UnityEngine.Object target,params (string name,UnityEngine.Object value)[] values)
    {
        var serialized=new SerializedObject(target);
        foreach(var pair in values)serialized.FindProperty(pair.name).objectReferenceValue=pair.value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }
}
