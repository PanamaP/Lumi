using Lumi.Core;
using Lumi.Core.Navigation;

namespace Lumi.Tests;

public class NavigationTests
{
    private static NavigationHost CreateHost()
    {
        var host = new NavigationHost();
        host.Router.Register("/home", () =>
        {
            var page = new BoxElement("div");
            page.AddChild(new TextElement("Home Page"));
            return page;
        });
        host.Router.Register("/settings", () =>
        {
            var page = new BoxElement("div");
            page.AddChild(new TextElement("Settings"));
            return page;
        });
        return host;
    }

    [Fact]
    public void Navigate_RegisteredRoute_ShowsPageContent()
    {
        var host = CreateHost();

        host.Router.Navigate("/home");

        Assert.Single(host.Root.Children);
        var page = host.Root.Children[0];
        Assert.Single(page.Children);
        Assert.Equal("Home Page", ((TextElement)page.Children[0]).Text);
    }

    [Fact]
    public void Navigate_BetweenRoutes_SwapsContent()
    {
        var host = CreateHost();

        host.Router.Navigate("/home");
        Assert.Equal("Home Page", ((TextElement)host.Root.Children[0].Children[0]).Text);

        host.Router.Navigate("/settings");
        Assert.Single(host.Root.Children);
        Assert.Equal("Settings", ((TextElement)host.Root.Children[0].Children[0]).Text);
    }

    [Fact]
    public void GoBack_ReturnsToPreviousPage()
    {
        var host = CreateHost();

        host.Router.Navigate("/home");
        host.Router.Navigate("/settings");
        host.Router.GoBack();

        Assert.Equal("home", host.Router.CurrentRoute);
        Assert.Equal("Home Page", ((TextElement)host.Root.Children[0].Children[0]).Text);
    }

    [Fact]
    public void CanGoBack_FalseInitially()
    {
        var host = CreateHost();

        Assert.False(host.Router.CanGoBack);
    }

    [Fact]
    public void CanGoBack_TrueAfterNavigation()
    {
        var host = CreateHost();

        host.Router.Navigate("/home");
        Assert.False(host.Router.CanGoBack);

        host.Router.Navigate("/settings");
        Assert.True(host.Router.CanGoBack);
    }

    [Fact]
    public void RouteChanged_FiresWithCorrectRoute()
    {
        var host = CreateHost();
        var firedRoutes = new List<string>();
        host.Router.RouteChanged += route => firedRoutes.Add(route);

        host.Router.Navigate("/home");
        host.Router.Navigate("/settings");

        Assert.Equal(["home", "settings"], firedRoutes);
    }

    [Fact]
    public void ParameterizedRoute_ExtractsParameters()
    {
        var host = new NavigationHost();
        RouteParameters? captured = null;
        host.Router.Register("user/{id}", p =>
        {
            captured = p;
            var page = new BoxElement("div");
            page.AddChild(new TextElement($"User {p["id"]}"));
            return page;
        });

        host.Router.Navigate("user/123");

        Assert.NotNull(captured);
        Assert.Equal("123", captured["id"]);
        Assert.Equal("User 123", ((TextElement)host.Root.Children[0].Children[0]).Text);
    }

    [Fact]
    public void ParameterizedRoute_MultipleParameters()
    {
        var host = new NavigationHost();
        RouteParameters? captured = null;
        host.Router.Register("org/{orgId}/user/{userId}", p =>
        {
            captured = p;
            return new BoxElement("div");
        });

        host.Router.Navigate("org/abc/user/42");

        Assert.NotNull(captured);
        Assert.Equal("abc", captured["orgId"]);
        Assert.Equal("42", captured["userId"]);
    }

    [Fact]
    public void Navigate_UnregisteredRoute_DoesNotCrash()
    {
        var host = CreateHost();

        Assert.True(host.Router.Navigate("/home"));
        Assert.False(host.Router.Navigate("/nonexistent"));

        // Should stay on current route
        Assert.Equal("home", host.Router.CurrentRoute);
        Assert.Single(host.Root.Children);
        Assert.Equal("Home Page", ((TextElement)host.Root.Children[0].Children[0]).Text);
    }

    [Fact]
    public void Navigate_UnregisteredRoute_NoContainerContent_WhenNoPriorNavigation()
    {
        var host = CreateHost();

        Assert.False(host.Router.Navigate("/nonexistent"));

        Assert.Empty(host.Root.Children);
        Assert.Equal(string.Empty, host.Router.CurrentRoute);
    }

    [Fact]
    public void MultipleNavigations_BuildHistoryCorrectly()
    {
        var host = CreateHost();

        host.Router.Navigate("/home");
        host.Router.Navigate("/settings");
        host.Router.Navigate("/home");

        Assert.True(host.Router.CanGoBack);

        host.Router.GoBack();
        Assert.Equal("settings", host.Router.CurrentRoute);

        host.Router.GoBack();
        Assert.Equal("home", host.Router.CurrentRoute);

        Assert.False(host.Router.CanGoBack);
    }

    [Fact]
    public void GoBack_FromFirstPage_DoesNothing()
    {
        var host = CreateHost();

        host.Router.Navigate("/home");
        host.Router.GoBack();

        Assert.Equal("home", host.Router.CurrentRoute);
        Assert.False(host.Router.CanGoBack);
    }

    [Fact]
    public void NavigationHost_CreatesRootAndRouter()
    {
        var host = new NavigationHost();

        Assert.NotNull(host.Root);
        Assert.NotNull(host.Router);
        Assert.Equal("nav-host", host.Root.TagName);
        Assert.Same(host.Root, host.Router.Container);
    }

    [Fact]
    public void Router_CurrentRoute_EmptyInitially()
    {
        var host = new NavigationHost();

        Assert.Equal(string.Empty, host.Router.CurrentRoute);
    }

    [Fact]
    public void RouteParameters_CaseInsensitiveKeys()
    {
        var parameters = new RouteParameters { ["Id"] = "42" };

        Assert.Equal("42", parameters["id"]);
        Assert.Equal("42", parameters["ID"]);
    }
}
