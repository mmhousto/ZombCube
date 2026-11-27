using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

namespace Com.GCTC.ZombCube
{
    /// <summary>
    /// Shop for blaster and skin cosmetics. The scene has a fixed set of item cards for
    /// each kind; they are used as slots and paged through the CosmeticDatabase entries,
    /// so adding a cosmetic only needs a database entry, not new UI.
    /// </summary>
    public class InventoryManager : MonoBehaviour, IBuyable, IUseable
    {
        [Tooltip("Blaster shop slots: the button on each card. The cards are reused for every page.")]
        public GameObject[] blasterItems;
        [Tooltip("Skin shop slots: the button on each card. The cards are reused for every page.")]
        public GameObject[] skinItems;

        public GameObject notEnoughPointsPanel, closeButton, convertConfirm, noButton;

        [Header("Paging buttons (optional)")]
        [Tooltip("Leave empty to create one at the bottom centre of the panel. If assigned, the click is wired automatically; don't add OnClick events for it.")]
        public Button blasterPrevPage;
        public Button blasterNextPage;
        public Button skinPrevPage;
        public Button skinNextPage;

        private enum Kind { Blaster, Skin }

        private class Slot
        {
            public GameObject card;
            public Button button;
            public TextMeshProUGUI buttonLabel;
            public TextMeshProUGUI nameLabel;
            public TextMeshProUGUI priceLabel;
            public Image preview;
        }

        [Tooltip("The players data object.")]
        private Player player;
        private CosmeticDatabase db;
        private Slot[] blasterSlots = new Slot[0];
        private Slot[] skinSlots = new Slot[0];
        private int blasterPage;
        private int skinPage;
        private int lastSignature = int.MinValue;

        private void Start()
        {
            player = Player.Instance;
            db = CosmeticDatabase.Instance;
            if (player == null || db == null)
            {
                Debug.LogWarning("InventoryManager: Player or CosmeticDatabase missing; shop disabled.");
                enabled = false;
                return;
            }

            player.EnsureCosmeticArrays();

            blasterSlots = BuildSlots(blasterItems, Kind.Blaster);
            skinSlots = BuildSlots(skinItems, Kind.Skin);

            if (blasterPrevPage == null) blasterPrevPage = CreatePageButton(blasterSlots, "PREV", true);
            if (blasterNextPage == null) blasterNextPage = CreatePageButton(blasterSlots, "NEXT", false);
            if (skinPrevPage == null) skinPrevPage = CreatePageButton(skinSlots, "PREV", true);
            if (skinNextPage == null) skinNextPage = CreatePageButton(skinSlots, "NEXT", false);

            if (blasterPrevPage != null) blasterPrevPage.onClick.AddListener(PrevBlasterPage);
            if (blasterNextPage != null) blasterNextPage.onClick.AddListener(NextBlasterPage);
            if (skinPrevPage != null) skinPrevPage.onClick.AddListener(PrevSkinPage);
            if (skinNextPage != null) skinNextPage.onClick.AddListener(NextSkinPage);

            Refresh();
        }

        private void Update()
        {
            // Ownership or the equipped cosmetic can change from cloud loads too.
            if (Signature() != lastSignature)
                Refresh();
        }

        #region Paging

        public void NextBlasterPage() { ChangePage(Kind.Blaster, 1); }
        public void PrevBlasterPage() { ChangePage(Kind.Blaster, -1); }
        public void NextSkinPage() { ChangePage(Kind.Skin, 1); }
        public void PrevSkinPage() { ChangePage(Kind.Skin, -1); }

        private void ChangePage(Kind kind, int delta)
        {
            int pages = PageCount(kind);
            if (kind == Kind.Blaster)
                blasterPage = Mathf.Clamp(blasterPage + delta, 0, pages - 1);
            else
                skinPage = Mathf.Clamp(skinPage + delta, 0, pages - 1);

            Refresh();
            KeepSelectionUsable(kind, delta);
        }

        private int PageCount(Kind kind)
        {
            int perPage = Slots(kind).Length;
            if (perPage == 0 || db == null) return 1;
            return Mathf.Max(1, Mathf.CeilToInt(db.Count / (float)perPage));
        }

        // Gamepad users keep a valid selection when a page has fewer cards.
        private void KeepSelectionUsable(Kind kind, int delta)
        {
            EventSystem eventSystem = EventSystem.current;
            if (eventSystem == null) return;

            GameObject current = eventSystem.currentSelectedGameObject;
            Selectable selectable = current != null ? current.GetComponent<Selectable>() : null;
            if (current != null && current.activeInHierarchy && (selectable == null || selectable.interactable))
                return;

            Button pageButton = delta > 0 ? NextButton(kind) : PrevButton(kind);
            GameObject target = null;
            if (pageButton != null && pageButton.interactable && pageButton.gameObject.activeInHierarchy)
                target = pageButton.gameObject;
            else
                foreach (Slot slot in Slots(kind))
                    if (slot.card.activeInHierarchy && slot.button != null) { target = slot.button.gameObject; break; }

            eventSystem.SetSelectedGameObject(null);
            eventSystem.SetSelectedGameObject(target);
        }

