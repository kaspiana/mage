using System.Data;
using Microsoft.Data.Sqlite;

namespace Mage.Engine;

public struct Archive {

    public string mageDir;
    public string fileDir;
    public SqliteConnection db;

}