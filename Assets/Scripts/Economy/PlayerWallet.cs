using UnityEngine;

public class PlayerWallet : MonoBehaviour
{

    [SerializeField, Min(0)] private int startingBanlance = 600;

    public int Banlance { get; private set;  }

    private void Awake()
    {
        Banlance = startingBanlance;
    }

    public void RestoreCheckpointBalance(int savedBalance)
    {
        if (savedBalance < 0) throw new System.ArgumentOutOfRangeException(nameof(savedBalance));
        Banlance = savedBalance;
    }

    public void AddReward(int amount)
    {
        if(amount <= 0)
        {
            return;
        }

        Banlance += amount;

        Debug.Log($"Nhan {amount} xu. So du hien tai : {Banlance}", this);

    }

    public bool TryAddRewardExact(int amount)
    {
        if(amount<0 || Banlance<0 || (long)Banlance+amount>int.MaxValue) return false;
        Banlance+=amount;
        Debug.Log($"Nhận {amount} xu. Số dư: {Banlance}",this);
        return true;
    }

    //Phuong thuc mua ban
    public bool TrySpend(int amount)
    {
        if(amount <= 0 || Banlance < amount)
        {
            return false;
        }

        Banlance -= amount;
        return true;
    }
}
