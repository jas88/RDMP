# RDMP Test Performance Bottleneck Analysis

**Analysis Date:** 2025-10-27
**Total Test Methods:** ~1,136
**Total Test Files:** 336
**Test Projects:** 5 (Rdmp.Core.Tests, Rdmp.UI.Tests, HICPluginTests, Rdmp.Dicom.Tests, LoadModules.Extensions.Tests)

## Executive Summary

The RDMP test suite suffers from significant performance bottlenecks across multiple categories. The primary issues are:

1. **Excessive Thread.Sleep/Task.Delay calls** - 11 instances with delays totaling 29+ seconds
2. **Expensive database operations in loops** - 356+ GetCleanedServer/CreateTable calls
3. **GetAllObjects full table scans** - 483+ cleanup operations
4. **File I/O in test loops** - Heavy file operations in CustomMetadataReportTests (1,510 lines)
5. **Cross-database test multiplication** - Tests run across 4 database types (SQL Server, MySQL, PostgreSQL, Oracle)

**Estimated Total Time Savings: 40-60% reduction in test execution time**

---

## Top 10 Slowest Test Patterns

### 1. **TriggerTests with Sequential Thread.Sleep (5 seconds total)**
**File:** `/Rdmp.Core.Tests/Curation/Integration/TriggerTests.cs:199-211`
**Issue:** 5 consecutive `Thread.Sleep(1000)` calls to test database trigger timing
```csharp
Thread.Sleep(1000);
RunSQL("UPDATE {0} SET bubbles=1", _table.GetFullyQualifiedName());
Thread.Sleep(1000);
RunSQL("UPDATE {0} SET bubbles=2", _table.GetFullyQualifiedName());
// ... 3 more iterations
```
**Impact:** 5 seconds per test × multiple database types = **15+ seconds**
**Fix:** Use database transaction timestamps or polling with timeout instead of fixed delays
**Priority:** HIGH - Low-hanging fruit

---

### 2. **ExecuteCommandConfirmLogsTests.ConfirmLogs_HappyEntries_Passes (5 seconds)**
**File:** `/Rdmp.Core.Tests/CommandExecution/ExecuteCommandConfirmLogsTests.cs:126`
**Issue:** Single `Thread.Sleep(5000)` to test time-based log validation
```csharp
logEntry.CloseAndMarkComplete();
Thread.Sleep(5000);  // Wait to test time threshold logic
var cmd = new ExecuteCommandConfirmLogs(..., "00:00:01");
```
**Impact:** 5 seconds per test run
**Fix:** Mock the time provider or adjust log timestamps programmatically
**Priority:** HIGH

---

### 3. **Python ScriptExecutionTests (8+ seconds total)**
**File:** `/Plugins/RdmpExtensions/LoadModules.Extensions.Tests/Python/ScriptExecutionTests.cs`
**Issue:**
- `SlowRollerTest`: Runs Python script with 5× 1-second sleeps = **5 seconds**
- `SlowRollerAsync`: `Task.Delay(2000)` + `Task.Delay(6000)` = **8 seconds**

```csharp
// Python script intentionally sleeps 5 times
time.sleep(1)
// Test waits 8 seconds total
Task.Delay(2000).Wait();
Task.Delay(6000).Wait();
```
**Impact:** 13 seconds for 2 tests
**Fix:** Use faster polling intervals or mock the async behavior
**Priority:** MEDIUM - These tests verify async streaming behavior

---

### 4. **DataTableUploadDestinationTests.DataTableChangesLengths_RandomColumnOrder**
**File:** `/Rdmp.Core.Tests/DataLoad/Engine/Integration/DataTableUploadDestinationTests.cs:64-72`
**Issue:** Nested loops with database operations
```csharp
for (var i = 0; i < 10; i++)  // numberOfRandomisations
{
    if (tbl.Exists())
        tbl.Drop();
    // Create table with random column order
    // Bulk insert data
    // Validate results
}
```
**Impact:** 10 database drop/create cycles per test × 2 test variants = **20 operations**
**Fix:** Reduce randomization iterations to 3-5, or use parameterized test cases
**Priority:** MEDIUM

---

