namespace Lumi.Core.Navigation;

/// <summary>
/// Manages route-to-page mappings and handles navigation between pages.
/// Pages are swapped by clearing and repopulating a container element's children.
/// </summary>
public class Router
{
    private readonly record struct RouteRegistration(string Pattern, string[] Segments, Func<RouteParameters, Element> Factory);

    private readonly List<RouteRegistration> _routes = [];
    private readonly List<string> _history = [];
    private const int MaxHistorySize = 100;

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
        var existing = _routes.FindIndex(r => r.Pattern == normalized);
        if (existing >= 0)
            _routes[existing] = new RouteRegistration(normalized, segments, pageFactory);
        else
            _routes.Add(new RouteRegistration(normalized, segments, pageFactory));
    }

    private bool _navigating;

    /// <summary>
    /// Navigate to the given route, swapping the container's content.
    /// Returns <c>true</c> if a matching route was found and navigation succeeded,
    /// or <c>false</c> if no route matched the given path.
    /// </summary>
    public bool Navigate(string route)
    {
        ArgumentNullException.ThrowIfNull(route);
        if (_navigating)
            throw new InvalidOperationException("Cannot navigate while a navigation is already in progress.");

        _navigating = true;
        try
        {
            var normalized = NormalizePath(route);
            var (registration, parameters) = MatchRoute(normalized);
            if (registration is null)
                return false;

            // Push current route onto history before switching.
            if (!string.IsNullOrEmpty(CurrentRoute))
            {
                _history.Add(CurrentRoute);
                if (_history.Count > MaxHistorySize)
                    _history.RemoveAt(0);
            }

            ApplyRoute(normalized, registration.Value, parameters);
            return true;
        }
        finally
        {
            _navigating = false;
        }
    }

    /// <summary>
    /// Navigate back to the previous route. Does nothing if there is no history.
    /// </summary>
    public void GoBack()
    {
        if (!CanGoBack)
            return;
        if (_navigating)
            throw new InvalidOperationException("Cannot navigate while a navigation is already in progress.");

        _navigating = true;
        try
        {
            var previousRoute = _history[^1];
            var (registration, parameters) = MatchRoute(previousRoute);
            if (registration is null)
                return;

            _history.RemoveAt(_history.Count - 1);
            ApplyRoute(previousRoute, registration.Value, parameters);
        }
        finally
        {
            _navigating = false;
        }
    }

    private void ApplyRoute(string route, RouteRegistration registration, RouteParameters parameters)
    {
        Element page;
        try
        {
            page = registration.Factory(parameters);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to create page for route '{route}'.", ex);
        }

        Container.ClearChildren();
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