        #endregion

        #region Display

        private Slot[] BuildSlots(GameObject[] items, Kind kind)
        {
            if (items == null) return new Slot[0];

            var slots = new Slot[items.Length];
            for (int i = 0; i < items.Length; i++)
            {
                GameObject item = items[i];
                var slot = new Slot();
                slot.button = item.GetComponent<Button>();
                slot.buttonLabel = item.GetComponentInChildren<TextMeshProUGUI>(true);
                slot.card = item.transform.parent != null ? item.transform.parent.gameObject : item;
                slot.nameLabel = FindLabel(slot.card.transform, "Name");
                slot.priceLabel = FindLabel(slot.card.transform, "Price");
                Transform previewTransform = slot.card.transform.Find("Image");
                slot.preview = previewTransform != null ? previewTransform.GetComponent<Image>() : null;

                if (slot.button != null)
                {
                    // The scene buttons call SelectBlaster/SelectSkin with a fixed index.
                    // Turn those calls off (keeping the click sound) and select by page.
                    UnityEvent onClick = slot.button.onClick;
                    for (int j = 0; j < onClick.GetPersistentEventCount(); j++)
                    {
                        string method = onClick.GetPersistentMethodName(j);
                        if (method == nameof(SelectBlaster) || method == nameof(SelectSkin))
                            onClick.SetPersistentListenerState(j, UnityEventCallState.Off);
                    }

                    int slotIndex = i;
                    slot.button.onClick.AddListener(() => OnSlotClicked(kind, slotIndex));
                }

                slots[i] = slot;
            }
            return slots;
        }

        private static TextMeshProUGUI FindLabel(Transform card, string childName)
        {
            Transform child = card.Find(childName);
            return child != null ? child.GetComponent<TextMeshProUGUI>() : null;
        }

        private Button CreatePageButton(Slot[] slots, string text, bool left)
        {
            if (slots.Length == 0 || slots[0].button == null || slots[0].card.transform.parent == null)
                return null;

            Transform panel = slots[0].card.transform.parent;
            RectTransform source = (RectTransform)slots[0].button.transform;
            GameObject clone = Instantiate(slots[0].button.gameObject, panel);
            clone.name = left ? "Prev Page" : "Next Page";

            // Side by side at the bottom centre of the panel, just under the cards,
            // so they stay clear of the shop's Back button in the corner.
            const float gap = 20f;
            var rect = (RectTransform)clone.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(left ? 1f : 0f, 1f);
            Vector2 size = source.rect.size;
            rect.sizeDelta = size.x > 1f && size.y > 1f ? size : new Vector2(160f, 40f);
            rect.anchoredPosition = new Vector2(left ? -gap / 2f : gap / 2f, -10f);
            rect.localScale = Vector3.one;

            TextMeshProUGUI label = clone.GetComponentInChildren<TextMeshProUGUI>(true);
            if (label != null) label.text = text;

            return clone.GetComponent<Button>();
        }

        private void Refresh()
        {
            if (player == null || db == null) return;
            player.EnsureCosmeticArrays();
            RefreshKind(Kind.Blaster);
            RefreshKind(Kind.Skin);
            lastSignature = Signature();
        }

        private void RefreshKind(Kind kind)
        {
            Slot[] slots = Slots(kind);
            int page = kind == Kind.Blaster ? blasterPage : skinPage;
            int[] owned = Owned(kind);
            int equipped = kind == Kind.Blaster ? player.currentBlaster : player.currentSkin;
            string suffix = kind == Kind.Blaster ? " Blaster" : " Skin";

            for (int i = 0; i < slots.Length; i++)
            {
                Slot slot = slots[i];
                int index = page * slots.Length + i;
                bool exists = index < db.Count;
                slot.card.SetActive(exists);
                if (!exists) continue;

                CosmeticEntry entry = db.Get(index);
                if (slot.nameLabel != null) slot.nameLabel.text = entry.displayName + suffix;
                if (slot.priceLabel != null) slot.priceLabel.text = PriceLabel(entry, kind);
                if (slot.preview != null)
                {
                    slot.preview.sprite = entry.previewSprite;
                    slot.preview.material = entry.previewMaterial;
                    slot.preview.color = entry.previewColor;
                }
                if (slot.buttonLabel != null)
                {
                    bool isOwned = index < owned.Length && owned[index] == 1;
                    slot.buttonLabel.text = index == equipped ? "USING" : isOwned ? "USE" : "BUY";
                }
            }

            int pages = PageCount(kind);
            SetPageButton(PrevButton(kind), pages > 1, page > 0);
            SetPageButton(NextButton(kind), pages > 1, page < pages - 1);
        }

