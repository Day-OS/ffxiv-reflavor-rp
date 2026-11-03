namespace ReflavorRP.Tests;

public static class TestAccents
{
    public static Accent Template(string name) => Accent.Templates.Single(a => a.Name == name);
}
