using UnityEngine;

public class PlayerWallet : MonoBehaviour
{

    [SerializeField, Min(0)] private int startingBanlance = 600;

    public int Banlance { get; private set;  }

    private void Awake()
    {
        Banlance = startingBanlance;
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
