using System;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static partial class UICompletionSetup
{
    private static Button IconButton(Transform parent,string name,Sprite glyph)
    {
        var rect=Rect(parent,name);var image=GetOrAdd<Image>(rect.gameObject);Record(image);
        image.sprite=FinalUISkin.UI("icon_normal");image.type=Image.Type.Sliced;image.pixelsPerUnitMultiplier=4;image.color=Color.white;image.raycastTarget=true;
        var button=GetOrAdd<Button>(rect.gameObject);Record(button);button.targetGraphic=image;button.transition=Selectable.Transition.SpriteSwap;
        var state=button.spriteState;state.highlightedSprite=FinalUISkin.UI("icon_selected");state.selectedSprite=state.highlightedSprite;
        state.pressedSprite=FinalUISkin.UI("icon_pressed");state.disabledSprite=FinalUISkin.UI("icon_disabled");button.spriteState=state;
        var icon=Picture(rect,"FinalUI_Glyph",glyph);icon.preserveAspect=true;Stretch(icon.rectTransform,3,3,3,3);return button;
    }
    private static void AudioPanel(AudioSettingsUI owner)
    {
        var ownerData=new SerializedObject(owner);
        var musicSlider=Ref<Slider>(ownerData,"musicSlider");var effectsSlider=Ref<Slider>(ownerData,"effectsSlider");
        var texts=owner.GetComponentsInChildren<TMP_Text>(true)
            .Concat(musicSlider!=null?musicSlider.transform.parent.GetComponentsInChildren<TMP_Text>(true):Array.Empty<TMP_Text>())
            .Concat(effectsSlider!=null?effectsSlider.transform.parent.GetComponentsInChildren<TMP_Text>(true):Array.Empty<TMP_Text>()).Distinct().ToArray();
        var music=texts.FirstOrDefault(t=>t.gameObject.name.Trim()=="MusicLabel" || t.text.Trim()=="Nhạc nền");
        var effects=texts.FirstOrDefault(t=>t.gameObject.name.Trim()=="EffectsLabel" || t.text.Trim()=="Hiệu ứng");
        if(music==null || effects==null)
        {
            var slider=Ref<Slider>(ownerData,"musicSlider");var parent=slider!=null?slider.transform.parent:owner.transform;
            if(music==null){var t=Text(parent,"FinalUI_MusicLabel",TMP_Settings.defaultFontAsset,23);t.text="Nhạc nền";Top(t.rectTransform,20,90,210,45);music=t;}
            if(effects==null){var t=Text(parent,"FinalUI_EffectsLabel",music.font,23);t.text="Hiệu ứng";Top(t.rectTransform,20,150,210,45);effects=t;}
        }
        var mb=IconButton(music.transform,"FinalUI_MusicToggle",FinalUISkin.UI("music_on"));
        var eb=IconButton(effects.transform,"FinalUI_EffectsToggle",FinalUISkin.UI("sound_on"));
        foreach(var pair in new[]{(button:mb,label:music),(button:eb,label:effects)})
        {
            var rt=pair.button.GetComponent<RectTransform>();Record(rt);rt.anchorMin=rt.anchorMax=rt.pivot=new Vector2(0,.5f);rt.anchoredPosition=Vector2.zero;rt.sizeDelta=new Vector2(38,38);
            Record(pair.label);var margin=pair.label.margin;margin.x=Mathf.Max(46,margin.x);pair.label.margin=margin;pair.label.raycastTarget=false;
        }
        var presenter=GetOrAdd<AudioIconPresenter>(owner.gameObject);Record(presenter);var data=new SerializedObject(presenter);
        Assign(data,"owner",owner);Assign(data,"musicButton",mb);Assign(data,"effectsButton",eb);
        Assign(data,"musicIcon",mb.transform.Find("FinalUI_Glyph").GetComponent<Image>());Assign(data,"effectsIcon",eb.transform.Find("FinalUI_Glyph").GetComponent<Image>());
        Assign(data,"musicOn",FinalUISkin.UI("music_on"));Assign(data,"musicOff",FinalUISkin.UI("music_off"));Assign(data,"effectsOn",FinalUISkin.UI("sound_on"));Assign(data,"effectsOff",FinalUISkin.UI("sound_off"));data.ApplyModifiedProperties();
    }
    private static void Health(TownHallHealthView owner)
    {
        var data=new SerializedObject(owner);var slider=Ref<Slider>(data,"healthBar");if(slider==null)return;
        var background=slider.transform.Find("Background");if(background!=null)
        {
            var image=background.GetComponent<Image>();if(image!=null){Record(image);image.sprite=FinalUISkin.UI("heart_track");image.type=Image.Type.Sliced;image.color=Color.white;image.raycastTarget=false;}
            Stretch(background.GetComponent<RectTransform>(),48,0,0,0);
        }
        if(slider.fillRect!=null)
        {
            var image=slider.fillRect.GetComponent<Image>();if(image!=null){Record(image);image.sprite=FinalUISkin.UI("heart_fill");image.type=Image.Type.Sliced;image.color=Color.white;image.raycastTarget=false;}
            if(slider.fillRect.parent is RectTransform area && area!=slider.transform)Stretch(area,52,5,5,5);
            else
            {
                var fillArea=Rect(slider.transform,"FinalUI_FillArea");Stretch(fillArea,52,5,5,5);
                Undo.SetTransformParent(slider.fillRect,fillArea,"Căn phần máu");Stretch(slider.fillRect,0,0,0,0);
            }
        }
        var icon=Picture(slider.transform,"FinalUI_Heart",FinalUISkin.Icon("integrity"));icon.preserveAspect=true;
        var rt=icon.rectTransform;Record(rt);rt.anchorMin=rt.anchorMax=rt.pivot=new Vector2(0,.5f);rt.anchoredPosition=new Vector2(6,0);rt.sizeDelta=new Vector2(36,36);
        var value=Ref<TMP_Text>(data,"valueText");if(value!=null)
        {
            if(value.transform.parent!=slider.transform)Undo.SetTransformParent(value.transform,slider.transform,"Căn chữ thanh máu");
            Stretch(value.rectTransform,48,0,0,0);Record(value);value.raycastTarget=false;
        }
    }
    private static void Wallet(WalletView owner)
    {
        var data=new SerializedObject(owner);var text=Ref<TMP_Text>(data,"balanceText");if(text==null)return;
        if(text.transform.parent!=null && text.transform.parent.name=="WalletFrame")
        {
            var frame=text.transform.parent.GetComponent<Image>();if(frame!=null){Record(frame);frame.sprite=FinalUISkin.UI("wallet_capsule");frame.color=Color.white;}
            var image=Picture(text.transform.parent,"FinalUI_Coin",FinalUISkin.Icon("gravecoin"));image.preserveAspect=true;
            var rt=image.rectTransform;Record(rt);rt.anchorMin=rt.anchorMax=rt.pivot=new Vector2(0,.5f);rt.anchoredPosition=new Vector2(12,0);rt.sizeDelta=new Vector2(64,64);
        }
        else Inline(text,FinalUISkin.Icon("gravecoin"),"FinalUI_Coin",36);
    }
    private static Sprite Character(string name)
    {
        var sprites=AssetDatabase.LoadAllAssetsAtPath("Assets/Art/Characters/"+name+".png").OfType<Sprite>().ToArray();
        if(sprites.Length==0)throw new InvalidOperationException("Thiếu sprite nhân vật "+name);return sprites[0];
    }
    private static void Mara(GardenTutorialController owner)
    {
        var data=new SerializedObject(owner);var panel=Ref<GameObject>(data,"panel");if(panel==null)return;
        var image=GetOrAdd<Image>(panel);Record(image);image.sprite=null;image.color=Color.clear;image.raycastTarget=true;
        var root=panel.GetComponent<RectTransform>();Record(root);root.sizeDelta=new Vector2(900,350);
        var bubble=Picture(panel.transform,"FinalUI_MaraSpeech",FinalUISkin.UI("mara_speech_bubble"));bubble.type=Image.Type.Sliced;bubble.pixelsPerUnitMultiplier=3;
        Stretch(bubble.rectTransform,180,84,12,12);Record(bubble.transform);bubble.transform.SetAsFirstSibling();
        var frame=Picture(panel.transform,"FinalUI_MaraFrame",FinalUISkin.UI("icon_normal"));frame.type=Image.Type.Sliced;frame.pixelsPerUnitMultiplier=4;
        var fr=frame.rectTransform;Record(fr);fr.anchorMin=fr.anchorMax=fr.pivot=new Vector2(0,.5f);fr.anchoredPosition=new Vector2(12,16);fr.sizeDelta=new Vector2(164,164);
        var portrait=Picture(panel.transform,"MaraPortrait",Character("mara_welcome"));portrait.preserveAspect=true;
        var pr=portrait.rectTransform;Record(pr);pr.anchorMin=pr.anchorMax=pr.pivot=new Vector2(0,.5f);pr.anchoredPosition=new Vector2(20,16);pr.sizeDelta=new Vector2(148,148);
        Record(frame.transform);frame.transform.SetSiblingIndex(1);Record(portrait.transform);portrait.transform.SetSiblingIndex(2);
        var title=Ref<TMP_Text>(data,"title");var body=Ref<TMP_Text>(data,"body");
        if(title!=null){Record(title);title.color=new Color(.2f,.08f,.13f,1);Top(title.rectTransform,252,30,620,46);title.enableAutoSizing=true;title.fontSizeMin=20;title.fontSizeMax=27;}
        if(body!=null){Record(body);body.color=new Color(.2f,.08f,.13f,1);Stretch(body.rectTransform,252,112,28,84);body.enableAutoSizing=true;body.fontSizeMin=18;body.fontSizeMax=23;}
        Primary(Ref<Button>(data,"actionButton"),FinalUISkin.UI("confirm_button"));Primary(Ref<Button>(data,"skipButton"),FinalUISkin.UI("cancel_button"));
        var presenter=GetOrAdd<MaraPortraitPresenter>(panel);Record(presenter);var view=new SerializedObject(presenter);
        Assign(view,"tutorial",owner);Assign(view,"portrait",portrait);Assign(view,"welcome",Character("mara_welcome"));Assign(view,"concerned",Character("mara_concerned"));Assign(view,"sleepy",Character("mara_sleepy"));Assign(view,"proud",Character("mara_proud"));view.ApplyModifiedProperties();
    }
    private static void Result(GameObject panel,bool victory,TMP_FontAsset font)
    {
        if(panel==null)return;Frame(panel,"modal_panel");panel=Surface(panel);
        var banner=Picture(panel.transform,"FinalUI_ResultBanner",FinalUISkin.UI(victory?"victory_banner":"defeat_banner"));banner.preserveAspect=true;
        var rt=banner.rectTransform;Record(rt);rt.anchorMin=rt.anchorMax=rt.pivot=new Vector2(.5f,1);rt.anchoredPosition=new Vector2(0,-24);rt.sizeDelta=new Vector2(420,90);
        var title=panel.GetComponentsInChildren<TMP_Text>(true).FirstOrDefault(t=>(t.name=="TitleText" || t.name=="Title") && t.GetComponentInParent<Button>()==null);
        if(title==null)title=Text(panel.transform,"FinalUI_ResultTitle",font,32);
        Record(title);title.text=victory?"Chiến thắng!":"Thất bại";title.alignment=TextAlignmentOptions.Center;
        var tr=title.rectTransform;Record(tr);tr.anchorMin=new Vector2(0,1);tr.anchorMax=new Vector2(1,1);tr.pivot=new Vector2(.5f,1);tr.offsetMin=new Vector2(24,-172);tr.offsetMax=new Vector2(-24,-120);
    }
    private static void Boss(WaveDirector waves,Canvas canvas,TMP_FontAsset font)
    {
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemies/GodotChapter1/OssuaryGiant_Chapter1.prefab");
        if(prefab==null || prefab.GetComponent<EnemyHealth>()==null)throw new InvalidOperationException("Thiếu prefab boss chương1.");
        var rect=Rect(canvas.transform,"FinalUI_BossHUD");Record(rect);rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(.5f,1);rect.anchoredPosition=new Vector2(0,-205);rect.sizeDelta=new Vector2(720,88);
        var backdrop=GetOrAdd<Image>(rect.gameObject);Record(backdrop);backdrop.sprite=FinalUISkin.UI("building_panel");backdrop.type=Image.Type.Sliced;backdrop.raycastTarget=false;backdrop.color=Color.white;backdrop.pixelsPerUnitMultiplier=2;
        var label=Text(rect,"FinalUI_BossLabel",font,24);Top(label.rectTransform,24,10,672,30);label.alignment=TextAlignmentOptions.Center;label.text="Boss";
        var track=Picture(rect,"FinalUI_BossTrack",FinalUISkin.UI("heart_track"));Stretch(track.rectTransform,24,15,24,48);
        var fill=Picture(track.transform,"FinalUI_BossFill",FinalUISkin.UI("boss_fill"));Stretch(fill.rectTransform,0,0,0,0);fill.type=Image.Type.Filled;fill.fillMethod=Image.FillMethod.Horizontal;fill.fillOrigin=0;fill.fillAmount=1;
        var presenter=GetOrAdd<BossHealthPresenter>(waves.gameObject);Record(presenter);var data=new SerializedObject(presenter);
        Assign(data,"bossDefinition",prefab.GetComponent<EnemyHealth>().Definition);Assign(data,"waves",waves);Assign(data,"panel",rect.gameObject);Assign(data,"fill",fill);Assign(data,"label",label);data.ApplyModifiedProperties();
        Record(rect.gameObject);rect.gameObject.SetActive(false);
    }
}
