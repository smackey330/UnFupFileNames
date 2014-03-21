using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Forms;
using System.Windows.Input;
using UnfuckUpFileNames.UiInfrastructure;

namespace UnfuckUpFileNames
{
    public class MainWindowModel : NotifyObject
    {
        private string _logFileName = "log-" + DateTime.Now.ToString("yyyy.MM.dd-hh.mm.ss") + ".log";
        private CancellationTokenSource _cancellationToken;
        private MainWindow view;
        private CommandBase _FindPathCommand;
        private CommandBase _FindBadFilesCommand;
        private CommandBase _ExecuteFileRenameCommand;
        private CommandBase _CancelActionCommand;
        private ListCollectionView _foundItems;
        private string _filePath;
        private string _pattern;
        private bool _recurseSubFolders = true;
        private bool _showWaiting;
        private bool _allChecked = true;
        private bool _allReadyRenamed = false;

        public MainWindowModel(MainWindow v)
        {
            this.view = v;

            _FindPathCommand = new CommandBase(FindPath);
            _FindBadFilesCommand = new CommandBase(this.FindBadFiles, this.FindBadFilesCanExecute);
            _ExecuteFileRenameCommand = new CommandBase(this.ExecuteFileRename, this.ExecuteFileRenameCanExecute);
            _CancelActionCommand = new CommandBase(this.CancelAction, this.CancelActionCanExecute);
            this.Pattern = ConfigurationManager.AppSettings["DefaultPattern"];
       }

        public CommandBase FindPathCommand { get { return this._FindPathCommand; } }
        public CommandBase FindBadFilesCommand { get { return this._FindBadFilesCommand; } }
        public CommandBase ExecuteFileRenameCommand { get { return this._ExecuteFileRenameCommand; } }
        public CommandBase CancelActionCommand { get { return this._CancelActionCommand; } }

        public bool AllChecked
        {
            get { return this._allChecked; }
            set
            {
                this._allChecked = value;

                foreach (var item in this.FoundItems.OfType<FileItem>())
                {
                    item.Selected = value;
                }

                this.RaisePropertyChanged("FoundItems");
                this.RaisePropertyChanged("AllChecked");
            }
        }

        public bool ShowWaiting
        {
            get { return this._showWaiting; }
            set
            {
                this._showWaiting = value;
                this.RaisePropertyChanged("ShowWaiting");
                this.CancelActionCommand.RaiseCanExecute();
            }
        }

        public bool RecurseSubFolders
        {
            get { return this._recurseSubFolders; }
            set
            {
                this._recurseSubFolders = value;
                this.RaisePropertyChanged("RecurseSubFolders");
            }
        }

        public string FilePath
        {
            get { return this._filePath; }
            set
            {
                this._filePath = value;
                this.RaisePropertyChanged("FilePath");
                this.ExecuteFileRenameCommand.RaiseCanExecute();
                this.FindBadFilesCommand.RaiseCanExecute();
            }
        }
        public string Pattern
        {
            get { return this._pattern; }
            set
            {
                this._pattern = value;
                this.RaisePropertyChanged("Pattern");
                this.ExecuteFileRenameCommand.RaiseCanExecute();
                this.FindBadFilesCommand.RaiseCanExecute();
            }
        }
        public ListCollectionView FoundItems 
        {
            get { return this._foundItems; }
            set
            {
                this._foundItems = value;
                this.RaisePropertyChanged("FoundItems");
            }
        }

        public void FindPath()
        {
            FolderBrowserDialog dialog = new FolderBrowserDialog();
            DialogResult result = dialog.ShowDialog();

            if (result == DialogResult.OK)
            {
                this.FilePath = dialog.SelectedPath;
            }

            this._FindBadFilesCommand.RaiseCanExecute();
        }

        private List<FileItem> _tempList;
        private Regex _patRegEx;

        public void CancelAction()
        {
            this._cancellationToken.Cancel();
            //this._cancellationToken.Token.CanBeCanceled
        }
        public bool CancelActionCanExecute()
        {
            return this._cancellationToken != null;
        }

