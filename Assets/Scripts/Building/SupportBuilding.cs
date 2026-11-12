using System;
using UnityEngine;
using UnityEngine.EventSystems;

[DisallowMultipleComponent]
[RequireComponent(typeof(BoxCollider2D))]
public class SupportBuilding : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private SupportDefinition definition;
    [Header("Tham chiếu trong scene - BuildManager truyền khi xây")]
    [SerializeField] private PlayerWallet wallet;
    [SerializeField] private TownHallHealth defendedHall;
    [SerializeField] private WaveDirector waveDirector;

    public static event Action<SupportBuilding> UpgradeRequested;
    public SupportDefinition Definition => definition;
    public float CurrentEffectiveRange => (definition!=null ? definition.Range : 0f) + GetFootprintRadius();
    public int Level { get; private set; } = 1;
    public float CurrentBonus => definition != null ? definition.GetBonusAtLevel(Level) : 0f;
    public bool IsMaxLevel => definition != null && Level >= definition.MaxLevel;
    public int NextUpgradeCost => definition != null ? definition.GetUpgradeCost(Level) : 0;

    private bool initialized;
    public bool CheckpointReady => initialized && isActiveAndEnabled;
    public bool RestoreLevel(int savedLevel)
    {
        if (definition == null || !definition.IsValid || savedLevel < 1 || savedLevel > definition.MaxLevel)
            return false;
        Level = savedLevel;
        return true;
    }
    public bool CanOperate => initialized && isActiveAndEnabled
        && definition != null && definition.IsValid
        && defendedHall != null && !defendedHall.IsDestroyed
        && waveDirector != null && waveDirector.CanBuildNow && Time.timeScale > 0f;
    public bool CanUpgradeNow => CanOperate && wallet != null && !IsMaxLevel;

    public void Configure(PlayerWallet newWallet, TownHallHealth newHall, WaveDirector newWaveDirector)
    {
        wallet = newWallet;
        defendedHall = newHall;
        waveDirector = newWaveDirector;
    }

    private void Start()
    {
        if (definition == null || !definition.IsValid || wallet == null
            || defendedHall == null || waveDirector == null)
        {
            Debug.LogError("SupportBuilding: thiếu dữ liệu hoặc tham chiếu trong scene.", this);
            enabled = false;
            return;
        }
        initialized = true;
    }

    public bool TryUpgrade()
    {
        int cost=NextUpgradeCost;
        if (!CanUpgradeNow || !wallet.TrySpend(cost))
            return false;
        GetComponent<BuildingPlacement>()?.RecordAdditionalInvestment(cost);
        Level++;
        GameAudio.Play(GameSound.Upgrade);
        return true;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if(BuildingMoveController.BlocksWorldClick(gameObject)) return;
        if (eventData.button == PointerEventData.InputButton.Left && CanOperate)
            UpgradeRequested?.Invoke(this);
    }

    // Chỉ gọi theo nhịp kiểm tra của tháp, không quét toàn scene mỗi frame.
    public static float GetStrongestBonusAt(Vector2 position, TownHallHealth hall)
    {
        float strongest = 0f;
        foreach (SupportBuilding support in UnityEngine.Object.FindObjectsByType<SupportBuilding>())
        {
            if (!support.CanOperate || support.defendedHall != hall)
                continue;

            Vector2 offset = (Vector2)support.transform.position - position;
            float radius = support.CurrentEffectiveRange;
            if (offset.sqrMagnitude <= radius * radius)
                strongest = Mathf.Max(strongest, support.CurrentBonus);
        }
        return strongest;
    }

    private float GetFootprintRadius()
    {
        BuildingPlacement placement = GetComponent<BuildingPlacement>();
        return placement != null && placement.IsPlaced ? placement.HalfSide
            : definition != null ? definition.Footprint * 0.64f : 0f;
    }

    private void OnDrawGizmosSelected()
    {
        if (definition == null || !definition.IsValid)
            return;
        Gizmos.color = new Color(0.7f, 0.4f, 1f);
        Gizmos.DrawWireSphere(transform.position, definition.Range + GetFootprintRadius());
    }
}
