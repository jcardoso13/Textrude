﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace TextrudeInteractive
{
    public class PaneCache<T> where T : IPane, new()
    {
        private readonly Action<T> _onNew;
        private readonly Queue<T> _panes = new();

        public PaneCache(Action<T> onNew) => _onNew = onNew;

        public T Obtain()
        {
            if (!_panes.Any())
            {
                var newPane = new T();
                _onNew(newPane);
                _panes.Enqueue(newPane);
            }

            return _panes.Dequeue();
        }

        public void Release(T pane)
        {
            pane.Clear();
            _panes.Enqueue(pane);
        }
    }
}