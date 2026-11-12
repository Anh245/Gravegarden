using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(BoxCollider2D))]
public class ProducerBuilding : MonoBehaviour, IPointerClickHandler
{
    [Header("Dữ liệu")]
    [SerializeField] private ProducerDefinition definition;

    [Header("Hiển thị")]
    [SerializeField] private TMP_Text storageText;

    [Header("Tham chiếu trong scene")]
    [SerializeField] private PlayerWallet wallet;
    [SerializeField] private TownHallHealth defendedHall;
    [SerializeField] private WaveDirector waveDirector;

    private double storedAmount;
    private double productionPerSecond;
    private int capacity;
    private string displayName;
    private bool initialized;
    private bool hasRestoredProgress;
    public bool CheckpointReady => initialized && isActiveAndEnabled;
    public double StoredAmount => storedAmount;
    public bool RestoreProgress(int savedLevel, double savedStoredAmount)
    {
        if (definition == null || !definition.IsValid || storageText == null
            || savedLevel < 1 || savedLevel > definition.MaxLevel
            || double.IsNaN(savedStoredAmount) || double.IsInfinity(savedStoredAmount)
            || savedStoredAmount < 0d || savedStoredAmount > definition.GetCapacityAtLevel(savedLevel))
            return false;
        Level = savedLevel;
        storedAmount = savedStoredAmount;
        hasRestoredProgress = true;
        if (initialized)
        {
            productionPerSecond = definition.GetProductionAtLevel(Level);
            capacity = definition.GetCapacityAtLevel(Level);
            RefreshDisplay();
        }
        return true;
    }

    public ProducerDefinition Definition => definition;
    public bool BelongsTo(PlayerWallet owner,TownHallHealth hall,WaveDirector waves)=>CheckpointReady
        && wallet==owner && defendedHall==hall && waveDirector==waves;

    public int CollectableAmount =>
        (int)System.Math.Floor(storedAmount);

    private bool CanOperate =>
        initialized &&
        wallet != null &&
        defendedHall != null &&
        waveDirector != null &&
        !defendedHall.IsDestroyed &&
        waveDirector.CanBuildNow &&
        Time.timeScale > 0f;

    public void Configure(
        PlayerWallet newWallet,
        TownHallHealth newDefendedHall,
        WaveDirector newWaveDirector)
    {
        wallet = newWallet;
        defendedHall = newDefendedHall;
        waveDirector = newWaveDirector;
    }
    public static event System.Action<ProducerBuilding>
    UpgradeRequested;

    public int Level { get; private set; } = 1;

    public double CurrentProductionPerSecond => productionPerSecond;

    public int CurrentCapacity => capacity;

    public bool IsMaxLevel =>
        definition != null && Level >= definition.MaxLevel;

    public int NextUpgradeCost =>
        definition != null ? definition.GetUpgradeCost(Level) : 0;

    public bool CanUpgradeNow =>
        isActiveAndEnabled
        && CanOperate
        && definition != null
        && !IsMaxLevel;

    public bool TryUpgrade()
    {
        if (!CanUpgradeNow)
            return false;

        int cost = NextUpgradeCost;

        if (cost <= 0 || !wallet.TrySpend(cost))
            return false;

        GetComponent<BuildingPlacement>()?.RecordAdditionalInvestment(cost);
        Level++;
        GameAudio.Play(GameSound.Upgrade);

        productionPerSecond =
            definition.GetProductionAtLevel(Level);

        capacity =
            definition.GetCapacityAtLevel(Level);

        // Giữ nguyên storedAmount để không mất tiền trong kho.

        RefreshDisplay();

        Debug.Log(
            $"{definition.DisplayName} đã lên cấp {Level}. " +
            $"Sản xuất: {productionPerSecond:0.##}/giây. " +
            $"Sức chứa: {capacity}.",
            this);

        return true;
    }

    private void Start()
    {
        if (definition == null || !definition.IsValid)
        {
            Debug.LogError(
                "ProducerBuilding thiếu dữ liệu hợp lệ.",
                this
            );

            enabled = false;
            return;
        }

        if (storageText == null ||
            wallet == null ||
            defendedHall == null ||
            waveDirector == null)
        {
            Debug.LogError(
                "ProducerBuilding chưa được gán đủ tham chiếu.",
                this
            );

            enabled = false;
            return;
        }

        productionPerSecond = definition.GetProductionAtLevel(Level);
        capacity = definition.GetCapacityAtLevel(Level);
        displayName = definition.DisplayName;

        if (!hasRestoredProgress) storedAmount = 0;
        initialized = true;

        RefreshDisplay();
    }

    private void Update()
    {
        if (!initialized)
            return;

        if (CanOperate)
        {
            bool wasFull = storedAmount >= capacity;
            storedAmount = System.Math.Min(
                capacity,
                storedAmount +
                productionPerSecond * Time.deltaTime
            );
            if (!wasFull && storedAmount >= capacity) GameAudio.Play(GameSound.Production);
        }

        RefreshDisplay();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if(BuildingMoveController.BlocksWorldClick(gameObject)) return;
        if (!CanOperate || !isActiveAndEnabled)
            return;

        if (eventData.button == PointerEventData.InputButton.Left)
        {
            Collect();
        }
        else if (
            eventData.button == PointerEventData.InputButton.Right)
        {
            UpgradeRequested?.Invoke(this);
        }
    }

    [ContextMenu("Thu hoạch thử")]

    public void Collect()=>TryCollect(out _);

    public bool TryCollect(out int collected,bool playSound=true)
    {
        collected=0;
        if(!CanOperate || !isActiveAndEnabled) return false;
        int amount=CollectableAmount;
        if(amount<=0 || !wallet.TryAddRewardExact(amount)) return false;
        storedAmount-=amount;
        collected=amount;
        if(playSound) GameAudio.Play(GameSound.Collect);
        RefreshDisplay();
        if(waveDirector!=null)waveDirector.NotifyProducerHarvested(this,collected);
        return true;
    }

    private void RefreshDisplay()
    {
        string hint;

        if (!CanOperate)
        {
            hint = "Đã dừng sản xuất";
        }
        else if (storedAmount >= capacity)
        {
            hint = "Kho đầy — bấm để thu";
        }
        else if (CollectableAmount > 0)
        {
            hint = "Bấm để thu hoạch";
        }
        else
        {
            hint = "Đang sản xuất...";
        }

        storageText.text =
            $"{displayName}\n" +
            $"{CollectableAmount}/{capacity}\n" +
            hint;
    }
}