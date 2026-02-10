using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ExcelDatabaseImportTool.Models.Configuration
{
    [Table("ForeignKeyMappings")]
    public class ForeignKeyMapping
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string ReferencedTable { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string ReferencedLookupField { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string ReferencedKeyField { get; set; } = string.Empty;

        // Navigation properties
        public virtual ICollection<FieldMapping> FieldMappings { get; set; } = new List<FieldMapping>();
    }
}