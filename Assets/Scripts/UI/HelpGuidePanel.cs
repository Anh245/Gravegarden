using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HelpGuidePanel : MonoBehaviour
{
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text bodyText;
    [SerializeField] private TMP_Text pageText;
    [SerializeField] private Button previousButton;
    [SerializeField] private Button nextButton;
    [SerializeField] private Button closeButton;
    private int page;
    private bool bound;
    public bool IsOpen => isActiveAndEnabled;

    private void Awake()
    {
        if(titleText==null||bodyText==null||pageText==null||previousButton==null||nextButton==null||closeButton==null)
        {Debug.LogError("HelpGuidePanel: thiếu tham chiếu giao diện.",this);enabled=false;return;}
        previousButton.onClick.AddListener(Previous);
        nextButton.onClick.AddListener(Next);
        closeButton.onClick.AddListener(Close);
        bound=true;
    }
    private void OnEnable(){page=0;if(bound)Refresh();}
    public void Open(){gameObject.SetActive(true);transform.SetAsLastSibling();}
    public void Close()=>gameObject.SetActive(false);
    private void Previous(){if(page>0){page--;Refresh();}}
    private void Next(){if(page+1<GraveGardenHelpContent.Count){page++;Refresh();}}
    private void Refresh()
    {
        var entry=GraveGardenHelpContent.Get(page);
        titleText.text=entry.Title;bodyText.text=entry.Body;
        pageText.text=$"{page+1} / {GraveGardenHelpContent.Count}";
        previousButton.interactable=page>0;
        nextButton.interactable=page+1<GraveGardenHelpContent.Count;
    }
    private void OnDestroy()
    {
        if(!bound)return;
        previousButton.onClick.RemoveListener(Previous);
        nextButton.onClick.RemoveListener(Next);
        closeButton.onClick.RemoveListener(Close);
    }
}
