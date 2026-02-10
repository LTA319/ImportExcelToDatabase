using Microsoft.EntityFrameworkCore;
using ExcelDatabaseImportTool.Models.Domain;
using ExcelDatabaseImportTool.Models.Configuration;

namespace ExcelDatabaseImportTool.Data.Context
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<DatabaseConfiguration> DatabaseConfigurations { get; set; }
        public DbSet<ImportConfiguration> ImportConfigurations { get; set; }
        public DbSet<FieldMapping> FieldMappings { get; set; }
        public DbSet<ForeignKeyMapping> ForeignKeyMappings { get; set; }
        public DbSet<ImportLog> ImportLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure DatabaseConfiguration
            modelBuilder.Entity<DatabaseConfiguration>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Server).IsRequired().HasMaxLength(255);
                entity.Property(e => e.Database).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Username).IsRequired().HasMaxLength(100);
                entity.Property(e => e.EncryptedPassword).IsRequired().HasMaxLength(500);
                entity.Property(e => e.ConnectionString).HasMaxLength(1000);
                entity.Property(e => e.Type).IsRequired();
                entity.Property(e => e.Port).IsRequired();
                entity.Property(e => e.CreatedDate).IsRequired();
                entity.Property(e => e.ModifiedDate).IsRequired();

                // Configure relationships
                entity.HasMany(d => d.ImportConfigurations)
                    .WithOne(i => i.DatabaseConfiguration)
                    .HasForeignKey(i => i.DatabaseConfigurationId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Configure ImportConfiguration
            modelBuilder.Entity<ImportConfiguration>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.TableName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.HasHeaderRow).IsRequired();
                entity.Property(e => e.CreatedDate).IsRequired();
                entity.Property(e => e.ModifiedDate).IsRequired();

                // Configure relationships
                entity.HasOne(i => i.DatabaseConfiguration)
                    .WithMany(d => d.ImportConfigurations)
                    .HasForeignKey(i => i.DatabaseConfigurationId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasMany(i => i.ImportLogs)
                    .WithOne(l => l.ImportConfiguration)
                    .HasForeignKey(l => l.ImportConfigurationId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Configure FieldMapping
            modelBuilder.Entity<FieldMapping>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.ExcelColumnName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.DatabaseFieldName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.IsRequired).IsRequired();
                entity.Property(e => e.DataType).IsRequired().HasMaxLength(50);

                // Configure relationships
                entity.HasOne(f => f.ForeignKeyMapping)
                    .WithMany(fk => fk.FieldMappings)
                    .HasForeignKey(f => f.ForeignKeyMappingId)
                    .OnDelete(DeleteBehavior.SetNull);

                // Add foreign key to ImportConfiguration
                entity.Property<int>("ImportConfigurationId");
                entity.HasOne<ImportConfiguration>()
                    .WithMany(i => i.FieldMappings)
                    .HasForeignKey("ImportConfigurationId")
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure ForeignKeyMapping
            modelBuilder.Entity<ForeignKeyMapping>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.ReferencedTable).IsRequired().HasMaxLength(100);
                entity.Property(e => e.ReferencedLookupField).IsRequired().HasMaxLength(100);
                entity.Property(e => e.ReferencedKeyField).IsRequired().HasMaxLength(100);

                // Configure relationships
                entity.HasMany(fk => fk.FieldMappings)
                    .WithOne(f => f.ForeignKeyMapping)
                    .HasForeignKey(f => f.ForeignKeyMappingId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // Configure ImportLog
            modelBuilder.Entity<ImportLog>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.ExcelFileName).IsRequired().HasMaxLength(255);
                entity.Property(e => e.StartTime).IsRequired();
                entity.Property(e => e.Status).IsRequired();
                entity.Property(e => e.TotalRecords).IsRequired();
                entity.Property(e => e.SuccessfulRecords).IsRequired();
                entity.Property(e => e.FailedRecords).IsRequired();
                entity.Property(e => e.ErrorDetails).HasMaxLength(2000);

                // Configure relationships
                entity.HasOne(l => l.ImportConfiguration)
                    .WithMany(i => i.ImportLogs)
                    .HasForeignKey(l => l.ImportConfigurationId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlite("Data Source=ExcelImportTool.db");
            }
        }
    }
}