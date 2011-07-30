﻿using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows;

namespace FacetedWorlds.MyCon.ImageUtilities
{
    public class RequestQueue
    {
        private Queue<Action<Action>> _requests = new Queue<Action<Action>>();

        public void QueueRequest(Action<Action> request)
        {
            bool wasEmpty;
            lock (this)
            {
                wasEmpty = _requests.Count == 0;
                _requests.Enqueue(request);
            }
            if (wasEmpty)
            {
                Deployment.Current.Dispatcher.BeginInvoke(() => request(InvokeNextRequest));
            }
        }

        private void InvokeNextRequest()
        {
            Action<Action> nextRequest = null;
            lock (this)
            {
                if (_requests.Count > 0)
                    nextRequest = _requests.Dequeue();
            }
            if (nextRequest != null)
            {
                Deployment.Current.Dispatcher.BeginInvoke(() => nextRequest(InvokeNextRequest));
            }
        }
    }
}