using System;
using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(BoneLilyTower))]
[RequireComponent(typeof(BoxCollider2D))]
public class TowerUpgradeClick : MonoBehaviour, IPointerClickHandler
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public static event Action<BoneLilyTower> TowerClicked;

    private BoneLilyTower tower;


    private void Awake()
    {
        tower = GetComponent<BoneLilyTower>();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
            return;
        if (tower == null || !tower.isActiveAndEnabled)
            return;

        TowerClicked?.Invoke(tower);
    }
}
