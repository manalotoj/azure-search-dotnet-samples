using System;
using System.Collections.Generic;
using System.Text;

namespace AzureSearchBackupRestoreIndex
{
  public partial class ImportExportSettings
  {
    public bool BackupRequired { get; set; }
    public bool DeleteAndCreatedIndex { get; set; }
    public string SourceSearchServiceName { get; set; }
    public string SourceAdminKey { get; set; }
    public string SourceIndexName { get; set; }
    public string TargetSearchServiceName { get; set; }
    public string TargetAdminKey { get; set; }
    public string TargetIndexName { get; set; }
    public string BackupDirectory { get; set; }
  }
}

