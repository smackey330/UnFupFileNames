using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnfuckUpFileNames.UiInfrastructure;

namespace UnfuckUpFileNames
{
    public class FileItem : NotifyObject
    {
        private bool _selected = true;

        public FileItem()
        {
        }

        public bool Selected
        {
            get { return this._selected; }
            set
            {
                this._selected = value;
                this.RaisePropertyChanged("Selected");
            }
        }
        public string OldFileName { get; set; }
        public string NewFileName { get; set; }
    }
}
