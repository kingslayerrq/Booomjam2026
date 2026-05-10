using System;

[Serializable]
public class GameSaveData
{
    public int currentDay;
    public bool isMorning;
    public float batteryLevel;

    public int playerHealth;
    // public float stamina;

    public GameSaveData(int currentDay, bool isMorning, float batteryLevel,  int playerHealth)
    {
        this.currentDay = currentDay;
        this.isMorning = isMorning;
        this.batteryLevel = batteryLevel;
        this.playerHealth = playerHealth;
        //this.stamina = stamina;
    }

    public override string ToString()
    {
        return $"Day: {this.currentDay}, Is Morning: {this.isMorning}, Battery Level: {this.batteryLevel}," +
               $" Player Health: {this.playerHealth}";
    }
}