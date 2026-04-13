using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Displays replay slot state in the terminal UI.
/// Lets the player select a slot and updates
/// the slot visuals when TerminalSession changes.
/// </summary>
public class ReplaySlotUI : MonoBehaviour
{
    [Header("Wiring")]
    [SerializeField] private Button[] slotButtons = new Button[3];
    [SerializeField] private Image[] slotImages = new Image[3];

    [Header("Colors")]
    [SerializeField] private Color emptyColor = Color.black;
    [SerializeField] private Color filledColor = Color.green;

    [Header("Selected slot visual")]
    [SerializeField] private float selectedScale = 1.4f;
    [SerializeField] private float normalScale = 1.0f;

    // ─────────────── LIFECYCLE ───────────────

    #region Lifecycle
    private void Start()
    {
        WireButtonCallbacks();

        // subscribe to session updates
        if (TerminalSession.Instance != null)
            TerminalSession.Instance.OnSlotsChanged += Refresh;

        Refresh();
    }

    private void OnDestroy()
    {
        if (TerminalSession.Instance != null)
            TerminalSession.Instance.OnSlotsChanged -= Refresh;
    }
    #endregion

    // ────────────────── UI ───────────────────

    #region Public API
    public void Refresh()
    {
        var session = TerminalSession.Instance;
        if (session == null) 
            return;

        for (int i = 0; i < slotButtons.Length; i++)
        {
            RefreshSlotVisual(i, session);
        }
    }
    #endregion
    // ──────────────── HELPERS ────────────────

    #region Helpers
    private void WireButtonCallbacks()
    {
        for (int i = 0; i < TerminalSession.SlotCount; i++)
        {
            if (slotButtons == null || i >= slotButtons.Length || slotButtons[i] == null)
                continue;

            int slotIndex = i;
            slotButtons[i].onClick.AddListener(() => OnSlotClicked(slotIndex));
        }
    }

    private void OnSlotClicked(int slotIndex)
    {
        var session = TerminalSession.Instance;
        if (session == null)
            return;

        session.SelectSlot(slotIndex);
        Refresh();
    }

    private void RefreshSlotVisual(int slotIndex, TerminalSession session)
    {
        if (slotImages == null || slotIndex >= slotImages.Length || slotImages[slotIndex] == null)
            return;

        bool hasClip = session.HasClip(slotIndex);

        slotImages[slotIndex].color = hasClip ? filledColor : emptyColor;

        float scale = session.SelectedSlot == slotIndex
            ? selectedScale
            : normalScale;

        slotImages[slotIndex].transform.localScale = Vector3.one * scale;
    }
    #endregion
}