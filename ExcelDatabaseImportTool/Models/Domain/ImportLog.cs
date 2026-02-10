using ExcelDatabaseImportTool.Models.Configuration;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ExcelDatabaseImportTool.Models.Domain
{
    [Table("ImportLogs")]
    public class ImportLog
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [ForeignKey("ImportConfiguration")]
        public int ImportConfigurationId { get; set; }

        [Required]
        [MaxLength(255)]
        public string ExcelFileName { get; set; } = string.Empty;

        [Required]
        public DateTime StartTime { get; set; }

        public DateTime? EndTime { get; set; }

        [Required]
        public ImportStatus Status { get; set; }

        [Range(0, int.MaxValue)]
        public int TotalRecords { get; set; }

        [Range(0, int.MaxValue)]
        public int SuccessfulRecords { get; set; }

        [Range(0, int.MaxValue)]
        public int FailedRecords { get; set; }

        public string ErrorDetails { get; set; } = string.Empty;

        // Navigation properties
        public virtual ImportConfiguration? ImportConfiguration { get; set; }
    }
}