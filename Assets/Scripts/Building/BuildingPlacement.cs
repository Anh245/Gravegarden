using UnityEngine;

public class BuildingPlacement : MonoBehaviour
{
    private BuildGrid grid;
    private int reservation;
    public int ReservationId => reservation;
    internal void ApplyGridMove(Vector2Int anchor,Vector3 center)
    {
        Anchor=anchor;
        transform.position=center;
    }
    public bool InvestmentRecorded { get; private set; }
    public long InvestedAmount { get; private set; }
    public bool SaleClaimed { get; private set; }
    public void RestoreInvestment(bool recorded,long amount)
    {
        if(!BuildingSaleMath.ValidInvestment(recorded,amount)) throw new System.ArgumentOutOfRangeException(nameof(amount));
        InvestmentRecorded=recorded;InvestedAmount=amount;
    }
    public void RecordAdditionalInvestment(int amount)
    {
        // Không tự đoán giá đã mua của công trình từ checkpoint cũ.
        if(!InvestmentRecorded) return;
        if(amount<=0 || InvestedAmount>BuildingSaleMath.MaxInvestment-amount)
            throw new System.InvalidOperationException("Tiền nâng cấp không hợp lệ.");
        InvestedAmount+=amount;
    }
    public bool TryClaimSale()
    {
        if(SaleClaimed || !IsPlaced) return false;
        SaleClaimed=true;return true;
    }
    public void CancelSaleClaim()=>SaleClaimed=false;
    public void ReleaseReservationNow()
    {
        BuildGrid owner=grid;int id=reservation;
        DetachCheckpointReservation();
        if(owner!=null && id>0) owner.ReleaseReservation(id);
    }
    public string BuildingId { get; private set; }
    public BuildGrid Grid => grid;
    public void DetachCheckpointReservation()
    {
        reservation = 0;
        grid = null;
    }
    public Vector2Int Anchor { get; private set; }
    public int SizeInCells { get; private set; }
    public float CellSize { get; private set; }
    public bool IsPlaced => reservation > 0 && grid != null;
    public float HalfSide => SizeInCells * CellSize * 0.5f;

    public void Initialize(BuildGrid owner, int reservationId, Vector2Int anchor, int size, string buildingId = "", int paid = 0)
    {
        RestoreInvestment(paid>0,paid);
        SaleClaimed=false;
        grid = owner;
        BuildingId = buildingId;
        reservation = reservationId;
        Anchor = anchor;
        SizeInCells = size;
        CellSize = owner.CellSize;
    }

    private void OnDestroy()
    {
        if (grid != null && reservation > 0)
            grid.ReleaseReservation(reservation);
    }
}
