namespace Domain.Services;

using Domain.Enums;

public sealed class TimerCalculationServiceFactory(IEnumerable<ITimerCalculationService> services) : ITimerCalculationServiceFactory
{
    private readonly Dictionary<TimerCalculationStrategy, ITimerCalculationService> _services =
        services.ToDictionary(s => s.SupportedStrategy);

    public ITimerCalculationService Resolve(TimerCalculationStrategy strategy)
        => _services.TryGetValue(strategy, out var svc)
            ? svc
            : _services[TimerCalculationStrategy.Additive];
}
