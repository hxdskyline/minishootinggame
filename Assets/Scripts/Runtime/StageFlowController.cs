using UnityEngine;

public class StageFlowController : MonoBehaviour
{
    public enum StageState
    {
        Waves,
        Boss,
        Cleared,
        GameOver
    }

    private static readonly PlayerUpgradeType[] AllUpgradeTypes =
    {
        PlayerUpgradeType.CannonDamage,
        PlayerUpgradeType.CannonTrajectory,
        PlayerUpgradeType.CannonFireRate,
        PlayerUpgradeType.IceDamage,
        PlayerUpgradeType.IceCount,
        PlayerUpgradeType.IceSlow,
        PlayerUpgradeType.AirBombCount,
        PlayerUpgradeType.AirBombExplosion,
        PlayerUpgradeType.AirBombSplit,
        PlayerUpgradeType.DroneDamage,
        PlayerUpgradeType.DroneDuration,
        PlayerUpgradeType.DroneWidth
    };

    public Transform player;
    public PlayerController2D playerController;
    public Sprite chaserEnemySprite;
    public Sprite shooterEnemySprite;
    public Sprite advancedEnemySprite;
    public Sprite bossSprite;
    public Sprite enemyProjectileSprite;
    public Sprite energyFragmentSprite;

    public int totalWaves = 4;
    public float timeBetweenWaves = 3f;
    public float spawnY = 7f;
    public int killsRequiredForBoss = 200;
    public StageState state = StageState.Waves;
    public int currentWave;
    public int enemiesDefeated;
    public int energyCollected;
    public int energyRequired = 3;
    public float energyProgress;

    private readonly PlayerUpgradeOffer[] currentOffers = new PlayerUpgradeOffer[3];
    private int currentOfferCount;
    private int upgradeCount;
    private float aliveEnemyWeight;
    private int queuedSpawns;
    private float nextBatchTime;
    private int lastSquadKillCount;
    private AudioSource normalMusic;
    private AudioSource bossMusic;
    private Health2D playerHealth;
    private Health2D bossHealth;
    private bool bossSpawned;
    private bool upgradeSelectionPending;
    private float previousTimeScale = 1f;
    private bool lifeRestoreOffered;

    public bool IsFinished => state == StageState.Cleared || state == StageState.GameOver;
    public bool BossAlive => bossHealth != null;
    public Health2D BossHealth => bossHealth;
    public bool UpgradeSelectionPending => upgradeSelectionPending && !IsFinished;
    public int CurrentOfferCount => currentOfferCount;

    private void Start()
    {
        FileLogger.Log("StageFlowController.Start, state=" + state + " kills=" + enemiesDefeated + " energy=" + energyCollected + "/" + energyRequired);
        nextBatchTime = Time.time + 0.5f;
        nextDiagLogTime = Time.time + 1f;
        nextWaveDiagTime = Time.time + 1f;
        normalMusic = SoundHelper.PlayMusic("BossTrueLastUpper_288", loop: true, volume: 0.36f);
        SquadChaser2D.SquadDefeated += OnSquadDefeated;
        if (player != null)
        {
            playerHealth = player.GetComponent<Health2D>();
            playerController = player.GetComponent<PlayerController2D>();
            if (playerHealth != null) playerHealth.Died += OnPlayerDied;
        }
    }

    private float nextStageLogTime;
    private float nextDiagLogTime;
    private float nextWaveDiagTime;

