using ExcelDatabaseImportTool.Tests.PropertyTests;

namespace ExcelDatabaseImportTool
{
    public static class TestRunner
    {
        public static void RunFileValidationTests()
        {
            FileValidationTests.RunFileValidationTests();
        }

        public static void RunDataValidationTests()
        {
            DataValidationTests.RunDataValidationTests();
        }

        public static void RunForeignKeyResolutionTests()
        {
            ForeignKeyResolutionTests.RunForeignKeyResolutionTests();
        }
    }
}