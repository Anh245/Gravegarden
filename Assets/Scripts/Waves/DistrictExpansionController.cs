using UnityEngine;

[DefaultExecutionOrder(-200)]
public class DistrictExpansionController : MonoBehaviour
{
    [SerializeField] private WaveDirector waves;
    [SerializeField] private DistrictDefenseRoutes routes;
    [SerializeField] private PlayerWallet wallet;
    [SerializeField] private BuildGrid grid;
    [SerializeField] private TownHallHealth primaryHall;
    [SerializeField] private TownHallHealth violetHall;
    [SerializeField] private TownHallHealth goldenHall;
    [SerializeField] private GameObject regionsRoot;
    [SerializeField] private GameObject violetHealthBar;
    [SerializeField] private GameObject goldenHealthBar;
    public bool IsExpanded { get; private set; }
    public bool CheckpointStateConsistent => regionsRoot != null && grid != null && primaryHall != null
        && violetHall != null && goldenHall != null && primaryHall.isActiveAndEnabled
        && regionsRoot.activeInHierarchy == IsExpanded && grid.ExpansionUnlocked == IsExpanded
        && violetHall.gameObject.activeInHierarchy == IsExpanded && goldenHall.gameObject.activeInHierarchy == IsExpanded;

    private void Awake()
    {
        // Luôn vào trận mới ở khu đầu; bản lưu nhiều khu sẽ được bổ sung ở mốc sau.
        IsExpanded = false;
        if (regionsRoot != null && !transform.IsChildOf(regionsRoot.transform)) regionsRoot.SetActive(false);
        if (violetHealthBar != null) violetHealthBar.SetActive(false);
        if (goldenHealthBar != null) goldenHealthBar.SetActive(false);
        if (grid != null) grid.SetExpansionUnlocked(false);
    }

    public bool TryValidate(out string error)
    {
        error = null;
        if (!isActiveAndEnabled || waves == null || routes == null || wallet == null || grid == null
            || primaryHall == null || violetHall == null || goldenHall == null || regionsRoot == null
            || violetHealthBar == null || goldenHealthBar == null)
        { error = "DistrictExpansionController: thiếu tham chiếu hoặc component đang tắt."; return false; }
        if (!grid.IsReady || !grid.ExpansionZonesAuthored)
        { error = "Chưa chuẩn bị vùng ô mở rộng trên BuildGrid."; return false; }
        if (primaryHall == violetHall || primaryHall == goldenHall || violetHall == goldenHall
            || primaryHall.MaxHealth <= 0 || violetHall.MaxHealth <= 0 || goldenHall.MaxHealth <= 0)
        { error = "Ba nhà phải độc lập và có Max Health dương."; return false; }
        if (transform.IsChildOf(regionsRoot.transform) || grid.transform.IsChildOf(regionsRoot.transform)
            || primaryHall.transform.IsChildOf(regionsRoot.transform)
            || !violetHall.transform.IsChildOf(regionsRoot.transform) || !goldenHall.transform.IsChildOf(regionsRoot.transform))
        { error = "Root mở rộng chỉ chứa hai khu phụ; Controller, Grid và HeartCrypt phải ở ngoài."; return false; }
        if (violetHealthBar == goldenHealthBar || violetHealthBar == regionsRoot || goldenHealthBar == regionsRoot
            || violetHealthBar.transform.IsChildOf(transform) || goldenHealthBar.transform.IsChildOf(transform)
            || transform.IsChildOf(violetHealthBar.transform) || transform.IsChildOf(goldenHealthBar.transform))
        { error = "Hai thanh máu phải là đối tượng UI riêng, không chứa Controller."; return false; }
        var configured = routes.ConfiguredBindings;
        if (configured.Length != 6) { error = "Cần đúng sáu dòng phân tuyến."; return false; }
        int first = 0, violet = 0, golden = 0;
        foreach (var binding in configured)
        {
            if (binding.route == null || binding.hall == null) { error="Bảng phân tuyến còn ô trống."; return false; }
            if (binding.hall == primaryHall) first++;
            else if (binding.hall == violetHall) violet++;
            else if (binding.hall == goldenHall) golden++;
            else { error="Bảng phân tuyến có nhà ngoài ba nhà đã gán."; return false; }
            if (binding.hall != primaryHall && !binding.route.transform.IsChildOf(regionsRoot.transform))
            { error="Bốn tuyến mới phải nằm trong Root mở rộng."; return false; }
        }
        if (first != 2 || violet != 2 || golden != 2)
        { error="Mỗi nhà phải có đúng hai tuyến."; return false; }
        return routes.TryGetActiveBindings(primaryHall, out _, out error);
    }

