using System.Runtime.InteropServices;
using NUnit.Framework;

// Setting ComVisible to false makes the types in this assembly not visible
// to COM components.  If you need to access a type in this assembly from
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("13825638-5252-413c-98bc-1aef3b1cb9e4")]

// Enable parallel test execution for UnitTests (tests with no database dependencies)
// - Fixtures run in parallel (different test classes can run simultaneously)
// - Tests within a fixture run sequentially (tests in the same class run one at a time)
// - DatabaseTests remain [NonParallelizable] and will run sequentially
[assembly: Parallelizable(ParallelScope.Fixtures)]
[assembly: LevelOfParallelism(8)]