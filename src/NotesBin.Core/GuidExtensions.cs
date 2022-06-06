using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NotesBin.Core;

public static class GuidExtensions
{
    public static string ToShortString(this Guid id)
    {
        return id.ToString().Substring(0, 8);
    }
}
