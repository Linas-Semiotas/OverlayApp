// ===============================
// TodoModule (UI Logic)
// ===============================

namespace OverlayApp.Modules.Todo
{
    // -------------------------------
    // DEPENDENCIES
    // -------------------------------
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.Json;
    using System.Text.Json.Nodes;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Input;
    using System.Windows.Media;
    using OverlayApp.SDK;
    using IO = System.IO;

    // -------------------------------
    // MODULE
    // -------------------------------
    public partial class TodoModule : System.Windows.Controls.UserControl
    {
        // -------------------------------
        // MODELS
        // -------------------------------
        private sealed class TodoEntry
        {
            public string Text { get; set; } = "";
            public bool Done { get; set; }
        }

        private sealed class ListRef
        {
            public int Id { get; set; }
            public string Name { get; set; } = "";
            public string File { get; set; } = "";
        }

        private sealed class IndexFile
        {
            public List<ListRef> Lists { get; set; } = new();
            public int ActiveListId { get; set; }
        }

        // -------------------------------
        // STATE
        // -------------------------------
        private readonly List<TodoEntry> _items = new();
        private IndexFile _index = new();

        // settings (not stored in index.json)
        private bool _showCompleted = true;
        private bool _doneAtBottom = true;
        private bool _wrapLongItems = true;
        private double _itemFontSize = 16;
        private double _backgroundOpacity = 0.85;

        // paths
        private string _filesDir = "";
        private string _indexPath = "";

        // -------------------------------
        // CTOR
        // -------------------------------
        public TodoModule()
        {
            InitializeComponent();
        }

        // -------------------------------
        // HOST ENTRY
        // -------------------------------
        public void ApplySettings(JsonObject settings)
        {
            _showCompleted = settings["showCompleted"]?.GetValue<bool>() ?? true;
            _doneAtBottom = settings["doneAtBottom"]?.GetValue<bool>() ?? true;
            _wrapLongItems = settings["wrapLongItems"]?.GetValue<bool>() ?? true;
            _itemFontSize = settings["itemFontSize"]?.GetValue<double>() ?? 16;
            _backgroundOpacity = settings["backgroundOpacity"]?.GetValue<double>() ?? 0.85;

            EnsureFilesLayout();
            LoadOrSeedIndex();
            EnsureActiveListValid();
            LoadActiveListIntoMemory();
            RenderListPicker();
            RenderItems();

            Bg.Opacity = Math.Max(0.3, Math.Min(1.0, _backgroundOpacity));

            // start expanded
            ToggleViewBtn.IsChecked = false;
            ToggleViewBtn.Content = "Collapse";
        }

        // -------------------------------
        // FS / INDEX
        // -------------------------------
        private void EnsureFilesLayout()
        {
            var dataDir = IO.Path.Combine("Data", "Modules", "Todo");
            _filesDir = IO.Path.Combine(dataDir, "Files");
            _indexPath = IO.Path.Combine(_filesDir, "index.json");
            IO.Directory.CreateDirectory(_filesDir);
        }

        private void LoadOrSeedIndex()
        {
            if (IO.File.Exists(_indexPath))
            {
                try
                {
                    _index = JsonSerializer.Deserialize<IndexFile>(IO.File.ReadAllText(_indexPath)) ?? new IndexFile();

                    if (_index.Lists.Count == 0 ||
                        _index.Lists.Any(l => l.Id == 0 || string.IsNullOrWhiteSpace(l.Name) || string.IsNullOrWhiteSpace(l.File)))
                    {
                        SeedStarterIndex();
                        return;
                    }

                    if (!_index.Lists.Any(l => l.Id == _index.ActiveListId))
                        _index.ActiveListId = _index.Lists[0].Id;

                    SaveIndex();
                    return;
                }
                catch
                {
                    // fallthrough to seed
                }
            }

            SeedStarterIndex();
        }

        private void SeedStarterIndex()
        {
            _index = new IndexFile();
            var id = NewId();
            var name = "Starting Todo";
            var file = $"{id}_{Sanitize(name)}.json";

            _index.Lists.Add(new ListRef { Id = id, Name = name, File = file });
            _index.ActiveListId = id;

            var starter = new JsonObject
            {
                ["items"] = new JsonArray {
                    new JsonObject { ["text"] = "Create new Todo", ["done"] = false },
                    new JsonObject { ["text"] = "Add tasks to new Todo", ["done"] = false },
                    new JsonObject { ["text"] = "Delete starter Todo (optional)", ["done"] = false }
                }
            };

            IO.File.WriteAllText(
                IO.Path.Combine(_filesDir, file),
                starter.ToJsonString(new JsonSerializerOptions { WriteIndented = true })
            );

            SaveIndex();
        }

        private void SaveIndex()
        {
            IO.File.WriteAllText(_indexPath, JsonSerializer.Serialize(_index, new JsonSerializerOptions { WriteIndented = true }));
        }

