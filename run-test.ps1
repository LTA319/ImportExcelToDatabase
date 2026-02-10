# Simple PowerShell script to run the field mapping consistency tests

Write-Host "Compiling and running Field Mapping Consistency Tests..."

# Create a simple C# program that calls the test
$code = @"
using System;
using System.Reflection;

class TestExecutor
{
    static void Main()
    {
        try
        {
            // Load the assembly
            var assembly = Assembly.LoadFrom("ExcelDatabaseImportTool/bin/Debug/net8.0-windows/ExcelDatabaseImportTool.dll");
            
            // Get the TestRunner type
            var testRunnerType = assembly.GetType("ExcelDatabaseImportTool.TestRunner");
            
            // Get the method
            var method = testRunnerType.GetMethod("RunFieldMappingConsistencyTests");
            
            // Invoke the method
            method.Invoke(null, null);
            
            Console.WriteLine("Tests completed successfully!");
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error running tests: " + ex.Message);
            Console.WriteLine(ex.StackTrace);
        }
    }
}
"@

# Save the code to a file
$code | Out-File -FilePath "TestExecutor.cs" -Encoding UTF8

# Compile it
Write-Host "Compiling test executor..."
dotnet build ExcelDatabaseImportTool/ExcelDatabaseImportTool.csproj 2>&1 | Out-Null

if ($LASTEXITCODE -eq 0) {
    Write-Host "Build successful, but there are multiple entry points. Running tests via reflection..."
    
    # Try to run using PowerShell reflection
    try {
        [System.Reflection.Assembly]::LoadFrom("$PWD/ExcelDatabaseImportTool/bin/Debug/net8.0-windows/ExcelDatabaseImportTool.dll") | Out-Null
        [ExcelDatabaseImportTool.TestRunner]::RunFieldMappingConsistencyTests()
        Write-Host "Tests completed! Check field_mapping_consistency_test_results.txt for results."
    }
    catch {
        Write-Host "Error: $_"
    }
}
else {
    Write-Host "Build failed."
}
