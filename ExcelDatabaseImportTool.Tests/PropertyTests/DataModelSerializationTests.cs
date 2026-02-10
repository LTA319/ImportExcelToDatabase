using ExcelDatabaseImportTool.Models.Configuration;
using ExcelDatabaseImportTool.Models.Domain;
using ExcelDatabaseImportTool.Services.Database;
using ExcelDatabaseImportTool.Interfaces.Services;
using System.IO;

namespace ExcelDatabaseImportTool.Tests.PropertyTests
{
    /// <summary>
    /// **Feature: excel-database-import-tool, Property 13: Sensitive data encryption**
    /// **Validates: Requirements 6.2**
    /// </summary>
    public static class DataModelSerializationTests
    {
        public static void RunEncryptionTests()
        {
            var encryptionService = new EncryptionService();
            var results = new List<string>();
            
            // Test with various password strings
            var testPasswords = new[]
            {
                "simplePassword123",
                "Complex!P@ssw0rd#2024",
                "短密码",
                "VeryLongPasswordWithManyCharacters1234567890!@#$%^&*()",
                "password with spaces",
                "123456789",
                "SpecialChars!@#$%^&*()_+-=[]{}|;':\",./<>?"
            };

            results.Add("Running encryption tests...");
            
            foreach (var password in testPasswords)
            {
                try
                {
                    // Arrange
                    var config = new DatabaseConfiguration
                    {
                        Id = 1,
                        Name = "Test Config",
                        Type = DatabaseType.MySQL,
                        Server = "localhost",
                        Database = "testdb",
                        Username = "testuser",
                        Port = 3306,
                        CreatedDate = DateTime.UtcNow,
                        ModifiedDate = DateTime.UtcNow
                    };

                    // Act - Encrypt the password
                    var encryptedPassword = encryptionService.Encrypt(password);
                    config.EncryptedPassword = encryptedPassword;

                    // Verify - Encrypted password should be different from original
                    if (config.EncryptedPassword == password)
                    {
                        results.Add($"FAIL: Encrypted password same as original for: {password}");
                        continue;
                    }

                    // Act - Decrypt the password
                    var decryptedPassword = encryptionService.Decrypt(config.EncryptedPassword);

                    // Verify - Decrypted password should match original
                    if (decryptedPassword != password)
                    {
                        results.Add($"FAIL: Decrypted password doesn't match original for: {password}");
                        continue;
                    }

                    // Verify - Encrypted password should not be empty
                    if (string.IsNullOrWhiteSpace(config.EncryptedPassword))
                    {
                        results.Add($"FAIL: Encrypted password is empty for: {password}");
                        continue;
                    }

                    results.Add($"PASS: Encryption test successful for password of length {password.Length}");
                }
                catch (Exception ex)
                {
                    results.Add($"ERROR: Exception during test for password '{password}': {ex.Message}");
                }
            }

            // Test empty password handling
            try
            {
                encryptionService.Encrypt("");
                results.Add("FAIL: Should have thrown exception for empty password");
            }
            catch (ArgumentException)
            {
                results.Add("PASS: Correctly threw exception for empty password");
            }

            try
            {
                encryptionService.Encrypt(null!);
                results.Add("FAIL: Should have thrown exception for null password");
            }
            catch (ArgumentException)
            {
                results.Add("PASS: Correctly threw exception for null password");
            }

            results.Add("Encryption tests completed.");
            
            // Write results to file
            File.WriteAllLines("encryption_test_results.txt", results);
        }
    }
}