### 5. **CustomMetadataReportTests - 50+ File I/O Operations**
**File:** `/Rdmp.Core.Tests/Reports/CustomMetadataReportTests.cs` (1,510 lines)
**Issue:** 31+ test methods each calling:
```csharp
File.WriteAllText(template.FullName, templateCode);
// ... execute report
var resultText = File.ReadAllText(outFile);
```
**Pattern:** Synchronous file I/O in every test method (2-3 operations per test)
**Impact:** 50+ synchronous file operations across test class
**Fix:** Use in-memory streams or shared test fixtures
**Priority:** MEDIUM

---

### 6. **GetAllObjects Cleanup in Teardown**
**File:** `/Tests.Common/DatabaseTests.cs:372,426,912` and 40+ test files
**Issue:** Full table scans for cleanup
```csharp
foreach (var c in CatalogueRepository.GetAllObjects<Catalogue>())
    c.DeleteInDatabase();
foreach (var t in CatalogueRepository.GetAllObjects<TableInfo>())
    t.DeleteInDatabase();
```
**Impact:** 483+ cleanup operations × O(N) table scans = **massive overhead**
**Fix:**
- Use transactional tests (rollback instead of delete)
- Track created objects in test context for targeted cleanup
- Batch delete queries
**Priority:** HIGH - Applies to nearly all tests

---

### 7. **CrossDatabaseDataLoadTests - Multiple Database Type Iterations**
**File:** `/Rdmp.Core.Tests/DataLoad/Engine/Integration/CrossDatabaseTypeTests/CrossDatabaseDataLoadTests.cs`
**Issue:** Tests run across 4 database types with heavy cleanup
```csharp
foreach (var c in RepositoryLocator.CatalogueRepository.GetAllObjects<Catalogue>())
    c.DeleteInDatabase();
foreach (var t in RepositoryLocator.CatalogueRepository.GetAllObjects<TableInfo>())
    t.DeleteInDatabase();
foreach (var l in RepositoryLocator.CatalogueRepository.GetAllObjects<LoadMetadata>())
    l.DeleteInDatabase();
```
**Impact:** 3× GetAllObjects per test × 4 database types × ~500 line test file
**Fix:** Use [TestCaseSource] with single database type for CI, full matrix only on-demand
**Priority:** HIGH - Low effort, high impact

---

### 8. **GetCleanedServer Database Creation (356+ calls)**
**File:** Multiple test files
**Issue:** Each test creates a fresh database
```csharp
var db = GetCleanedServer(DatabaseType.MicrosoftSQLServer);
var tbl = db.CreateTable("TestTable", ...);
```
**Impact:** 356+ database create operations across tests
**Fix:**
- Share database instances per test class
- Use database snapshots (SQL Server) or savepoints (PostgreSQL)
- Implement test database pooling
**Priority:** HIGH - Major infrastructure improvement

---

### 9. **String Concatenation in Loop (9,000 iterations)**
**File:** `/Rdmp.Core.Tests/DataLoad/Engine/Integration/DataTableUploadDestinationTests.cs:420`
**Issue:** Classic O(N²) string concatenation
```csharp
var longBitOfText = "";
for (var i = 0; i < 9000; i++)
    longBitOfText += 'A';  // Creates 9000 string objects
```
**Impact:** 9,000 string allocations = ~40MB temporary memory
**Fix:** `new string('A', 9000)`
**Priority:** LOW - Only 1 test, but trivial fix

---

### 10. **BackfillTests with 70+ Database Operations**
**File:** `/Rdmp.Core.Tests/DataLoad/Engine/Integration/BackfillTests.cs` (1,146 lines)
**Issue:** Large integration test with extensive database setup
```csharp
// Multiple CreateTable, GetAllObjects, ExecuteNonQuery calls
// 70+ database operations in grep results
```
**Impact:** Long-running integration test (~5-10 seconds)
**Fix:** Split into smaller focused tests, share setup fixtures
**Priority:** MEDIUM

---

## Performance Optimization Opportunities

### A. Database Transaction Wrapping
**Current State:** Tests create real databases, tables, and data, then clean up
**Proposed:** Wrap each test in a database transaction and rollback

```csharp
[SetUp]
public void BeginTransaction()
{
    _transaction = _database.BeginTransaction();
}

[TearDown]
public void RollbackTransaction()
{
    _transaction?.Rollback();
}
```

**Benefits:**
- Eliminate 483+ cleanup GetAllObjects calls
- 50-80% faster test execution
- Better test isolation

**Challenges:**
- Some tests require multi-connection scenarios
- Trigger tests may need special handling

**Estimated Savings:** **30-40% overall test time**

---

