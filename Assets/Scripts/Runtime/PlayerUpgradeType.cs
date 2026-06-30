public enum PlayerUpgradeType
{
    CannonDamage,
    CannonTrajectory,
    CannonFireRate,
    IceDamage,
    IceCount,
    IceSlow,
    AirBombCount,
    AirBombExplosion,
    AirBombSplit,
    DroneDamage,
    DroneDuration,
    DroneWidth,
    LifeRestore
}

public struct PlayerUpgradeOffer
{
    public PlayerUpgradeType type;
    public int currentLevel;
    public int nextLevel;
    public string title;
    public string description;

    public PlayerUpgradeOffer(PlayerUpgradeType type, int currentLevel, int nextLevel, string title, string description)
    {
        this.type = type;
        this.currentLevel = currentLevel;
        this.nextLevel = nextLevel;
        this.title = title;
        this.description = description;
    }
}
