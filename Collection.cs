using System.Collections.Generic;

namespace OneButtonApp.Models
{
    public class Collection
    {
        public string Name { get; set; } = "";
        public List<int> Movies { get; set; } = new List<int>();
    }
}