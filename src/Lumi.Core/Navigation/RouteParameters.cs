namespace Lumi.Core.Navigation;

/// <summary>
/// Dictionary of parameters extracted from a parameterized route pattern.
/// For example, pattern "user/{id}" matched against "user/123" yields {"id": "123"}.
/// </summary>
public class RouteParameters : Dictionary<string, string>
{
    public RouteParameters() : base(StringComparer.OrdinalIgnoreCase) { }
}