    private void Update()
    {
        if (Time.time >= nextDiagLogTime)
        {
            nextDiagLogTime = Time.time + 2f;
            FileLogger.Log("Stage UpdateTick: enabled=" + enabled + " active=" + gameObject.activeInHierarchy + " state=" + state + " pending=" + upgradeSelectionPending + " finished=" + IsFinished + " time=" + Time.time);
        }

        if (upgradeSelectionPending || IsFinished)
        {
            if (Time.time >= nextStageLogTime)
            {
                nextStageLogTime = Time.time + 3f;
                FileLogger.Log("Stage Update BLOCKED: pending=" + upgradeSelectionPending + " finished=" + IsFinished + " state=" + state);
            }
            return;
        }

        if (Time.time >= nextStageLogTime)
        {
            nextStageLogTime = Time.time + 3f;
            FileLogger.Log("Stage Update: state=" + state + " alive=" + aliveEnemyWeight + " target=" + GetTargetAliveCount() + " queued=" + queuedSpawns + " kills=" + enemiesDefeated + " timeScale=" + Time.timeScale);
        }

        if (state == StageState.Waves)
        {
            UpdateWaves();
        }
        else if (state == StageState.Boss && bossSpawned && bossHealth == null)
        {
            state = StageState.Cleared;
        }
    }

    public PlayerUpgradeOffer GetOffer(int index)
    {
        if (index < 0 || index >= currentOfferCount) return default;
        return currentOffers[index];
    }

    public void CollectEnergy(int amount)
    {
        if (IsFinished || playerController == null) return;

        energyCollected += amount;
        energyProgress = Mathf.Clamp01(energyCollected / (float)energyRequired);

        if (!upgradeSelectionPending && energyCollected >= energyRequired)
        {
            energyCollected = energyRequired;
            energyProgress = 1f;
            BeginUpgradeSelection();
        }
    }

    private void BeginUpgradeSelection()
    {
        if (upgradeSelectionPending) return;

        BuildUpgradeOffers();
        if (currentOfferCount == 0)
        {
            energyCollected = 0;
            energyProgress = 0f;
            return;
        }

        previousTimeScale = Time.timeScale;
        upgradeSelectionPending = true;
        Time.timeScale = 0f;
        SoundHelper.PlayOneShot("PlayerLevelUp_304", 0.7f);
    }

    private static readonly PlayerUpgradeType[][] WeaponCategories =
    {
        new[] { PlayerUpgradeType.CannonDamage, PlayerUpgradeType.CannonTrajectory, PlayerUpgradeType.CannonFireRate },
        new[] { PlayerUpgradeType.IceDamage, PlayerUpgradeType.IceCount, PlayerUpgradeType.IceSlow },
        new[] { PlayerUpgradeType.AirBombCount, PlayerUpgradeType.AirBombExplosion, PlayerUpgradeType.AirBombSplit },
        new[] { PlayerUpgradeType.DroneDamage, PlayerUpgradeType.DroneDuration, PlayerUpgradeType.DroneWidth }
    };

    private void BuildUpgradeOffers()
    {
        currentOfferCount = 0;
        if (playerController == null) return;

        var categories = new System.Collections.Generic.List<int>();
        for (int c = 0; c < WeaponCategories.Length; c++)
            categories.Add(c);

        bool canOfferLifeRestore = !lifeRestoreOffered
            && playerHealth != null
            && playerHealth.currentHealth <= 2
            && playerHealth.currentHealth > 0;
        if (canOfferLifeRestore)
            categories.Add(-1);

        for (int i = categories.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            int temp = categories[i];
            categories[i] = categories[j];
            categories[j] = temp;
        }

        for (int c = 0; c < categories.Count && currentOfferCount < currentOffers.Length; c++)
        {
            if (categories[c] == -1)
            {
                currentOffers[currentOfferCount] = new PlayerUpgradeOffer(
                    PlayerUpgradeType.LifeRestore, 0, 1, "回复生命", "恢复2点生命值");
                lifeRestoreOffered = true;
                currentOfferCount++;
                continue;
            }

            PlayerUpgradeType[] branch = WeaponCategories[categories[c]];
            var available = new System.Collections.Generic.List<PlayerUpgradeType>();
            for (int b = 0; b < branch.Length; b++)
            {
                if (playerController.GetUpgradeLevel(branch[b]) < 3)
                    available.Add(branch[b]);
            }
            if (available.Count == 0) continue;

            PlayerUpgradeType type = available[Random.Range(0, available.Count)];
            int level = playerController.GetUpgradeLevel(type);
            currentOffers[currentOfferCount] = CreateOffer(type, level);
            currentOfferCount++;
        }
    }

