using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
[RequireComponent(typeof(Button))]
public class UIButtonSound : MonoBehaviour, IPointerClickHandler, ISubmitHandler
{
    private Button button;
    private void Awake()=>button=GetComponent<Button>();
    private void Play() {if(button!=null && button.IsActive() && button.IsInteractable()) GameAudio.Play(GameSound.Accept,true);}
    public void OnPointerClick(PointerEventData data) {if(data.button==PointerEventData.InputButton.Left) Play();}
    public void OnSubmit(BaseEventData data)=>Play();
}
