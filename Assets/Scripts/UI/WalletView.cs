using System.Globalization;
using TMPro;
using UnityEngine;

public class WalletView : MonoBehaviour
{
    [SerializeField] private PlayerWallet wallet;
    [SerializeField] private TMP_Text balanceText;

    private int displayedBalance = -1;

    private readonly CultureInfo vietnamese = CultureInfo.GetCultureInfo("vi-VN");

    private void Start()
    {
        if( wallet == null || balanceText == null)
        {
            Debug.LogError(
                "WalletView chua gan Wallet hoac Balance Text",
                this
            );

            enabled = false;
            return;


        }

        Refresh();
    }

    private void Update()
    {
        if(displayedBalance != wallet.Banlance)
        {
            Refresh();
        }
    }

    private void Refresh()
    {
        displayedBalance = wallet.Banlance;
        balanceText.text = "Xu Âm Giới: " + displayedBalance.ToString("N0", vietnamese);
    }
}
