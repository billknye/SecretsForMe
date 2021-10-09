using Bunit;
using Xunit;
using NotesBin.App.Pages;

namespace NotesBin.App.Tests;

public class UnitTest1
{
    [Fact]
    public void Index_Component_RendersCorrectly1()
    {
        using var context = new TestContext();

        var cut = context.RenderComponent<Pages.Index>();

        cut.MarkupMatches(@"<h1>Hello, world!</h1>

Welcome to your new app.");
    }
}