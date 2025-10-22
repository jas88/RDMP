using System;
using System.Collections.Generic;
using Rdmp.Core.DataFlowPipeline;
using Rdmp.Core.ReusableLibraryCode.Progress;
using SCIStorePlugin.Data;

namespace SCIStorePlugin.Repositories;

public interface IRepositorySupportsDateRangeQueries<T> : IRepository<T>
{
    IEnumerable<CombinedReportData> ReadForInterval(DateTime day, TimeSpan timeSpan, IDataLoadEventListener listener, GracefulCancellationToken token);
}