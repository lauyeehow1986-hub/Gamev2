using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SGPathway.Data;
using SGPathway.Engine;

namespace SGPathway.UI
{
    /// <summary>
    /// Bandersnatch-style branch prompt — appears when the player raises
    /// <see cref="WalkthroughPlayer.OnBranchPrompt"/>. Renders option buttons,
    /// forwards the picked index back to the player.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BranchOverlay : MonoBehaviour
    {
        [SerializeField] private WalkthroughPlayer player;
        [SerializeField] private RectTransform root;
        [SerializeField] private TMP_Text promptLabel;
        [SerializeField] private Button optionButtonPrefab;
        [SerializeField] private RectTransform optionsContainer;

        private readonly List<Button> _spawned = new List<Button>();

        private void OnEnable()
        {
            if (player != null) player.OnBranchPrompt.AddListener(OnPrompt);
            SetVisible(false);
        }

        private void OnDisable()
        {
            if (player != null) player.OnBranchPrompt.RemoveListener(OnPrompt);
        }

        private void OnPrompt(ChapterSO chapter, BranchPoint branch)
        {
            ClearOptions();
            if (promptLabel != null)
            {
                branch.prompt.GetLocalizedStringAsync().Completed += op =>
                {
                    if (op.Status == UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationStatus.Succeeded)
                        promptLabel.text = op.Result;
                };
            }
            for (int i = 0; i < branch.options.Count; i++)
            {
                int index = i;
                var opt = branch.options[i];
                var btn = Instantiate(optionButtonPrefab, optionsContainer);
                var label = btn.GetComponentInChildren<TMP_Text>();
                if (label != null)
                {
                    opt.label.GetLocalizedStringAsync().Completed += op =>
                    {
                        if (op.Status == UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationStatus.Succeeded)
                            label.text = op.Result;
                    };
                }
                btn.onClick.AddListener(() =>
                {
                    SetVisible(false);
                    player.ChooseBranch(index);
                });
                _spawned.Add(btn);
            }
            SetVisible(true);
        }

        private void ClearOptions()
        {
            for (int i = 0; i < _spawned.Count; i++)
                if (_spawned[i] != null) Destroy(_spawned[i].gameObject);
            _spawned.Clear();
        }

        private void SetVisible(bool visible)
        {
            if (root != null) root.gameObject.SetActive(visible);
        }
    }
}
