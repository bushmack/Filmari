using System.Collections.Generic;

namespace filamri.Models
{
    public class Collection
    {
        public string Name { get; set; } = "";
        public List<int> Movies { get; set; } = new();
    }
}