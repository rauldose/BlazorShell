using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlazorShell.Domain.Entities
{
    public enum PermissionType
    {
        Read,
        Write,
        Delete,
        Execute,
        Admin,
        View
    }
}
