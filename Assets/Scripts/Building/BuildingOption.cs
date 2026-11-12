using System;
using UnityEngine;

[Serializable]
public class BuildingOption
{
    [Tooltip("Mã lưu cố định, ví dụ bone_lily. Dùng chữ thường, số, dấu _. Không đổi sau khi đã lưu.")]
    [SerializeField] private string saveId;
    public string SaveId => saveId;
    [Tooltip("Ảnh trên thẻ cửa hàng; để trống sẽ lấy SpriteRenderer đầu tiên của prefab.")]
    [SerializeField] private Sprite shopIcon;
    public Sprite ShopIcon
    {
        get
        {
            if(shopIcon!=null)return shopIcon;
            GameObject prefab=IsProducer?producer.gameObject:IsSupport?support.gameObject:tower!=null?tower.gameObject:null;
            SpriteRenderer visual=prefab!=null?prefab.GetComponentInChildren<SpriteRenderer>(true):null;
            return visual!=null?visual.sprite:null;
        }
    }
    [Tooltip("Chỉ gán đúng một trong ba loại: Tower, Producer, Support.")]
    [SerializeField] private BoneLilyTower tower;
    [SerializeField] private ProducerBuilding producer;
    [SerializeField] private SupportBuilding support;

    public BoneLilyTower Tower => tower;
    public ProducerBuilding Producer => producer;
    public SupportBuilding Support => support;
    public bool IsProducer => producer != null;
    public bool IsSupport => support != null;
    public int FootprintCells => IsProducer ? producer.Definition.FootprintCells
        : IsSupport ? support.Definition.FootprintCells : tower.Definition.FootprintCells;

    public bool IsValid
    {
        get
        {
            int count = (tower != null ? 1 : 0) + (producer != null ? 1 : 0)
                + (support != null ? 1 : 0);
            if (count != 1)
                return false;
            if (tower != null)
                return tower.gameObject.activeSelf && tower.enabled
                    && tower.Definition != null && tower.Definition.IsValid;
            if (producer != null)
                return producer.gameObject.activeSelf && producer.enabled
                    && producer.Definition != null && producer.Definition.IsValid;
            return support.gameObject.activeSelf && support.enabled
                && support.Definition != null && support.Definition.IsValid;
        }
    }

    public string DisplayName => IsProducer ? producer.Definition.DisplayName
        : IsSupport ? support.Definition.DisplayName : tower.Definition.DisplayName;
    public int BaseCost => IsProducer ? producer.Definition.BaseBuildCost
        : IsSupport ? support.Definition.BuildCost : tower.Definition.BuildCost;
    public int UnlockWave => IsProducer ? producer.Definition.UnlockWave
        : IsSupport ? support.Definition.UnlockWave : tower.Definition.UnlockWave;
}
