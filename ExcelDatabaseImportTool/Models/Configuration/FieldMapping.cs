using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ExcelDatabaseImportTool.Models.Configuration
{
    [Table("FieldMappings")]
    public class FieldMapping
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string ExcelColumnName { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string DatabaseFieldName { get; set; } = string.Empty;

        public bool IsRequired { get; set; }

        [Required]
        [MaxLength(50)]
        public string DataType { get; set; } = string.Empty;

        [ForeignKey("ImportConfiguration")]
        public int ImportConfigurationId { get; set; }

        [ForeignKey("ForeignKeyMapping")]
        public int? ForeignKeyMappingId { get; set; }

        // Navigation properties
        public virtual ImportConfiguration? ImportConfiguration { get; set; }
        public virtual ForeignKeyMapping? ForeignKeyMapping { get; set; }
    }
}