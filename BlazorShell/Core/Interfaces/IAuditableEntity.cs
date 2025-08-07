namespace BlazorShell.Core.Interfaces
{
    public interface IAuditableEntity
    {
        DateTime CreatedDate { get; set; }
        DateTime? ModifiedDate { get; set; }
        string? CreatedBy { get; set; }
        string? ModifiedBy { get; set; }
    }
}