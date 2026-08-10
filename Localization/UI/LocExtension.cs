using Avalonia.Markup.Xaml;
using MinePackEditor.Assets.Localization.Settings;
using System;
using System.Collections.Generic;
using System.Text;

namespace MinePackEditor.Localization.UI
{
    internal class LocExtension : MarkupExtension
    {
        public string Key { get; set; } = string.Empty;

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return UILanguage.Get(Key);
        }
    }
}
