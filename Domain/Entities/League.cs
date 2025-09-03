namespace Domain.Entities;

using Domain.Common;
using Domain.Exceptions;

public class League : AggregateRoot
{
    public string Name { get; private set; } = string.Empty;

    private League() { }

    public static League Create(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new DomainException("League name required");
        return new League { Name = name.Trim() };
    }
}
