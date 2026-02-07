using Application.Models;

namespace Infrastructure.Helpers;

public class GradualValueGenerator
{
    private readonly Random _random = new();

    /// <summary>
    /// Gera o próximo valor com variação gradual e realista
    /// </summary>
    /// <param name="state">Estado atual do sensor</param>
    /// <param name="minValue">Valor mínimo permitido</param>
    /// <param name="maxValue">Valor máximo permitido</param>
    /// <returns>Próximo valor gerado</returns>
    public double GenerateNext(SensorSimulatorState state, double minValue, double maxValue)
    {
        if (_random.Next(100) < 10)
        {
            state.IsIncreasing = !state.IsIncreasing;
        }

        var variation = state.IsIncreasing ? state.Delta : -state.Delta;
        
        var randomFactor = 1 + (_random.NextDouble() * 0.4 - 0.2); // 0.8 a 1.2
        variation *= randomFactor;

        var nextValue = state.CurrentValue + variation;

        if (nextValue >= maxValue)
        {
            nextValue = maxValue;
            state.IsIncreasing = false;
        }
        else if (nextValue <= minValue)
        {
            nextValue = minValue;
            state.IsIncreasing = true;
        }

        nextValue = Math.Clamp(nextValue, minValue, maxValue);

        state.CurrentValue = nextValue;

        return Math.Round(nextValue, 2);
    }
}
