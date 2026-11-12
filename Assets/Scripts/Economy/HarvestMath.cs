using System;
using System.Collections.Generic;
public static class HarvestMath
{
    public static bool TryTotal(IEnumerable<double> stores,int balance,out int total,out int ready,out string error)
    {
        total=ready=0;error="";
        if(stores==null || balance<0) {error="Dữ liệu ví hoặc kho không hợp lệ.";return false;}
        long sum=0;
        foreach(double stored in stores)
        {
            if(double.IsNaN(stored) || double.IsInfinity(stored) || stored<0 || stored>int.MaxValue)
            {error="Dữ liệu kho không hợp lệ.";total=ready=0;return false;}
            int amount=(int)Math.Floor(stored);
            if(amount<=0) continue;
            sum+=amount;ready++;
            if(sum>int.MaxValue-(long)balance)
            {error="Ví không đủ chỗ để nhận toàn bộ tiền.";total=ready=0;return false;}
        }
        total=(int)sum;return true;
    }
}
