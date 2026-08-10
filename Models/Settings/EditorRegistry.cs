using MinePackEditor.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MinePackEditor.Models.Settings
{
    public sealed class EditorRegistry
    {
        private static readonly Lazy<EditorRegistry> _lazy = new(() => new());
        public static EditorRegistry Instance => _lazy.Value;

        public IReadOnlyList<EditorDefinition> AvailableEditors { get; } = new List<EditorDefinition>
    {
        new("text"),
        new("image"),
        new("unsupported")
    };

        private static readonly Dictionary<string, string> _builtinDefaults = new(StringComparer.OrdinalIgnoreCase)
        {
            [".mcf"] = "text",
            [".txt"] = "text",
            [".cs"] = "text",
            [".xaml"] = "text",
            [".axaml"] = "text",
            [".json"] = "text",
            [".xml"] = "text",
            [".md"] = "text",
            [".png"] = "image",
            [".jpg"] = "image",
            [".jpeg"] = "image",
            [".bmp"] = "image",
            [".webp"] = "image",
            [".ico"] = "image",
            [".gif"] = "image"
        };

        public string ResolveEditor(string? filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath)) return "unsupported";

            var ext = System.IO.Path.GetExtension(filePath).ToLowerInvariant();
            if (string.IsNullOrEmpty(ext)) return "unsupported";

            var userAssoc = SettingsService.Instance.Settings.FileAssociations
                .FirstOrDefault(a => a.Extension.Equals(ext, StringComparison.OrdinalIgnoreCase));
            if (userAssoc != null && AvailableEditors.Any(e => e.Id == userAssoc.EditorId))
                return userAssoc.EditorId;

            if (_builtinDefaults.TryGetValue(ext, out var builtIn))
                return builtIn;

            return "unsupported";
        }

        public string? GetBoundEditor(string extension)
        {
            var ext = extension.ToLowerInvariant();
            if (!ext.StartsWith('.')) ext = "." + ext;

            var user = SettingsService.Instance.Settings.FileAssociations
                .FirstOrDefault(a => a.Extension == ext);
            return user?.EditorId;
        }

        public void SetAssociation(string extension, string editorId)
        {
            var ext = extension.ToLowerInvariant();
            if (!ext.StartsWith('.')) ext = "." + ext;

            var list = SettingsService.Instance.Settings.FileAssociations;
            var existing = list.FirstOrDefault(a => a.Extension == ext);

            if (existing != null)
            {
                existing.EditorId = editorId;
            }
            else
            {
                list.Add(new FileAssociation { Extension = ext, EditorId = editorId });
            }
        }

        public void RemoveAssociation(string extension)
        {
            var ext = extension.ToLowerInvariant();
            if (!ext.StartsWith('.')) ext = "." + ext;

            var list = SettingsService.Instance.Settings.FileAssociations;
            var existing = list.FirstOrDefault(a => a.Extension == ext);
            if (existing != null) list.Remove(existing);
        }
    }
}
