using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

namespace Com.GCTC.ZombCube
{
    /// <summary>
    /// Level select cards. Each card is a map, or a map plus texture pack for maps with
    /// more than one pack. The scene's cards cover the first options; cards for any
    /// further maps in the LevelDatabase are copied from the first card at runtime.
    /// </summary>
    public class LevelSelect : MonoBehaviour
    {
        public GameObject soloButton;
        [Tooltip("The toggles on the level cards already in the scene, in option order.")]
        public Toggle[] levelToggles;

        [Header("Card layout")]
        [Tooltip("Horizontal distance between card centres when they fit.")]
        public float cardSpacing = 520f;

        /// <summary>Texture pack of the selected map (used by MaterialForLevel).</summary>
        public static int levelSelected = 0;
        /// <summary>Index of the selected map in the LevelDatabase.</summary>
        public static int mapSelected = 0;

        private struct Option
        {
            public int map;
            public int pack;
        }

        private readonly List<Option> options = new List<Option>();
        private readonly List<Toggle> toggles = new List<Toggle>();

        private void Start()
        {
            BuildOptions();
            toggles.AddRange(levelToggles);
            AddCardsForNewOptions();
            LayoutCards();
            Select(0, false);
        }

        /// <summary>Called by the level card toggles in the scene.</summary>
        public void SelectLevel(int level)
        {
            Select(level, true);
        }

        private void Select(int option, bool fromPlayer)
        {
            if (options.Count == 0) return;
            option = Mathf.Clamp(option, 0, options.Count - 1);

            mapSelected = options[option].map;
            levelSelected = options[option].pack;

            for (int i = 0; i < toggles.Count; i++)
            {
                if (toggles[i] == null) continue;
                toggles[i].SetIsOnWithoutNotify(i == option);
                toggles[i].interactable = i != option;
            }

            if (fromPlayer && EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(null);
                EventSystem.current.SetSelectedGameObject(soloButton);
            }
        }

        private void BuildOptions()
        {
            options.Clear();
            LevelDatabase db = LevelDatabase.Instance;
            if (db == null || db.Count == 0)
            {
                // Original behaviour: one map, two texture packs.
                options.Add(new Option { map = 0, pack = 0 });
                options.Add(new Option { map = 0, pack = 1 });
                return;
            }

            for (int map = 0; map < db.Count; map++)
            {
                int packs = Mathf.Max(1, db.entries[map].texturePackCount);
                for (int pack = 0; pack < packs; pack++)
                    options.Add(new Option { map = map, pack = pack });
            }
        }

        private void AddCardsForNewOptions()
        {
            if (levelToggles == null || levelToggles.Length == 0 || levelToggles[0] == null) return;

            GameObject template = levelToggles[0].transform.parent.gameObject;
            LevelDatabase db = LevelDatabase.Instance;

            for (int option = toggles.Count; option < options.Count; option++)
            {
                GameObject card = Instantiate(template, template.transform.parent);
                card.name = "Level " + option;
                LevelEntry level = db != null ? db.Get(options[option].map) : null;

                Toggle toggle = card.GetComponentInChildren<Toggle>(true);
                if (toggle != null)
                {
                    // The copied toggle calls SelectLevel(0); switch that off, keep the sound.
                    UnityEvent<bool> changed = toggle.onValueChanged;
                    for (int j = 0; j < changed.GetPersistentEventCount(); j++)
                        if (changed.GetPersistentMethodName(j) == nameof(SelectLevel))
                            changed.SetPersistentListenerState(j, UnityEventCallState.Off);

                    int captured = option;
                    toggle.onValueChanged.AddListener(isOn => { if (isOn) Select(captured, true); });
                }

                foreach (Transform child in card.transform)
                {
                    if (child.GetComponent<Toggle>() != null) continue;

                    TextMeshProUGUI label = child.GetComponent<TextMeshProUGUI>();
                    if (label != null)
                    {
                        label.text = CardName(level, options[option].pack);
                        continue;
                    }

                    Image preview = child.GetComponent<Image>();
                    if (preview != null && level != null)
                    {
                        preview.sprite = level.preview;
                        preview.color = level.previewColor;
                    }
                }

                toggles.Add(toggle);
            }
        }

        private static string CardName(LevelEntry level, int pack)
        {
            if (level == null) return "Level";
            if (level.texturePackCount > 1 && level.texturePackNames != null && pack < level.texturePackNames.Length)
                return level.displayName + "\n" + level.texturePackNames[pack];
            return level.displayName;
        }

        // Spread all cards evenly around the centre, shrinking them if they don't fit.
        private void LayoutCards()
        {
            if (toggles.Count <= levelToggles.Length) return;   // scene layout unchanged

            var cards = new List<RectTransform>();
            foreach (Toggle toggle in toggles)
                if (toggle != null) cards.Add((RectTransform)toggle.transform.parent);
            if (cards.Count == 0) return;

            var parent = (RectTransform)cards[0].parent;
            float available = parent.rect.width > 1f ? parent.rect.width : 1920f;
            float cardWidth = cards[0].rect.width > 1f ? cards[0].rect.width : 400f;
            float spacing = Mathf.Min(cardSpacing, (available - 80f) / cards.Count);
            float scale = Mathf.Min(1f, (spacing - 20f) / cardWidth);
            float y = cards[0].anchoredPosition.y;

            for (int i = 0; i < cards.Count; i++)
            {
                cards[i].anchoredPosition = new Vector2((i - (cards.Count - 1) / 2f) * spacing, y);
                cards[i].localScale = new Vector3(scale, scale, 1f);
            }
        }
    }
}
