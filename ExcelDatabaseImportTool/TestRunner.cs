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

        public static void RunTransactionAtomicityTests()
        {
            TransactionAtomicityTests.RunTransactionAtomicityTests();
        }

        public static void RunErrorHandlingContinuityTests()
        {
            ErrorHandlingContinuityTests.RunErrorHandlingContinuityTests();
        }

        public static void RunImportStatisticsTests()
        {
            ImportStatisticsTests.RunImportStatisticsTests();
        }

        public static void RunComprehensiveLoggingTests()
        {
            ComprehensiveLoggingTests.RunComprehensiveLoggingTests();
        }

        public static void RunFieldMappingConsistencyTests()
        {
            FieldMappingConsistencyTests.RunFieldMappingConsistencyTests();
        }
    }
}