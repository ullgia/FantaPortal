namespace Domain.Enums;

public enum TimerCalculationStrategy
{
    Additive = 0,
    BucketProgression = 1,
    Adaptive = 2,
    Diminishing = 3,
    TimeBank = 4,
    Jitter = 5,
    GraceWindow = 6
}
