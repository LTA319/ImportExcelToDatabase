using NUnit.Framework;
using OfficeOpenXml;
using System.Reflection;

[assembly: LevelOfParallelism(1)]

namespace ExcelDatabaseImportTool.Tests
{
    [SetUpFixture]
    public class GlobalSetup
    {
        [OneTimeSetUp]
        public void RunBeforeAnyTests()
        {
            // Debug: Test reflection first
            TestLicenseSetup.TestReflection();
            
            // Set EPPlus license for all tests using the new EPPlus 8.x API
            // EPPlus 8.x requires: ExcelPackage.License.SetLicense(LicenseType.NonCommercial)
            try
            {
                // Get the static License property
                var licenseProperty = typeof(ExcelPackage).GetProperty("License", 
                    BindingFlags.Public | BindingFlags.Static);
                
                if (licenseProperty != null)
                {
                    var licenseObject = licenseProperty.GetValue(null);
                    if (licenseObject != null)
                    {
                        // Get the LicenseType enum type
                        var licenseTypeType = licenseObject.GetType().Assembly.GetType("OfficeOpenXml.LicenseType");
                        if (licenseTypeType != null && licenseTypeType.IsEnum)
                        {
                            // Get the NonCommercial enum value
                            var nonCommercialValue = Enum.Parse(licenseTypeType, "NonCommercial");
                            
                            // Get the SetLicense method
                            var setLicenseMethod = licenseObject.GetType().GetMethod("SetLicense", 
                                BindingFlags.Public | BindingFlags.Instance,
                                null,
                                new[] { licenseTypeType },
                                null);
                            
                            if (setLicenseMethod != null)
                            {
                                setLicenseMethod.Invoke(licenseObject, new[] { nonCommercialValue });
                                Console.WriteLine("EPPlus license set successfully to NonCommercial");
                                return;
                            }
                            else
                            {
                                Console.WriteLine("ERROR: SetLicense method not found");
                            }
                        }
                        else
                        {
                            Console.WriteLine("ERROR: LicenseType enum not found or not an enum");
                        }
                    }
                    else
                    {
                        Console.WriteLine("ERROR: License property returned null");
                    }
                }
                else
                {
                    Console.WriteLine("ERROR: License property not found on ExcelPackage");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR: Failed to set EPPlus license: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
            
            // If we get here, the license setting failed
            Console.WriteLine("WARNING: Could not set EPPlus license. Tests may fail.");
        }
    }
}