    private PlayerUpgradeOffer CreateOffer(PlayerUpgradeType type, int currentLevel)
    {
        int nextLevel = Mathf.Min(3, currentLevel + 1);
        bool unlocking = currentLevel == 0 && IsWeaponLocked(type);

        switch (type)
        {
            case PlayerUpgradeType.CannonDamage:
                return new PlayerUpgradeOffer(type, currentLevel, nextLevel, "主炮·伤害", "");
            case PlayerUpgradeType.CannonTrajectory:
                return new PlayerUpgradeOffer(type, currentLevel, nextLevel, "主炮·弹道", "");
            case PlayerUpgradeType.CannonFireRate:
                return new PlayerUpgradeOffer(type, currentLevel, nextLevel, "主炮·射速", "");

            case PlayerUpgradeType.IceDamage:
                return unlocking
                    ? new PlayerUpgradeOffer(type, currentLevel, nextLevel, "解锁·追踪弹", "")
                    : new PlayerUpgradeOffer(type, currentLevel, nextLevel, "追踪弹·伤害", "");
            case PlayerUpgradeType.IceCount:
                return unlocking
                    ? new PlayerUpgradeOffer(type, currentLevel, nextLevel, "解锁·追踪弹", "")
                    : new PlayerUpgradeOffer(type, currentLevel, nextLevel, "追踪弹·数量", "");
            case PlayerUpgradeType.IceSlow:
                return unlocking
                    ? new PlayerUpgradeOffer(type, currentLevel, nextLevel, "解锁·追踪弹", "")
                    : new PlayerUpgradeOffer(type, currentLevel, nextLevel, "追踪弹·减速", "");

            case PlayerUpgradeType.AirBombCount:
                return unlocking
                    ? new PlayerUpgradeOffer(type, currentLevel, nextLevel, "解锁·空爆弹", "")
                    : new PlayerUpgradeOffer(type, currentLevel, nextLevel, "空爆弹·数量", "");
            case PlayerUpgradeType.AirBombExplosion:
                return unlocking
                    ? new PlayerUpgradeOffer(type, currentLevel, nextLevel, "解锁·空爆弹", "")
                    : new PlayerUpgradeOffer(type, currentLevel, nextLevel, "空爆弹·爆炸", "");
            case PlayerUpgradeType.AirBombSplit:
                return unlocking
                    ? new PlayerUpgradeOffer(type, currentLevel, nextLevel, "解锁·空爆弹", "")
                    : new PlayerUpgradeOffer(type, currentLevel, nextLevel, "空爆弹·分裂", "");

            case PlayerUpgradeType.DroneDamage:
                return unlocking
                    ? new PlayerUpgradeOffer(type, currentLevel, nextLevel, "解锁·无人机", "")
                    : new PlayerUpgradeOffer(type, currentLevel, nextLevel, "无人机·伤害", "");
            case PlayerUpgradeType.DroneDuration:
                return unlocking
                    ? new PlayerUpgradeOffer(type, currentLevel, nextLevel, "解锁·无人机", "")
                    : new PlayerUpgradeOffer(type, currentLevel, nextLevel, "无人机·持续", "");
            case PlayerUpgradeType.DroneWidth:
                return unlocking
                    ? new PlayerUpgradeOffer(type, currentLevel, nextLevel, "解锁·无人机", "")
                    : new PlayerUpgradeOffer(type, currentLevel, nextLevel, "无人机·宽度", "");
            default:
                return new PlayerUpgradeOffer(type, currentLevel, nextLevel, "强化", "");
        }
    }

    private bool IsWeaponLocked(PlayerUpgradeType type)
    {
        if (playerController == null) return false;
        switch (type)
        {
            case PlayerUpgradeType.IceDamage:
            case PlayerUpgradeType.IceCount:
            case PlayerUpgradeType.IceSlow:
                return !playerController.iceUnlocked;
            case PlayerUpgradeType.AirBombCount:
            case PlayerUpgradeType.AirBombExplosion:
            case PlayerUpgradeType.AirBombSplit:
                return !playerController.airBombUnlocked;
            case PlayerUpgradeType.DroneDamage:
            case PlayerUpgradeType.DroneDuration:
            case PlayerUpgradeType.DroneWidth:
                return !playerController.droneUnlocked;
            default:
                return false;
        }
    }

