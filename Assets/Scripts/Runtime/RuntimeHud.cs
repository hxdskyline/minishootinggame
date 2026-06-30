using UnityEngine;

public class RuntimeHud : MonoBehaviour
{
    public Health2D player;
    public StageFlowController stage;
    public Sprite heartSprite;

    private Texture2D heartTexture;

    private void OnGUI()
    {
        if (heartTexture == null && heartSprite != null)
        {
            heartTexture = heartSprite.texture;
        }

        DrawHealthHearts();
        DrawStageInfo();

        if (stage != null && stage.IsFinished)
        {
            DrawResultPanel();
        }
    }

    private void DrawHealthHearts()
    {
        if (player == null) return;

        for (int i = 0; i < player.maxHealth; i++)
        {
            Rect rect = new Rect(16 + i * 34, 16, 28, 28);
            if (i < player.currentHealth)
            {
                if (heartTexture != null)
                {
                    GUI.DrawTexture(rect, heartTexture, ScaleMode.ScaleToFit, true);
                }
                else
                {
                    GUI.Box(rect, "♥");
                }
            }
            else
            {
                GUI.color = new Color(1f, 1f, 1f, 0.25f);
                GUI.Box(rect, "");
                GUI.color = Color.white;
            }
        }
    }

    private void DrawStageInfo()
    {
        const int width = 360;
        GUILayout.BeginArea(new Rect(16, 52, width, 120), GUI.skin.box);
        GUILayout.Label("Move: WASD / Arrow Keys");
        GUILayout.Label("Shoot upward: Left Mouse Button");

        if (stage != null)
        {
            GUILayout.Label("State: " + stage.state);
            GUILayout.Label("Wave: " + Mathf.Min(stage.currentWave, stage.totalWaves) + " / " + stage.totalWaves);
            GUILayout.Label("Enemies defeated: " + stage.enemiesDefeated);
        }

        GUILayout.EndArea();
    }

    private void DrawResultPanel()
    {
        const int width = 420;
        const int height = 180;
        Rect rect = new Rect((Screen.width - width) * 0.5f, (Screen.height - height) * 0.5f, width, height);

        GUILayout.BeginArea(rect, GUI.skin.window);
        GUILayout.Space(20f);
        GUILayout.Label(stage.state == StageFlowController.StageState.Cleared ? "Stage Clear" : "Game Over");
        GUILayout.Space(12f);
        GUILayout.Label("Enemies defeated: " + stage.enemiesDefeated);
        GUILayout.Label("Stop and Play again to restart.");
        GUILayout.EndArea();
    }
}
