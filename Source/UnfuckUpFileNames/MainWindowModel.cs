using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Input;
using UnfuckUpFileNames.UiInfrastructure;

namespace UnfuckUpFileNames
{
    public class MainWindowModel : NotifyObject
    {
        private MainWindow view;
        private CommandBase _FindPathCommand;
        private CommandBase _FindBadFilesCommand;
        private CommandBase _ExecuteFileRenameCommand;
        private List<FileItem> _foundItems;
        private string _filePath;
        private string _pattern;

        public MainWindowModel(MainWindow v)
        {
            this.view = v;

            _FindPathCommand = new CommandBase(FindPath);
            _FindBadFilesCommand = new CommandBase(this.FindBadFiles, this.FindBadFilesCanExecute);
            _ExecuteFileRenameCommand = new CommandBase(this.ExecuteFileRename, this.ExecuteFileRenameCanExecute);
            this.Pattern = ConfigurationManager.AppSettings["DefaultPattern"];
       }

        public CommandBase FindPathCommand { get { return this._FindPathCommand; } }
        public CommandBase FindBadFilesCommand { get { return this._FindBadFilesCommand; } }
        public CommandBase ExecuteFileRenameCommand { get { return this._ExecuteFileRenameCommand; } }

        public string FilePath 
        {
            get { return this._filePath; }
            set
            {
                this._filePath = value;
                this.RaisePropertyChanged("FilePath");
            }
        }
        public string Pattern
        {
            get { return this._pattern; }
            set
            {
                this._pattern = value;
                this.RaisePropertyChanged("Pattern");
            }
        }
        public List<FileItem> FoundItems 
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

        public void FindBadFiles()
        {
            _patRegEx = new Regex(this.Pattern, RegexOptions.IgnoreCase);

            Task findFiles = new Task(() =>
            {
                _tempList = new List<FileItem>();
                DirectoryInfo di = new DirectoryInfo(this.FilePath);
                FindFiles(di);
            });

            Task filesFound = findFiles.ContinueWith((parm) => {
                this.FoundItems = this._tempList;
            }, TaskScheduler.FromCurrentSynchronizationContext());

            findFiles.Start();
        }

        public void FindFiles(DirectoryInfo currentDirectory)
        {
            foreach (FileInfo fi in currentDirectory.GetFiles().ToList())
            {
                if (_patRegEx.IsMatch(fi.FullName)){
                    FileItem item = new FileItem();
                    item.OldFileName = fi.FullName;
                    item.NewFileName = _patRegEx.Replace(fi.FullName, string.Empty);
                    this._tempList.Add(item);
                }
            }

            foreach (DirectoryInfo di in currentDirectory.GetDirectories())
            {
                FindFiles(di);
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

        }
        public bool ExecuteFileRenameCanExecute()
        {
            return false;
        }
    }
}
