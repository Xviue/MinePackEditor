using MinePackEditor.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace MinePackEditor.ViewModels
{
    public class SaveFileItem : ViewModelBase
    {
        public FileDocument Document { get; }

        private bool _isIncluded = true;
        public bool IsIncluded
        {
            get => _isIncluded;
            set { _isIncluded = value; OnPropertyChanged(); }
        }

        public SaveFileItem(FileDocument document) => Document = document;
    }
}
