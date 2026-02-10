using ExcelDatabaseImportTool.Services.Database;
using System.IO;

namespace ExcelDatabaseImportTool.Tests.PropertyTests
{
    /// <summary>
    /// **Feature: excel-database-import-tool, Property 13: Sensitive data encryption**
    /// **Validates: Requirements 6.2**
    /// </summary>
    public static class EncryptionTestRunner
    {
        public static void RunEncryptionTests()
        {
            var results = new List<string>();
            results.Add("Running encryption tests...");

            try
            {
                var encryptionService = new EncryptionService();

                // Test basic encryption/decryption
                TestBasicEncryptionDecryption(encryptionService, results);
                
                // Test encryption consistency
                TestEncryptionConsistency(encryptionService, results);
                
                // Test round trip with various passwords
                TestRoundTripWithVariousPasswords(encryptionService, results);
                
                results.Add("Encryption tests completed.");
            }
            catch (Exception ex)
            {
                results.Add($"ERROR: Exception during encryption tests: {ex.Message}");
                results.Add($"Stack trace: {ex.StackTrace}");
            }
            
            // Write results to file
            File.WriteAllLines("encryption_test_results.txt", results);
        }

        private static void TestBasicEncryptionDecryption(EncryptionService encryptionService, List<string> results)
        {
            var testPasswords = new[]
            {
                "simple_password",
                "complex_P@ssw0rd!",
                "very_long_password_with_many_characters_123456789",
                "password with spaces",
                "пароль_с_unicode",
                "123456789",
                "!@#$%^&*()"
            };

            foreach (var password in testPasswords)
            {
                try
                {
                    // Encrypt the password
                    var encrypted = encryptionService.Encrypt(password);
                    
                    // Verify it's actually encrypted (different from original)
                    if (encrypted == password)
                    {
                        results.Add($"FAIL: Password not encrypted for: {password}");
                        continue;
                    }
                    
                    // Decrypt it back
                    var decrypted = encryptionService.Decrypt(encrypted);
                    
                    // Verify round trip
                    if (decrypted != password)
                    {
                        results.Add($"FAIL: Round trip failed for password: {password}");
                        results.Add($"  Original: {password}");
                        results.Add($"  Decrypted: {decrypted}");
                        continue;
                    }
                    
                    results.Add($"PASS: Round trip successful for password type: {GetPasswordType(password)}");
                }
                catch (Exception ex)
                {
                    results.Add($"ERROR: Exception during basic encryption test for password '{password}': {ex.Message}");
                }
            }
        }

        private static void TestEncryptionConsistency(EncryptionService encryptionService, List<string> results)
        {
            var testPassword = "test_password_123";
            
            try
            {
                // Encrypt the same password multiple times
                var encrypted1 = encryptionService.Encrypt(testPassword);
                var encrypted2 = encryptionService.Encrypt(testPassword);
                var encrypted3 = encryptionService.Encrypt(testPassword);
                
                // All encrypted values should be identical (since we're using fixed IV for testing)
                if (encrypted1 != encrypted2 || encrypted2 != encrypted3)
                {
                    results.Add($"FAIL: Encryption not consistent");
                    results.Add($"  Encrypted 1: {encrypted1}");
                    results.Add($"  Encrypted 2: {encrypted2}");
                    results.Add($"  Encrypted 3: {encrypted3}");
                    return;
                }
                
                // All should decrypt to the same original value
                var decrypted1 = encryptionService.Decrypt(encrypted1);
                var decrypted2 = encryptionService.Decrypt(encrypted2);
                var decrypted3 = encryptionService.Decrypt(encrypted3);
                
                if (decrypted1 != testPassword || decrypted2 != testPassword || decrypted3 != testPassword)
                {
                    results.Add($"FAIL: Decryption not consistent");
                    return;
                }
                
                results.Add($"PASS: Encryption/decryption is consistent");
            }
            catch (Exception ex)
            {
                results.Add($"ERROR: Exception during encryption consistency test: {ex.Message}");
            }
        }

        private static void TestRoundTripWithVariousPasswords(EncryptionService encryptionService, List<string> results)
        {
            // Test with edge cases
            var edgeCasePasswords = new[]
            {
                "a", // Single character
                "", // Empty string (should throw)
                new string('x', 1000), // Very long password
                "\n\r\t", // Special whitespace characters
                "null", // String that looks like null
                "password\0with\0nulls" // Password with null characters
            };

            foreach (var password in edgeCasePasswords)
            {
                try
                {
                    if (string.IsNullOrEmpty(password))
                    {
                        // Should throw exception
                        try
                        {
                            encryptionService.Encrypt(password);
                            results.Add($"FAIL: Should have thrown exception for empty/null password");
                        }
                        catch (ArgumentException)
                        {
                            results.Add($"PASS: Correctly threw exception for empty/null password");
                        }
                        continue;
                    }
                    
                    var encrypted = encryptionService.Encrypt(password);
                    var decrypted = encryptionService.Decrypt(encrypted);
                    
                    if (decrypted != password)
                    {
                        results.Add($"FAIL: Round trip failed for edge case password");
                        continue;
                    }
                    
                    results.Add($"PASS: Round trip successful for edge case: {GetPasswordType(password)}");
                }
                catch (Exception ex)
                {
                    results.Add($"ERROR: Exception during edge case test: {ex.Message}");
                }
            }
        }

        private static string GetPasswordType(string password)
        {
            if (string.IsNullOrEmpty(password)) return "empty";
            if (password.Length == 1) return "single_char";
            if (password.Length > 100) return "very_long";
            if (password.Contains(' ')) return "with_spaces";
            if (password.Any(c => c > 127)) return "unicode";
            if (password.All(char.IsDigit)) return "numeric";
            if (password.All(c => !char.IsLetterOrDigit(c))) return "special_chars";
            return "normal";
        }
    }
}