    private void EndUpgradeSelection()
    {
        if (!upgradeSelectionPending) return;

        upgradeSelectionPending = false;
        Time.timeScale = previousTimeScale <= 0f ? 1f : previousTimeScale;
    }

    public void ChooseUpgrade(int offerIndex)
    {
        if (!upgradeSelectionPending || offerIndex < 0 || offerIndex >= currentOfferCount) return;

        if (playerController != null)
        {
            playerController.ApplyUpgrade(currentOffers[offerIndex].type);
        }

        SoundHelper.PlayOneShot("PlayerHappy_448", 0.7f);

        upgradeCount++;
        energyRequired = GetFibonacciEnergyRequirement(upgradeCount);
        energyCollected = 0;
        energyProgress = 0f;
        currentOfferCount = 0;
        EndUpgradeSelection();
        nextBatchTime = Mathf.Max(nextBatchTime, Time.time + 0.5f);
    }

    private int GetFibonacciEnergyRequirement(int completedUpgrades)
    {
        int first = 3;
        int second = 5;
        if (completedUpgrades <= 0) return first;
        if (completedUpgrades == 1) return second;

        for (int i = 2; i <= completedUpgrades; i++)
        {
            int next = first + second;
            first = second;
            second = next;
        }

        return second;
    }

    private float GetDifficultyMultiplier()
    {
        return 1f + enemiesDefeated / 25;
    }

    private float GetTargetAliveCount()
    {
        float baseCount = 2 + enemiesDefeated / 8;
        return baseCount * GetDifficultyMultiplier();
    }

    private void UpdateWaves()
    {
        if (bossSpawned) return;

        if (Time.time >= nextWaveDiagTime)
        {
            nextWaveDiagTime = Time.time + 2f;
            FileLogger.Log("UpdateWaves: alive=" + aliveEnemyWeight + " target=" + GetTargetAliveCount() + " queued=" + queuedSpawns + " nextBatch=" + nextBatchTime + " time=" + Time.time);
        }

        if (enemiesDefeated - lastSquadKillCount >= 15)
        {
            lastSquadKillCount = enemiesDefeated;
            SpawnSquadWave();
        }

        if (queuedSpawns > 0 && Time.time >= nextBatchTime)
        {
            float deficit = GetTargetAliveCount() - aliveEnemyWeight;
            int toSpawn = Mathf.Min(queuedSpawns, Mathf.Max(1, Mathf.CeilToInt(deficit)));
            for (int i = 0; i < toSpawn; i++)
                SpawnSingleEnemy();
            queuedSpawns -= toSpawn;
            if (queuedSpawns > 0) nextBatchTime = Time.time + 2f;
        }
        else if (queuedSpawns == 0 && aliveEnemyWeight < GetTargetAliveCount() - 0.5f && Time.time >= nextBatchTime)
        {
            SpawnSingleEnemy();
            nextBatchTime = Time.time + 2f;
        }
    }

    private void SpawnSquadWave()
    {
        int squadId = SquadChaser2D.BeginSquad();
        float squadX = GetSpawnXRange() - 0.8f;
        float startX = Random.Range(-squadX, squadX);
        for (int i = 0; i < 5; i++)
        {
            float x = startX + (i - 2) * 0.55f;
            Vector2 position = new Vector2(x, spawnY + i * 0.05f);
            GameObject enemy = CreateEnemy("Squad Chaser", chaserEnemySprite, position, 0.5f, 1, 0f, false, false, 0.5f);

            enemy.transform.localScale = Vector3.one * 0.36f;

            var squad = enemy.AddComponent<SquadChaser2D>();
            squad.squadIndex = i;
            squad.AssignSquad(squadId);
            squad.descendSpeed = 1.75f;
            squad.sAmplitude = 1.8f;
            squad.sDuration = 2.8f;

            enemy.AddComponent<DestroyWhenOffscreen2D>().bottomY = -9f;
        }
    }

