using ExcelDatabaseImportTool.Data.Context;
using ExcelDatabaseImportTool.Services.Database;
using ExcelDatabaseImportTool.Utilities;
using Microsoft.EntityFrameworkCore;

// Simple console tool to fix corrupted passwords in the database
Console.WriteLine("Excel Database Import Tool - Password Fix Utility");
Console.WriteLine("==================================================\n");

var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
optionsBuilder.UseSqlite("Data Source=ExcelImportTool.db");

using var context = new ApplicationDbContext(optionsBuilder.Options);
var encryptionService = new EncryptionService();
var fixer = new FixCorruptedPasswords(context, encryptionService);

Console.WriteLine("Choose an option:");
Console.WriteLine("1. Clear all passwords (you'll need to re-enter them)");
Console.WriteLine("2. Try to re-encrypt passwords (if they were stored as plain text)");
Console.Write("\nEnter option (1 or 2): ");

var choice = Console.ReadLine();

try
{
    if (choice == "1")
    {
        var count = await fixer.ClearAllPasswordsAsync();
        Console.WriteLine($"\n✓ Cleared passwords for {count} configuration(s).");
        Console.WriteLine("Please open the application and re-enter your passwords.");
    }
    else if (choice == "2")
    {
        var count = await fixer.FixPasswordsAsync();
        Console.WriteLine($"\n✓ Fixed {count} password(s).");
        if (count == 0)
        {
            Console.WriteLine("No passwords needed fixing, or they couldn't be fixed.");
            Console.WriteLine("Consider using option 1 to clear passwords instead.");
        }
    }
    else
    {
        Console.WriteLine("\nInvalid option.");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"\n✗ Error: {ex.Message}");
}

Console.WriteLine("\nPress any key to exit...");
Console.ReadKey();
