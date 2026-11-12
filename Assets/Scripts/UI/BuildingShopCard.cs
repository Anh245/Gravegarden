using System;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class BuildingShopSprites
{
    public Sprite affordable,unaffordable,locked,selected,placed,tray;
    public bool IsReady=>affordable!=null && unaffordable!=null && locked!=null
        && selected!=null && placed!=null && tray!=null;
    public Sprite Get(ShopCardState state)
    {
        switch(state)
        {
            case ShopCardState.Locked:return locked;
            case ShopCardState.Unaffordable:return unaffordable;
            case ShopCardState.Selected:return selected;
            case ShopCardState.Placed:return placed;
            default:return affordable;
        }
    }
}

public class BuildingShopCard : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private Image background;
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text titleText,roleText,priceText,stateText;
    private BuildingShopController owner;
    private int index;
    private static readonly CultureInfo Vietnamese=CultureInfo.GetCultureInfo("vi-VN");
    public bool IsConfigured=>button!=null && background!=null && icon!=null && titleText!=null
        && roleText!=null && priceText!=null && stateText!=null;
    public void Bind(BuildingShopController controller,int optionIndex)
    {
        owner=controller;index=optionIndex;
        button.transition=Selectable.Transition.None;
        button.onClick.AddListener(Clicked);
    }
    public void Render(BuildingOption option,BuildingShopSprites skins,int price,bool unlocked,
        int owned,bool affordable,bool selected,bool justPlaced,bool canInteract,bool tutorialLocked=false)
    {
        ShopCardState state=ShopCardPolicy.State(unlocked,affordable,selected,justPlaced);
        background.sprite=skins.Get(state);background.color=Color.white;
        titleText.text=option.DisplayName;
        roleText.text=(option.IsProducer?"THU NHẬP":option.IsSupport?"HỖ TRỢ":"PHÒNG THỦ")+" | Có: "+owned;
        Sprite picture=option.ShopIcon;
        icon.sprite=picture;icon.enabled=picture!=null;
        icon.color=unlocked?Color.white:new Color(0.45f,0.45f,0.5f,1f);
        priceText.text=tutorialLocked?"Hướng dẫn":unlocked?price.ToString("N0",Vietnamese):"Đợt "+option.UnlockWave;
        priceText.color=unlocked && !affordable?new Color(0.91f,0.61f,0.64f):new Color(0.95f,0.84f,0.6f);
        stateText.text=tutorialLocked?"Theo bước của Mara":state==ShopCardState.Locked?"Chưa mở khóa":state==ShopCardState.Placed?"Đã xây"
            :state==ShopCardState.Unaffordable?"Chưa đủ xu":selected?"Đang chọn":"Chọn công trình";
        button.interactable=canInteract && unlocked;
    }
    private void Clicked(){if(owner!=null)owner.SelectCard(index);}
    private void OnDestroy(){if(button!=null)button.onClick.RemoveListener(Clicked);}
}