    private float GetSpawnXRange()
    {
        Camera cam = Camera.main;
        if (cam == null || !cam.orthographic) return 4.5f;
        return cam.orthographicSize * cam.aspect - 0.5f;
    }

    private void SpawnSingleEnemy()
    {
        FileLogger.Log("SpawnSingleEnemy called, parent=" + (transform != null) + " sprite=" + (chaserEnemySprite != null));
        bool shooter = enemiesDefeated >= 5 && Random.value < 0.25f;
        bool advanced = enemiesDefeated >= 12 && Random.value < 0.12f;
        float spawnX = GetSpawnXRange();
        float x = Random.Range(-spawnX, spawnX);

        Sprite sprite;
        string name;
        float radius, health, speed, weight;
        bool shoots;

        if (advanced)
        {
            sprite = advancedEnemySprite;
            name = "Advanced Shooter";
            radius = 0.5f;
            health = 5;
            speed = 1.7f;
            weight = 1f;
            shoots = true;
        }
        else if (shooter)
        {
            sprite = shooterEnemySprite;
            name = "Shooter";
            radius = 0.42f;
            health = 4;
            speed = 1.9f;
            weight = 1f;
            shoots = true;
        }
        else
        {
            sprite = chaserEnemySprite;
            name = "Chaser";
            radius = 0.6f;
            health = 1;
            speed = 2.5f;
            weight = 0.5f;
            shoots = false;
        }

        Vector2 position = new Vector2(x, spawnY);
        GameObject enemy = CreateEnemy(name, sprite, position, radius, (int)health, speed, shoots, false, weight);
        enemy.AddComponent<DestroyWhenOffscreen2D>().bottomY = -9f;
        FileLogger.Log("Spawned " + name + " at " + position + " alive=" + aliveEnemyWeight);

        if (advanced)
        {
            var pos = enemy.GetComponent<ShooterEnemyPositioner2D>();
            if (pos != null) pos.neverDrift = true;
        }

        var enemyShooter = enemy.GetComponent<EnemyShooter2D>();
        if (enemyShooter != null)
        {
            if (advanced)
            {
                enemyShooter.burstCount = 5;
                enemyShooter.burstCooldown = 3f;
            }
            else
            {
                enemyShooter.burstCount = 2;
                enemyShooter.burstOnce = true;
                enemy.transform.localScale = Vector3.one * 0.75f;
                var pos = enemy.GetComponent<ShooterEnemyPositioner2D>();
                if (pos != null) pos.sideExit = true;
            }
        }
    }

    private void SpawnBoss()
    {
        if (bossSpawned) return;

        state = StageState.Boss;
        bossSpawned = true;

        GameObject boss = CreateEnemy("Boss", bossSprite, new Vector2(0f, spawnY), 1.6f, 240, 0.8f, true, true);
        boss.transform.localScale = Vector3.one * 0.96f;
        bossHealth = boss.GetComponent<Health2D>();
        bossHealth.Died += OnBossDied;

        var bossBody = boss.GetComponent<Rigidbody2D>();
        if (bossBody != null) bossBody.isKinematic = true;

        var movement = boss.AddComponent<BossMovement2D>();
        movement.target = player;
        movement.stage = this;

        var shooter = boss.GetComponent<EnemyShooter2D>();
        if (shooter != null)
        {
            shooter.autoFire = false;
            shooter.useBurstFire = false;
            shooter.fireInterval = 0.4f;
            shooter.projectileSpeed = 5f;
            shooter.range = 12f;
        }

        StopMusic(normalMusic);
        normalMusic = null;
        bossMusic = SoundHelper.PlayMusic("BossFinalB_380", loop: true, volume: 0.42f);
    }

    private static void StopMusic(AudioSource source)
    {
        if (source != null) Object.Destroy(source.gameObject);
    }

