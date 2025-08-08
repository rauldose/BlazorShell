using System;
using System.ComponentModel.DataAnnotations;

namespace BlazorShell.Domain.Entities
{
    /// <summary>
    /// Represents a system configuration setting
    /// </summary>
    public class Setting : IAuditableEntity
    {
        public int Id { get; set; }
        /// <summary>
        /// The unique key for the setting
        /// </summary>
        [Required]
        [MaxLength(255)]
        public string Key { get; set; } = string.Empty;

        /// <summary>
        /// The value of the setting
        /// </summary>
        [Required]
        public string Value { get; set; } = string.Empty;

        /// <summary>
        /// Optional description of what this setting controls
        /// </summary>
        [MaxLength(500)]
        public string? Description { get; set; }

        /// <summary>
        /// Category for grouping related settings
        /// </summary>
        [MaxLength(100)]
        public string? Category { get; set; }

        /// <summary>
        /// Data type hint for the value (e.g., "String", "Integer", "Boolean", "Password", "Json")
        /// </summary>
        [MaxLength(50)]
        public string? DataType { get; set; } = "String";

        /// <summary>
        /// Whether this setting is visible in the UI
        /// </summary>
        public bool IsVisible { get; set; } = true;

        /// <summary>
        /// Whether this setting is read-only
        /// </summary>
        public bool IsReadOnly { get; set; } = false;

        /// <summary>
        /// Display order for UI presentation
        /// </summary>
        public int DisplayOrder { get; set; } = 0;

        /// <summary>
        /// Validation rules in JSON format (optional)
        /// </summary>
        public string? ValidationRules { get; set; }

        /// <summary>
        /// Default value for the setting
        /// </summary>
        public string? DefaultValue { get; set; }

        /// <summary>
        /// Whether the setting value should be encrypted in storage
        /// </summary>
        public bool IsEncrypted { get; set; } = false;

        /// <summary>
        /// Whether this is a public setting (visible to all users)
        /// </summary>
        public bool IsPublic { get; set; } = true;
        public DateTime CreatedDate  { get; set; }
        public DateTime? ModifiedDate  { get; set; }
        public string? CreatedBy  { get; set; }
        public string? ModifiedBy  { get; set; }
    }
}