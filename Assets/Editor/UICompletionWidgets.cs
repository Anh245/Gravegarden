using System;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static partial class UICompletionSetup
{
    private static T GetOrAdd<T>(GameObject obj) where T:Component
    {var item=obj.GetComponent<T>();return item!=null?item:Undo.AddComponent<T>(obj);}
    private static void Record(UnityEngine.Object obj){if(obj!=null)Undo.RecordObject(obj,"Hoàn thiện UI");}
    private static RectTransform Rect(Transform parent,string name)
    {
        Transform found=parent.Find(name);
        if(found!=null)return found.GetComponent<RectTransform>();
        var obj=new GameObject(name,typeof(RectTransform));Undo.RegisterCreatedObjectUndo(obj,"Thêm thành phần UI");obj.transform.SetParent(parent,false);return obj.GetComponent<RectTransform>();
    }
    private static Image Picture(Transform parent,string name,Sprite sprite)
    {
        var rect=Rect(parent,name);var image=GetOrAdd<Image>(rect.gameObject);Record(image);
        image.sprite=sprite;image.color=Color.white;image.raycastTarget=false;return image;
    }
    private static TextMeshProUGUI Text(Transform parent,string name,TMP_FontAsset font,float size)
    {
        var rect=Rect(parent,name);var text=GetOrAdd<TextMeshProUGUI>(rect.gameObject);Record(text);
        text.font=font;text.fontSize=size;text.color=new Color(1,.96f,.86f,1);text.raycastTarget=false;
        text.enableAutoSizing=true;text.fontSizeMin=16;text.fontSizeMax=size;text.textWrappingMode=TextWrappingModes.Normal;
        return text;
    }
    private static void Stretch(RectTransform rt,float left,float bottom,float right,float top)
    {Record(rt);rt.anchorMin=Vector2.zero;rt.anchorMax=Vector2.one;rt.pivot=new Vector2(.5f,.5f);rt.offsetMin=new Vector2(left,bottom);rt.offsetMax=new Vector2(-right,-top);}
    private static void Top(RectTransform rt,float left,float top,float width,float height)
    {Record(rt);rt.anchorMin=rt.anchorMax=rt.pivot=new Vector2(0,1);rt.anchoredPosition=new Vector2(left,-top);rt.sizeDelta=new Vector2(width,height);}
    private static void Bottom(RectTransform rt,float y,float height)
    {Record(rt);rt.anchorMin=new Vector2(0,0);rt.anchorMax=new Vector2(1,0);rt.pivot=new Vector2(.5f,0);rt.offsetMin=new Vector2(24,y);rt.offsetMax=new Vector2(-24,y+height);}
    private static GameObject Surface(GameObject panel)
    {
        foreach(string name in new[]{"WindowFrame","Wireframe","Content"})
        {
            var child=panel.transform.Find(name);
            if(child!=null && child.GetComponent<Image>()!=null)return child.gameObject;
        }
        return panel;
    }
    private static void Frame(GameObject panel,string sprite)
    {
        panel=Surface(panel);
        var image=GetOrAdd<Image>(panel);Record(image);image.sprite=FinalUISkin.UI(sprite);image.type=Image.Type.Sliced;
        image.color=Color.white;image.pixelsPerUnitMultiplier=sprite=="modal_panel"?2:1;
    }
    private static void Primary(Button button,Sprite glyph=null,string label=null)
    {
        if(button==null)return;Record(button);
        var image=button.targetGraphic as Image;if(image==null)image=GetOrAdd<Image>(button.gameObject);Record(image);
        image.sprite=FinalUISkin.UI("primary_normal");image.type=Image.Type.Sliced;image.pixelsPerUnitMultiplier=4;image.color=Color.white;button.targetGraphic=image;
        button.transition=Selectable.Transition.SpriteSwap;
        var state=button.spriteState;state.highlightedSprite=FinalUISkin.UI("primary_selected");state.selectedSprite=state.highlightedSprite;
        state.pressedSprite=FinalUISkin.UI("primary_pressed");state.disabledSprite=FinalUISkin.UI("primary_disabled");button.spriteState=state;
        var text=button.GetComponentInChildren<TMP_Text>(true);
        if(text!=null)
        {
            Record(text);if(label!=null)text.text=label;text.color=new Color(1,.96f,.86f,1);
            text.enableAutoSizing=true;text.fontSizeMin=14;text.fontSizeMax=Mathf.Min(26,Mathf.Max(20,text.fontSize));
            text.alignment=TextAlignmentOptions.Center;text.raycastTarget=false;
            Stretch(text.rectTransform,0,0,0,0);text.margin=new Vector4(glyph!=null?60:12,5,12,5);
        }
        if(glyph!=null)
        {
            var icon=Picture(button.transform,"FinalUI_ActionIcon",glyph);icon.preserveAspect=true;
            var rt=icon.rectTransform;Record(rt);rt.anchorMin=rt.anchorMax=rt.pivot=new Vector2(0,.5f);rt.anchoredPosition=new Vector2(8,0);rt.sizeDelta=new Vector2(40,40);
        }
    }
    private static void Square(Button button,Sprite sprite)
    {
        if(button==null)return;Record(button);var image=button.targetGraphic as Image;if(image==null)image=GetOrAdd<Image>(button.gameObject);Record(image);
        image.sprite=sprite;image.type=Image.Type.Simple;image.preserveAspect=true;image.color=Color.white;button.targetGraphic=image;
        button.transition=Selectable.Transition.None;Record(button.GetComponent<RectTransform>());button.GetComponent<RectTransform>().sizeDelta=new Vector2(60,60);
        foreach(var text in button.GetComponentsInChildren<TMP_Text>(true)){Record(text.gameObject);text.gameObject.SetActive(false);}
    }
    private static void Inline(TMP_Text text,Sprite sprite,string name,float size=32)
    {
        if(text==null)return;Record(text);var image=Picture(text.transform,name,sprite);image.preserveAspect=true;
        var rt=image.rectTransform;Record(rt);rt.anchorMin=rt.anchorMax=rt.pivot=new Vector2(0,.5f);rt.anchoredPosition=Vector2.zero;rt.sizeDelta=new Vector2(size,size);
        Vector4 margin=text.margin;margin.x=Mathf.Max(margin.x,size+8);text.margin=margin;text.raycastTarget=false;
    }
    private static void Assign(SerializedObject data,string name,UnityEngine.Object obj){var property=data.FindProperty(name);if(property==null)throw new InvalidOperationException("Thiếu trường "+name);property.objectReferenceValue=obj;}
    private static T Ref<T>(SerializedObject data,string name) where T:UnityEngine.Object
    {var property=data.FindProperty(name);return property==null?null:property.objectReferenceValue as T;}
    private static void SkinFields(MonoBehaviour owner,string[] fields)
    {
        if(owner==null)return;var data=new SerializedObject(owner);
        foreach(string field in fields)Primary(Ref<Button>(data,field));
    }
    private static void Details(MonoBehaviour owner,int kind)
    {
        var data=new SerializedObject(owner);var panel=Ref<GameObject>(data,"panel");var info=Ref<TMP_Text>(data,"infoText");
        if(panel==null||info==null)throw new InvalidOperationException(owner.name+": thiếu Panel hoặc InfoText.");
        Frame(panel,"building_panel");var root=panel.GetComponent<RectTransform>();Record(root);
        root.anchorMin=root.anchorMax=root.pivot=new Vector2(1,.5f);root.anchoredPosition=new Vector2(-24,0);root.sizeDelta=new Vector2(560,760);root.localScale=Vector3.one;
        var header=Text(panel.transform,"FinalUI_Name",info.font,30);Top(header.rectTransform,24,24,512,44);header.text="Thông tin công trình";
        foreach(var old in panel.GetComponentsInChildren<TMP_Text>(true))
            if(old!=header && old!=info && old.gameObject.name.ToLowerInvariant().Contains("title"))
            {Record(old.gameObject);old.gameObject.SetActive(false);}
        var level=Text(panel.transform,"FinalUI_Level",info.font,22);Top(level.rectTransform,24,73,512,30);level.text="Cấp 1 / 3";
        string[][] icons={new[]{"damage","speed","range"},new[]{"production","collect","storage"},new[]{"speed","range","upgrade"}};
        var rows=new TMP_Text[3];
        for(int i=0;i<3;i++)
        {
            var image=Picture(panel.transform,"FinalUI_StatIcon_"+i,FinalUISkin.Icon(icons[kind][i]));image.preserveAspect=true;Top(image.rectTransform,24,120+i*58,40,40);
            var value=Text(panel.transform,"FinalUI_StatValue_"+i,info.font,22);Top(value.rectTransform,80,116+i*58,456,52);value.alignment=TextAlignmentOptions.MidlineLeft;rows[i]=value;
        }
        var divider=Picture(panel.transform,"FinalUI_Divider",FinalUISkin.UI("bone_divider"));Top(divider.rectTransform,24,300,512,16);
        Record(info);Stretch(info.rectTransform,24,260,24,335);info.fontSize=22;info.enableAutoSizing=true;info.fontSizeMin=16;info.fontSizeMax=22;info.alignment=TextAlignmentOptions.TopLeft;info.raycastTarget=false;
        string[] buttons={"upgradeButton","moveButton","sellButton","closeButton"};string[] glyphs={"upgrade_button","move_button","sell_button","cancel_button"};
        for(int i=0;i<4;i++){var button=Ref<Button>(data,buttons[i]);if(button==null)continue;Primary(button,FinalUISkin.UI(glyphs[i]));Bottom(button.GetComponent<RectTransform>(),188-i*58,52);}
        var sell=Ref<Button>(data,"sellButton");if(sell!=null)
        {
            var coin=Picture(sell.transform,"FinalUI_RefundCoin",FinalUISkin.Icon("sell"));coin.preserveAspect=true;var rect=coin.rectTransform;
            Record(rect);rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(1,.5f);rect.anchoredPosition=new Vector2(-10,0);rect.sizeDelta=new Vector2(25,25);
            var text=Ref<TMP_Text>(data,"sellButtonText");if(text!=null){Record(text);var margin=text.margin;margin.z=42;text.margin=margin;}
        }
        var presenter=GetOrAdd<BuildingStatRows>(panel);Record(presenter);var view=new SerializedObject(presenter);
        Assign(view,"titleText",header);Assign(view,"levelText",level);var array=view.FindProperty("values");array.arraySize=3;
        for(int i=0;i<3;i++)array.GetArrayElementAtIndex(i).objectReferenceValue=rows[i];view.ApplyModifiedProperties();
        Record(owner);data.Update();Assign(data,"statRows",presenter);data.ApplyModifiedProperties();
    }
    private static void BuildPanel(BuildManager manager)
    {
        var data=new SerializedObject(manager);var panel=Ref<GameObject>(data,"buildPanel");if(panel==null)return;
        Frame(panel,"building_panel");var root=panel.GetComponent<RectTransform>();Record(root);
        root.anchorMin=root.anchorMax=root.pivot=new Vector2(1,.5f);root.anchoredPosition=new Vector2(-24,0);root.sizeDelta=new Vector2(560,560);root.localScale=Vector3.one;
        var info=Ref<TMP_Text>(data,"infoText");if(info!=null){Record(info);Stretch(info.rectTransform,24,150,24,125);info.enableAutoSizing=true;info.fontSizeMin=18;info.fontSizeMax=24;info.alignment=TextAlignmentOptions.TopLeft;}
        var title=Text(panel.transform,"FinalUI_BuildTitle",info!=null?info.font:TMP_Settings.defaultFontAsset,30);title.text="Xây công trình";Top(title.rectTransform,24,24,512,48);
        foreach(var old in panel.GetComponentsInChildren<TMP_Text>(true))if(old!=title && old.gameObject.name.ToLowerInvariant().Contains("title")){Record(old.gameObject);old.gameObject.SetActive(false);}
        var drop=Ref<TMP_Dropdown>(data,"buildingDropdown");if(drop!=null)Top(drop.GetComponent<RectTransform>(),24,80,512,40);
        var build=Ref<Button>(data,"buildButton");var close=Ref<Button>(data,"closeButton");Primary(build,FinalUISkin.UI("confirm_button"),"Xây");Primary(close,FinalUISkin.UI("cancel_button"),"Đóng");
        if(build!=null)Bottom(build.GetComponent<RectTransform>(),80,52);if(close!=null)Bottom(close.GetComponent<RectTransform>(),20,52);
    }
}
