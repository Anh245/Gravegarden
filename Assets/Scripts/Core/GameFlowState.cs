public enum GardenEntryMode { None, NewGame, ContinueCheckpoint }

// Chỉ giữ ý định chuyển scene, không giữ ví/công trình/quái sống.
public sealed class GameFlowState
{
    public bool IsTransitioning { get; private set; }
    private GardenEntryMode pending;
    private string message;

    public bool TryBegin(GardenEntryMode entry, string nextMessage = "")
    {
        if (IsTransitioning) return false;
        IsTransitioning = true;
        pending = entry;
        message = nextMessage ?? "";
        return true;
    }
    public void Arrived() => IsTransitioning = false;
    public void Cancel(string error)
    {
        IsTransitioning = false;
        pending = GardenEntryMode.None;
        message = error ?? "";
    }
    public GardenEntryMode ConsumeEntry()
    {
        GardenEntryMode result = pending;
        pending = GardenEntryMode.None;
        return result;
    }
    public string ConsumeMessage()
    {
        string result = message ?? "";
        message = "";
        return result;
    }
}
