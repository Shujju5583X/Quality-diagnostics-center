using System.ComponentModel.DataAnnotations;

namespace LabSystem.Core.Models
{
    public class Setting
    {
        [Key]
        public string Key { get; set; }
        public string Value { get; set; }
    }
}
