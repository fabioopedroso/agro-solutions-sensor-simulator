namespace Application.Models;

public class SensorSimulatorState
{
    public double CurrentValue { get; set; }
    public bool IsIncreasing { get; set; }
    public double Delta { get; set; }

    public SensorSimulatorState(double initialValue, double delta)
    {
        CurrentValue = initialValue;
        Delta = delta;
        IsIncreasing = Random.Shared.Next(2) == 0;
    }
}
