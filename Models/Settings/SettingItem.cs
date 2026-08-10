using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;

namespace MinePackEditor.Models.Settings
{
    /// <summary>
    /// 供 UI 直接绑定的单个设置项。
    /// 封装了与 AppSettings 实例的双向同步。
    /// </summary>
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

        #region === 核心值访问（修复数值类型绑定问题）===

        /// <summary>
        /// 通用值访问器。对数值类型自动返回 double，确保 Slider/NumericUpDown 能正确读取。
        /// </summary>
        public object? CurrentValue
        {
            get
            {
                // 关键修复：数值类型统一暴露为 double，避免 Avalonia 绑定 object?→double 转换失败
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
                }
            }
        }

        /// <summary>
        /// 专用于 Slider / NumericUpDown 的强类型 double 值。
        /// </summary>
        public double DoubleValue
        {
            get => _currentValue == null ? 0 : Convert.ToDouble(_currentValue);
            set => CurrentValue = value; // 走 CurrentValue setter，自动完成类型转换与持久化
        }

        /// <summary>
        /// 约束值：最小值（double）
        /// </summary>
        public double MinValue => Constraint.Min == null ? 0 : Convert.ToDouble(Constraint.Min);

        /// <summary>
        /// 约束值：最大值（double）
        /// </summary>
        public double MaxValue => Constraint.Max == null ? 100 : Convert.ToDouble(Constraint.Max);

        /// <summary>
        /// 约束值：步长（double）
        /// </summary>
        public double StepValue => Constraint.Step == null ? 1 : Convert.ToDouble(Constraint.Step);

        /// <summary>
        /// 专用于 ComboBox 的选中项。与 Constraint.Options 类型匹配。
        /// </summary>
        public SettingOption? SelectedOption
        {
            get => Constraint.Options?.FirstOrDefault(o => Equals(o.Value, _currentValue));
            set
            {
                if (!Equals(SelectedOption, value))
                {
                    CurrentValue = value?.Value;
                }
            }
        }

        #endregion

        /// <summary>
        /// 关联到 AppSettings 的具体属性，建立双向监听
        /// </summary>
        public void Attach(AppSettings settings, PropertyInfo property)
        {
            Detach();
            _settings = settings;
            _property = property;
            _currentValue = property.GetValue(settings);
            _settings.PropertyChanged += OnSettingsPropertyChanged;
        }

        public void Detach()
        {
            if (_settings != null)
            {
                _settings.PropertyChanged -= OnSettingsPropertyChanged;
                _settings = null;
            }
            _property = null;
        }

        public void ResetToDefault()
        {
            CurrentValue = DefaultValue;
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
}
