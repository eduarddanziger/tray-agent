using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media;

namespace Tray.Agent
{
    public static class UtilityExtensions
    {
        public static TChildItem FindVisualChild<TChildItem>(this DependencyObject parent)
            where TChildItem : DependencyObject
        {
            if (parent == null)
                throw new ArgumentNullException(nameof(parent));

            if (VisualTreeHelper.GetChildrenCount(parent) == 0)
                return null;

            var stack = new Stack<Tuple<DependencyObject, int>>();
            stack.Push(Tuple.Create(parent, 0));
            while (stack.Count > 0)
            {
                var current = stack.Pop();
                // ReSharper disable once InvocationIsSkipped
                Debug.Assert(current != null, "current != null");

                var next = Tuple.Create(current.Item1, current.Item2 + 1);
                if (next.Item2 < VisualTreeHelper.GetChildrenCount(next.Item1))
                    stack.Push(next);

                var child = VisualTreeHelper.GetChild(current.Item1, current.Item2);

                if (child is TChildItem item)
                    return item;

                if (VisualTreeHelper.GetChildrenCount(child) != 0)
                    stack.Push(Tuple.Create(child, 0));
            }

            return null;
        }
    }
}