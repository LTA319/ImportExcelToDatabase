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