        public void FindBadFiles()
        {
            _patRegEx = new Regex(this.Pattern, RegexOptions.IgnoreCase);
            this.ExecuteFileRenameCommand.RaiseCanExecute();
            this.FindBadFilesCommand.RaiseCanExecute();
            _cancellationToken = new CancellationTokenSource();
            this.ShowWaiting = true;

            Task findFiles = new Task(() =>
            {
                _tempList = new List<FileItem>();
                DirectoryInfo di = new DirectoryInfo(this.FilePath);
                FindFiles(di);
            }, _cancellationToken.Token);


            Task filesFound = findFiles.ContinueWith((parm) => {

                ListCollectionView col = new ListCollectionView(this._tempList);
                col.GroupDescriptions.Add(new PropertyGroupDescription("FolderPath"));

                this._allReadyRenamed = false;
                this.ExecuteFileRenameCommand.RaiseCanExecute();
                this.FoundItems = col;
                this.ShowWaiting = false;
                _cancellationToken = null;

            }, TaskScheduler.FromCurrentSynchronizationContext());


            findFiles.Start();
        }

        public void FindFiles(DirectoryInfo currentDirectory)
        {
            if (this._cancellationToken != null && this._cancellationToken.IsCancellationRequested)
            {
                this._cancellationToken.Token.ThrowIfCancellationRequested();
            }

            foreach (FileInfo fi in currentDirectory.GetFiles().ToList())
            {
                if (_patRegEx.IsMatch(fi.Name))
                {
                    FileItem item = new FileItem();
                    item.OldFullPath = fi.FullName;
                    item.NewFullPath = Path.Combine(currentDirectory.FullName, _patRegEx.Replace(fi.Name, string.Empty));

                    item.OldFileName = fi.Name;
                    item.NewFileName = _patRegEx.Replace(fi.Name, string.Empty);
                  
                    item.FolderPath = currentDirectory.FullName;

                    this._tempList.Add(item);
                }
            }

            if (RecurseSubFolders)
            {
                foreach (DirectoryInfo di in currentDirectory.GetDirectories())
                {
                    FindFiles(di);
                }
            }
        }

        public bool FindBadFilesCanExecute()
        {
            bool cool = false;

            if (!string.IsNullOrWhiteSpace(this.FilePath) && Directory.Exists(this.FilePath))
            {
                if (!string.IsNullOrWhiteSpace(this.Pattern))
                {
                    cool = true;
                }
            }

            return cool;
        }

        public void ExecuteFileRename()
        {
            this.ShowWaiting = true;
            var checkedItems = (from item in this.FoundItems.SourceCollection.OfType<FileItem>()
                                where item.Selected
                                select item);

            ParallelOptions options = new ParallelOptions();
            options.MaxDegreeOfParallelism = 2;

            Parallel.ForEach(checkedItems, options, (FileItem item) =>
            {
                try
                {
                    File.Move(item.OldFullPath, item.NewFullPath);
                }
                catch (Exception ex)
                {
                    WriteLog("Error", ex);
                }
            });

            this.ShowWaiting = false;
            _allReadyRenamed = true;

            this.FindBadFilesCommand.RaiseCanExecute();
            this.ExecuteFileRenameCommand.RaiseCanExecute();
        }

        public bool ExecuteFileRenameCanExecute()
        {
            bool cool = false;

            if (!string.IsNullOrWhiteSpace(this.FilePath) && Directory.Exists(this.FilePath) && !this._allReadyRenamed)
            {
                if (!string.IsNullOrWhiteSpace(this.Pattern))
                {
                    cool = true;
                }
            }

            return cool;
        }

        private void WriteLog(string message)
        {
            using (StreamWriter sw = new StreamWriter(_logFileName, true))
            {
                sw.WriteLineAsync(message);
            }
        }
        private void WriteLog(string message, Exception ex)
        {
            WriteLog(message + " :: " + ex.ToString());
        }
    }
}
