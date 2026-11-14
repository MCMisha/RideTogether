namespace RideTogether.Patterns.Maybe;

public readonly record struct Maybe<T>
{
    private readonly T? _value;
    public bool HasValue { get; }

    private Maybe(T value)
    {
        _value = value;
        HasValue = true;
    }

    public static Maybe<T> Some(T? value) => 
        value is not null ? new(value) : None;
        
    public static Maybe<T> None => default;

    public T Value => _value ?? throw new InvalidOperationException();

    public TResult Match<TResult>(Func<T, TResult> some, Func<TResult> none) =>
        HasValue ? some(_value!) : none();

    public Maybe<TResult> Map<TResult>(Func<T, TResult> map) =>
        HasValue ? Maybe<TResult>.Some(map(_value!)) : Maybe<TResult>.None;
}