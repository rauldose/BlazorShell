namespace BlazorShell.Core.Entities
{
    // Application settings
    public class Setting
    {
        public int Id { get; set; }
        public string? Category { get; set; }
        public string? Key { get; set; }
        public string? Value { get; set; }
        public string? DataType { get; set; }
        public string? Description { get; set; }
        public bool IsPublic { get; set; }
        public bool IsEncrypted { get; set; }
        public DateTime ModifiedDate { get; set; }
        public string? ModifiedBy { get; set; }
    }
}