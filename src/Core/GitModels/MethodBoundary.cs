namespace Core.GitModels;

public record struct MethodBoundary(
    string fullName,
    int startLine,
    int endLine,
    List<string> calls);