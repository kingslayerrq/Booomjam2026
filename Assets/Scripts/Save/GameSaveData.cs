using System;

[Serializable]
public class GameSaveData
{
    public int currentDay;
    public bool isMorning;
    public float batteryLevel;
    public float stamina;

    public GameSaveData(int currentDay, bool isMorning, float batteryLevel, float stamina)
    {
        this.currentDay = currentDay;
        this.isMorning = isMorning;
        this.batteryLevel = batteryLevel;
        this.stamina = stamina;
    }

    public override string ToString()
    {
        return $"Day: {this.currentDay}, Is Morning: {this.isMorning}, Battery Level: {this.batteryLevel}," +
               $"  Stamina: {this.stamina}";
    }
}