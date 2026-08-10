using Avalonia.Markup.Xaml;
using System;
using System.Collections.Generic;
using System.Text;

namespace MinePackEditor.Assets.Localization.Settings
{
    public class ConfigLocExtension : MarkupExtension
    {
        public string Id { get; set; } = string.Empty;

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return SettingsLang.Get(Id);
        }
    }
}
