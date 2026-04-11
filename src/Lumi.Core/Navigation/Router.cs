namespace Lumi.Core.Navigation;

/// <summary>
/// Manages route-to-page mappings and handles navigation between pages.
/// Pages are swapped by clearing and repopulating a container element's children.
/// </summary>
public class Router
{
    private readonly record struct RouteRegistration(string Pattern, string[] Segments, Func<RouteParameters, Element> Factory);

    private readonly List<RouteRegistration> _routes = [];
    private readonly Stack<string> _history = new();

    /// <summary>
    /// The container element whose children are swapped on navigation.
    /// </summary>
    public Element Container { get; }

    /// <summary>
    /// The route string that was last successfully navigated to, or empty if none.
    /// </summary>
    public string CurrentRoute { get; private set; } = string.Empty;

    /// <summary>
    /// Raised after a successful navigation with the new route string.
    /// </summary>
    public event Action<string>? RouteChanged;

    /// <summary>
    /// True when there is at least one previous route to go back to.
    /// </summary>
    public bool CanGoBack => _history.Count > 0;

    public Router(Element container)
    {
        Container = container ?? throw new ArgumentNullException(nameof(container));
    }

    /// <summary>
    /// Register a route pattern with a parameterless page factory.
    /// </summary>
    public void Register(string pattern, Func<Element> pageFactory)
    {
        ArgumentNullException.ThrowIfNull(pageFactory);
        Register(pattern, _ => pageFactory());
    }

    /// <summary>
    /// Register a route pattern with a page factory that receives extracted parameters.
    /// Patterns may contain parameter placeholders like "user/{id}/details".
    /// </summary>
    public void Register(string pattern, Func<RouteParameters, Element> pageFactory)
    {
        ArgumentNullException.ThrowIfNull(pattern);
        ArgumentNullException.ThrowIfNull(pageFactory);

        var normalized = NormalizePath(pattern);
        var segments = normalized.Split('/', StringSplitOptions.RemoveEmptyEntries);
        _routes.Add(new RouteRegistration(normalized, segments, pageFactory));
    }

    /// <summary>
    /// Navigate to the given route, swapping the container's content.
    /// If the route doesn't match any registration, the current page is preserved.
    /// </summary>
    public void Navigate(string route)
    {
        ArgumentNullException.ThrowIfNull(route);

        var normalized = NormalizePath(route);
        var (registration, parameters) = MatchRoute(normalized);
        if (registration is null)
            return;

        // Push current route onto history before switching.
        if (!string.IsNullOrEmpty(CurrentRoute))
            _history.Push(CurrentRoute);

        ApplyRoute(normalized, registration.Value, parameters);
    }

    /// <summary>
    /// Navigate back to the previous route. Does nothing if there is no history.
    /// </summary>
    public void GoBack()
    {
        if (!CanGoBack)
            return;

        var previousRoute = _history.Pop();
        var (registration, parameters) = MatchRoute(previousRoute);
        if (registration is null)
            return;

        ApplyRoute(previousRoute, registration.Value, parameters);
    }

    private void ApplyRoute(string route, RouteRegistration registration, RouteParameters parameters)
    {
        Container.ClearChildren();

        var page = registration.Factory(parameters);
        Container.AddChild(page);

        CurrentRoute = route;
        RouteChanged?.Invoke(route);
    }

    private (RouteRegistration? Registration, RouteParameters Parameters) MatchRoute(string normalizedRoute)
    {
        var routeSegments = normalizedRoute.Split('/', StringSplitOptions.RemoveEmptyEntries);

        foreach (var reg in _routes)
        {
            var parameters = TryMatch(reg.Segments, routeSegments);
            if (parameters is not null)
                return (reg, parameters);
        }

        return (null, new RouteParameters());
    }

    private static RouteParameters? TryMatch(string[] patternSegments, string[] routeSegments)
    {
        if (patternSegments.Length != routeSegments.Length)
            return null;

        var parameters = new RouteParameters();

        for (int i = 0; i < patternSegments.Length; i++)
        {
            var pattern = patternSegments[i];
            var value = routeSegments[i];

            if (pattern.StartsWith('{') && pattern.EndsWith('}'))
            {
                var name = pattern[1..^1];
                parameters[name] = value;
            }
            else if (!string.Equals(pattern, value, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }
        }

        return parameters;
    }

    private static string NormalizePath(string path)
    {
        return path.Trim().TrimStart('/');
    }
}
