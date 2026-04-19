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

        for (int i = 1; i < profiles.Count; i++)
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
}