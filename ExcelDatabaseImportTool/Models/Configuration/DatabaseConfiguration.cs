using ExcelDatabaseImportTool.Models.Domain;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ExcelDatabaseImportTool.Models.Configuration
{
    [Table("DatabaseConfigurations")]
    public class DatabaseConfiguration
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        public DatabaseType Type { get; set; }

        [Required]
        [MaxLength(255)]
        public string Server { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string Database { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string Username { get; set; } = string.Empty;

        [Required]
        [MaxLength(500)]
        public string EncryptedPassword { get; set; } = string.Empty;

        [Range(1, 65535)]
        public int Port { get; set; }

        [MaxLength(1000)]
        public string ConnectionString { get; set; } = string.Empty;

        [Required]
        public DateTime CreatedDate { get; set; }

        [Required]
        public DateTime ModifiedDate { get; set; }

        // Navigation properties
        public virtual ICollection<ImportConfiguration> ImportConfigurations { get; set; } = new List<ImportConfiguration>();
    }
}