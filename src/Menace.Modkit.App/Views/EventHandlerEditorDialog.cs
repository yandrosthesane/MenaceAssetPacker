using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Menace.Modkit.App.Services;
using Menace.Modkit.App.ViewModels;

namespace Menace.Modkit.App.Views;

/// <summary>
/// Full-width modal editor for EventHandlers arrays in Skills and Perks.
/// Provides dynamic UI generation based on schema, enum resolution, and JSON toggle view.
/// </summary>
public class EventHandlerEditorDialog : Window
{
    private readonly EventHandlerEditorViewModel _viewModel;
    private readonly ListBox _handlerListBox;
    private readonly StackPanel _editorPanel;
    private readonly ScrollViewer _editorScroll;
    private readonly Button _jsonToggleBtn;
    private bool _jsonViewMode = false;
    private TextBox? _jsonTextBox;

    public EventHandlerEditorDialog(
        string fieldName,
        JsonElement arrayElement,
        StatsEditorViewModel parentVm)
    {
        Title = $"Edit {fieldName}";
        Width = 900;
        Height = 600;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Background = new SolidColorBrush(Color.Parse("#1E1E1E"));
        CanResize = true;

        _viewModel = new EventHandlerEditorViewModel(fieldName, arrayElement, parentVm);

        _handlerListBox = new ListBox();
        _editorPanel = new StackPanel { Spacing = 8 };
        _editorScroll = new ScrollViewer();
        _jsonToggleBtn = new Button { Content = "View as JSON" };

        Content = BuildUI();
    }

    private Control BuildUI()
    {
        var mainGrid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("250,4,*"),
            RowDefinitions = new RowDefinitions("*,Auto"),
            Margin = new Thickness(8)
        };

        // Left: List of handlers
        var leftPanel = BuildHandlerList();
        mainGrid.Children.Add(leftPanel);
        Grid.SetColumn(leftPanel, 0);
        Grid.SetRow(leftPanel, 0);

        // Splitter
        var splitter = new GridSplitter
        {
            Background = new SolidColorBrush(Color.Parse("#2D2D2D")),
            ResizeDirection = GridResizeDirection.Columns
        };
        mainGrid.Children.Add(splitter);
        Grid.SetColumn(splitter, 1);
        Grid.SetRow(splitter, 0);

        // Right: Editor panel
        var rightPanel = BuildEditorPanel();
        mainGrid.Children.Add(rightPanel);
        Grid.SetColumn(rightPanel, 2);
        Grid.SetRow(rightPanel, 0);

        // Bottom: Buttons
        var buttonRow = BuildButtonRow();
        mainGrid.Children.Add(buttonRow);
        Grid.SetColumn(buttonRow, 0);
        Grid.SetColumnSpan(buttonRow, 3);
        Grid.SetRow(buttonRow, 1);

