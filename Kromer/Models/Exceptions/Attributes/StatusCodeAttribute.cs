using System.Diagnostics.CodeAnalysis;
using System.Net;

namespace Kromer.Models.Exceptions.Attributes;

[AttributeUsage(AttributeTargets.All)]
public sealed class StatusCodeAttribute : Attribute
{
    public static readonly StatusCodeAttribute Default = new();

    private HttpStatusCode StatusCodeValue { get; set; }
    
    public StatusCodeAttribute() : this(HttpStatusCode.BadRequest)
    {
    }

    public StatusCodeAttribute(HttpStatusCode statusCode)
    {
        StatusCodeValue = statusCode;
    }

    public HttpStatusCode StatusCode => StatusCodeValue;

    public override bool Equals([NotNullWhen(true)] object? obj) =>
        obj is StatusCodeAttribute other && other.StatusCode == StatusCode;

    public override int GetHashCode() => StatusCode.GetHashCode();

    public override bool IsDefaultAttribute() => Equals(Default);
}