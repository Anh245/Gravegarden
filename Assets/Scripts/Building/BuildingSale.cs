using UnityEngine;
public static class BuildingSale
{
    public static bool TryQuote(BuildManager manager,BuildingPlacement placement,
        out int refund,out int stock,out int total,out string error)
    {
        refund=stock=total=0;error="Hiện chưa thể bán công trình.";
        if(manager==null || !manager.CheckpointReady || manager.MoveInputBlocked || SceneNavigator.IsTransitioning || Time.timeScale<=0f
            || manager.CheckpointHall==null || manager.CheckpointHall.IsDestroyed
            || manager.CheckpointWaves==null || !manager.CheckpointWaves.CanBuildNow
            || placement==null || !placement.isActiveAndEnabled || !placement.IsPlaced || placement.SaleClaimed
            || placement.gameObject.scene!=manager.gameObject.scene
            || placement.Grid!=manager.CheckpointGrid || !placement.Grid.IsReady
            || !placement.transform.IsChildOf(manager.CheckpointBuildingsParent)) return false;
        BuildingOption option=null;
        foreach(BuildingOption item in manager.CheckpointOptions)
            if(item.SaveId==placement.BuildingId) {option=item;break;}
        if(option==null || !option.IsValid) return false;
        var tower=placement.GetComponent<BoneLilyTower>();
        var producer=placement.GetComponent<ProducerBuilding>();
        var support=placement.GetComponent<SupportBuilding>();
        if((tower!=null?1:0)+(producer!=null?1:0)+(support!=null?1:0)!=1) return false;
        double stored=0d;
        if(option.IsProducer && producer!=null && producer.CheckpointReady && producer.Definition==option.Producer.Definition)
            stored=producer.StoredAmount;
        else if(option.IsSupport && support!=null && support.CheckpointReady && support.Definition==option.Support.Definition) {}
        else if(!option.IsProducer && !option.IsSupport && tower!=null && tower.CheckpointReady && tower.Definition==option.Tower.Definition) {}
        else return false;
        return BuildingSaleMath.TryQuote(placement.InvestmentRecorded,placement.InvestedAmount,stored,
            manager.CheckpointWallet.Banlance,out refund,out stock,out total,out error);
    }
    public static string ButtonText(BuildManager manager,BuildingPlacement placement,out bool canSell)
    {
        canSell=TryQuote(manager,placement,out int refund,out int stock,out int total,out string error);
        return canSell?$"Bán: +{total} xu\nHoàn {refund} + kho {stock}":"Không thể bán\n"+error;
    }
    public static bool TrySell(BuildManager manager,BuildingPlacement placement,out string error)
    {
        if(!TryQuote(manager,placement,out _,out _,out int total,out error)) return false;
        if(!placement.TryClaimSale()) {error="Công trình đã được bán.";return false;}
        if(!manager.CheckpointWallet.TryAddRewardExact(total))
        {placement.CancelSaleClaim();error="Không thể nhận đủ tiền bán.";return false;}
        placement.gameObject.SetActive(false);
        placement.ReleaseReservationNow();
        Object.Destroy(placement.gameObject);
        GameAudio.Play(GameSound.Collect);
        manager.CloseForCheckpoint();
        foreach(TowerUpgradeUI ui in Object.FindObjectsByType<TowerUpgradeUI>())
            if(ui.gameObject.scene==manager.gameObject.scene) ui.ClosePanel();
        foreach(ProducerUpgradeUI ui in Object.FindObjectsByType<ProducerUpgradeUI>())
            if(ui.gameObject.scene==manager.gameObject.scene) ui.ClosePanel();
        foreach(SupportUpgradeUI ui in Object.FindObjectsByType<SupportUpgradeUI>())
            if(ui.gameObject.scene==manager.gameObject.scene) ui.ClosePanel();
        error="";return true;
    }
}
