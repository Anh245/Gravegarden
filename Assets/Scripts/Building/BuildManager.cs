using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BuildManager : MonoBehaviour
{
    [Header("Hệ thống trong scene")]
    [SerializeField] private PlayerWallet wallet;
    [SerializeField] private TownHallHealth defendedHall;
    [SerializeField] private Transform buildingsParent;

    [Header("Công trình")]
    [SerializeField] private BoneLilyTower boneLilyPrefab;

    [SerializeField, Min(1)]
    private int boneLilyCost = 150;

    [Header("Giao diện")]
    [SerializeField] private GameObject buildPanel;
    [SerializeField] private TMP_Text infoText;
    [SerializeField] private Button buildButton;
    [SerializeField] private Button closeButton;

    private BuildSlot selectedSlot;
    private bool initialized;

    

    private void Start()
    {
        if (wallet == null ||
            defendedHall == null ||
            buildingsParent == null ||
            boneLilyPrefab == null ||
            buildPanel == null ||
            infoText == null ||
            buildButton == null ||
            closeButton == null)
        {
            Debug.LogError(
                "BuildManager chưa được gán đủ tham chiếu.",
                this
            );

            enabled = false;
            return;
        }

        if (boneLilyCost <= 0 ||
            !boneLilyPrefab.gameObject.activeSelf ||
            !boneLilyPrefab.enabled ||
            !buildingsParent.gameObject.activeInHierarchy)
        {
            Debug.LogError(
                "Kiểm tra giá xây, prefab Bone Lily và Buildings.",
                this
            );

            enabled = false;
            return;
        }

        buildButton.onClick.AddListener(BuildSelectedSlot);
        closeButton.onClick.AddListener(ClosePanel);

        initialized = true;
        ClosePanel();
    }

    private void Update()
    {
        if (!initialized)
            return;

        if (defendedHall.IsDestroyed)
        {
            ClosePanel();
            return;
        }

        if (!buildPanel.activeSelf)
            return;

        if (selectedSlot == null || selectedSlot.IsOccupied)
        {
            ClosePanel();
            return;
        }

        RefreshPanel();
    }

    public void SelectSlot(BuildSlot slot)
    {
        if (!initialized ||
            defendedHall.IsDestroyed ||
            slot == null ||
            slot.IsOccupied)
        {
            return;
        }

        selectedSlot = slot;
        buildPanel.SetActive(true);
        RefreshPanel();
    }

    private void RefreshPanel()
    {
        bool canAfford = wallet.Banlance >= boneLilyCost;

        infoText.text =
            $"Giá: {boneLilyCost} tiền\n" +
            $"Bạn có: {wallet.Banlance} tiền\n" +
            (canAfford
                ? "Có thể xây."
                : "Không đủ tiền.");

        buildButton.interactable =
            canAfford &&
            selectedSlot != null &&
            !selectedSlot.IsOccupied &&
            !defendedHall.IsDestroyed;
    }

    private void BuildSelectedSlot()
    {
        if (!initialized ||
            defendedHall.IsDestroyed ||
            selectedSlot == null ||
            selectedSlot.IsOccupied)
        {
            return;
        }

        // Kiểm tra tiền lại ngay khi người chơi xác nhận.
        if (!wallet.TrySpend(boneLilyCost))
        {
            RefreshPanel();
            return;
        }

        BoneLilyTower tower = Instantiate(
            boneLilyPrefab,
            selectedSlot.BuildPosition,
            Quaternion.identity,
            buildingsParent
        );

        tower.Configure(defendedHall);

        selectedSlot.MarkOccupied();

        ClosePanel();
    }

    private void ClosePanel()
    {
        selectedSlot = null;
        buildPanel.SetActive(false);
    }

    private void OnDestroy()
    {
        if (buildButton != null)
        {
            buildButton.onClick.RemoveListener(
                BuildSelectedSlot
            );
        }

        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(
                ClosePanel
            );
        }
    }
    
    //Dung xay dung cong trinh khi ket thuc dot
    public void StopBuilding()
    {
        initialized = false;

        buildButton.interactable = false;

        ClosePanel();
    }
}