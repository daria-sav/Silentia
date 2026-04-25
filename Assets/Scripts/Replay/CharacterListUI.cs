using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Shows the list of available character profiles during TerminalPaused.
/// Each row: key number + icon + display name.
/// profiles[0] is the default body and is skipped.
/// Profile1 action → profiles[1], Profile2 → profiles[2], etc.
/// </summary>
public class CharacterListUI : MonoBehaviour
{
    [Header("Wiring")]
    [SerializeField] private Transform entriesRoot;
    [SerializeField] private GameObject entryPrefab;

    private readonly List<GameObject> spawnedEntries = new();

    [Header("Tutorial Restrictions")]
    [SerializeField] private int maxProfilesShown = -1; // -1 = no limit

    // ─────────────── LIFECYCLE ───────────────

    #region Lifecycle
    private void Awake()
    {
        if (TerminalSession.Instance != null)
            TerminalSession.Instance.OnStateChanged += OnStateChanged;

        gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (TerminalSession.Instance != null)
            TerminalSession.Instance.OnStateChanged -= OnStateChanged;
    }
    #endregion

    // ─────────────── HANDLERS ────────────────

    #region Handlers
    private void OnStateChanged(TerminalSession.TerminalState state)
    {
        bool show = state == TerminalSession.TerminalState.TerminalPaused;

        if (show)
            Rebuild();

        gameObject.SetActive(show);
    }
    #endregion

    // ─────────────── BUILD ───────────────────

    #region Build
    private void Rebuild()
    {
        foreach (var e in spawnedEntries)
            Destroy(e);
        spawnedEntries.Clear();

        var profiles = TerminalSession.Instance?.GetProfiles();

        if (profiles == null || entryPrefab == null || entriesRoot == null)
            return;

        int limit = maxProfilesShown > 0
            ? Mathf.Min(maxProfilesShown + 1, profiles.Count)
            : profiles.Count;

        for (int i = 1; i < limit; i++)
        {
            var profile = profiles[i];
            if (profile == null)
                continue;

            var entry = Instantiate(entryPrefab, entriesRoot);
            spawnedEntries.Add(entry);

            // KeyImage/KeyLabel
            var keyLabel = entry.transform.Find("KeyImage/KeyLabel")?.GetComponent<TMP_Text>();
            if (keyLabel != null)
                keyLabel.text = i.ToString();

            // Frame/Icon
            var icon = entry.transform.Find("Frame/Icon")?.GetComponent<Image>();
            if (icon != null)
            {
                icon.sprite = profile.slotIcon;
                icon.enabled = profile.slotIcon != null;
            }

            // Frame/NameLabel
            var nameLabel = entry.transform.Find("Frame/NameLabel")?.GetComponent<TMP_Text>();
            if (nameLabel != null)
                nameLabel.text = profile.displayName;
        }
    }
    #endregion

    // ─────────────── HIGHLIGHT ───────────────

    #region Highlight
    private GameObject _highlightedEntry;
    private Color _originalFrameColor;

    public void HighlightProfile(CharacterProfile profile)
    {
        ClearHighlight();
        if (profile == null) return;

        foreach (var entry in spawnedEntries)
        {
            var nameLabel = entry.transform.Find("Frame/NameLabel")?.GetComponent<TMP_Text>();
            if (nameLabel == null || nameLabel.text != profile.displayName) continue;

            _highlightedEntry = entry;

            var frame = entry.transform.Find("Frame")?.GetComponent<Image>();
            if (frame != null)
            {
                _originalFrameColor = frame.color;
                frame.color = new Color(1f, 0.85f, 0.25f, 1f); 
            }

            break;
        }
    }

    public void ClearHighlight()
    {
        if (_highlightedEntry == null) return;

        var frame = _highlightedEntry.transform.Find("Frame")?.GetComponent<Image>();
        if (frame != null)
            frame.color = _originalFrameColor;

        _highlightedEntry = null;
    }
    #endregion
}