using ExcelDatabaseImportTool;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("Running Field Mapping Consistency Tests...");
        TestRunner.RunFieldMappingConsistencyTests();
        Console.WriteLine("Tests completed. Check field_mapping_consistency_test_results.txt for results.");
    }
}
