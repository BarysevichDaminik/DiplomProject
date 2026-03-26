namespace Core.GitModels;

public record CodeEntity
{
    required public string FullName { get; init; }
    required public string Path { get; init; }
    public EntityType Type { get; init; }
    public string ImpactReason { get; init; } = "Direct";
    public int PropagationLevel { get; init; }
}

public enum EntityType
{
    Method,
    Property,
    Constructor
}