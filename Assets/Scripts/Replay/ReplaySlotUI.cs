using UnityEngine;
using UnityEngine.UI;

public class ReplaySlotUI : MonoBehaviour
{
    [Header("Wiring")]
    [SerializeField] private Button[] slotButtons = new Button[3];
    [SerializeField] private Image[] slotImages = new Image[3];

    [Header("Colors")]
    [SerializeField] private Color emptyColor = Color.black;
    [SerializeField] private Color filledColor = Color.green;

    [Header("Selected slot visual")]
    [SerializeField] private float selectedScale = 1.1f;
    [SerializeField] private float normalScale = 1.0f;

    private void Start()
    {
        // Wire button callbacks
        for (int i = 0; i < 3; i++)
        {
            int idx = i;
            if (slotButtons[idx] != null)
            {
                slotButtons[idx].onClick.AddListener(() =>
                {
                    var session = TerminalSession.Instance;
                    if (session != null)
                    {
                        session.SelectSlot(idx);
                        Refresh();
                    }
                });
            }
        }

        // Subscribe to session updates
        if (TerminalSession.Instance != null)
            TerminalSession.Instance.OnSlotsChanged += Refresh;

        Refresh();
    }

    private void OnDestroy()
    {
        if (TerminalSession.Instance != null)
            TerminalSession.Instance.OnSlotsChanged -= Refresh;
    }

    public void Refresh()
    {
        var session = TerminalSession.Instance;
        if (session == null) return;

        for (int i = 0; i < 3; i++)
        {
            bool hasClip = session.HasClip(i);

            if (slotImages != null && i < slotImages.Length && slotImages[i] != null)
            {
                slotImages[i].color = hasClip ? filledColor : emptyColor;

                float scale = (session.SelectedSlot == i) ? selectedScale : normalScale;
                slotImages[i].transform.localScale = Vector3.one * scale;
            }
        }
    }
}