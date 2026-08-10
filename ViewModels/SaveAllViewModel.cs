using MinePackEditor.Models;
using MinePackEditor.Service;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace MinePackEditor.ViewModels
{
    public class SaveAllViewModel : DialogViewModelBase<SaveAllResult>
    {
        public ObservableCollection<SaveFileItem> Files { get; } = new();

        public SaveAllViewModel(IEnumerable<FileDocument> files)
        {
            foreach (var f in files)
                Files.Add(new SaveFileItem(f));
        }

        public SaveAllViewModel()
        {
            // 设计时假数据
            Files.Add(new SaveFileItem(new FileDocument { FileName = "First.txt" }));
            Files.Add(new SaveFileItem(new FileDocument { FileName = "Second.txt" }));
        }

        public void ConfirmSave()
        {
            Result = new SaveAllResult
            {
                Result = DialogResult.OK,
                FilesToSave = Files.Where(f => f.IsIncluded).Select(f => f.Document).ToList()
            };
            Close();
        }

        public void ConfirmDontSave()
        {
            Result = new SaveAllResult
            {
                Result = DialogResult.No,
                FilesToSave = new List<FileDocument>()
            };
            Close();
        }

        public void ConfirmCancel()
        {
            Result = new SaveAllResult
            {
                Result = DialogResult.Cancel,
                FilesToSave = new List<FileDocument>()
            };
            Close();
        }
    }
}