    private GameObject CreateEnemy(string name, Sprite sprite, Vector2 position, float radius, int healthValue, float speed, bool shoots, bool isBoss, float weight = 1f)
    {
        GameObject enemy = new GameObject(name);
        enemy.transform.SetParent(transform);
        enemy.transform.position = position;

        var renderer = enemy.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingOrder = 10;

        var body = enemy.AddComponent<Rigidbody2D>();
        body.gravityScale = 0f;
        body.freezeRotation = true;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        enemy.transform.localScale = Vector3.one * 0.6f;

        var collider = enemy.AddComponent<CircleCollider2D>();
        collider.radius = radius;

        var health = enemy.AddComponent<Health2D>();
        health.maxHealth = healthValue;
        health.currentHealth = healthValue;
        if (!isBoss)
        {
            health.Died += OnEnemyDied;
            var tracker = enemy.AddComponent<EnemyLifeTracker>();
            tracker.stage = this;
            tracker.weight = weight;
            aliveEnemyWeight += weight;
        }

        var status = enemy.AddComponent<EnemyStatus2D>();
        status.isBoss = isBoss;

        if (shoots)
        {
            var shooter = enemy.AddComponent<EnemyShooter2D>();
            shooter.target = player;
            shooter.projectileSprite = enemyProjectileSprite;
            shooter.fireInterval = 0.6f;

            if (!isBoss)
            {
                var positioner = enemy.AddComponent<ShooterEnemyPositioner2D>();
                positioner.targetY = 3.2f + Random.Range(-0.8f, 0.8f);
                positioner.moveSpeed = speed;
            }
        }
        else
        {
            var chaser = enemy.AddComponent<EnemyChaser2D>();
            chaser.target = player;
            chaser.moveSpeed = speed * 0.55f * (2f / 3f);
            chaser.maxMoveSpeed = speed * 1.8f * (2f / 3f);
            chaser.acceleration = 0.8f;
        }

        return enemy;
    }

    private void OnSquadDefeated(Vector3 position)
    {
        SpawnEnergyFragments(position, 2);
    }

    public void OnEnemyRemoved(float weight)
    {
        aliveEnemyWeight -= weight;
        if (aliveEnemyWeight < 0f) aliveEnemyWeight = 0f;

        if (aliveEnemyWeight < GetTargetAliveCount())
        {
            queuedSpawns++;
            if (queuedSpawns == 1) nextBatchTime = Time.time + 2f;
        }
    }

    private void OnEnemyDied(Health2D health)
    {
        enemiesDefeated++;

        float enemyScale = health.transform.localScale.x;
        EnemyDeathEffect2D.Spawn(health.transform.position, enemyScale);

        var sr = health.GetComponent<SpriteRenderer>();
        bool isAdvanced = sr != null && sr.sprite == advancedEnemySprite;
        bool isShooter = sr != null && sr.sprite == shooterEnemySprite;

        SoundHelper.PlayOneShot(isAdvanced ? "ExplosionCrystal3_282" : "ExplosionCrystal2.2_362", 0.35f);

        if (!bossSpawned && enemiesDefeated >= killsRequiredForBoss)
        {
            SpawnBoss();
        }

        int energyCount = 0;
        if (isAdvanced) energyCount = 3;
        else energyCount = 1;

        if (energyCount > 0)
            SpawnEnergyFragments(health.transform.position, energyCount);
    }

    public GameObject CreateBossChaser(Vector3 position)
    {
        if (IsFinished || player == null) return null;
        GameObject enemy = CreateEnemy("Boss Chaser", chaserEnemySprite, position, 0.5f, 1, 2.6f, false, false, 0.5f);
        enemy.AddComponent<DestroyWhenOffscreen2D>().bottomY = -9f;
        return enemy;
    }

    private void OnBossDied(Health2D health)
    {
        FileLogger.Log("OnBossDied");
        bossHealth = null;
        state = StageState.Cleared;
        StopMusic(bossMusic);
        bossMusic = null;
        SoundHelper.PlayMusic("BossTemple_406", loop: true, volume: 0.18f);
        SoundHelper.PlayOneShot("PickupJingleMajorCrystalBoss", volume: 0.8f);
        SoundHelper.PlayOneShot("PlayerBomb_662", volume: 0.6f);
        FreezeGameplay();
    }