        return mainGrid;
    }

    private Control BuildHandlerList()
    {
        var panel = new Border
        {
            Background = new SolidColorBrush(Color.Parse("#252526")),
            BorderBrush = new SolidColorBrush(Color.Parse("#3E3E3E")),
            BorderThickness = new Thickness(1),
            Padding = new Thickness(8)
        };

        var stack = new StackPanel { Spacing = 8 };

        stack.Children.Add(new TextBlock
        {
            Text = $"EventHandlers ({_viewModel.Handlers.Count})",
            Foreground = Brushes.White,
            FontWeight = FontWeight.SemiBold
        });

        _handlerListBox.ItemsSource = _viewModel.Handlers;
        _handlerListBox.Background = Brushes.Transparent;
        _handlerListBox.BorderThickness = new Thickness(0);

        _handlerListBox.ItemTemplate = new Avalonia.Controls.Templates.FuncDataTemplate<HandlerItem>(
            (item, _) => BuildHandlerListItem(item));

        _handlerListBox.SelectionChanged += OnHandlerSelected;

        var scrollViewer = new ScrollViewer
        {
            Content = _handlerListBox,
            MaxHeight = 450
        };
        stack.Children.Add(scrollViewer);

        // Add button
        var addButton = new Button
        {
            Content = "+ Add EventHandler",
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Background = new SolidColorBrush(Color.Parse("#2D2D2D")),
            Foreground = Brushes.White,
            BorderBrush = new SolidColorBrush(Color.Parse("#3E3E3E")),
            BorderThickness = new Thickness(1)
        };
        addButton.Click += OnAddHandler;
        stack.Children.Add(addButton);

        panel.Child = stack;
        return panel;
    }

    private Control BuildHandlerListItem(HandlerItem item)
    {
        var grid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("Auto,*,Auto"),
            Margin = new Thickness(0, 2)
        };

        grid.Children.Add(new TextBlock
        {
            Text = $"[{item.Index}]",
            Foreground = new SolidColorBrush(Color.Parse("#8ECDC8")),
            FontSize = 11,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 8, 0)
        });
        Grid.SetColumn(grid.Children[0], 0);

        grid.Children.Add(new TextBlock
        {
            Text = item.TypeName ?? "(empty)",
            Foreground = Brushes.White,
            FontSize = 12,
            TextTrimming = TextTrimming.CharacterEllipsis,
            VerticalAlignment = VerticalAlignment.Center
        });
        Grid.SetColumn(grid.Children[1], 1);

        var deleteBtn = new Button
        {
            Content = "×",
            Foreground = new SolidColorBrush(Color.Parse("#CC4444")),
            Background = Brushes.Transparent,
            BorderThickness = new Thickness(0),
            FontSize = 16,
            Padding = new Thickness(4, 0),
            Cursor = new Cursor(StandardCursorType.Hand)
        };
        deleteBtn.Click += (_, _) => OnDeleteHandler(item);
        grid.Children.Add(deleteBtn);
        Grid.SetColumn(deleteBtn, 2);

        return grid;
    }

    private Control BuildEditorPanel()
    {
        var panel = new Border
        {
            Background = new SolidColorBrush(Color.Parse("#252526")),
            BorderBrush = new SolidColorBrush(Color.Parse("#3E3E3E")),
            BorderThickness = new Thickness(1),
            Padding = new Thickness(12)
        };

        _editorScroll.Content = _editorPanel;
        panel.Child = _editorScroll;
        return panel;
    }

    private void OnHandlerSelected(object? sender, SelectionChangedEventArgs e)
    {
        if (_handlerListBox.SelectedItem is not HandlerItem item)
        {
            _editorPanel.Children.Clear();
            return;
        }

        _viewModel.SelectedHandler = item;
        RenderHandlerEditor(item);
    }

    private void RenderHandlerEditor(HandlerItem item)
    {
        _editorPanel.Children.Clear();

        if (_jsonViewMode)
        {
            RenderJsonEditor(item);
            return;
        }

        // Title
        _editorPanel.Children.Add(new TextBlock
        {
            Text = $"EventHandler [{item.Index}]",
            Foreground = Brushes.White,
            FontSize = 14,
            FontWeight = FontWeight.SemiBold,
            Margin = new Thickness(0, 0, 0, 8)
        });

        // _type dropdown
        _editorPanel.Children.Add(CreateTypeSelector(item));

        // Dynamic fields based on selected type
        if (!string.IsNullOrEmpty(item.TypeName))
        {
            RenderFieldsForType(item);
        }
    }

    private Control CreateTypeSelector(HandlerItem item)
    {
        var stack = new StackPanel { Spacing = 4 };

        stack.Children.Add(new TextBlock
        {
            Text = "_type",
            Foreground = Brushes.White,
            Opacity = 0.8,
            FontSize = 11,
            FontWeight = FontWeight.SemiBold
        });

        var allTypes = _viewModel.GetAllEventHandlerTypes();

        var comboBox = new ComboBox
        {
            ItemsSource = allTypes,
            SelectedItem = item.TypeName,
            Background = new SolidColorBrush(Color.Parse("#1E1E1E")),
            Foreground = Brushes.White,
            BorderBrush = new SolidColorBrush(Color.Parse("#3E3E3E")),
            BorderThickness = new Thickness(1),
            FontSize = 12
        };

        comboBox.SelectionChanged += (_, _) =>
        {
            if (comboBox.SelectedItem is string newType)
            {
                item.TypeName = newType;
                item.Data["_type"] = newType;
                _viewModel.InitializeFieldsForType(item, newType);
                RenderHandlerEditor(item);
            }
        };

        stack.Children.Add(comboBox);
        return stack;
    }

    private void RenderFieldsForType(HandlerItem item)
    {
        var fields = _viewModel.ParentVm.SchemaService?
            .GetAllEmbeddedClassFields(item.TypeName!);

        if (fields == null || fields.Count == 0)
        {
            _editorPanel.Children.Add(new TextBlock
            {
                Text = "No schema found for this type",
                Foreground = new SolidColorBrush(Color.Parse("#FFB347")),
                FontStyle = FontStyle.Italic,
                Margin = new Thickness(0, 8)
            });
            return;
        }

        // Render each field
        foreach (var field in fields.Where(f => f.Name != "_type"))
        {
            var fieldControl = CreateFieldControl(item, field);
            if (fieldControl != null)
                _editorPanel.Children.Add(fieldControl);
        }
    }

    private Control? CreateFieldControl(HandlerItem item, SchemaService.FieldMeta field)
    {
        var fieldStack = new StackPanel { Spacing = 4, Margin = new Thickness(0, 4) };

        // Label
        fieldStack.Children.Add(new TextBlock
        {
            Text = field.Name,
            Foreground = Brushes.White,
            Opacity = 0.8,
            FontSize = 11,
            FontWeight = FontWeight.SemiBold
        });

        // Get current value
        item.Data.TryGetValue(field.Name, out var currentValue);

        // Create control based on field category
        Control control = field.Category switch
        {
            "enum" => CreateEnumDropdownForField(item, field, currentValue),
            "reference" => CreateReferencePickerForField(item, field, currentValue),
            "primitive" => CreatePrimitiveControlForField(item, field, currentValue),
            _ => CreateTextBoxForField(item, field, currentValue)
        };

        fieldStack.Children.Add(control);
        return fieldStack;
    }

    private Control CreateEnumDropdownForField(HandlerItem item, SchemaService.FieldMeta field, object? currentValue)
    {
        var enumTypeName = field.Type;
        var currentIntValue = currentValue is long l ? (int)l : 0;

        var enumValues = _viewModel.ParentVm.SchemaService?.GetEnumValues(enumTypeName);
        if (enumValues == null || enumValues.Count == 0)
        {
            return CreateTextBoxForField(item, field, currentValue);
        }

        var displayItems = enumValues
            .OrderBy(kvp => kvp.Key)
            .Select(kvp => new { DisplayText = $"{kvp.Value} ({kvp.Key})", Value = kvp.Key })
            .ToList();

        var selectedItem = displayItems.FirstOrDefault(x => x.Value == currentIntValue);

        var comboBox = new ComboBox
        {
            ItemsSource = displayItems,
            SelectedItem = selectedItem,
            Background = new SolidColorBrush(Color.Parse("#1E1E1E")),
            Foreground = Brushes.White,
            BorderBrush = new SolidColorBrush(Color.Parse("#3E3E3E")),
            BorderThickness = new Thickness(1),
            FontSize = 12
        };

        comboBox.ItemTemplate = new Avalonia.Controls.Templates.FuncDataTemplate<dynamic>((x, _) =>
            new TextBlock
            {
                Text = x?.DisplayText ?? "",
                Foreground = Brushes.White
            }
        );

        comboBox.SelectionChanged += (_, _) =>
        {
            if (comboBox.SelectedItem != null)
            {
                var value = ((dynamic)comboBox.SelectedItem).Value;
                item.Data[field.Name] = (long)value;
            }
        };

        return comboBox;
    }

    private Control CreateReferencePickerForField(HandlerItem item, SchemaService.FieldMeta field, object? currentValue)
    {
        var refType = field.Type;
        var instanceNames = _viewModel.ParentVm.GetTemplateInstanceNames(refType);

        var currentText = currentValue?.ToString() ?? "";

        var autoComplete = new AutoCompleteBox
        {
            Text = currentText,
            ItemsSource = instanceNames,
            FilterMode = AutoCompleteFilterMode.ContainsOrdinal,
            MinimumPrefixLength = 0,
            Background = new SolidColorBrush(Color.Parse("#1E1E1E")),
            Foreground = Brushes.White,
            BorderBrush = new SolidColorBrush(Color.Parse("#3E3E3E")),
            BorderThickness = new Thickness(1),
            FontSize = 12
        };

        autoComplete.GetObservable(AutoCompleteBox.TextProperty)
            .Subscribe(text =>
            {
                item.Data[field.Name] = text ?? "";
            });

        return autoComplete;
    }

    private Control CreatePrimitiveControlForField(HandlerItem item, SchemaService.FieldMeta field, object? currentValue)
    {
        // Check if it's a boolean
        if (currentValue is bool boolVal)
        {
            var checkBox = new CheckBox
            {
                IsChecked = boolVal,
                Content = boolVal ? "True" : "False",
                Foreground = Brushes.White,
                FontSize = 12
            };

            checkBox.IsCheckedChanged += (s, _) =>
            {
                if (s is CheckBox cb)
                {
                    var isChecked = cb.IsChecked ?? false;
                    cb.Content = isChecked ? "True" : "False";
                    item.Data[field.Name] = isChecked;
                }
            };

            return checkBox;
        }

        return CreateTextBoxForField(item, field, currentValue);
    }

    private Control CreateTextBoxForField(HandlerItem item, SchemaService.FieldMeta field, object? currentValue)
    {
        var textBox = new TextBox
        {
            Text = currentValue?.ToString() ?? "",
            Background = new SolidColorBrush(Color.Parse("#1E1E1E")),
            Foreground = Brushes.White,
            BorderBrush = new SolidColorBrush(Color.Parse("#3E3E3E")),
            BorderThickness = new Thickness(1),
            Padding = new Thickness(8, 6),
            FontSize = 12
        };

        textBox.TextChanged += (_, _) =>
        {
            var text = textBox.Text ?? "";

            // Try to parse as appropriate type
            if (field.Type.ToLowerInvariant() is "int" or "int32")
            {
                item.Data[field.Name] = long.TryParse(text, out var l) ? l : (object)text;
            }
            else if (field.Type.ToLowerInvariant() is "float" or "single" or "double")
            {
                item.Data[field.Name] = double.TryParse(text, out var d) ? d : (object)text;
            }
            else
            {
                item.Data[field.Name] = text;
            }
        };

        return textBox;
    }

    private void RenderJsonEditor(HandlerItem item)
    {
        _editorPanel.Children.Add(new TextBlock
        {
            Text = "JSON Editor (Advanced)",
            Foreground = Brushes.White,
            FontSize = 14,
            FontWeight = FontWeight.SemiBold,
            Margin = new Thickness(0, 0, 0, 8)
        });

        var json = JsonSerializer.Serialize(item.Data, new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        });

        var validationText = new TextBlock
        {
            Foreground = new SolidColorBrush(Color.Parse("#CC4444")),
            FontSize = 11,
            Margin = new Thickness(0, 4),
            IsVisible = false
        };

        _jsonTextBox = new TextBox
        {
            Text = json,
            AcceptsReturn = true,
            TextWrapping = TextWrapping.NoWrap,
            FontFamily = new FontFamily("monospace"),
            FontSize = 11,
            Background = new SolidColorBrush(Color.Parse("#1E1E1E")),
            Foreground = Brushes.White,
            BorderBrush = new SolidColorBrush(Color.Parse("#3E3E3E")),
            MinHeight = 400
        };

        // Real-time JSON validation
        _jsonTextBox.TextChanged += (_, _) =>
        {
            try
            {
                var text = _jsonTextBox.Text ?? "";
                if (string.IsNullOrWhiteSpace(text))
                {
                    validationText.IsVisible = false;
                    _jsonTextBox.BorderBrush = new SolidColorBrush(Color.Parse("#3E3E3E"));
                    return;
                }

                JsonDocument.Parse(text);
                validationText.IsVisible = false;
                _jsonTextBox.BorderBrush = new SolidColorBrush(Color.Parse("#3E3E3E"));
            }
            catch (JsonException ex)
            {
                validationText.Text = $"Invalid JSON: {ex.Message}";
                validationText.IsVisible = true;
                _jsonTextBox.BorderBrush = new SolidColorBrush(Color.Parse("#CC4444"));
            }
        };

        _editorPanel.Children.Add(_jsonTextBox);
        _editorPanel.Children.Add(validationText);
    }

    private Control BuildButtonRow()
    {
        var panel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(0, 8, 0, 0)
        };

        _jsonToggleBtn.Background = new SolidColorBrush(Color.Parse("#2D2D2D"));
        _jsonToggleBtn.Foreground = Brushes.White;
        _jsonToggleBtn.BorderBrush = new SolidColorBrush(Color.Parse("#3E3E3E"));
        _jsonToggleBtn.BorderThickness = new Thickness(1);
        _jsonToggleBtn.Click += (_, _) =>
        {
            _jsonViewMode = !_jsonViewMode;
            _jsonToggleBtn.Content = _jsonViewMode ? "View as Form" : "View as JSON";
            if (_viewModel.SelectedHandler != null)
                RenderHandlerEditor(_viewModel.SelectedHandler);
        };
        panel.Children.Add(_jsonToggleBtn);

        var cancelBtn = new Button
        {
            Content = "Cancel",
            Background = new SolidColorBrush(Color.Parse("#2D2D2D")),
            Foreground = Brushes.White,
            BorderBrush = new SolidColorBrush(Color.Parse("#3E3E3E")),
            BorderThickness = new Thickness(1)
        };
        cancelBtn.Click += (_, _) => Close(null);
        panel.Children.Add(cancelBtn);

        var applyBtn = new Button
        {
            Content = "Apply",
            Background = new SolidColorBrush(Color.Parse("#0078D4")),
            Foreground = Brushes.White,
            BorderThickness = new Thickness(0)
        };
        applyBtn.Click += async (_, _) =>
        {
            // Sync JSON edits back to data if in JSON mode
            if (_jsonViewMode && _jsonTextBox != null && _viewModel.SelectedHandler != null)
            {
                try
                {
                    var text = _jsonTextBox.Text ?? "{}";
                    var parsed = JsonSerializer.Deserialize<Dictionary<string, object?>>(text);
                    if (parsed != null)
                        _viewModel.SelectedHandler.Data = parsed;
                }
                catch (JsonException ex)
                {
                    // Show error dialog
                    var errorDialog = new Window
                    {
                        Title = "Invalid JSON",
                        Width = 400,
                        Height = 200,
                        WindowStartupLocation = WindowStartupLocation.CenterOwner,
                        Background = new SolidColorBrush(Color.Parse("#1E1E1E"))
                    };

                    var panel = new StackPanel
                    {
                        Margin = new Thickness(16),
                        Spacing = 12
                    };

                    panel.Children.Add(new TextBlock
                    {
                        Text = "Cannot apply changes - JSON is invalid:",
                        Foreground = Brushes.White,
                        FontWeight = FontWeight.SemiBold
                    });

                    panel.Children.Add(new TextBlock
                    {
                        Text = ex.Message,
                        Foreground = new SolidColorBrush(Color.Parse("#CC4444")),
                        TextWrapping = TextWrapping.Wrap
                    });

                    var okBtn = new Button
                    {
                        Content = "OK",
                        HorizontalAlignment = HorizontalAlignment.Right,
                        Background = new SolidColorBrush(Color.Parse("#0078D4")),
                        Foreground = Brushes.White,
                        BorderThickness = new Thickness(0)
                    };
                    okBtn.Click += (_, _) => errorDialog.Close();
                    panel.Children.Add(okBtn);

                    errorDialog.Content = panel;
                    await errorDialog.ShowDialog(this);
                    return;
                }
            }

            Close(_viewModel.BuildResult());
        };
        panel.Children.Add(applyBtn);

        return panel;
    }

    private void OnAddHandler(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var newHandler = new HandlerItem
        {
            Index = _viewModel.Handlers.Count,
            TypeName = null,
            Data = new Dictionary<string, object?>()
        };

        _viewModel.Handlers.Add(newHandler);
        _handlerListBox.SelectedItem = newHandler;

        // Reindex handlers
        for (int i = 0; i < _viewModel.Handlers.Count; i++)
        {
            _viewModel.Handlers[i].Index = i;
        }
    }

    private void OnDeleteHandler(HandlerItem item)
    {
        var index = _viewModel.Handlers.IndexOf(item);
        _viewModel.Handlers.Remove(item);

        // Reindex handlers
        for (int i = 0; i < _viewModel.Handlers.Count; i++)
        {
            _viewModel.Handlers[i].Index = i;
        }

        // Select next item or previous if at end
        if (_viewModel.Handlers.Count > 0)
        {
            var newIndex = Math.Min(index, _viewModel.Handlers.Count - 1);
            _handlerListBox.SelectedItem = _viewModel.Handlers[newIndex];
        }
        else
        {
            _editorPanel.Children.Clear();
        }
    }
}
