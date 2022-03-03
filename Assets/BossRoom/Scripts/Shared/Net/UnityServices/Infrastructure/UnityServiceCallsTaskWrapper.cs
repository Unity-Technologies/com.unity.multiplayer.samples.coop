using System;
using System.Threading.Tasks;
using UnityEngine;

namespace Unity.Multiplayer.Samples.BossRoom.Shared.Net.UnityServices.Infrastructure
{
    /// <summary>
    /// Unity Services need for asynchronous requests with some basic safety wrappers. This is a shared place for that.
    /// This will also permit parsing incoming exceptions for any service-specific errors that should be displayed to the player.
    /// </summary>
    public static class UnityServiceCallsTaskWrapper
    {
        public static async void RunTaskAsync<TException>(Task task, Action onComplete, Action onFailed, Action<TException> onException) where TException : Exception
        {
            string currentTrace = Environment.StackTrace; // For debugging. If we don't get the calling context here, it's lost once the async operation begins.
            try
            {
                await task;
            }
            catch (TException e)
            {
                onException?.Invoke(e);
                Debug.LogError($"AsyncRequest threw an exception. Call stack before async call:\n{currentTrace}\n"); // Note that we log here instead of creating a new Exception in case of a change in calling context during the async call. E.g. Relay has its own exception handling that would intercept this call stack.
                throw e;
            }
            finally
            {
                if (task.IsFaulted)
                {
                    onFailed?.Invoke();
                }
                else
                {
                    onComplete?.Invoke();
                }
            }
        }

        public static async void RunTaskAsync<TResult, TException>(Task<TResult> task, Action<TResult> onComplete, Action onFailed, Action<TException> onException) where TException : Exception
        {
            string currentTrace = Environment.StackTrace; // For debugging. If we don't get the calling context here, it's lost once the async operation begins.
            try
            {
                await task;
            }
            catch (TException e)
            {
                onException?.Invoke(e);
                Debug.LogError($"AsyncRequest threw an exception. Call stack before async call:\n{currentTrace}\n"); // Note that we log here instead of creating a new Exception in case of a change in calling context during the async call. E.g. Relay has its own exception handling that would intercept this call stack.
                throw e;
            }
            finally
            {
                if (task.IsFaulted)
                {
                    onFailed?.Invoke();
                }
                else
                {
                    onComplete?.Invoke(task.Result);
                }
            }
        }
    }
}
