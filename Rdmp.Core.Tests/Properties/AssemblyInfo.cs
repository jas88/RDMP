using System.Runtime.InteropServices;
using NUnit.Framework;

// Setting ComVisible to false makes the types in this assembly not visible
// to COM components.  If you need to access a type in this assembly from
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("13825638-5252-413c-98bc-1aef3b1cb9e4")]

// Disable parallel test execution to prevent database deadlocks
// - Multiple test assemblies share TEST_Catalogue database
// - Running database tests in parallel causes deadlocks and foreign key violations
// - Tests must run sequentially to avoid resource conflicts
[assembly: NonParallelizable]
[assembly: LevelOfParallelism(1)]