    private void SpawnEnergyFragments(Vector3 position, int count)
    {
        if (energyFragmentSprite == null) return;

        for (int i = 0; i < count; i++)
        {
            float angle = i * Mathf.PI * 2f / count;
            Vector3 offset = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * 0.25f;
            GameObject fragment = new GameObject("Energy Fragment");
            fragment.transform.SetParent(transform);
            fragment.transform.position = position + offset;
            fragment.transform.localScale = Vector3.one * 0.75f;

            var renderer = fragment.AddComponent<SpriteRenderer>();
            renderer.sprite = energyFragmentSprite;
            renderer.sortingOrder = 15;

            var collider = fragment.AddComponent<CircleCollider2D>();
            collider.radius = 0.18f;
            collider.isTrigger = true;

            var body = fragment.AddComponent<Rigidbody2D>();
            body.gravityScale = 0f;

            var energy = fragment.AddComponent<EnergyFragment2D>();
            energy.player = player;
            energy.stage = this;
        }
    }

    private void OnPlayerDied(Health2D health)
    {
        FileLogger.Log("OnPlayerDied");
        EndUpgradeSelection();
        state = StageState.GameOver;
        FreezeGameplay();
    }

    private void CleanupScene()
    {
        var renderers = FindObjectsOfType<SpriteRenderer>();
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] == null) continue;
            renderers[i].enabled = false;
        }

        var colliders = FindObjectsOfType<Collider2D>();
        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i] == null) continue;
            colliders[i].enabled = false;
        }

        var bodies = FindObjectsOfType<Rigidbody2D>();
        for (int i = 0; i < bodies.Length; i++)
        {
            if (bodies[i] == null) continue;
            bodies[i].simulated = false;
        }

        var beams = FindObjectsOfType<DroneBeamWeapon2D>();
        for (int i = 0; i < beams.Length; i++)
        {
            if (beams[i] == null) continue;
            Object.Destroy(beams[i].gameObject);
        }
    }

    private void FreezeGameplay()
    {
        FileLogger.Log("FreezeGameplay called, state=" + state);
        Time.timeScale = 0f;
    }

    public void ResetForNewGame()
    {
        FileLogger.Log("ResetForNewGame called");

        state = StageState.Waves;
        currentWave = 0;
        enemiesDefeated = 0;
        energyCollected = 0;
        energyRequired = 3;
        energyProgress = 0f;
        upgradeCount = 0;
        aliveEnemyWeight = 0f;
        queuedSpawns = 0;
        lastSquadKillCount = 0;
        bossSpawned = false;
        bossHealth = null;
        upgradeSelectionPending = false;
        previousTimeScale = 1f;
        lifeRestoreOffered = false;
        currentOfferCount = 0;
        nextBatchTime = Time.time + 0.5f;
        nextStageLogTime = Time.time + 3f;
        nextDiagLogTime = Time.time + 1f;
        nextWaveDiagTime = Time.time + 1f;

        if (player != null)
        {
            if (playerHealth != null) playerHealth.Died -= OnPlayerDied;
            playerHealth = player.GetComponent<Health2D>();
            playerController = player.GetComponent<PlayerController2D>();
            if (playerHealth != null) playerHealth.Died += OnPlayerDied;
        }

        StopMusic(normalMusic);
        normalMusic = SoundHelper.PlayMusic("BossTrueLastUpper_288", loop: true, volume: 0.36f);
        StopMusic(bossMusic);
        bossMusic = null;

        Time.timeScale = 1f;
        FileLogger.Log("ResetForNewGame complete");
    }

    private void OnDestroy()
    {
        FileLogger.Log("StageFlowController.OnDestroy, state=" + state);
        SquadChaser2D.SquadDefeated -= OnSquadDefeated;
        if (upgradeSelectionPending)
        {
            Time.timeScale = previousTimeScale <= 0f ? 1f : previousTimeScale;
        }
    }
}
