using System;

namespace BlazorShell.Core.Services;

public class AuthenticationStateChangedEventArgs : EventArgs
{
    public bool IsAuthenticated { get; set; }
    public string? UserName { get; set; }
    public string? UserId { get; set; }
}
