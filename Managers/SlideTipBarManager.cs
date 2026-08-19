using Avalonia.Threading;
using MinePackEditor.Controls.TemplatedControls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MinePackEditor.Managers
{
    /// <summary>
    /// 全局 SlideTipBar 管理器。允许从 ViewModel / 任意代码层
    /// 通过 BarId + TipId 激活指定的 SlideTip。
    /// </summary>
    public static class SlideTipBarManager
    {
        private static readonly Dictionary<string, WeakReference<SlideTipBar>> _bars = new();

        /// <summary>注册 Bar（由 SlideTipBar 自身在 Loaded 时调用）</summary>
        public static void Register(string barId, SlideTipBar bar)
        {
            _bars[barId] = new WeakReference<SlideTipBar>(bar);
        }

        /// <summary>注销 Bar（由 SlideTipBar 自身在 Unloaded 时调用）</summary>
        public static void Unregister(string barId)
        {
            _bars.Remove(barId);
        }

        /// <summary>激活指定 Bar 中的指定 Tip</summary>
        public static void Activate(string barId, string tipId)
        {
            Dispatcher.UIThread.Post(() =>
            {
                if (!_bars.TryGetValue(barId, out var weakRef)) return;
                if (!weakRef.TryGetTarget(out var bar)) return;

                bar.Activate(tipId);
            });
        }

        /// <summary>激活指定 Bar 中的指定 Tip 实例</summary>
        public static void Activate(string barId, SlideTip tip)
        {
            Dispatcher.UIThread.Post(() =>
            {
                if (!_bars.TryGetValue(barId, out var weakRef)) return;
                if (!weakRef.TryGetTarget(out var bar)) return;

                bar.Activate(tip);
            });
        }

        /// <summary>便捷方法：直接通过 TipId 在所有已注册 Bar 中搜索并激活（首个匹配）</summary>
        public static void ActivateByTipId(string tipId)
        {
            Dispatcher.UIThread.Post(() =>
            {
                foreach (var weakRef in _bars.Values)
                {
                    if (!weakRef.TryGetTarget(out var bar)) continue;
                    var tip = bar.Items.FirstOrDefault(t => t.Id == tipId);
                    if (tip != null)
                    {
                        bar.Activate(tip);
                        return;
                    }
                }
            });
        }
    }
}
