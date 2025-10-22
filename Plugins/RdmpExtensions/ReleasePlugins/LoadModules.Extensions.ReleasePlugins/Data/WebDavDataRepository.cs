using System;
using System.Data.Common;
using CatalogueLibrary.Repositories.Construction;
using MapsDirectlyToDatabaseTable;

namespace LoadModules.Extensions.ReleasePlugins.Data
{
    public class WebDavDataRepository : TableRepository
    {
        readonly ObjectConstructor _constructor = new ObjectConstructor();

        public WebDavDataRepository(DbConnectionStringBuilder connectionStringBuilder) : base(null, connectionStringBuilder)
        {
            
        }

        protected override IMapsDirectlyToDatabaseTable ConstructEntity(Type t, DbDataReader reader)
        {
            return _constructor.ConstructIMapsDirectlyToDatabaseObject(t, this, reader);
        }
    }
}