using System.Collections.Generic;
using Rdmp.Core.DataLoad.Modules.DataProvider;
using Rdmp.Core.ReusableLibraryCode.Progress;

namespace SCIStorePlugin.Repositories;

public abstract class WsRepository<T> : IRepository<T>
{
    protected readonly WebServiceConfiguration WsConfig;

    public abstract IEnumerable<T> ReadAll();
    public abstract void Create(IEnumerable<T> reports, IDataLoadEventListener listener);

    public abstract event AfterReadAllHandler AfterReadAll;
    public abstract event AfterReadSingleHandler AfterReadSingle;
    public abstract event WsNotifyHandler Notify;

    public abstract void CheckWebServiceConnection();

    protected WsRepository(WebServiceConfiguration wsConfig)
    {
        WsConfig = wsConfig;
    }
}