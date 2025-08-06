using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;

namespace BlazorShell.Domain.Entities
{
    public interface IAuditableEntity
    {
        DateTime CreatedDate { get; set; }
        DateTime? ModifiedDate { get; set; }
        string? CreatedBy { get; set; }
        string? ModifiedBy { get; set; }
    }

}
