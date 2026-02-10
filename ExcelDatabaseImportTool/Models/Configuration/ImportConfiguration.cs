using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ExcelDatabaseImportTool.Models.Domain;

namespace ExcelDatabaseImportTool.Models.Configuration
{
    [Table("ImportConfigurations")]
    public class ImportConfiguration
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [ForeignKey("DatabaseConfiguration")]
        public int DatabaseConfigurationId { get; set; }

        [Required]
        [MaxLength(100)]
        public string TableName { get; set; } = string.Empty;

        public List<FieldMapping> FieldMappings { get; set; } = new List<FieldMapping>();

        public bool HasHeaderRow { get; set; }

        [Required]
        public DateTime CreatedDate { get; set; }

        [Required]
        public DateTime ModifiedDate { get; set; }

        // Navigation properties
        public virtual DatabaseConfiguration? DatabaseConfiguration { get; set; }
        public virtual ICollection<ImportLog> ImportLogs { get; set; } = new List<ImportLog>();
    }
}