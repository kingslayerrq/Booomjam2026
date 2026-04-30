using System;

[Serializable]
public class GameSaveData
{
    public int currentDay;
    public float batteryLevel;

    public GameSaveData(int currentDay, float batteryLevel)
    {
        this.currentDay = currentDay;
        this.batteryLevel = batteryLevel;
    }
}