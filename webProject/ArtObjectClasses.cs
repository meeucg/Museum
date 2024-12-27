using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace webProject
{
    public class ResponceInfo
    {
        public string LicenseText { get; set; }
        public List<string?> LicenseLinks { get; set; }
    }
    public class Config
    {
        public string IiifUrl { get; set; }
        public string WebsiteUrl { get; set; }
    }
    public class ApiResponceSingle
    {
        public Art Data { get; set; }
        public ResponceInfo Info { get; set; }
        public Config Config { get; set; }
    }
    public class ApiResponceMultiple
    {
        public List<Art> Data { get; set; }
        public ResponceInfo Info { get; set; }
        public Config Config { get; set; }
    }
    public class Art
    {
        public string? Title { get; set; }
        public string? DateDisplay { get; set; }
        public string? ArtistDisplay { get; set; }
        public List<DimensionsDetail> DimensionsDetail { get; set; }
        public string? ArtworkTypeTitle { get; set; }
        public string ImageId { get; set; }
    }

    public class DimensionsDetail
    {
        public int? Depth { get; set; }
        public int? Width { get; set; }
        public int? Height { get; set; }
        public int? Diameter { get; set; }
        public string? Clarification { get; set; }
    }
}
