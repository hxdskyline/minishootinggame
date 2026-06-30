using UnityEngine;

public static class MinishootRuntimeBootstrap
{
    private static bool bootstrapped;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetBootstrapState()
    {
        bootstrapped = false;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (bootstrapped) return;
        bootstrapped = true;

        Screen.SetResolution(621, 1080, FullScreenMode.Windowed);
        Physics2D.gravity = Vector2.zero;

        var root = new GameObject("Minishoot Runtime Bootstrap");
        Object.DontDestroyOnLoad(root);

        Sprite fallbackPlayerSprite = CreateCircleSprite("Runtime_Player_Sprite", new Color(0.2f, 0.8f, 1f, 1f), 64);
        Sprite fallbackEnemySprite = CreateCircleSprite("Runtime_Enemy_Sprite", new Color(1f, 0.25f, 0.25f, 1f), 48);
        Sprite fallbackPlayerProjectileSprite = CreateCircleSprite("Runtime_Player_Projectile_Sprite", new Color(1f, 0.9f, 0.2f, 1f), 24);
        Sprite arenaSprite = CreateSolidSprite("Runtime_Arena_Sprite", new Color(0.06f, 0.08f, 0.12f, 1f));

        Sprite playerIdle = RecoveredSpriteLoader.LoadSprite("Sprite_42_42", fallbackPlayerSprite);
        Sprite playerRightTransition = RecoveredSpriteLoader.LoadSprite("Sprite_43_43", playerIdle);
        Sprite playerRightTilt = RecoveredSpriteLoader.LoadSprite("Sprite_44_44", playerIdle);
        Sprite playerLeftTilt = RecoveredSpriteLoader.LoadSprite("Sprite_45_45", playerIdle);
        Sprite playerLeftTransition = RecoveredSpriteLoader.LoadSprite("Sprite_46_46", playerIdle);
        Sprite chaserEnemySprite = RecoveredSpriteLoader.LoadSprite("Sprite_175_175", fallbackEnemySprite);
        Sprite shooterEnemySprite = RecoveredSpriteLoader.LoadSprite("Sprite_765_765", fallbackEnemySprite);
        Sprite advancedEnemySprite = RecoveredSpriteLoader.LoadSprite("Sprite_819_819", fallbackEnemySprite);
        Sprite bossSprite = RecoveredSpriteLoader.LoadSprite("Texture2D_2_2_001", fallbackEnemySprite);
        Sprite playerProjectileSprite = RecoveredSpriteLoader.LoadSprite("Texture2D_412_412", fallbackPlayerProjectileSprite);
        Sprite enemyProjectileSprite = RecoveredSpriteLoader.LoadSprite("Sprite_175_175", fallbackEnemySprite);
        Sprite heartSprite = RecoveredSpriteLoader.LoadSprite("Sprite_881_881_001");
        Sprite energyFragmentSprite = RecoveredSpriteLoader.LoadSprite("Sprite_852_852_002");

        CreateScrollingBackground(root.transform, arenaSprite);

        GameObject player = CreateActor("Player", playerIdle, new Vector2(0f, -3.5f), 0.38f, 0);
        player.transform.SetParent(root.transform);
        player.transform.localScale = Vector3.one * 0.7f;
        var playerHealth = player.AddComponent<Health2D>();
        playerHealth.maxHealth = 6;
        playerHealth.currentHealth = 6;
        var controller = player.AddComponent<PlayerController2D>();
        controller.projectileSprite = playerProjectileSprite;
        controller.idleSprite = playerIdle;
        controller.rightTransitionSprite = playerRightTransition;
        controller.rightTiltSprite = playerRightTilt;
        controller.leftTransitionSprite = playerLeftTransition;
        controller.leftTiltSprite = playerLeftTilt;

        Camera camera = Camera.main;
        if (camera == null)
        {
            GameObject cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            camera = cameraObject.AddComponent<Camera>();
            cameraObject.AddComponent<AudioListener>();
        }

        camera.orthographic = true;
        camera.orthographicSize = 6f;
        camera.transform.position = new Vector3(0f, 0f, -10f);
        var follow = camera.GetComponent<CameraFollow2D>();
        if (follow != null) Object.Destroy(follow);

        var stage = root.AddComponent<StageFlowController>();
        stage.player = player.transform;
        stage.chaserEnemySprite = chaserEnemySprite;
        stage.shooterEnemySprite = shooterEnemySprite;
        stage.advancedEnemySprite = advancedEnemySprite;
        stage.bossSprite = bossSprite;
        stage.enemyProjectileSprite = enemyProjectileSprite;
        stage.energyFragmentSprite = energyFragmentSprite;

        var hud = root.AddComponent<CombatHudUGUI>();
        hud.player = playerHealth;
        hud.stage = stage;
        hud.heartSprite = heartSprite;
    }

    private static void CreateScrollingBackground(Transform parent, Sprite sprite)
    {
        for (int i = 0; i < 3; i++)
        {
            GameObject tile = new GameObject("Scrolling Background " + i);
            tile.transform.SetParent(parent);
            tile.transform.position = new Vector3(0f, -16f + i * 16f, 0f);
            tile.transform.localScale = new Vector3(24f, 16f, 1f);

            var renderer = tile.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = -100;

            var scrolling = tile.AddComponent<ScrollingBackground2D>();
            scrolling.scrollSpeed = 2f;
            scrolling.resetBelowY = -18f;
            scrolling.resetOffsetY = 48f;
        }
    }

    private static GameObject CreateActor(string name, Sprite sprite, Vector2 position, float radius, int sortingOrder)
    {
        GameObject actor = new GameObject(name);
        actor.transform.position = position;

        var renderer = actor.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingOrder = sortingOrder;

        var body = actor.AddComponent<Rigidbody2D>();
        body.gravityScale = 0f;
        body.freezeRotation = true;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        var collider = actor.AddComponent<CircleCollider2D>();
        collider.radius = radius;

        return actor;
    }

    private static Sprite CreateSolidSprite(string name, Color color)
    {
        var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        texture.name = name + "_Texture";
        texture.SetPixel(0, 0, color);
        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
    }

    private static Sprite CreateCircleSprite(string name, Color color, int size)
    {
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.name = name + "_Texture";
        float center = (size - 1) * 0.5f;
        float radius = size * 0.45f;
        float outlineRadius = size * 0.48f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - center;
                float dy = y - center;
                float distance = Mathf.Sqrt(dx * dx + dy * dy);

                if (distance <= radius)
                {
                    texture.SetPixel(x, y, color);
                }
                else if (distance <= outlineRadius)
                {
                    texture.SetPixel(x, y, Color.white);
                }
                else
                {
                    texture.SetPixel(x, y, Color.clear);
                }
            }
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }
}
