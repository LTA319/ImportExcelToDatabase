using System;
using System.Reflection;
using OfficeOpenXml;

namespace ExcelDatabaseImportTool.Tests
{
    public class TestLicenseSetup
    {
        public static void TestReflection()
        {
            Console.WriteLine("=== Testing EPPlus License Reflection ===");
            
            // Check if License property exists
            var licenseProperty = typeof(ExcelPackage).GetProperty("License", 
                BindingFlags.Public | BindingFlags.Static);
            Console.WriteLine($"License property found: {licenseProperty != null}");
            
            if (licenseProperty != null)
            {
                Console.WriteLine($"License property type: {licenseProperty.PropertyType.FullName}");
                
                var licenseObject = licenseProperty.GetValue(null);
                Console.WriteLine($"License object is null: {licenseObject == null}");
                
                if (licenseObject != null)
                {
                    Console.WriteLine($"License object type: {licenseObject.GetType().FullName}");
                    
                    // List all methods on the license object
                    var methods = licenseObject.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance);
                    Console.WriteLine($"Methods on License object:");
                    foreach (var method in methods)
                    {
                        Console.WriteLine($"  - {method.Name}({string.Join(", ", method.GetParameters().Select(p => p.ParameterType.Name))})");
                    }
                    
                    // Check for LicenseType enum
                    var assembly = licenseObject.GetType().Assembly;
                    var licenseTypeType = assembly.GetType("OfficeOpenXml.LicenseType");
                    Console.WriteLine($"LicenseType found: {licenseTypeType != null}");
                    
                    if (licenseTypeType != null)
                    {
                        Console.WriteLine($"LicenseType is enum: {licenseTypeType.IsEnum}");
                        if (licenseTypeType.IsEnum)
                        {
                            var values = Enum.GetNames(licenseTypeType);
                            Console.WriteLine($"LicenseType values: {string.Join(", ", values)}");
                        }
                    }
                }
            }
        }
    }
}
