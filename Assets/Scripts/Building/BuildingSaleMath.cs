using System;
public static class BuildingSaleMath
{
    // Một lần mua và tối đa hai lần nâng cấp, mỗi giao dịch dùng số nguyên 32 bit.
    public const long MaxInvestment = 3L * int.MaxValue;
    public static bool ValidInvestment(bool recorded,long amount) => recorded
        ? amount>0 && amount<=MaxInvestment : amount==0;
    public static bool TryQuote(bool recorded,long invested,double stored,int balance,
        out int refund,out int stock,out int total,out string error)
    {
        refund=stock=total=0;error="";
        if(!recorded) {error="Bản lưu cũ chưa ghi tiền đầu tư; chưa thể bán công trình này.";return false;}
        if(!ValidInvestment(recorded,invested) || balance<0 || double.IsNaN(stored)
            || double.IsInfinity(stored) || stored<0 || stored>int.MaxValue)
        {error="Dữ liệu tiền đầu tư hoặc kho không hợp lệ.";return false;}
        long back=invested*65L/100L;
        long whole=(long)Math.Floor(stored);
        long sum=back+whole;
        if(back>int.MaxValue || sum>int.MaxValue || sum>int.MaxValue-(long)balance)
        {error="Ví không còn đủ chỗ để nhận toàn bộ tiền bán.";return false;}
        refund=(int)back;stock=(int)whole;total=(int)sum;return true;
    }
}
