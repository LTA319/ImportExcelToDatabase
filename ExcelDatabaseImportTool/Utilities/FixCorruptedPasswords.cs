using ExcelDatabaseImportTool.Data.Context;
using ExcelDatabaseImportTool.Interfaces.Services;
using Microsoft.EntityFrameworkCore;

namespace ExcelDatabaseImportTool.Utilities
{
    /// <summary>
    /// Utility to fix corrupted passwords in database configurations.
    /// This can be run as a one-time fix if passwords were stored incorrectly.
    /// </summary>
    public class FixCorruptedPasswords
    {
        private readonly ApplicationDbContext _context;
        private readonly IEncryptionService _encryptionService;

        public FixCorruptedPasswords(ApplicationDbContext context, IEncryptionService encryptionService)
        {
            _context = context;
            _encryptionService = encryptionService;
        }

        /// <summary>
        /// Attempts to re-encrypt passwords that may have been stored as plain text.
        /// </summary>
        public async Task<int> FixPasswordsAsync()
        {
            var configs = await _context.DatabaseConfigurations.ToListAsync();
            int fixedCount = 0;

            foreach (var config in configs)
            {
                if (string.IsNullOrWhiteSpace(config.EncryptedPassword))
                    continue;

                try
                {
                    // Try to decrypt - if it works, password is already encrypted correctly
                    _encryptionService.Decrypt(config.EncryptedPassword);
                }
                catch (FormatException)
                {
                    // Password is not properly encrypted - assume it's plain text and re-encrypt
                    try
                    {
                        config.EncryptedPassword = _encryptionService.Encrypt(config.EncryptedPassword);
                        fixedCount++;
                    }
                    catch
                    {
                        // If encryption fails, skip this entry
                        continue;
                    }
                }
            }

            if (fixedCount > 0)
            {
                await _context.SaveChangesAsync();
            }

            return fixedCount;
        }

        /// <summary>
        /// Clears all passwords from database configurations.
        /// Users will need to re-enter passwords.
        /// </summary>
        public async Task<int> ClearAllPasswordsAsync()
        {
            var configs = await _context.DatabaseConfigurations.ToListAsync();
            
            foreach (var config in configs)
            {
                config.EncryptedPassword = string.Empty;
            }

            await _context.SaveChangesAsync();
            return configs.Count;
        }
    }
}
