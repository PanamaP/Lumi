namespace Lumi.Core.Navigation;

/// <summary>
/// A convenience wrapper that pairs a container element with a Router.
/// Drop the <see cref="Root"/> element into a window's element tree to
/// enable page-based navigation.
/// </summary>
public class NavigationHost
{
    /// <summary>
    /// The container element that holds the current page.
    /// </summary>
    public Element Root { get; }

    /// <summary>
    /// The router that manages navigation within <see cref="Root"/>.
    /// </summary>
    public Router Router { get; }

    public NavigationHost()
    {
        Root = new BoxElement("nav-host");
        Router = new Router(Root);
    }
}
