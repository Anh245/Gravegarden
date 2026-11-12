using System;
using System.Collections.Generic;
using UnityEngine;

// Một nguồn dữ liệu cho tuyến, mục tiêu tấn công và điều kiện thất bại.
public class DistrictDefenseRoutes : MonoBehaviour
{
    [Serializable]
    public struct Binding
    {
        public EnemyRoute route;
        public TownHallHealth hall;
        public Binding(EnemyRoute route, TownHallHealth hall)
        { this.route = route; this.hall = hall; }
    }

    [SerializeField] private Binding[] bindings = new Binding[0];
    public Binding[] ConfiguredBindings => bindings == null ? new Binding[0] : (Binding[])bindings.Clone();

    public bool HasDestroyedActiveHall
    {
        get
        {
            if (bindings == null) return false;
            foreach (Binding item in bindings)
                if (item.hall != null && item.hall.gameObject.activeInHierarchy && item.hall.IsDestroyed)
                    return true;
            return false;
        }
    }

    public bool HasAdditionalActiveHall(TownHallHealth primary)
    {
        if (bindings == null) return false;
        foreach (Binding item in bindings)
            if (item.hall != null && item.hall != primary && item.hall.gameObject.activeInHierarchy) return true;
        return false;
    }

    public bool TryGetActiveBindings(TownHallHealth primary, out Binding[] active, out string error)
    {
        active = null; error = "";
        if (!isActiveAndEnabled) { error = "DistrictDefenseRoutes đang tắt."; return false; }
        if (primary == null || !primary.isActiveAndEnabled)
        { error = "Nhà chính ban đầu đang thiếu hoặc bị tắt."; return false; }
        if (bindings == null || bindings.Length == 0)
        { error = "Bảng tuyến → nhà chính đang trống."; return false; }
        var seen = new HashSet<EnemyRoute>();
        var result = new List<Binding>();
        bool foundPrimary = false;
        foreach (Binding item in bindings)
        {
            if (item.route == null || item.hall == null || !item.route.IsValid)
            { error = "Có dòng thiếu tuyến/nhà chính hoặc tuyến chưa đủ điểm."; return false; }
            if (item.route.gameObject.scene != gameObject.scene || item.hall.gameObject.scene != gameObject.scene)
            { error = "Tuyến và nhà chính phải thuộc cùng scene với DistrictDefenseRoutes."; return false; }
            if (!seen.Add(item.route))
            { error = "Một tuyến bị gán nhiều lần: " + item.route.name; return false; }
            if (!item.hall.gameObject.activeInHierarchy) continue;
            if (!item.hall.enabled || !item.route.isActiveAndEnabled)
            { error = "Nhà đang bật nhưng component máu hoặc tuyến đang tắt: " + item.hall.name; return false; }
            if (item.hall == primary) foundPrimary = true;
            result.Add(item);
        }
        if (!foundPrimary || result.Count == 0)
        { error = "Phải có tuyến đang hoạt động bảo vệ nhà chính ban đầu."; return false; }
        active = result.ToArray();
        return true;
    }

    public bool MatchesCurrentBindings(TownHallHealth primary, Binding[] snapshot)
    {
        if (!isActiveAndEnabled || primary == null || !primary.isActiveAndEnabled
            || bindings == null || snapshot == null) return false;
        int index = 0;
        foreach (Binding item in bindings)
        {
            if (item.route == null || item.hall == null) return false;
            if (!item.hall.gameObject.activeInHierarchy) continue;
            if (!item.hall.enabled || !item.route.isActiveAndEnabled || !item.route.IsValid
                || index >= snapshot.Length || item.route != snapshot[index].route || item.hall != snapshot[index].hall)
                return false;
            index++;
        }
        return index == snapshot.Length;
    }
}
