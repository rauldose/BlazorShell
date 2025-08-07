using System;
using System.Reflection;

namespace BlazorShell.Application.Models;

public class RouteInfo
{
    public string Template { get; set; } = string.Empty;
    public Type ComponentType { get; set; } = null!;
    public string ModuleName { get; set; } = string.Empty;
    public Assembly Assembly { get; set; } = null!;
    public int Priority { get; set; }
}
