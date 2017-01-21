using EasyMySql.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EasyMySql.Core
{
    internal sealed class UniqueKeyHandler : DataHandler<UniqueKey>
    {
        public static readonly UniqueKeyHandler Instance = new UniqueKeyHandler();

        private UniqueKeyHandler()
        {
            tableName = Constants.InternalTablePrefix + "UniqueKeys";
        }
    }

    internal sealed class UniqueKey : DataObject
    {
        public string IndexName { get; set; }
        public string TableName { get; set; }
        public string ColumnName { get; set; }
    }
}
