#if !(UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX || UNITY_STANDALONE_OSX || STEAMWORKS_WIN || STEAMWORKS_LIN_OSX)
#define DISABLESTEAMWORKS
#endif

using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Com.GCTC.ZombCube
{
    /// <summary>
    /// The Stats screen's extra pages, built at runtime from the screen's own buttons, panels and fonts so
    /// they match the existing style:
    /// - Overview / Highest Waves tabs (best wave on every map in Solo, Co-op and Party).
    /// - In-game Leaderboards (Steam: Top, Friends and Around Me for every board).
    /// - In-game Achievements with progress, for every player (signed in or not).
    /// Game Center players keep the Apple UI for leaderboards and achievements.
    /// </summary>
    public class StatsScreens : MonoBehaviour
    {
        private const int LeaderboardRows = 10;

        // Templates taken from the Stats screen.
        private RectTransform statsRoot, overviewPanel, morePanel;
        private Button buttonTemplate, closeTemplate;
        private TextMeshProUGUI titleTemplate, labelTemplate, valueTemplate;
        private Image panelTemplate;

        private Color accent = new Color(0.2f, 0.8f, 1f, 1f);
        private Color dimAccent;
        private Color textColor = Color.white;

        // Pages.
        private RectTransform wavesPanel;
        private Button overviewTab, wavesTab;
        private readonly List<GameObject> overviewOnly = new List<GameObject>();
        private RectTransform leaderboardPage, achievementsPage;
        private TextMeshProUGUI wavesTableText;

        // Leaderboards state.
        private readonly List<Button> mapTabs = new List<Button>();
        private readonly List<Button> modeTabs = new List<Button>();
        private readonly List<Button> scopeTabs = new List<Button>();
        private readonly List<TextMeshProUGUI[]> rowCells = new List<TextMeshProUGUI[]>();
        private readonly List<Image> rowBacks = new List<Image>();
        private TextMeshProUGUI boardTitle, boardStatus, boardYourBest;
        private int mapIndex, modeIndex, scopeIndex;
        private int requestId;

        // Achievements state.
        private RectTransform achievementContent;
        private TextMeshProUGUI achievementSummary;
        private Image achievementSummaryFill;

        private struct BoardChoice
        {
            public string label;
            public string steamName;
            public Func<int> yourBest;
            public bool percent;
        }

        /// <summary>Builds the pages; returns null if the Stats screen doesn't have the expected pieces.</summary>
        public static StatsScreens Create(StatsManager stats)
        {
            if (stats == null || stats.achievementsButton == null) return null;
            var screens = stats.gameObject.AddComponent<StatsScreens>();
            if (!screens.Build(stats))
            {
                Destroy(screens);
                return null;
            }
            return screens;
        }

        // ------------------------------------------------------------------ build

        private bool Build(StatsManager stats)
        {
            overviewPanel = stats.transform as RectTransform;
            morePanel = stats.achievementsButton.transform.parent as RectTransform;
            statsRoot = morePanel != null ? morePanel.parent as RectTransform : null;
            buttonTemplate = stats.achievementsButton.GetComponent<Button>();
            panelTemplate = morePanel != null ? morePanel.GetComponent<Image>() : null;
            Transform close = morePanel != null ? morePanel.Find("Back") : null;
            closeTemplate = close != null ? close.GetComponent<Button>() : null;
            Transform title = statsRoot != null ? statsRoot.Find("StatsTitle") : null;
            titleTemplate = title != null ? title.GetComponent<TextMeshProUGUI>() : null;
            labelTemplate = stats.soloWaveLabel != null ? FindName(stats.soloWaveLabel.transform.parent) : null;
            valueTemplate = stats.soloWaveLabel;

            if (overviewPanel == null || statsRoot == null || buttonTemplate == null || panelTemplate == null
                || titleTemplate == null || valueTemplate == null)
                return false;
            if (labelTemplate == null) labelTemplate = valueTemplate;

            Image buttonImage = buttonTemplate.GetComponent<Image>();
            if (buttonImage != null) accent = buttonImage.color;
            dimAccent = new Color(accent.r * 0.45f, accent.g * 0.45f, accent.b * 0.45f, Mathf.Max(0.6f, accent.a * 0.8f));
            textColor = valueTemplate.color;

            BuildWavesTab();
            leaderboardPage = BuildLeaderboardPage();
            achievementsPage = BuildAchievementsPage();
            return true;
        }

        private static TextMeshProUGUI FindName(Transform row)
        {
            if (row == null) return null;
            Transform name = row.Find("Name");
            return name != null ? name.GetComponent<TextMeshProUGUI>() : null;
        }

        // Overview / Highest Waves tabs above the stats panel.
        private void BuildWavesTab()
        {
            RectTransform tabs = NewRect("StatsTabs", statsRoot);
            tabs.anchorMin = tabs.anchorMax = new Vector2(0.5f, 1f);
            tabs.pivot = new Vector2(0.5f, 1f);
            tabs.anchoredPosition = new Vector2(0f, -190f);
            tabs.sizeDelta = new Vector2(640f, 60f);
            tabs.SetSiblingIndex(overviewPanel.GetSiblingIndex() + 1);   // under the More/Delete overlays
            var layout = tabs.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 20f;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = layout.childForceExpandHeight = true;

            overviewTab = NewButton(tabs, "OVERVIEW", () => ShowWaves(false), 30f);
            wavesTab = NewButton(tabs, "HIGHEST WAVES", () => ShowWaves(true), 30f);

            wavesPanel = NewRect("WavesPanel", statsRoot);
            wavesPanel.anchorMin = overviewPanel.anchorMin;
            wavesPanel.anchorMax = overviewPanel.anchorMax;
            wavesPanel.pivot = overviewPanel.pivot;
            wavesPanel.anchoredPosition = overviewPanel.anchoredPosition;
            wavesPanel.sizeDelta = overviewPanel.sizeDelta;
            wavesPanel.SetSiblingIndex(overviewPanel.GetSiblingIndex() + 1);

            Image back = wavesPanel.gameObject.AddComponent<Image>();
            back.color = new Color(0f, 0f, 0f, 0.35f);
            back.raycastTarget = false;

            wavesTableText = NewText(valueTemplate, wavesPanel, "", 36f, TextAlignmentOptions.Center);
            Stretch(wavesTableText.rectTransform, 30f, 20f);
            wavesTableText.enableAutoSizing = true;
            wavesTableText.fontSizeMin = 18f;
            wavesTableText.fontSizeMax = 40f;

            wavesPanel.gameObject.SetActive(false);
            SetTab(overviewTab, true);
            SetTab(wavesTab, false);
        }

        // ------------------------------------------------------------------ waves

        public void ShowWaves(bool show)
        {
            if (wavesPanel == null) return;
            if (show) RefreshWaves();
            wavesPanel.gameObject.SetActive(show);
            // The overview panel holds StatsManager and this component, so it's hidden rather than deactivated.
            CanvasGroup group = overviewPanel.GetComponent<CanvasGroup>();
            if (group == null) group = overviewPanel.gameObject.AddComponent<CanvasGroup>();
            group.alpha = show ? 0f : 1f;
            group.blocksRaycasts = group.interactable = !show;
            SetTab(overviewTab, !show);
            SetTab(wavesTab, show);
        }

        /// <summary>Shows the Overview tab (called whenever the Stats screen opens).</summary>
        public void ResetTabs() => ShowWaves(false);

        private void RefreshWaves()
        {
            Player player = Player.Instance;
            string hex = ColorUtility.ToHtmlStringRGB(accent);
            var sb = new System.Text.StringBuilder();
            sb.Append("<line-height=140%>");
            sb.Append($"<color=#{hex}><b><pos=0%>MAP<pos=48%>SOLO<pos=66%>CO-OP<pos=84%>PARTY</b></color>\n");

            LevelDatabase db = LevelDatabase.Instance;
            int count = db != null ? db.Count : 0;
            for (int i = 0; i < count; i++)
            {
                LevelEntry level = db.Get(i);
                if (level == null) continue;
                string name = string.IsNullOrEmpty(level.displayName) ? level.id : level.displayName;
                sb.Append($"<pos=0%>{name.ToUpperInvariant()}");
                sb.Append($"<pos=48%>{Wave(player, level.id, WaveMode.Solo)}");
                sb.Append($"<pos=66%>{Wave(player, level.id, WaveMode.Coop)}");
                sb.Append($"<pos=84%>{Wave(player, level.id, WaveMode.Party)}\n");
            }
            wavesTableText.alignment = TextAlignmentOptions.TopLeft;
            wavesTableText.text = sb.ToString();
        }

        private static string Wave(Player player, string levelId, WaveMode mode)
        {
            int best = player != null ? player.BestWave(levelId, mode) : 0;
            return best > 0 ? best.ToString("N0") : "-";
        }

        // ------------------------------------------------------------------ leaderboards

        public void OpenLeaderboards()
        {
            if (leaderboardPage == null) return;
            OpenPage(leaderboardPage);
            SelectBoard(mapIndex, modeIndex, scopeIndex);
            Select(mapTabs.Count > 0 ? mapTabs[mapIndex].gameObject : null);
        }

        private RectTransform BuildLeaderboardPage()
        {
            RectTransform page = NewPage("LeaderboardsPage", "LEADERBOARDS");
            RectTransform body = NewRect("Body", page);
            Stretch(body, 40f, 0f);
            body.offsetMax = new Vector2(-40f, -130f);
            body.offsetMin = new Vector2(40f, 30f);
            var column = body.gameObject.AddComponent<VerticalLayoutGroup>();
            column.spacing = 12f;
            column.childControlWidth = column.childControlHeight = true;
            column.childForceExpandWidth = true;
            column.childForceExpandHeight = false;

            // Map, mode and scope tabs.
            RectTransform maps = TabRow(body, 64f);
            foreach (string map in MapLabels())
            {
                int index = mapTabs.Count;
                mapTabs.Add(NewButton(maps, map, () => SelectBoard(index, modeIndex, scopeIndex), 28f));
            }

            RectTransform modes = TabRow(body, 56f);
            for (int i = 0; i < 3; i++)
            {
                int index = i;
                modeTabs.Add(NewButton(modes, "", () => SelectBoard(mapIndex, index, scopeIndex), 26f));
            }

            RectTransform scopes = TabRow(body, 56f);
            string[] scopeNames = { "TOP 10", "FRIENDS", "AROUND ME" };
            for (int i = 0; i < scopeNames.Length; i++)
            {
                int index = i;
                scopeTabs.Add(NewButton(scopes, scopeNames[i], () => SelectBoard(mapIndex, modeIndex, index), 26f));
            }

            boardTitle = NewText(labelTemplate, Row(body, 50f), "", 32f, TextAlignmentOptions.Center);
            Stretch(boardTitle.rectTransform, 0f, 0f);
            boardTitle.color = accent;

            // Header + rows.
            AddRow(body, new[] { "#", "PLAYER", "SCORE" }, true);
            for (int i = 0; i < LeaderboardRows; i++) AddRow(body, new[] { "", "", "" }, false);

            RectTransform footer = Row(body, 44f);
            boardYourBest = NewText(labelTemplate, footer, "", 26f, TextAlignmentOptions.Left);
            Stretch(boardYourBest.rectTransform, 10f, 0f);
            boardStatus = NewText(labelTemplate, footer, "", 26f, TextAlignmentOptions.Right);
            Stretch(boardStatus.rectTransform, 10f, 0f);

            WireTabNavigation(new[] { mapTabs, modeTabs, scopeTabs });
            page.gameObject.SetActive(false);
            return page;
        }

        private void AddRow(RectTransform parent, string[] cells, bool header)
        {
            RectTransform row = Row(parent, header ? 40f : 38f);
            Image back = row.gameObject.AddComponent<Image>();
            back.raycastTarget = false;
            back.color = header ? new Color(0f, 0f, 0f, 0f) : new Color(1f, 1f, 1f, rowBacks.Count % 2 == 0 ? 0.06f : 0.02f);

            float[] starts = { 0f, 0.14f, 0.72f };
            float[] ends = { 0.14f, 0.72f, 1f };
            TextAlignmentOptions[] aligns = { TextAlignmentOptions.Center, TextAlignmentOptions.Left, TextAlignmentOptions.Right };
            var texts = new TextMeshProUGUI[3];
            for (int c = 0; c < 3; c++)
            {
                TextMeshProUGUI t = NewText(header ? labelTemplate : valueTemplate, row, cells[c], header ? 24f : 28f, aligns[c]);
                RectTransform rt = t.rectTransform;
                rt.anchorMin = new Vector2(starts[c], 0f);
                rt.anchorMax = new Vector2(ends[c], 1f);
                rt.offsetMin = new Vector2(12f, 0f);
                rt.offsetMax = new Vector2(-12f, 0f);
                t.enableAutoSizing = true;
                t.fontSizeMin = 14f;
                t.fontSizeMax = header ? 24f : 28f;
                if (header) t.color = accent;
                texts[c] = t;
            }
            if (!header)
            {
                rowCells.Add(texts);
                rowBacks.Add(back);
            }
        }

        private static IEnumerable<string> MapLabels()
        {
            LevelDatabase db = LevelDatabase.Instance;
            int count = db != null ? db.Count : 0;
            for (int i = 0; i < count; i++)
            {
                LevelEntry level = db.Get(i);
                yield return (string.IsNullOrEmpty(level.displayName) ? level.id : level.displayName).ToUpperInvariant();
            }
            yield return "OVERALL";
        }

        private bool IsOverall(int map) => map >= mapTabs.Count - 1;

        private BoardChoice Board(int map, int mode)
        {
            if (IsOverall(map))
            {
                switch (mode)
                {
                    case 1:
                        return new BoardChoice { label = "CUBES DESTROYED", steamName = "Cubes Destroyed", yourBest = () => Player.Instance != null ? Player.Instance.cubesEliminated : 0 };
                    case 2:
                        return new BoardChoice
                        {
                            label = "BEST ACCURACY", steamName = "Best Accuracy", percent = true,
                            yourBest = () => Player.Instance != null && Player.Instance.totalProjectilesFired != 0 ? Player.Instance.cubesEliminated * 100 / Player.Instance.totalProjectilesFired : 0
                        };
                    default:
                        return new BoardChoice { label = "MOST POINTS", steamName = "Most Points", yourBest = () => Player.Instance != null ? Player.Instance.totalPointsEarned : 0 };
                }
            }

            LevelEntry level = LevelDatabase.Instance != null ? LevelDatabase.Instance.Get(map) : null;
            string levelId = level != null ? level.id : WaveLeaderboards.Arena;
            WaveMode waveMode = mode == 1 ? WaveMode.Coop : mode == 2 ? WaveMode.Party : WaveMode.Solo;
            WaveBoard board = WaveLeaderboards.For(levelId, waveMode);
            string mapName = level != null && !string.IsNullOrEmpty(level.displayName) ? level.displayName : levelId;
            string modeName = waveMode == WaveMode.Coop ? "CO-OP" : waveMode.ToString().ToUpperInvariant();
            return new BoardChoice
            {
                label = $"HIGHEST {modeName} WAVE - {mapName.ToUpperInvariant()}",
                steamName = board != null ? board.SteamName : null,
                yourBest = () => Player.Instance != null ? Player.Instance.BestWave(levelId, waveMode) : 0
            };
        }

        private void SelectBoard(int map, int mode, int scope)
        {
            mapIndex = Mathf.Clamp(map, 0, Mathf.Max(0, mapTabs.Count - 1));
            modeIndex = Mathf.Clamp(mode, 0, 2);
            scopeIndex = Mathf.Clamp(scope, 0, 2);

            string[] modeNames = IsOverall(mapIndex) ? new[] { "POINTS", "CUBES", "ACCURACY" } : new[] { "SOLO", "CO-OP", "PARTY" };
            for (int i = 0; i < modeTabs.Count; i++) SetLabel(modeTabs[i], modeNames[i]);
            for (int i = 0; i < mapTabs.Count; i++) SetTab(mapTabs[i], i == mapIndex);
            for (int i = 0; i < modeTabs.Count; i++) SetTab(modeTabs[i], i == modeIndex);
            for (int i = 0; i < scopeTabs.Count; i++) SetTab(scopeTabs[i], i == scopeIndex);

            BoardChoice board = Board(mapIndex, modeIndex);
            boardTitle.text = board.label;
            int best = board.yourBest();
            boardYourBest.text = best > 0 ? $"YOUR BEST: {best:N0}{(board.percent ? "%" : "")}" : "YOUR BEST: -";
            ClearRows();
            LoadEntries(board, (LeaderboardScope)scopeIndex);
        }

        private void ClearRows()
        {
            for (int i = 0; i < rowCells.Count; i++)
            {
                foreach (TextMeshProUGUI t in rowCells[i]) { t.text = ""; t.color = textColor; }
                rowBacks[i].color = new Color(1f, 1f, 1f, i % 2 == 0 ? 0.06f : 0.02f);
            }
        }

        private void LoadEntries(BoardChoice board, LeaderboardScope scope)
        {
            int id = ++requestId;
#if !DISABLESTEAMWORKS
            if (LeaderboardManager.HasSteamLeaderboards && !string.IsNullOrEmpty(board.steamName))
            {
                boardStatus.text = "LOADING...";
                SteamLeaderboardManager.Instance.RequestEntries(board.steamName, scope, LeaderboardRows, rows =>
                {
                    if (id != requestId || this == null) return;   // a newer board was picked meanwhile
                    ShowEntries(rows, board.percent);
                });
                return;
            }
#endif
            boardStatus.text = "SIGN IN WITH STEAM TO SEE LEADERBOARDS";
        }

        private void ShowEntries(List<LeaderboardRow> rows, bool percent)
        {
            if (rows == null)
            {
                boardStatus.text = "COULDN'T REACH STEAM";
                return;
            }
            boardStatus.text = rows.Count == 0 ? (scopeIndex == 1 ? "NO FRIENDS ON THIS BOARD YET" : "NO SCORES YET - BE THE FIRST!") : "";

            for (int i = 0; i < rowCells.Count; i++)
            {
                if (i >= rows.Count) continue;
                LeaderboardRow r = rows[i];
                TextMeshProUGUI[] cells = rowCells[i];
                cells[0].text = r.rank.ToString("N0");
                cells[1].text = string.IsNullOrEmpty(r.name) ? "?" : r.name;
                cells[2].text = r.score.ToString("N0") + (percent ? "%" : "");
                Color c = r.isMe ? accent : textColor;
                foreach (TextMeshProUGUI t in cells) t.color = c;
                if (r.isMe) rowBacks[i].color = new Color(accent.r, accent.g, accent.b, 0.18f);
                else if (r.rank <= 3 && scopeIndex == 0) cells[0].text = $"<b>{r.rank}</b>";
            }
        }

        // ------------------------------------------------------------------ achievements

        public void OpenAchievements()
        {
            if (achievementsPage == null) return;
            OpenPage(achievementsPage);
            RefreshAchievements();
            Scrollbar bar = achievementsPage.GetComponentInChildren<Scrollbar>(true);
            Select(bar != null ? bar.gameObject : null);
        }

        private RectTransform BuildAchievementsPage()
        {
            RectTransform page = NewPage("AchievementsPage", "ACHIEVEMENTS");

            // Summary: "7 / 16 UNLOCKED" and an overall bar.
            RectTransform summary = NewRect("Summary", page);
            summary.anchorMin = new Vector2(0f, 1f);
            summary.anchorMax = new Vector2(1f, 1f);
            summary.pivot = new Vector2(0.5f, 1f);
            summary.offsetMin = new Vector2(60f, -180f);
            summary.offsetMax = new Vector2(-60f, -125f);
            achievementSummary = NewText(labelTemplate, summary, "", 30f, TextAlignmentOptions.Left);
            achievementSummary.rectTransform.anchorMin = new Vector2(0f, 0f);
            achievementSummary.rectTransform.anchorMax = new Vector2(0.4f, 1f);
            achievementSummary.rectTransform.offsetMin = achievementSummary.rectTransform.offsetMax = Vector2.zero;
            RectTransform summaryBar = NewBar(summary, out achievementSummaryFill);
            summaryBar.anchorMin = new Vector2(0.42f, 0.3f);
            summaryBar.anchorMax = new Vector2(1f, 0.7f);
            summaryBar.offsetMin = summaryBar.offsetMax = Vector2.zero;

            // Scrolling grid of cards.
            RectTransform scrollArea = NewRect("Scroll", page);
            Stretch(scrollArea, 0f, 0f);
            scrollArea.offsetMin = new Vector2(50f, 35f);
            scrollArea.offsetMax = new Vector2(-50f, -195f);
            var scroll = scrollArea.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 40f;

            RectTransform viewport = NewRect("Viewport", scrollArea);
            Stretch(viewport, 0f, 0f);
            viewport.offsetMax = new Vector2(-34f, 0f);
            viewport.gameObject.AddComponent<RectMask2D>();
            Image viewportHit = viewport.gameObject.AddComponent<Image>();
            viewportHit.color = new Color(0f, 0f, 0f, 0f);   // lets the mouse wheel / drag reach the scroll view

            achievementContent = NewRect("Content", viewport);
            achievementContent.anchorMin = new Vector2(0f, 1f);
            achievementContent.anchorMax = new Vector2(1f, 1f);
            achievementContent.pivot = new Vector2(0.5f, 1f);
            achievementContent.sizeDelta = Vector2.zero;
            var grid = achievementContent.gameObject.AddComponent<GridLayoutGroup>();
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 2;
            grid.spacing = new Vector2(16f, 16f);
            grid.padding = new RectOffset(4, 4, 4, 4);
            achievementContent.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // A scrollbar the controller can select and move.
            RectTransform barArea = NewRect("Scrollbar", scrollArea);
            barArea.anchorMin = new Vector2(1f, 0f);
            barArea.anchorMax = new Vector2(1f, 1f);
            barArea.pivot = new Vector2(1f, 0.5f);
            barArea.sizeDelta = new Vector2(24f, 0f);
            Image barBack = barArea.gameObject.AddComponent<Image>();
            barBack.color = new Color(0f, 0f, 0f, 0.4f);
            RectTransform handleArea = NewRect("Sliding Area", barArea);
            Stretch(handleArea, 0f, 0f);
            RectTransform handle = NewRect("Handle", handleArea);
            Stretch(handle, 0f, 0f);
            Image handleImage = handle.gameObject.AddComponent<Image>();
            handleImage.color = accent;
            var scrollbar = barArea.gameObject.AddComponent<Scrollbar>();
            scrollbar.direction = Scrollbar.Direction.BottomToTop;
            scrollbar.handleRect = handle;
            scrollbar.targetGraphic = handleImage;
            ColorBlock colors = scrollbar.colors;
            colors.highlightedColor = Color.white;
            colors.selectedColor = Color.white;
            scrollbar.colors = colors;

            scroll.viewport = viewport;
            scroll.content = achievementContent;
            scroll.verticalScrollbar = scrollbar;
            scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;

            page.gameObject.SetActive(false);
            return page;
        }

        private void RefreshAchievements()
        {
            for (int i = achievementContent.childCount - 1; i >= 0; i--)
                Destroy(achievementContent.GetChild(i).gameObject);

            // Two columns sized to the visible width.
            Canvas.ForceUpdateCanvases();
            var grid = achievementContent.GetComponent<GridLayoutGroup>();
            float width = ((RectTransform)achievementContent.parent).rect.width;
            grid.cellSize = new Vector2(Mathf.Max(200f, (width - grid.spacing.x - 8f) / 2f), 150f);

            Player player = Player.Instance;
            int unlocked = 0;
            foreach (AchievementDef a in AchievementCatalog.All)
            {
                bool done = IsUnlocked(player, a);
                if (done) unlocked++;
                long value = player != null ? player.StatFor(a.Stat) : 0;
                AddCard(a, done, value);
            }

            int total = AchievementCatalog.All.Count;
            achievementSummary.text = $"{unlocked} / {total} UNLOCKED";
            SetFill(achievementSummaryFill, total > 0 ? (float)unlocked / total : 0f);
            achievementContent.anchoredPosition = Vector2.zero;
        }

        private static bool IsUnlocked(Player player, AchievementDef a)
        {
            if (player != null && player.HasAchievement(a)) return true;
#if !DISABLESTEAMWORKS
            if (SteamManager.IsReady && CloudSaveLogin.Instance != null && CloudSaveLogin.Instance.currentSSO == CloudSaveLogin.ssoOption.Steam
                && Steamworks.SteamUserStats.GetAchievement(a.Id, out bool achieved) && achieved)
                return true;
#endif
            return false;
        }

        private void AddCard(AchievementDef a, bool done, long value)
        {
            RectTransform card = NewRect(a.Id, achievementContent);
            Image back = card.gameObject.AddComponent<Image>();
            back.sprite = panelTemplate.sprite;
            back.type = panelTemplate.type;
            back.color = done ? new Color(accent.r, accent.g, accent.b, 0.30f) : new Color(0f, 0f, 0f, 0.45f);
            back.raycastTarget = false;

            TextMeshProUGUI name = NewText(labelTemplate, card, a.Name.ToUpperInvariant(), 30f, TextAlignmentOptions.TopLeft);
            Place(name.rectTransform, 0f, 0.62f, 1f, 1f, 18f, 10f);
            name.color = done ? accent : textColor;

            TextMeshProUGUI state = NewText(labelTemplate, card, done ? "UNLOCKED" : "", 22f, TextAlignmentOptions.TopRight);
            Place(state.rectTransform, 0.6f, 0.62f, 1f, 1f, 18f, 12f);
            state.color = accent;

            TextMeshProUGUI desc = NewText(valueTemplate, card, a.Description, 22f, TextAlignmentOptions.TopLeft);
            Place(desc.rectTransform, 0f, 0.3f, 1f, 0.62f, 18f, 2f);
            desc.textWrappingMode = TextWrappingModes.Normal;
            desc.color = done ? textColor : new Color(textColor.r, textColor.g, textColor.b, 0.75f);

            if (a.HasProgress)
            {
                RectTransform bar = NewBar(card, out Image fill);
                Place(bar, 0f, 0.1f, 0.62f, 0.24f, 18f, 0f);
                SetFill(fill, done ? 1f : AchievementCatalog.Progress(a, value, false));
                TextMeshProUGUI count = NewText(valueTemplate, card, done ? $"{a.Target:N0}" : $"{Math.Min(value, a.Target):N0} / {a.Target:N0}", 20f, TextAlignmentOptions.Right);
                Place(count.rectTransform, 0.62f, 0.04f, 1f, 0.3f, 18f, 0f);
            }
            else if (!done)
            {
                TextMeshProUGUI hint = NewText(valueTemplate, card, "LOCKED", 20f, TextAlignmentOptions.Left);
                Place(hint.rectTransform, 0f, 0.04f, 1f, 0.3f, 18f, 0f);
                hint.color = new Color(textColor.r, textColor.g, textColor.b, 0.5f);
            }
        }

        // ------------------------------------------------------------------ pages

        private RectTransform NewPage(string name, string title)
        {
            RectTransform page = (RectTransform)Instantiate(panelTemplate.gameObject, statsRoot).transform;
            page.name = name;
            for (int i = page.childCount - 1; i >= 0; i--) Destroy(page.GetChild(i).gameObject);
            foreach (MonoBehaviour extra in page.GetComponents<MonoBehaviour>())
                if (!(extra is Graphic)) Destroy(extra);
            page.anchorMin = Vector2.zero;
            page.anchorMax = Vector2.one;
            page.offsetMin = new Vector2(180f, 60f);
            page.offsetMax = new Vector2(-180f, -60f);
            page.SetAsLastSibling();

            TextMeshProUGUI heading = NewText(titleTemplate, page, title, 64f, TextAlignmentOptions.Center);
            heading.rectTransform.anchorMin = new Vector2(0f, 1f);
            heading.rectTransform.anchorMax = new Vector2(1f, 1f);
            heading.rectTransform.pivot = new Vector2(0.5f, 1f);
            heading.rectTransform.anchoredPosition = new Vector2(0f, -20f);
            heading.rectTransform.sizeDelta = new Vector2(-240f, 100f);
            heading.enableAutoSizing = true;
            heading.fontSizeMax = 64f;
            heading.fontSizeMin = 30f;

            if (closeTemplate != null)
            {
                Button close = Instantiate(closeTemplate, page);
                close.name = "Close";
                close.onClick = new Button.ButtonClickedEvent();
                close.onClick.AddListener(ClosePages);
            }
            return page;
        }

        private void OpenPage(RectTransform page)
        {
            if (morePanel != null) morePanel.gameObject.SetActive(false);
            if (leaderboardPage != null) leaderboardPage.gameObject.SetActive(page == leaderboardPage);
            if (achievementsPage != null) achievementsPage.gameObject.SetActive(page == achievementsPage);
        }

        /// <summary>Closes the Leaderboards/Achievements page and returns to the More panel.</summary>
        public void ClosePages()
        {
            bool wasOpen = IsPageOpen;
            if (leaderboardPage != null) leaderboardPage.gameObject.SetActive(false);
            if (achievementsPage != null) achievementsPage.gameObject.SetActive(false);
            if (wasOpen && morePanel != null)
            {
                morePanel.gameObject.SetActive(true);
                Select(buttonTemplate.gameObject);
            }
        }

        public bool IsPageOpen =>
            (leaderboardPage != null && leaderboardPage.gameObject.activeSelf) ||
            (achievementsPage != null && achievementsPage.gameObject.activeSelf);

        private void Update()
        {
            // Escape / controller B closes an open page.
            if (!IsPageOpen) return;
            var keyboard = UnityEngine.InputSystem.Keyboard.current;
            var pad = UnityEngine.InputSystem.Gamepad.current;
            if ((keyboard != null && keyboard.escapeKey.wasPressedThisFrame) || (pad != null && pad.buttonEast.wasPressedThisFrame))
                ClosePages();
        }

        // ------------------------------------------------------------------ helpers

        private static RectTransform NewRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = parent.gameObject.layer;
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            return rt;
        }

        private static void Stretch(RectTransform rt, float x, float y)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(x, y);
            rt.offsetMax = new Vector2(-x, -y);
        }

        private static void Place(RectTransform rt, float minX, float minY, float maxX, float maxY, float padX, float padTop)
        {
            rt.anchorMin = new Vector2(minX, minY);
            rt.anchorMax = new Vector2(maxX, maxY);
            rt.offsetMin = new Vector2(padX, 0f);
            rt.offsetMax = new Vector2(-padX, -padTop);
        }

        private static RectTransform Row(RectTransform parent, float height)
        {
            RectTransform row = NewRect("Row", parent);
            var element = row.gameObject.AddComponent<LayoutElement>();
            element.minHeight = element.preferredHeight = height;
            return row;
        }

        private static RectTransform TabRow(RectTransform parent, float height)
        {
            RectTransform row = Row(parent, height);
            var layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 14f;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = layout.childForceExpandHeight = true;
            return row;
        }

        private TextMeshProUGUI NewText(TextMeshProUGUI template, Transform parent, string text, float size, TextAlignmentOptions align)
        {
            TextMeshProUGUI t = Instantiate(template, parent);
            t.name = "Text";
            t.text = text;
            t.fontSize = size;
            t.enableAutoSizing = false;
            t.alignment = align;
            t.raycastTarget = false;
            t.richText = true;
            t.overflowMode = TextOverflowModes.Ellipsis;
            RectTransform rt = t.rectTransform;
            rt.localScale = Vector3.one;
            rt.localRotation = Quaternion.identity;
            return t;
        }

        private Button NewButton(Transform parent, string label, Action onClick, float fontSize)
        {
            Button b = Instantiate(buttonTemplate, parent);
            b.name = string.IsNullOrEmpty(label) ? "Tab" : label;
            b.gameObject.SetActive(true);
            b.transform.localScale = Vector3.one;
            b.onClick = new Button.ButtonClickedEvent();   // drop the template's Inspector listeners
            b.onClick.AddListener(() => onClick());
            TextMeshProUGUI text = b.GetComponentInChildren<TextMeshProUGUI>(true);
            if (text != null)
            {
                text.text = label;
                text.enableAutoSizing = true;
                text.fontSizeMax = fontSize;
                text.fontSizeMin = 14f;
                RectTransform rt = text.rectTransform;
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = new Vector2(10f, 4f);
                rt.offsetMax = new Vector2(-10f, -4f);
            }
            return b;
        }

        private static void SetLabel(Button b, string label)
        {
            TextMeshProUGUI text = b.GetComponentInChildren<TextMeshProUGUI>(true);
            if (text != null) text.text = label;
        }

        // Selected tabs use the button colour; the others are dimmed.
        private void SetTab(Button b, bool selected)
        {
            if (b == null) return;
            Image image = b.GetComponent<Image>();
            if (image != null) image.color = selected ? accent : dimAccent;
            TextMeshProUGUI text = b.GetComponentInChildren<TextMeshProUGUI>(true);
            if (text != null) text.alpha = selected ? 1f : 0.7f;
        }

        private RectTransform NewBar(Transform parent, out Image fill)
        {
            RectTransform bar = NewRect("Bar", parent);
            Image back = bar.gameObject.AddComponent<Image>();
            back.color = new Color(0f, 0f, 0f, 0.5f);
            back.raycastTarget = false;
            RectTransform fillRect = NewRect("Fill", bar);
            fill = fillRect.gameObject.AddComponent<Image>();
            fill.color = accent;
            fill.raycastTarget = false;
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = new Vector2(0f, 1f);
            fillRect.offsetMin = fillRect.offsetMax = Vector2.zero;
            return bar;
        }

        private static void SetFill(Image fill, float amount)
        {
            if (fill == null) return;
            RectTransform rt = fill.rectTransform;
            rt.anchorMax = new Vector2(Mathf.Clamp01(amount), 1f);
            rt.offsetMax = Vector2.zero;
        }

        // Up/down moves between tab rows; left/right within a row.
        private static void WireTabNavigation(List<Button>[] rows)
        {
            for (int r = 0; r < rows.Length; r++)
            {
                List<Button> row = rows[r];
                for (int i = 0; i < row.Count; i++)
                {
                    var nav = new Navigation { mode = Navigation.Mode.Explicit };
                    nav.selectOnLeft = i > 0 ? row[i - 1] : null;
                    nav.selectOnRight = i < row.Count - 1 ? row[i + 1] : null;
                    nav.selectOnUp = r > 0 ? Nearest(rows[r - 1], i, row.Count) : null;
                    nav.selectOnDown = r < rows.Length - 1 ? Nearest(rows[r + 1], i, row.Count) : null;
                    row[i].navigation = nav;
                }
            }
        }

        private static Selectable Nearest(List<Button> row, int index, int fromCount)
        {
            if (row.Count == 0) return null;
            int i = Mathf.Clamp(Mathf.RoundToInt((index + 0.5f) / fromCount * row.Count - 0.5f), 0, row.Count - 1);
            return row[i];
        }

        private static void Select(GameObject target)
        {
            if (EventSystem.current == null || target == null) return;
            EventSystem.current.SetSelectedGameObject(null);
            EventSystem.current.SetSelectedGameObject(target);
        }
    }
}
