using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace webProject.Entities
{
    public class Collection
    {
        public Collection(string Name, string Description) {
            this.Name = Name;
            this.Description = Description;
        }
        public string Name { get; set; }
        public string Description { get; set; }
    }
}
