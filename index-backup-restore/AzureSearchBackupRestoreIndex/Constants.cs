using System;
using System.Collections.Generic;
using System.Text;

namespace AzureSearchBackupRestoreIndex
{
  public static class Constants
  {
    private const int MiB = 1000000; //1048576;
    public const int MaxRequestSize = 59 * MiB;
    public const string ValuePropertyName = "value";
  }
}
