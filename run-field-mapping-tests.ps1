# Load the assembly
Add-Type -Path "ExcelDatabaseImportTool/bin/Debug/net8.0-windows/ExcelDatabaseImportTool.dll"

# Run the tests
[ExcelDatabaseImportTool.TestRunner]::RunFieldMappingConsistencyTests()

Write-Host "Tests completed. Check field_mapping_consistency_test_results.txt for results."
