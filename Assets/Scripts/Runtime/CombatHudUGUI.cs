using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CombatHudUGUI : MonoBehaviour
{
    public Health2D player;
    public StageFlowController stage;
    public Sprite heartSprite;

    private Image[] hearts;
    private Image energyFill;
    private Text stateText;
    private Text resultText;
    private Button restartButton;
    private GameObject upgradePanel;
    private Button[] upgradeButtons;
    private Text[] upgradeButtonTexts;
    private GameObject bossBarRoot;
    private Image bossBarFill;
    private Text bossBarText;
    private float resultShowTime;
    private const int BossHealthPerPage = 80;

    private void Start()
    {
        FileLogger.Log("CombatHudUGUI.Start");
        BuildHud();
    }

    private void Update()
    {
        UpdateBossBar();
        UpdateHearts();
        UpdateEnergyBar();
        UpdateUpgradePanel();
        UpdateTexts();
    }

    private void BuildHud()
    {
        GameObject canvasObject = new GameObject("Combat HUD Canvas");
        canvasObject.transform.SetParent(transform);

        var canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;

        var scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(621f, 1080f);
        scaler.matchWidthOrHeight = 1f;

        canvasObject.AddComponent<GraphicRaycaster>();
        EnsureEventSystem();

        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        RectTransform root = canvasObject.GetComponent<RectTransform>();

        // Full-width energy bar at the very top
        GameObject barRoot = new GameObject("Energy Bar");
        barRoot.transform.SetParent(root, false);
        var barBg = barRoot.AddComponent<Image>();
        barBg.color = new Color(0f, 0f, 0f, 0.5f);
        RectTransform barRect = barBg.rectTransform;
        barRect.anchorMin = new Vector2(0f, 1f);
        barRect.anchorMax = new Vector2(1f, 1f);
        barRect.pivot = new Vector2(0.5f, 1f);
        barRect.anchoredPosition = Vector2.zero;
        barRect.sizeDelta = new Vector2(0f, 36f);

        GameObject fill = new GameObject("Energy Fill");
        fill.transform.SetParent(barRoot.transform, false);
        energyFill = fill.AddComponent<Image>();
        energyFill.color = new Color(0.3f, 0.95f, 1f, 0.85f);
        RectTransform fillRect = energyFill.rectTransform;
        fillRect.anchorMin = new Vector2(0f, 0f);
        fillRect.anchorMax = new Vector2(0f, 1f);
        fillRect.pivot = new Vector2(0f, 0.5f);
        fillRect.anchoredPosition = Vector2.zero;
        fillRect.sizeDelta = Vector2.zero;

        // Hearts below energy bar
        hearts = new Image[player != null ? player.maxHealth : 6];
        for (int i = 0; i < hearts.Length; i++)
        {
            GameObject heart = new GameObject("Heart " + i);
            heart.transform.SetParent(root, false);
            var image = heart.AddComponent<Image>();
            image.sprite = heartSprite;
            image.preserveAspect = true;
            RectTransform rect = image.rectTransform;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(20f + i * 50f, -48f);
            rect.sizeDelta = new Vector2(44f, 44f);
            hearts[i] = image;
        }

        // Stage info text
        stateText = CreateText(root, "Stage Text", font, new Vector2(20f, -102f), new Vector2(580f, 36f), 22, TextAnchor.UpperLeft);

        // Boss HP bar
        bossBarRoot = new GameObject("Boss HP Bar");
        bossBarRoot.transform.SetParent(root, false);
        var bossBgImg = bossBarRoot.AddComponent<Image>();
        bossBgImg.color = new Color(0.1f, 0.05f, 0.05f, 0.75f);
        RectTransform bossBgRect = bossBgImg.rectTransform;
        bossBgRect.anchorMin = new Vector2(0.5f, 1f);
        bossBgRect.anchorMax = new Vector2(0.5f, 1f);
        bossBgRect.pivot = new Vector2(0.5f, 1f);
        bossBgRect.anchoredPosition = new Vector2(0f, -4f);
        bossBgRect.sizeDelta = new Vector2(500f, 22f);

        GameObject bossFillObj = new GameObject("Boss HP Fill");
        bossFillObj.transform.SetParent(bossBarRoot.transform, false);
        bossBarFill = bossFillObj.AddComponent<Image>();
        bossBarFill.color = new Color(0.9f, 0.2f, 0.15f, 0.9f);
        RectTransform bossFillRect = bossBarFill.rectTransform;
        bossFillRect.anchorMin = new Vector2(0f, 0f);
        bossFillRect.anchorMax = new Vector2(1f, 1f);
        bossFillRect.pivot = new Vector2(0f, 0.5f);
        bossFillRect.anchoredPosition = Vector2.zero;
        bossFillRect.sizeDelta = Vector2.zero;

        bossBarText = CreateText(bossBarRoot.transform as RectTransform, "Boss Bar Text", font, Vector2.zero, new Vector2(480f, 20f), 14, TextAnchor.MiddleCenter);
        RectTransform bossTextRect = bossBarText.rectTransform;
        bossTextRect.anchorMin = new Vector2(0.5f, 0.5f);
        bossTextRect.anchorMax = new Vector2(0.5f, 0.5f);
        bossTextRect.pivot = new Vector2(0.5f, 0.5f);
        bossBarRoot.SetActive(false);

        BuildUpgradePanel(root, font);

        // Result text
        resultText = CreateText(root, "Result Text", font, new Vector2(0f, -60f), new Vector2(500f, 160f), 44, TextAnchor.MiddleCenter);
        resultText.raycastTarget = false;
        RectTransform resultRect = resultText.rectTransform;
        resultRect.anchorMin = new Vector2(0.5f, 0.5f);
        resultRect.anchorMax = new Vector2(0.5f, 0.5f);
        resultRect.pivot = new Vector2(0.5f, 0.5f);
        resultText.gameObject.SetActive(false);

        // Restart button
        GameObject restartObj = new GameObject("Restart Button");
        restartObj.transform.SetParent(root, false);
        var restartBg = restartObj.AddComponent<Image>();
        restartBg.color = new Color(0.2f, 0.5f, 0.3f, 0.9f);
        restartButton = restartObj.AddComponent<Button>();
        restartButton.targetGraphic = restartBg;
        restartButton.onClick.AddListener(OnRestartClicked);

        RectTransform restartRect = restartBg.rectTransform;
        restartRect.anchorMin = new Vector2(0.5f, 0.5f);
        restartRect.anchorMax = new Vector2(0.5f, 0.5f);
        restartRect.pivot = new Vector2(0.5f, 0.5f);
        restartRect.anchoredPosition = new Vector2(0f, -80f);
        restartRect.sizeDelta = new Vector2(200f, 50f);

        Text restartText = CreateText(restartRect, "Restart Label", font, Vector2.zero, new Vector2(180f, 40f), 22, TextAnchor.MiddleCenter);
        RectTransform rtRect = restartText.rectTransform;
        rtRect.anchorMin = new Vector2(0.5f, 0.5f);
        rtRect.anchorMax = new Vector2(0.5f, 0.5f);
        rtRect.pivot = new Vector2(0.5f, 0.5f);
        restartText.text = "重新开始";

        restartObj.SetActive(false);
    }

    private void EnsureEventSystem()
    {
        if (EventSystem.current != null) return;

        GameObject eventSystem = new GameObject("EventSystem");
        eventSystem.AddComponent<EventSystem>();
        eventSystem.AddComponent<StandaloneInputModule>();
    }

    private void BuildUpgradePanel(RectTransform root, Font font)
    {
        upgradePanel = new GameObject("Upgrade Choice Panel");
        upgradePanel.transform.SetParent(root, false);
        var panelImage = upgradePanel.AddComponent<Image>();
        panelImage.color = new Color(0f, 0f, 0f, 0.78f);
        RectTransform panelRect = panelImage.rectTransform;
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = Vector2.zero;
        panelRect.sizeDelta = new Vector2(480f, 500f);

        Text title = CreateText(panelRect, "Upgrade Title", font, new Vector2(0f, -20f), new Vector2(420f, 40f), 28, TextAnchor.MiddleCenter);
        RectTransform titleRect = title.rectTransform;
        titleRect.anchorMin = new Vector2(0.5f, 1f);
        titleRect.anchorMax = new Vector2(0.5f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        title.text = "选择强化";

        upgradeButtons = new Button[3];
        upgradeButtonTexts = new Text[3];
        CreateUpgradeButton(panelRect, font, 0, new Vector2(0f, -100f));
        CreateUpgradeButton(panelRect, font, 1, new Vector2(0f, -220f));
        CreateUpgradeButton(panelRect, font, 2, new Vector2(0f, -340f));

        upgradePanel.SetActive(false);
    }

    private void CreateUpgradeButton(RectTransform parent, Font font, int offerIndex, Vector2 anchoredPosition)
    {
        GameObject buttonObject = new GameObject("Upgrade Option " + offerIndex);
        buttonObject.transform.SetParent(parent, false);
        var image = buttonObject.AddComponent<Image>();
        image.color = new Color(0.12f, 0.2f, 0.32f, 0.95f);
        var button = buttonObject.AddComponent<Button>();
        int capturedIndex = offerIndex;
        button.onClick.AddListener(() =>
        {
            if (stage != null) stage.ChooseUpgrade(capturedIndex);
        });

        RectTransform rect = image.rectTransform;
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = new Vector2(440f, 100f);

        Text text = CreateText(rect, "Label", font, Vector2.zero, new Vector2(400f, 80f), 24, TextAnchor.MiddleCenter);
        RectTransform textRect = text.rectTransform;
        textRect.anchorMin = new Vector2(0.5f, 0.5f);
        textRect.anchorMax = new Vector2(0.5f, 0.5f);
        textRect.pivot = new Vector2(0.5f, 0.5f);
        upgradeButtons[offerIndex] = button;
        upgradeButtonTexts[offerIndex] = text;
    }

    private Text CreateText(RectTransform parent, string name, Font font, Vector2 anchoredPosition, Vector2 size, int fontSize, TextAnchor alignment)
    {
        GameObject textObject = new GameObject(name);
        textObject.transform.SetParent(parent, false);
        var text = textObject.AddComponent<Text>();
        text.font = font;
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = Color.white;
        RectTransform rect = text.rectTransform;
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;
        return text;
    }

    private void UpdateBossBar()
    {
        if (bossBarRoot == null || stage == null) return;

        bool show = stage.BossAlive && stage.BossHealth != null;
        bossBarRoot.SetActive(show);
        if (!show) return;

        int hp = stage.BossHealth.currentHealth;
        int maxHp = stage.BossHealth.maxHealth;
        int pageHp = hp % BossHealthPerPage;
        if (pageHp == 0 && hp > 0) pageHp = BossHealthPerPage;
        float fill = pageHp / (float)BossHealthPerPage;
        int totalPages = Mathf.CeilToInt(maxHp / (float)BossHealthPerPage);
        int currentPage = totalPages - (hp - 1) / BossHealthPerPage;

        RectTransform fillRect = bossBarFill.rectTransform;
        fillRect.anchorMax = new Vector2(fill, 1f);
        bossBarText.text = (totalPages - currentPage + 1).ToString();
    }

    private void UpdateHearts()
    {
        if (hearts == null || player == null) return;

        for (int i = 0; i < hearts.Length; i++)
        {
            hearts[i].enabled = i < player.currentHealth;
        }
    }

    private void UpdateEnergyBar()
    {
        if (energyFill == null || stage == null) return;

        RectTransform rect = energyFill.rectTransform;
        rect.anchorMax = new Vector2(stage.energyProgress, 1f);
        rect.sizeDelta = Vector2.zero;
    }

    private void UpdateUpgradePanel()
    {
        if (upgradePanel == null || stage == null) return;

        bool show = stage.UpgradeSelectionPending;
        upgradePanel.SetActive(show);
        if (!show || upgradeButtons == null) return;

        for (int i = 0; i < upgradeButtons.Length; i++)
        {
            bool active = i < stage.CurrentOfferCount;
            upgradeButtons[i].gameObject.SetActive(active);
            if (!active) continue;

            PlayerUpgradeOffer offer = stage.GetOffer(i);
            upgradeButtonTexts[i].text = offer.title;
        }
    }

    private float nextLogTime;
    private bool lastFinished;

    private void UpdateTexts()
    {
        if (stage == null) return;

        if (stateText != null)
        {
            stateText.text = "击杀 " + stage.enemiesDefeated + " / " + stage.killsRequiredForBoss + "    能量 " + stage.energyCollected + " / " + stage.energyRequired;
        }

        if (resultText != null)
        {
            bool finished = stage.IsFinished;
            if (finished != lastFinished)
            {
                FileLogger.Log("finished changed to " + finished + ", state=" + stage.state);
                lastFinished = finished;
            }
            resultText.gameObject.SetActive(finished);
            if (finished)
            {
                if (resultShowTime == 0f)
                {
                    resultShowTime = Time.unscaledTime;
                    FileLogger.Log("Result screen shown, state=" + stage.state);
                }
                resultText.text = stage.state == StageFlowController.StageState.Cleared
                    ? "Stage Clear\n击败数: " + stage.enemiesDefeated
                    : "Game Over\n击败数: " + stage.enemiesDefeated;

                if (restartButton != null)
                {
                    bool showButton = Time.unscaledTime - resultShowTime > 1f;
                    restartButton.gameObject.SetActive(showButton);
                }
            }
            else
            {
                resultShowTime = 0f;
            }
        }

        if (Time.time >= nextLogTime)
        {
            nextLogTime = Time.time + 3f;
            FileLogger.Log("HUD state: kills=" + stage.enemiesDefeated + " energy=" + stage.energyCollected + "/" + stage.energyRequired + " finished=" + stage.IsFinished + " state=" + stage.state + " timeScale=" + Time.timeScale);
        }
    }

    private void OnRestartClicked()
    {
        FileLogger.Log("OnRestartClicked called");
        restartButton.interactable = false;
        Time.timeScale = 1f;

        SquadChaser2D.ResetAllStatics();
        IceMissile2D.ResetAllStatics();
        ExplosionFlash2D.ResetAllStatics();

        if (stage != null)
        {
            Transform stageTransform = stage.transform;
            for (int i = stageTransform.childCount - 1; i >= 0; i--)
                Object.Destroy(stageTransform.GetChild(i).gameObject);

            var projectiles = Object.FindObjectsOfType<Projectile2D>();
            for (int i = 0; i < projectiles.Length; i++)
                Object.Destroy(projectiles[i].gameObject);

            var ice = Object.FindObjectsOfType<IceMissile2D>();
            for (int i = 0; i < ice.Length; i++)
                Object.Destroy(ice[i].gameObject);

            var bombs = Object.FindObjectsOfType<AirBombProjectile2D>();
            for (int i = 0; i < bombs.Length; i++)
                Object.Destroy(bombs[i].gameObject);

            var drones = Object.FindObjectsOfType<DroneBeamWeapon2D>();
            for (int i = 0; i < drones.Length; i++)
                Object.Destroy(drones[i].gameObject);

            var fires = Object.FindObjectsOfType<FireResidue2D>();
            for (int i = 0; i < fires.Length; i++)
                Object.Destroy(fires[i].gameObject);

            var flashes = Object.FindObjectsOfType<ExplosionFlash2D>();
            for (int i = 0; i < flashes.Length; i++)
                Object.Destroy(flashes[i].gameObject);

            var energies = Object.FindObjectsOfType<EnergyFragment2D>();
            for (int i = 0; i < energies.Length; i++)
                Object.Destroy(energies[i].gameObject);

            stage.ResetForNewGame();
        }

        if (player != null)
        {
            var health = player.GetComponent<Health2D>();
            if (health != null)
            {
                health.currentHealth = health.maxHealth;
            }

            var ctrl = player.GetComponent<PlayerController2D>();
            if (ctrl != null)
            {
                ctrl.cannonDamageLevel = 0;
                ctrl.cannonTrajectoryLevel = 0;
                ctrl.cannonFireRateLevel = 0;
                ctrl.iceDamageLevel = 0;
                ctrl.iceCountLevel = 0;
                ctrl.iceSlowLevel = 0;
                ctrl.airBombCountLevel = 0;
                ctrl.airBombExplosionLevel = 0;
                ctrl.airBombSplitLevel = 0;
                ctrl.droneDamageLevel = 0;
                ctrl.droneDurationLevel = 0;
                ctrl.droneWidthLevel = 0;
                ctrl.lifeRestoreLevel = 0;
                ctrl.iceUnlocked = false;
                ctrl.airBombUnlocked = false;
                ctrl.droneUnlocked = false;
            }

            player.gameObject.SetActive(true);
        }

        resultShowTime = 0f;
        lastFinished = false;
        FileLogger.Log("Restart complete");
    }
}
