namespace Orders.Application;

/// <summary>Outcome of a command: either a value or validation errors keyed by field name.</summary>
public sealed class Result<T>
{
    internal Result(T? value, IReadOnlyDictionary<string, string[]>? errors)
    {
        Value = value;
        Errors = errors;
    }

    public T? Value { get; }

    public IReadOnlyDictionary<string, string[]>? Errors { get; }

    public bool IsSuccess => Errors is null;
}

public static class Result
{
    public static Result<T> Success<T>(T value) => new(value, null);

    public static Result<T> Invalid<T>(string field, string message) =>
        new(default, new Dictionary<string, string[]> { [field] = [message] });
}
