using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using webProject.Models;

namespace webProject.Entities;

public class Picture
{
    public long Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string ImageUrl { get; set; }
    public long? OwnerId { get; set; }
    public string ContentType { get; set; }
}