    public bool TryUnlock(out string error)
    {
        error = null;
        if (IsExpanded) return true; // Không thưởng hoặc hồi máu lần thứ hai.
        if (!TryValidate(out error)) return false;
        bool previousActive = regionsRoot.activeSelf;
        regionsRoot.SetActive(true); // Awake của nhà phụ chạy trước khi khôi phục máu.
        if (!routes.TryGetActiveBindings(primaryHall, out var active, out error) || active.Length != 6
            || !violetHall.isActiveAndEnabled || !goldenHall.isActiveAndEnabled)
        {
            regionsRoot.SetActive(previousActive);
            if (string.IsNullOrEmpty(error)) error="Hai nhà/tuyến phụ chưa bật đầy đủ.";
            return false;
        }
        if (!wallet.TryAddRewardExact(2000))
        { regionsRoot.SetActive(previousActive); error="Ví không thể nhận đủ 2.000 xu mở rộng."; return false; }
        primaryHall.RestoreCheckpointHealth(primaryHall.MaxHealth);
        violetHall.RestoreCheckpointHealth(violetHall.MaxHealth);
        goldenHall.RestoreCheckpointHealth(goldenHall.MaxHealth);
        grid.SetExpansionUnlocked(true);
        violetHealthBar.SetActive(true);
        goldenHealthBar.SetActive(true);
        IsExpanded = true;
        Debug.Log("Mở rộng hoàn tất: ba khu hoạt động, hồi đầy máu ba nhà, +2.000 xu (một lần).", this);
        return true;
    }

    public System.Collections.Generic.Dictionary<string,int> CheckpointHallLimits()
        => new System.Collections.Generic.Dictionary<string,int> {
            {GardenCheckpointFormat.Heart, primaryHall.MaxHealth},
            {GardenCheckpointFormat.Violet, violetHall.MaxHealth},
            {GardenCheckpointFormat.Golden, goldenHall.MaxHealth} };

    public HallSaveData[] CaptureCheckpointHalls() => new[] {
        new HallSaveData { hallId=GardenCheckpointFormat.Heart, health=primaryHall.CurrentHealth, maxHealth=primaryHall.MaxHealth },
        new HallSaveData { hallId=GardenCheckpointFormat.Violet, health=IsExpanded?violetHall.CurrentHealth:0, maxHealth=violetHall.MaxHealth },
        new HallSaveData { hallId=GardenCheckpointFormat.Golden, health=IsExpanded?goldenHall.CurrentHealth:0, maxHealth=goldenHall.MaxHealth } };

    // Khôi phục trạng thái: không cấp thưởng, không tự hồi đầy máu.
    public bool TryApplyCheckpointState(bool expanded, HallSaveData[] saved, out string error)
    {
        if (!TryValidate(out error)) return false;
        if (saved == null || saved.Length != 3) { error="Checkpoint thiếu ba nhà."; return false; }
        HallSaveData heart=null, violet=null, golden=null;
        foreach(var item in saved)
        {
            if(item==null) {error="Checkpoint có nhà trống.";return false;}
            if(item.hallId==GardenCheckpointFormat.Heart && heart==null) heart=item;
            else if(item.hallId==GardenCheckpointFormat.Violet && violet==null) violet=item;
            else if(item.hallId==GardenCheckpointFormat.Golden && golden==null) golden=item;
            else {error="Checkpoint có mã nhà lạ hoặc trùng.";return false;}
        }
        if(heart==null || violet==null || golden==null || heart.maxHealth!=primaryHall.MaxHealth
            || violet.maxHealth!=violetHall.MaxHealth || golden.maxHealth!=goldenHall.MaxHealth
            || heart.health<=0 || heart.health>heart.maxHealth
            || (expanded && (violet.health<=0 || violet.health>violet.maxHealth || golden.health<=0 || golden.health>golden.maxHealth))
            || (!expanded && (violet.health!=0 || golden.health!=0)))
        {error="Máu ba nhà trong checkpoint không hợp lệ.";return false;}
        bool previous=regionsRoot.activeSelf;
        regionsRoot.SetActive(expanded);
        if(!routes.TryGetActiveBindings(primaryHall,out var active,out error) || active.Length!=(expanded?6:2))
        {regionsRoot.SetActive(previous);if(string.IsNullOrEmpty(error))error="Không bật/tắt đủ tuyến khi tải.";return false;}
        primaryHall.RestoreCheckpointHealth(heart.health);
        if(expanded)
        {
            violetHall.RestoreCheckpointHealth(violet.health);
            goldenHall.RestoreCheckpointHealth(golden.health);
        }
        IsExpanded=expanded;
        grid.SetExpansionUnlocked(expanded);
        violetHealthBar.SetActive(expanded);
        goldenHealthBar.SetActive(expanded);
        return true;
    }

    [ContextMenu("Kiểm tra mở khu (Play, giữa hai đợt)")]
    private void PreviewUnlock()
    {
        if (!Application.isPlaying || waves == null || !waves.IsBetweenWaves || Time.timeScale <= 0)
        { Debug.LogWarning("Chỉ kiểm tra trong Play, khi chưa chạy đợt và không tạm dừng.", this); return; }
        if (!TryUnlock(out string error)) Debug.LogError(error, this);
    }
}
