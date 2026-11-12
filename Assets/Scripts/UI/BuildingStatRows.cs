using TMPro;
using UnityEngine;

public class BuildingStatRows : MonoBehaviour
{
    [SerializeField] private TMP_Text titleText, levelText;
    [SerializeField] private TMP_Text[] values = new TMP_Text[3];
    public bool IsConfigured => titleText!=null && levelText!=null && values!=null && values.Length==3 && values[0]!=null && values[1]!=null && values[2]!=null;
    private static string F(double value) => value.ToString("0.##", System.Globalization.CultureInfo.GetCultureInfo("vi-VN"));
    private string Distance(float range, Component actor)
    {
        var placement=actor.GetComponent<BuildingPlacement>();
        return placement!=null && placement.CellSize>0 ? F(range/placement.CellSize)+" ô" : F(range);
    }
    private void Header(string name,int level,int max)
    {titleText.text=name;levelText.text="Cấp "+level+" / "+max;}
    public void ShowTower(BoneLilyTower tower)
    {
        Header(tower.Definition.DisplayName,tower.Level,tower.Definition.MaxLevel);
        values[0].text="Sát thương: "+F(tower.CurrentDamage);
        values[1].text="Chu kỳ bắn: "+F(tower.CurrentAttackInterval)+" giây/lần\nHỗ trợ: +"+F(tower.CurrentSupportBonus*100)+"%";
        values[2].text="Tầm bắn: "+Distance(tower.CurrentEffectiveRange,tower);
    }
    public void ShowProducer(ProducerBuilding producer)
    {
        Header(producer.Definition.DisplayName,producer.Level,producer.Definition.MaxLevel);
        values[0].text="Sản xuất: "+F(producer.CurrentProductionPerSecond)+" xu/giây";
        values[1].text="Có thể thu: "+producer.CollectableAmount+" xu";
        values[2].text="Sức chứa: "+producer.CurrentCapacity+" xu";
    }
    public void ShowSupport(SupportBuilding support)
    {
        Header(support.Definition.DisplayName,support.Level,support.Definition.MaxLevel);
        values[0].text="Tăng tốc đánh: +"+F(support.CurrentBonus*100)+"%";
        values[1].text="Phạm vi: "+Distance(support.CurrentEffectiveRange,support);
        values[2].text="Chỉ áp dụng hiệu ứng mạnh nhất";
    }
}

