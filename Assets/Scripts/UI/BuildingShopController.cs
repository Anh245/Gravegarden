using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DefaultExecutionOrder(1300)]
public class BuildingShopController : MonoBehaviour
{
    [SerializeField] private BuildManager buildManager;
    [SerializeField] private BuildModeController buildMode;
    [SerializeField] private GardenSaveController saveController;
    [SerializeField] private GameObject shopPanel;
    [SerializeField] private RectTransform content;
    [SerializeField] private BuildingShopCard cardTemplate;
    [SerializeField] private Button closeButton;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private BuildingShopSprites skins=new BuildingShopSprites();
    private readonly List<BuildingShopCard> cards=new List<BuildingShopCard>();
    private bool initialized;
    private bool oldDropdownVisible;
    private float nextRefresh;
    private bool CanShow=>initialized && isActiveAndEnabled && buildManager.CanUseShop
        && buildMode.IsBuildMode && !saveController.IsOfflineDialogOpen;
    private void Start()
    {
        if(shopPanel!=null)shopPanel.SetActive(false);
        if(buildManager==null || !buildManager.CheckpointReady || buildMode==null || saveController==null
            || shopPanel==null || content==null || cardTemplate==null || !cardTemplate.IsConfigured
            || closeButton==null || statusText==null || skins==null || !skins.IsReady)
        {Debug.LogError("BuildingShopController: công cụ tạo UI chưa gán đủ tham chiếu. Dropdown cũ vẫn được giữ.",this);enabled=false;return;}
        oldDropdownVisible=buildManager.LegacyDropdown.gameObject.activeSelf;
        for(int i=0;i<buildManager.CheckpointOptions.Count;i++)
        {
            BuildingShopCard card=Instantiate(cardTemplate,content);
            card.name="ShopCard_"+buildManager.CheckpointOptions[i].SaveId;
            card.Bind(this,i);card.gameObject.SetActive(true);cards.Add(card);
        }
        cardTemplate.gameObject.SetActive(false);
        buildManager.LegacyDropdown.gameObject.SetActive(false);
        closeButton.onClick.AddListener(CloseShop);
        initialized=true;Refresh();
    }
    private void Update()
    {
        if(!initialized)return;
        if(shopPanel.activeSelf && !CanShow)shopPanel.SetActive(false);
        if(Time.unscaledTime>=nextRefresh)
        {nextRefresh=Time.unscaledTime+0.12f;Refresh();}
    }
    private void Refresh()
    {
        if(!initialized)return;
        bool show=CanShow;
        if(show && !shopPanel.activeSelf)
        {
            CloseUpgradePanels();shopPanel.SetActive(true);shopPanel.transform.SetAsLastSibling();
        }
        else if(!show){shopPanel.SetActive(false);return;}
        int balance=buildManager.CheckpointWallet.Banlance;
        for(int i=0;i<cards.Count;i++)
        {
            if(!buildManager.TryGetShopOffer(i,out int price,out bool unlocked,out int owned))continue;
            bool placed=buildManager.LastPlacedOptionIndex==i && Time.unscaledTime-buildManager.LastPlacementTime<0.65f;
            cards[i].Render(buildManager.CheckpointOptions[i],skins,price,unlocked,owned,balance>=price,
                buildManager.SelectedShopIndex==i,placed,CanShow,
                !buildManager.CheckpointWaves.TutorialAllowsBuilding(buildManager.CheckpointOptions[i]));
        }
        statusText.text=buildManager.HasSelectedBuildSlot
            ?"Chọn thẻ rồi bấm Xây trong bảng bên phải."
            :"Chọn công trình, chọn ô trống trên bản đồ, rồi bấm Xây. Kéo ngang để xem thêm.";
    }
    public void SelectCard(int index)
    {
        if(!CanShow || !buildManager.TrySelectShopOption(index))return;
        GameAudio.Play(GameSound.Accept);Refresh();
    }
    private void CloseShop(){buildMode.ExitBuildMode();shopPanel.SetActive(false);}
    private void CloseUpgradePanels()
    {
        foreach(TowerUpgradeUI ui in Object.FindObjectsByType<TowerUpgradeUI>())if(ui.gameObject.scene==gameObject.scene)ui.ClosePanel();
        foreach(ProducerUpgradeUI ui in Object.FindObjectsByType<ProducerUpgradeUI>())if(ui.gameObject.scene==gameObject.scene)ui.ClosePanel();
        foreach(SupportUpgradeUI ui in Object.FindObjectsByType<SupportUpgradeUI>())if(ui.gameObject.scene==gameObject.scene)ui.ClosePanel();
    }
    private void OnEnable()
    {
        TowerUpgradeClick.TowerClicked+=OnTowerSelected;
        ProducerBuilding.UpgradeRequested+=OnProducerSelected;
        SupportBuilding.UpgradeRequested+=OnSupportSelected;
        if(!initialized)return;
        if(buildManager!=null && buildManager.LegacyDropdown!=null)buildManager.LegacyDropdown.gameObject.SetActive(false);
        Refresh();
    }
    private void OnDisable()
    {
        TowerUpgradeClick.TowerClicked-=OnTowerSelected;
        ProducerBuilding.UpgradeRequested-=OnProducerSelected;
        SupportBuilding.UpgradeRequested-=OnSupportSelected;
        if(shopPanel!=null)shopPanel.SetActive(false);
        if(initialized && buildManager!=null && buildManager.LegacyDropdown!=null)
            buildManager.LegacyDropdown.gameObject.SetActive(oldDropdownVisible);
    }
    private void OnTowerSelected(BoneLilyTower actor)=>ExistingSelected(actor);
    private void OnProducerSelected(ProducerBuilding actor)=>ExistingSelected(actor);
    private void OnSupportSelected(SupportBuilding actor)=>ExistingSelected(actor);
    private void ExistingSelected(Component actor)
    {
        if(initialized && actor!=null && actor.gameObject.scene==gameObject.scene && buildMode.IsBuildMode)CloseShop();
    }
    private void OnDestroy(){if(closeButton!=null)closeButton.onClick.RemoveListener(CloseShop);}
}
