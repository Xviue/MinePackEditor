using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using CommunityToolkit.Mvvm.Input;

namespace MinePackEditor.Models.Settings;

public class SettingItem : INotifyPropertyChanged, IDisposable
{
    private AppSettings? _settings;
    private PropertyInfo? _property;
    private object? _currentValue;

    public string Id { get; set; } = string.Empty;
    public string PropertyName { get; set; } = string.Empty;
    public string DisplayNameKey { get; set; } = string.Empty;
    public string DescriptionKey { get; set; } = string.Empty;
    public SettingEditorType EditorType { get; set; }
    public SettingConstraint Constraint { get; set; } = new();
    public Type ValueType { get; set; } = typeof(object);
    public object? DefaultValue { get; set; }
    public bool AllowListEdit { get; set; } = false;
    public object? OriginalValue { get; private set; }
    private bool _isOriginalValueSet;

    #region === 通用值访问 ===

    /// <summary>当前值是否与原始值不同</summary>
    public bool IsModified
    {
        get
        {
            if (OriginalValue == null && _currentValue == null) return false;
            if (OriginalValue == null || _currentValue == null) return true;
            return !OriginalValue.Equals(_currentValue);
        }
    }

    public object? CurrentValue
    {
        get
        {
            if (_currentValue != null && IsNumericType(ValueType))
                return Convert.ToDouble(_currentValue);
            return _currentValue;
        }
        set
        {
            if (!Equals(_currentValue, value))
            {
                _currentValue = value;
                if (_property != null && _settings != null)
                {
                    try
                    {
                        var targetType = Nullable.GetUnderlyingType(_property.PropertyType) ?? _property.PropertyType;
                        var converted = Convert.ChangeType(value, targetType);
                        _property.SetValue(_settings, converted);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[SettingItem] 写入 {Id} 失败: {ex.Message}");
                    }
                }
                OnPropertyChanged(nameof(CurrentValue));
                OnPropertyChanged(nameof(DoubleValue));
                OnPropertyChanged(nameof(SelectedOption));
                OnPropertyChanged(nameof(ListItems));
                OnPropertyChanged(nameof(IsModified));
            }
        }
    }

    #endregion

    #region === 数值类型（Slider/NumericUpDown） ===

    public double DoubleValue
    {
        get => _currentValue == null ? 0 : Convert.ToDouble(_currentValue);
        set => CurrentValue = value;
    }

    public double MinValue => Constraint.Min == null ? 0 : Convert.ToDouble(Constraint.Min);
    public double MaxValue => Constraint.Max == null ? 100 : Convert.ToDouble(Constraint.Max);
    public double StepValue => Constraint.Step == null ? 1 : Convert.ToDouble(Constraint.Step);

    #endregion

    #region === ComboBox ===

    public SettingOption? SelectedOption
    {
        get => Constraint.Options?.FirstOrDefault(o => Equals(o.Value, _currentValue));
        set
        {
            if (!Equals(SelectedOption, value))
                CurrentValue = value?.Value;
        }
    }

    #endregion

    #region === 列表编辑器（关键修复：所有缺失属性在此） ===

    /// <summary>
    /// 强类型暴露列表集合，供 ListBox.ItemsSource 绑定。
    /// 解决 object? 类型推断失败问题。
    /// </summary>
    public IList? ListItems => _currentValue as IList;

    private object? _selectedListItem;
    public object? SelectedListItem
    {
        get => _selectedListItem;
        set
        {
            if (!Equals(_selectedListItem, value))
            {
                _selectedListItem = value;
                OnPropertyChanged(nameof(SelectedListItem));
            }
        }
    }

    private bool _isDebugVisible;
    public bool IsDebugVisible
    {
        get => _isDebugVisible;
        set
        {
            if (_isDebugVisible != value)
            {
                _isDebugVisible = value;
                OnPropertyChanged(nameof(IsDebugVisible));
            }
        }
    }

    public IRelayCommand? AddListItemCommand { get; private set; }
    public IRelayCommand? RemoveListItemCommand { get; private set; }

    #endregion

    public void Attach(AppSettings settings, PropertyInfo property)
    {
        Detach();
        _settings = settings;
        _property = property;
        _currentValue = property.GetValue(settings);
        // 关键：只在首次 Attach 时记录原始值，之后不再改变
        if (!_isOriginalValueSet)
        {
            OriginalValue = _currentValue;
            _isOriginalValueSet = true;
        }
        _settings.PropertyChanged += OnSettingsPropertyChanged;

        // 初始化列表编辑器命令
        if (EditorType == SettingEditorType.ListEditor && _currentValue is IList list)
        {
            var itemType = property.PropertyType.IsGenericType
                ? property.PropertyType.GetGenericArguments()[0]
                : typeof(object);

            AddListItemCommand = new RelayCommand(() =>
            {
                try
                {
                    var newItem = Activator.CreateInstance(itemType);
                    list.Add(newItem);
                    OnPropertyChanged(nameof(CurrentValue));
                    OnPropertyChanged(nameof(ListItems));
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[SettingItem] 添加列表项失败: {ex.Message}");
                }
            });

            RemoveListItemCommand = new RelayCommand(() =>
            {
                if (SelectedListItem != null && list.Contains(SelectedListItem))
                {
                    list.Remove(SelectedListItem);
                    SelectedListItem = null;
                    OnPropertyChanged(nameof(CurrentValue));
                    OnPropertyChanged(nameof(ListItems));
                }
            });
        }
    }
    public void ResetToDefault()
    {
        CurrentValue = DefaultValue;
    }


    public void Detach()
    {
        if (_settings != null)
        {
            _settings.PropertyChanged -= OnSettingsPropertyChanged;
            _settings = null;
        }
        _property = null;
        AddListItemCommand = null;
        RemoveListItemCommand = null;
    }

    private void OnSettingsPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (_property != null && e.PropertyName == _property.Name)
        {
            var newValue = _property.GetValue(_settings);
            if (!Equals(_currentValue, newValue))
            {
                _currentValue = newValue;
                OnPropertyChanged(nameof(CurrentValue));
                OnPropertyChanged(nameof(DoubleValue));
                OnPropertyChanged(nameof(SelectedOption));
                OnPropertyChanged(nameof(ListItems));
            }
        }
    }

    private static bool IsNumericType(Type type)
    {
        var t = Nullable.GetUnderlyingType(type) ?? type;
        return t == typeof(int) || t == typeof(long) || t == typeof(float)
            || t == typeof(double) || t == typeof(decimal)
            || t == typeof(short) || t == typeof(byte);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged(string propertyName) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    public void Dispose() => Detach();
}