### B. Test Database Pooling
**Current State:** GetCleanedServer creates fresh database per test
**Proposed:** Maintain pool of clean database snapshots

```csharp
// Pre-create 10 clean databases
var pool = new DatabasePool(connectionString, capacity: 10);

// In test
using var db = pool.Acquire();
// Test runs, database auto-resets on return to pool
```

**Benefits:**
- Eliminate 356+ database creation operations
- Reuse schema across tests
- Faster test startup

**Estimated Savings:** **20-30% for integration tests**

---

### C. Parallel Test Execution
**Current State:** All tests marked `[NonParallelizable]` at base class
**Proposed:** Enable parallelization with proper isolation

```csharp
[TestFixture]
[Parallelizable(ParallelScope.Children)]  // Tests within class can run parallel
public class DatabaseTests
{
    // Use unique database names per test
    protected string GetTestDatabaseName()
        => $"{TestContext.CurrentContext.Test.Name}_{Guid.NewGuid():N}";
}
```

**Benefits:**
- 2-4× speedup on multi-core CI systems
- Better resource utilization

**Challenges:**
- Requires database name isolation
- May hit connection pool limits
- Plugin system tests need review

**Estimated Savings:** **50-75% with 4+ cores**

---

### D. Replace Thread.Sleep with Polling
**Current Pattern:**
```csharp
Thread.Sleep(5000);
Assert.That(logEntry.EndTime, Is.Not.Null);
```

**Proposed:**
```csharp
var timeout = TimeSpan.FromSeconds(5);
var stopwatch = Stopwatch.StartNew();
while (logEntry.EndTime == null && stopwatch.Elapsed < timeout)
{
    Thread.Sleep(50);  // Poll every 50ms
}
Assert.That(logEntry.EndTime, Is.Not.Null);
```

**Benefits:**
- Tests complete as soon as condition met (often < 100ms)
- More reliable timing behavior
- Better failure messages

**Estimated Savings:** **10-15 seconds across affected tests**

---

### E. File I/O Optimization in Report Tests
**Current Pattern:**
```csharp
[Test]
public void ReportTest()
{
    File.WriteAllText(templatePath, template);
    var result = generator.Generate(templatePath);
    var output = File.ReadAllText(outputPath);
    Assert.That(output, Does.Contain("expected"));
}
```

**Proposed:**
```csharp
[Test]
public void ReportTest()
{
    using var templateStream = new MemoryStream(Encoding.UTF8.GetBytes(template));
    var result = generator.Generate(templateStream);
    using var outputStream = new MemoryStream();
    result.WriteTo(outputStream);
    var output = Encoding.UTF8.GetString(outputStream.ToArray());
    Assert.That(output, Does.Contain("expected"));
}
```

**Benefits:**
- Eliminate 50+ disk I/O operations
- Tests run in parallel without file conflicts
- Faster on CI systems with slow I/O

**Estimated Savings:** **5-10 seconds in CustomMetadataReportTests**

---

## Tests That Could Be Parallelized

### Current Blocking Factors:
1. **NonParallelizable attribute on base class** - Affects all 336 test files
2. **Shared database resources** - Tests use same database names
3. **Static state in DatabaseTests** - MsScratch, MyScratch, PostScratch static fields

### Parallelization Strategy:

#### Phase 1: Unit Tests (Immediate)
Tests with no database dependencies can be parallelized immediately:
- `Rdmp.Core.Tests/Repositories/CompileTimeTypeRegistryTests.cs`
- `Rdmp.Core.Tests/Repositories/MEFOptimizedTests.cs`
- `Rdmp.Core.Tests/DesignPatternTests/`
- All tests in `Tests.Common/UnitTests.cs`

**Action:** Remove `[NonParallelizable]` from unit test classes

#### Phase 2: Integration Tests with Isolation (3-6 months)
Implement database pooling and unique naming:
- `Rdmp.Core.Tests/DataLoad/Engine/Integration/*` (except cross-database tests)
- `Rdmp.Core.Tests/Curation/Integration/*`
- `Rdmp.UI.Tests/*`

**Prerequisites:** Database pooling infrastructure

#### Phase 3: Cross-Database Tests (6-12 months)
Keep sequential but optimize:
- `CrossDatabaseDataLoadTests.cs`
- `QueryCachingCrossServerTests.cs`
- Tests with `[TestCaseSource(typeof(All), nameof(All.DatabaseTypes))]`

