using System;

namespace Ats.Integration.Attributes;


[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public sealed class TopicAttribute : Attribute
{
    public string RoutingKey { get; }
    public TopicAttribute(string routingKey) => RoutingKey = routingKey;
}