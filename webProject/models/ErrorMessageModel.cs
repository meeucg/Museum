using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace webProject.Models;

public class ErrorMessageModel
{
    public ErrorMessageModel(string? error)
    {
        Error = error ?? "Unhandled exception";
    }

    public string? Error { get; set; }
}
