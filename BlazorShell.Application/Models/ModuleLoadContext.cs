using System;
using System.Collections.Generic;
using System.Reflection;
using BlazorShell.Application.Configuration;
using BlazorShell.Application.Interfaces;

namespace BlazorShell.Application.Models;

public class ModuleLoadContext
{
    public Assembly Assembly { get; set; } = null!;
    public IModule? ModuleInstance { get; set; }
    public DateTime LoadedAt { get; set; }
    public List<Type> ComponentTypes { get; set; } = new();
    public string? AssemblyPath { get; set; }
    public ModuleConfig? Config { get; set; }
}