        private bool EnsureActiveListValid()
        {
            if (_index.Lists == null || _index.Lists.Count == 0)
            {
                var id = NewId();
                var name = "List";
                var file = $"{id}_{Sanitize(name)}.json";
                IO.File.WriteAllText(IO.Path.Combine(_filesDir, file), """{ "items": [] }""");
                _index.Lists = new List<ListRef> { new ListRef { Id = id, Name = name, File = file } };
                _index.ActiveListId = id;
                SaveIndex();
                return true;
            }

            if (!_index.Lists.Any(l => l.Id == _index.ActiveListId))
            {
                _index.ActiveListId = _index.Lists[0].Id;
                SaveIndex();
                return true;
            }

            return false;
        }

        // -------------------------------
        // LIST IO
        // -------------------------------
        private void LoadActiveListIntoMemory()
        {
            _items.Clear();

            var cur = _index.Lists.FirstOrDefault(l => l.Id == _index.ActiveListId);
            if (cur == null)
            {
                EnsureActiveListValid();
                cur = _index.Lists.FirstOrDefault(l => l.Id == _index.ActiveListId);
            }
            if (cur == null) return;

            var path = IO.Path.Combine(_filesDir, cur.File);

            try
            {
                if (!IO.File.Exists(path)) IO.File.WriteAllText(path, """{ "items": [] }""");

                var node = JsonNode.Parse(IO.File.ReadAllText(path))!.AsObject();
                var arr = (node["items"] as JsonArray)?.OfType<JsonObject>() ?? Enumerable.Empty<JsonObject>();

                foreach (var o in arr)
                    _items.Add(new TodoEntry
                    {
                        Text = o["text"]?.GetValue<string>() ?? "",
                        Done = o["done"]?.GetValue<bool>() ?? false
                    });
            }
            catch
            {
                IO.File.WriteAllText(path, """{ "items": [] }""");
            }
        }

        private void SaveActiveListToFile()
        {
            var cur = _index.Lists.FirstOrDefault(l => l.Id == _index.ActiveListId);
            if (cur == null) return;

            var path = IO.Path.Combine(_filesDir, cur.File);
            var payload = new JsonObject
            {
                ["items"] = new JsonArray(_items.Select(it => new JsonObject { ["text"] = it.Text, ["done"] = it.Done }).ToArray())
            };

            try
            {
                IO.File.WriteAllText(path, payload.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
            }
            catch
            {
                var newName = $"{cur.Id}_{Sanitize(cur.Name)}_{DateTime.UtcNow.Ticks}.json";
                IO.File.WriteAllText(
                    IO.Path.Combine(_filesDir, newName),
                    payload.ToJsonString(new JsonSerializerOptions { WriteIndented = true })
                );
                AppendDeleteList(path);
                cur.File = newName;
                SaveIndex();
            }
        }

        // -------------------------------
        // TOP BAR
        // -------------------------------
        private void ToggleViewBtn_Click(object sender, RoutedEventArgs e)
        {
            bool compact = ToggleViewBtn.IsChecked == true;
            ActionsRow.Visibility = compact ? Visibility.Collapsed : Visibility.Visible;
            ComposerRow.Visibility = compact ? Visibility.Collapsed : Visibility.Visible;
            ToggleViewBtn.Content = compact ? "Expand" : "Collapse";
        }

        // -------------------------------
        // EVENTS
        // -------------------------------
        private void ListPicker_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var id = (ListPicker.SelectedItem as ComboBoxItem)?.Tag as int?;
            if (id == null) return;

            if (_index.Lists.Any(l => l.Id == id.Value))
            {
                _index.ActiveListId = id.Value;
                SaveIndex();
                LoadActiveListIntoMemory();
                RenderItems();
            }
        }

        private void NewListBtn_Click(object s, RoutedEventArgs e)
        {
            var input = Ui.Current?.Prompt("New list name", defaultText: "New List", ok: "Create", cancel: "Cancel");
            if (string.IsNullOrWhiteSpace(input)) return;

            var name = UniqueListName(input.Trim());
            var id = NewId();
            var file = $"{id}_{Sanitize(name)}.json";

            IO.File.WriteAllText(IO.Path.Combine(_filesDir, file), """{ "items": [] }""");

            _index.Lists.Add(new ListRef { Id = id, Name = name, File = file });
            _index.ActiveListId = id;
            SaveIndex();

            LoadActiveListIntoMemory();
            RenderListPicker();
            RenderItems();
        }

        private void RenameListBtn_Click(object s, RoutedEventArgs e)
        {
            var cur = _index.Lists.FirstOrDefault(l => l.Id == _index.ActiveListId);
            if (cur == null) return;

            var input = Ui.Current?.Prompt("Rename list", defaultText: cur.Name, ok: "Save", cancel: "Cancel");
            if (string.IsNullOrWhiteSpace(input)) return;

            cur.Name = UniqueListName(input.Trim());
            SaveIndex();
            RenderListPicker();
        }

