namespace SupportAI.Collectors.Windows;

public interface IColector
{
    string Name { get; }
    ColectorVelocidad Speed { get; }
    Task CollectAsync(CancellationToken ct = default);
}

public enum ColectorVelocidad { Rapido, Medio, Lento }
