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

    public ProducerDefinition Definition => definition;

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

        Level++;

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
        capacity = definition.Capacity;

        storedAmount = 0;
        initialized = true;

        RefreshDisplay();
    }

    private void Update()
    {
        if (!initialized)
            return;

        if (CanOperate)
        {
            storedAmount = System.Math.Min(
                capacity,
                storedAmount +
                productionPerSecond * Time.deltaTime
            );
        }

        RefreshDisplay();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
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

    public void Collect()
    {
        Debug.Log(
        $"Thu hoạch | initialized={initialized} | " +
        $"CanOperate={CanOperate} | " +
        $"Tiền có thể thu={CollectableAmount}",
        this
    );

        if (!CanOperate)
        {
            Debug.LogWarning(
                "Chưa thể thu: kiểm tra tham chiếu, " +
                "trạng thái trận và thời gian game.",
                this
            );

            return;
        }

        int amount = CollectableAmount;

        if (amount <= 0)
        {
            Debug.Log("Kho chưa đủ 1 tiền để thu.", this);
            return;
        }

        int balanceBefore = wallet.Banlance;

        storedAmount -= amount;
        wallet.AddReward(amount);

        RefreshDisplay();

        Debug.Log(
            $"Đã thu {amount} tiền | " +
            $"Ví {wallet.name}: {balanceBefore} → {wallet.Banlance}",
            this
        );
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