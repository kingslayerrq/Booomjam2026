using System;

[Serializable]
public class GameSaveData
{
    public int currentDay;
    public float batteryLevel;
    public int playerHealth;

    public GameSaveData(int currentDay, float batteryLevel, int playerHealth)
    {
        this.currentDay = currentDay;
        this.batteryLevel = batteryLevel;
        this.playerHealth = playerHealth;
    }

    public override string ToString()
    {
        return $"Day: {currentDay}, Battery Level: {batteryLevel}, Player Health: {playerHealth}";
    }
}
