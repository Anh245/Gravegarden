using System;
using UnityEngine;

[Serializable]
public class BuildingOption
{
    [Tooltip("Gán Tower hoặc Producer, không gán cả hai.")]
    [SerializeField] private BoneLilyTower tower;

    [SerializeField] private ProducerBuilding producer;

    public BoneLilyTower Tower => tower;
    public ProducerBuilding Producer => producer;

    public bool IsProducer => producer != null;

    public bool IsValid
    {
        get
        {
            // Phải có đúng một loại.
            if ((tower == null) == (producer == null))
                return false;

            if (tower != null)
            {
                return tower.gameObject.activeSelf &&
                       tower.enabled &&
                       tower.Definition != null &&
                       tower.Definition.IsValid;
            }

            return producer.gameObject.activeSelf &&
                   producer.enabled &&
                   producer.Definition != null &&
                   producer.Definition.IsValid;
        }
    }

    public string DisplayName =>
        IsProducer
            ? producer.Definition.DisplayName
            : tower.Definition.DisplayName;

    public int BaseCost =>
        IsProducer
            ? producer.Definition.BaseBuildCost
            : tower.Definition.BuildCost;

    public int UnlockWave =>
        IsProducer
            ? producer.Definition.UnlockWave
            : tower.Definition.UnlockWave;
}