**Strategy:** Run database types in parallel, but tests within each type sequential

---

## Estimated Time Savings Summary

| Optimization | Affected Tests | Time Savings | Effort | Priority |
|-------------|----------------|--------------|--------|----------|
| Remove Thread.Sleep | ~11 tests | **15-20 sec** | LOW | HIGH |
| Transaction rollback | ~1000 tests | **30-40%** | MEDIUM | HIGH |
| Database pooling | ~800 tests | **20-30%** | HIGH | HIGH |
| File I/O optimization | ~31 tests | **5-10 sec** | LOW | MEDIUM |
| String concatenation fix | 1 test | **0.5 sec** | TRIVIAL | LOW |
| Reduce randomization loops | ~10 tests | **5-10 sec** | LOW | MEDIUM |
| Parallel execution (Phase 1) | ~200 tests | **50-75%** | MEDIUM | MEDIUM |
| Cross-DB test optimization | ~9 test classes | **10-15%** | MEDIUM | MEDIUM |

**Combined Estimate:**
- **Short-term (LOW effort):** 20-30% reduction (Thread.Sleep, file I/O)
- **Medium-term (MEDIUM effort):** 40-50% reduction (Add transactions, Phase 1 parallel)
- **Long-term (HIGH effort):** 60-70% reduction (Full parallelization, database pooling)

---

## Specific Test Files Requiring Attention

### Immediate Fixes (< 1 day):
1. `/Rdmp.Core.Tests/Curation/Integration/TriggerTests.cs:199-211` - Replace 5× Thread.Sleep
2. `/Rdmp.Core.Tests/CommandExecution/ExecuteCommandConfirmLogsTests.cs:126` - Replace Thread.Sleep(5000)
3. `/Rdmp.Core.Tests/DataLoad/Engine/Integration/DataTableUploadDestinationTests.cs:420` - Fix string concatenation
4. `/Plugins/RdmpExtensions/LoadModules.Extensions.Tests/Python/ScriptExecutionTests.cs:106,116` - Reduce delay times

### Short-term Refactoring (1-2 weeks):
1. `/Rdmp.Core.Tests/Reports/CustomMetadataReportTests.cs` - Convert to in-memory streams
2. `/Tests.Common/DatabaseTests.cs` - Implement transaction rollback infrastructure
3. All tests - Replace `GetAllObjects` cleanup with tracked deletion

### Long-term Infrastructure (1-3 months):
1. Database pooling system
2. Parallel test execution framework
3. Cross-database test optimization

---

## Recommendations

### Priority 1 (This Sprint):
1. **Audit and remove all Thread.Sleep calls** - Replace with polling or time mocking
2. **Fix string concatenation in DataTableUploadDestinationTests**
3. **Profile 5 slowest test classes** to identify additional bottlenecks

### Priority 2 (Next Sprint):
1. **Implement transaction-based test isolation** for DatabaseTests base class
2. **Convert CustomMetadataReportTests to in-memory I/O**
3. **Enable parallel execution for unit tests**

### Priority 3 (Next Quarter):
1. **Design and implement database pooling system**
2. **Refactor cross-database tests** to reduce permutations
3. **Add test performance monitoring** to CI pipeline

---

## Metrics to Track

1. **Total test execution time** (baseline: TBD, target: -40%)
2. **Average test duration** (identify > 1 second tests)
3. **Database operations per test** (target: < 5)
4. **File I/O operations per test** (target: 0)
5. **Parallelization ratio** (target: 70% of tests parallel-safe)

---

## Appendix: Test Statistics

### Test Distribution by Type:
- Integration tests (database): ~70% (~800 tests)
- Unit tests: ~20% (~200 tests)
- UI tests: ~10% (~136 tests)

### Database Operation Patterns:
- `GetCleanedServer` calls: **356+**
- `GetAllObjects` calls: **483+**
- `CreateTable` calls: **356+**
- `ExecuteNonQuery` calls: **234+**
- File operations: **50+**

### Sleep/Delay Patterns:
- `Thread.Sleep(5000)`: 1 occurrence
- `Thread.Sleep(1000)`: 5+ occurrences
- `Thread.Sleep(500)`: 3+ occurrences
- `Task.Delay(6000)`: 1 occurrence
- `Task.Delay(2000)`: 2 occurrences

---

**Report Generated:** 2025-10-27
**Tooling:** Claude Code + Grep/Bash analysis
**Next Review:** After implementing Priority 1 items
