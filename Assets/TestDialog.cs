using TMPro;
using UnityEngine;
using WitShells.DialogsManager;

public class TestDialog : MonoBehaviour, ISubtitleTextUI, IDialogActionRequire
{
    [SerializeField] private TMP_Text subtitleTextUI;
    [SerializeField] private string dialogId = "Test";

    public string Id => dialogId;
    public string ActionId => dialogId;

    [SerializeField] private bool actionComplete = false;

    public bool IsActionComplete()
    {
        return actionComplete;
    }

    public void SetSubtitleText(string text)
    {
        subtitleTextUI.text = text;
    }
}
