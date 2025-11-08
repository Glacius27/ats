namespace Ats.Messaging.Abstractions;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class TopicAttribute : Attribute
{
    public string Name { get; }

    public TopicAttribute(string name)
    {
        Name = name;
    }
}