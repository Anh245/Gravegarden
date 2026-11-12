using UnityEngine;
using UnityEngine.UI;
public class MaraPortraitPresenter : MonoBehaviour
{
    [SerializeField] private GardenTutorialController tutorial;
    [SerializeField] private Image portrait;
    [SerializeField] private Sprite welcome,concerned,sleepy,proud;
    private void LateUpdate()
    {
        if(tutorial==null||portrait==null)return;
        Sprite next=welcome;
        switch(tutorial.CurrentStep)
        {
            case GardenTutorialProgress.Step.Lily: next=concerned;break;
            case GardenTutorialProgress.Step.Harvest: next=sleepy;break;
            case GardenTutorialProgress.Step.Ready: next=proud;break;
        }
        if(next!=null)portrait.sprite=next;
    }
}
