namespace Box2D.NET.Test.Primitives;

public class DummyObject<T>(T value)
{
    public T Value = value;

    public DummyObject() : this(default(T))
    {
    }
}