        private void DeleteListBtn_Click(object s, RoutedEventArgs e)
        {
            if (_index.Lists.Count <= 1)
            {
                Ui.Current?.Info("You need at least one list.");
                return;
            }

            var cur = _index.Lists.FirstOrDefault(l => l.Id == _index.ActiveListId);
            if (cur == null) return;

            if (Ui.Current?.Confirm($"Delete list '{cur.Name}'?\nItems will be removed.", "Confirm", "Delete", "Cancel") != true)
                return;

            var full = IO.Path.Combine(_filesDir, cur.File);
            try
            {
                if (IO.File.Exists(full)) IO.File.Delete(full);
            }
            catch
            {
                AppendDeleteList(full);
            }

            _index.Lists.Remove(cur);
            EnsureActiveListValid();
            SaveIndex();

            LoadActiveListIntoMemory();
            RenderListPicker();
            RenderItems();
        }

        private void AddItemBtn_Click(object s, RoutedEventArgs e) => AddItemFromBox();

        private void NewItemBox_KeyDown(object s, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) AddItemFromBox();
        }

        // -------------------------------
        // RENDERING
        // -------------------------------
        private void RenderListPicker()
        {
            ListPicker.Items.Clear();

            foreach (var l in _index.Lists)
            {
                var item = new ComboBoxItem { Content = l.Name, Tag = l.Id };
                ListPicker.Items.Add(item);
                if (l.Id == _index.ActiveListId) ListPicker.SelectedItem = item;
            }

            if (ListPicker.SelectedItem == null && ListPicker.Items.Count > 0)
                ListPicker.SelectedIndex = 0;
        }

        private void RenderItems()
        {
            ItemsHost.Children.Clear();

            IEnumerable<TodoEntry> src = _items;
            if (!_showCompleted) src = src.Where(i => !i.Done);
            if (_doneAtBottom) src = src.OrderBy(i => i.Done ? 1 : 0);

            foreach (var e in src)
                ItemsHost.Children.Add(BuildRow(e));
        }

        private FrameworkElement BuildRow(TodoEntry entry)
        {
            var grid = new Grid { Margin = new Thickness(0, 0, 0, 6) };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });                       // checkbox
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });  // text
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });                       // delete

            var cb = new CheckBox
            {
                IsChecked = entry.Done,
                Style = (Style)FindResource("CustomCheckBox"),
                Margin = new Thickness(0, 0, 6, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            cb.Checked += (_, __) => { entry.Done = true; SaveActiveListToFile(); RenderItems(); };
            cb.Unchecked += (_, __) => { entry.Done = false; SaveActiveListToFile(); RenderItems(); };
            Grid.SetColumn(cb, 0);

            var txt = new TextBlock
            {
                Text = entry.Text,
                FontSize = _itemFontSize,
                TextWrapping = _wrapLongItems ? TextWrapping.Wrap : TextWrapping.NoWrap,
                TextTrimming = _wrapLongItems ? TextTrimming.None : TextTrimming.CharacterEllipsis,
                VerticalAlignment = VerticalAlignment.Center
            };

            txt.SetResourceReference(TextBlock.ForegroundProperty, "B_Text");

            if (entry.Done)
            {
                txt.Opacity = 0.55;
                txt.TextDecorations = TextDecorations.Strikethrough;
            }
            Grid.SetColumn(txt, 1);

            var del = new Button
            {
                Content = "✕",
                Width = 28,
                Height = 28,
                Style = (Style)FindResource("HoverButtonStyle"),
                Margin = new Thickness(6, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            del.Click += (_, __) => { _items.Remove(entry); SaveActiveListToFile(); RenderItems(); };
            Grid.SetColumn(del, 2);

            grid.Children.Add(cb);
            grid.Children.Add(txt);
            grid.Children.Add(del);
            return grid;
        }

        // -------------------------------
        // HELPERS
        // -------------------------------
        private void AddItemFromBox()
        {
            var text = NewItemBox.Text?.Trim();
            if (string.IsNullOrEmpty(text)) return;

            _items.Add(new TodoEntry { Text = text, Done = false });
            NewItemBox.Text = "";
            SaveActiveListToFile();
            RenderItems();
        }

        private int NewId()
        {
            var r = new Random();
            int id;
            var used = _index.Lists.Select(l => l.Id).ToHashSet();
            do id = r.Next(100000, 999999); while (used.Contains(id));
            return id;
        }

        private static string Sanitize(string name)
        {
            foreach (var c in IO.Path.GetInvalidFileNameChars()) name = name.Replace(c, '_');
            return name.Trim();
        }

        private void AppendDeleteList(string fullPath)
        {
            try
            {
                var listPath = IO.Path.Combine(IO.Path.GetDirectoryName(fullPath)!, ".deleteList.txt");
                IO.File.AppendAllLines(listPath, new[] { IO.Path.GetFileName(fullPath) });
            }
            catch { }
        }

        private string UniqueListName(string baseName)
        {
            var taken = _index.Lists.Select(l => l.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
            if (!taken.Contains(baseName)) return baseName;

            for (int i = 2; i < int.MaxValue; i++)
            {
                var c = $"{baseName} ({i})";
                if (!taken.Contains(c)) return c;
            }

            return baseName + "_" + Guid.NewGuid().ToString("N")[..6];
        }
    }
}
