using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace webProject.Models;

public class UserPictureModel
{
    public string Name { get; set; }
    public string Description { get; set; }
    public string ImageBase64 { get; set; }
    public string ContentType { get; set; }
}