        private static void SetPageButton(Button button, bool visible, bool interactable)
        {
            if (button == null) return;
            button.gameObject.SetActive(visible);
            button.interactable = interactable;
        }

        private static string PriceLabel(CosmeticEntry entry, Kind kind)
        {
            int price = kind == Kind.Blaster ? entry.blasterPrice : entry.skinPrice;
            if (price <= 0) return "FREE";
            return price + (entry.currency == CosmeticCurrency.Points ? " Points" : " Coins");
        }

        private int Signature()
        {
            if (player == null) return 0;
            unchecked
            {
                int hash = player.currentBlaster * 397 ^ player.currentSkin;
                if (player.ownedBlasters != null)
                    foreach (int flag in player.ownedBlasters) hash = hash * 31 + flag;
                if (player.ownedSkins != null)
                    foreach (int flag in player.ownedSkins) hash = hash * 31 + flag;
                return hash;
            }
        }

        private Slot[] Slots(Kind kind) { return kind == Kind.Blaster ? blasterSlots : skinSlots; }
        private int[] Owned(Kind kind) { return kind == Kind.Blaster ? player.ownedBlasters : player.ownedSkins; }
        private Button PrevButton(Kind kind) { return kind == Kind.Blaster ? blasterPrevPage : skinPrevPage; }
        private Button NextButton(Kind kind) { return kind == Kind.Blaster ? blasterNextPage : skinNextPage; }

        #endregion

        #region Buying and using

        private void OnSlotClicked(Kind kind, int slotIndex)
        {
            int perPage = Slots(kind).Length;
            int page = kind == Kind.Blaster ? blasterPage : skinPage;
            int index = page * perPage + slotIndex;

            if (kind == Kind.Blaster)
                SelectBlaster(index);
            else
                SelectSkin(index);
        }

        private bool IsValid(Kind kind, int index)
        {
            if (player == null || db == null) return false;
            player.EnsureCosmeticArrays();
            return index >= 0 && index < db.Count && index < Owned(kind).Length;
        }

        private bool TryBuy(Kind kind, int index)
        {
            if (!IsValid(kind, index)) return false;

            int[] owned = Owned(kind);
            if (owned[index] == 1) return true;

            CosmeticEntry entry = db.Get(index);
            int price = kind == Kind.Blaster ? entry.blasterPrice : entry.skinPrice;
            if (entry.currency == CosmeticCurrency.Points)
            {
                if (player.points < price) { NotEnough(); return false; }
                player.points -= price;
            }
            else
            {
                if (player.coins < price) { NotEnough(); return false; }
                player.coins -= price;
            }

            owned[index] = 1;
            return true;
        }

        public void CanConvert()
        {
            if (player.points >= 10_000)
            {
                convertConfirm.SetActive(true);
                EventSystem.current.SetSelectedGameObject(null);
                EventSystem.current.SetSelectedGameObject(noButton);
            }
            else
            {
                NotEnough();
            }
        }

        public void ConvertCoin()
        {
            if (player.points >= 10_000)
            {
                player.points -= 10_000;
                player.coins++;
                TrySavePlayerData();
            }
            else
            {
                convertConfirm.SetActive(false);
                NotEnough();
            }
        }

        public void BuyBlaster(int index)
        {
            TryBuy(Kind.Blaster, index);
        }

        public void Use(int index)
        {
            if (IsValid(Kind.Blaster, index))
                player.currentBlaster = index;
        }

        /// <summary>Equips the blaster cosmetic if owned, otherwise tries to buy it.</summary>
        public void SelectBlaster(int index)
        {
            if (!IsValid(Kind.Blaster, index)) return;

            if (player.ownedBlasters[index] == 1)
                Use(index);
            else
                TryBuy(Kind.Blaster, index);

            TrySavePlayerData();
            Refresh();
        }

        public void BuySkin(int index)
        {
            TryBuy(Kind.Skin, index);
        }

        public void UseSkin(int index)
        {
            if (IsValid(Kind.Skin, index))
                player.currentSkin = index;
        }

        /// <summary>Equips the skin cosmetic if owned, otherwise tries to buy it.</summary>
        public void SelectSkin(int index)
        {
            if (!IsValid(Kind.Skin, index)) return;

            if (player.ownedSkins[index] == 1)
                UseSkin(index);
            else
                TryBuy(Kind.Skin, index);

            TrySavePlayerData();
            Refresh();
        }

        private void TrySavePlayerData()
        {
            try
            {
                SaveSystem.SavePlayer(player);
            }
            catch
            {
                Debug.Log("Failed to save local data.");
            }

            try
            {
                CloudSaveLogin.SaveCloudDataIfSignedIn();
            }
            catch
            {
                Debug.Log("Failed to save cloud data.");
            }
        }

        private void NotEnough()
        {
            notEnoughPointsPanel.SetActive(true);
            EventSystem.current.SetSelectedGameObject(null);
            EventSystem.current.SetSelectedGameObject(closeButton);
        }

        #endregion
    }
}
