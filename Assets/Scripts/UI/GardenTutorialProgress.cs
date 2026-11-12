public sealed class GardenTutorialProgress
{
    public enum Step { Welcome, Orchard, Lily, Harvest, Ready, Complete }
    public Step Current { get; private set; } = Step.Welcome;
    public bool BlocksWave => Current != Step.Complete;
    public void Start() { if(Current==Step.Welcome)Current=Step.Orchard; }
    public void Placed(bool orchard,bool lily)
    {
        if(Current==Step.Orchard && orchard)Current=Step.Lily;
        else if(Current==Step.Lily && lily)Current=Step.Harvest;
    }
    public void Harvested(int amount) { if(Current==Step.Harvest && amount>0)Current=Step.Ready; }
    public void Confirm() { if(Current==Step.Ready)Current=Step.Complete; }
    public void Skip() { Current=Step.Complete; }
    public bool Allows(bool orchard,bool lily)
    {
        if(Current==Step.Welcome)return false;
        if(Current==Step.Orchard)return orchard;
        if(Current==Step.Lily)return lily;
        return true;
    }
}
