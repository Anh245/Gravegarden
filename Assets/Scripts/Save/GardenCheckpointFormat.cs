using System;
using System.Collections.Generic;

public static class GardenCheckpointFormat
{
    public const int Version = 2;
    public const string FileName = "gravegarden_checkpoint_v2.json";
    public const string Heart = "heart", Violet = "violet", Golden = "golden";

    public static HallSaveData[] CloneHalls(HallSaveData[] source)
    {
        if (source == null) return null;
        var result = new HallSaveData[source.Length];
        for (int i=0;i<source.Length;i++)
            if (source[i]!=null) result[i]=new HallSaveData { hallId=source[i].hallId, health=source[i].health, maxHealth=source[i].maxHealth };
        return result;
    }

    public static bool ValidateHeader(GardenSaveData data, out string error)
    {
        error="";
        if(data==null || data.schemaVersion!=Version) error="Bản lưu trống hoặc không phải phiên bản2.";
        else if(!CheckpointValidator.IsValidId(data.scenarioId) || string.IsNullOrEmpty(data.layoutSignature) || string.IsNullOrEmpty(data.waveSignature))
            error="Thiếu thông tin nhận diện bản lưu.";
        else if(data.savedUtcTicks<=0 || data.savedUtcTicks>DateTime.MaxValue.Ticks || data.balance<0)
            error="Thời điểm lưu hoặc số dư không hợp lệ.";
        else if(!PreparationClock.Valid(data.preparationSeconds))
            error="Thời gian chuẩn bị trong bản lưu không hợp lệ.";
        else if(data.completedWaves<0 || data.completedWaves>=GodotEndlessPolicy.MaxSupportedWave)
            error="Số đợt đã hoàn thành không hợp lệ.";
        else if(data.halls==null || (data.halls.Length!=1 && data.halls.Length!=3))
            error="Bản lưu phải có một hoặc ba nhà chính.";
        if(error.Length!=0)return false;
        var ids=new HashSet<string>(StringComparer.Ordinal);
        HallSaveData primary=null;
        foreach(var hall in data.halls)
        {
            if(hall==null || !ids.Add(hall.hallId??"") || (hall.hallId!=Heart && hall.hallId!=Violet && hall.hallId!=Golden)
                || hall.maxHealth<=0 || hall.health<0 || hall.health>hall.maxHealth)
            {error="Máu hoặc mã nhà chính bị thiếu, trùng hoặc không hợp lệ.";return false;}
            if(hall.hallId==Heart)primary=hall;
            bool active=hall.hallId==Heart || data.expanded;
            if((active && hall.health<=0) || (!active && hall.health!=0))
            {error="Trạng thái hoạt động và máu nhà chính không khớp.";return false;}
        }
        if(primary==null || primary.health!=data.hallHealth || primary.maxHealth!=data.hallMaxHealth)
        {error="Máu HeartCrypt không khớp dữ liệu tổng.";return false;}
        if(data.expanded && (data.halls.Length!=3 || data.completedWaves<5))
        {error="Mở khu yêu cầu ba nhà và đã hoàn thành ít nhất5đợt.";return false;}
        if(data.halls.Length==3 && !data.expanded && data.completedWaves>=5)
        {error="Đã hoàn thành đợt5 nhưng trạng thái khu chưa mở.";return false;}
        if(data.halls.Length==1 && (data.expanded || !ids.Contains(Heart)))
        {error="Bản lưu một nhà không thể mở hai khu phụ.";return false;}
        return true;
    }
}
