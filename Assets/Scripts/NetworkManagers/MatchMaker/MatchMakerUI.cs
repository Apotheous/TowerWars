using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MatchMakerUI : MonoBehaviour
{
    

    [SerializeField] private Button enterQueueBtn;
    [SerializeField] private Button exitQueueBtn;

    [SerializeField] private GameObject queuePanel;

    [SerializeField] private GameObject MainMenuCanvas;

    [SerializeField] private Counter counter;   
    void Start()
    {
        enterQueueBtn.onClick.AddListener(EnterQueueBtn);
        exitQueueBtn.onClick.AddListener(ExitQueueBtn);
    }

    // Update is called once per frame
    private void EnterQueueBtn()
    {
        OpenQueuePanel();
        MatchMakerManager.Instance.ClientJoin();
        counter.StartCount();
    }
    private void ExitQueueBtn()
    {
        MatchMakerManager.Instance.LeaveQueueAsync();
        counter.ResetCount();
    }

    private void OpenQueuePanel()
    {
        queuePanel?.SetActive(true);
    }
    private void CloseQueuePanel()
    {
        queuePanel?.SetActive(false);
    }
    public void CloseCloseCanvas()
    {
        MainMenuCanvas?.SetActive(false);
    }

}
