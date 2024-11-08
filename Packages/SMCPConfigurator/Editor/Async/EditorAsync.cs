
using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;

public sealed class EditorAsync : IDisposable
{
    public bool IsStarted => _taskCompletionSource != null;
    
    bool _isDisposed;
    Func<bool> _isFinishedCondition;
    TaskCompletionSource<bool> _taskCompletionSource;
    CancellationTokenSource _cancellationTokenSource;
    
    public async Task StartAsync(Func<bool> isFinishedCondition, CancellationToken cancellationToken = default)
    {
        if (IsStarted)
        {
            throw new InvalidOperationException("Task is already started.");
        }

        if (isFinishedCondition())
        {
            return;
        }
        
        if (cancellationToken.IsCancellationRequested)
        {
            throw new OperationCanceledException(cancellationToken);
        }
        
        _isFinishedCondition = isFinishedCondition;
        _taskCompletionSource = new TaskCompletionSource<bool>();
        _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _cancellationTokenSource.Token.Register(() => Cancel(new OperationCanceledException("Operation was cancelled by token")));
        
        EditorApplication.update += OnUpdate;

        try
        {
            await _taskCompletionSource.Task;
        }
        finally
        {
            Cleanup();
        }
    }

    public void Cancel(Exception exception = default)
    {
        if (!IsStarted)
        {
            return;
        }

        _taskCompletionSource.TrySetException(exception ?? new OperationCanceledException("Operation was cancelled"));
        Cleanup();
    }

    void OnUpdate()
    {
        if (_isFinishedCondition())
        {
            _taskCompletionSource.SetResult(true);
            Cleanup();
        }
    }

    void Cleanup()
    {
        _taskCompletionSource = default;
        _cancellationTokenSource?.Dispose();
        _cancellationTokenSource = default;
        EditorApplication.update -= OnUpdate;
    }

    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }
        _isDisposed = true;

        if (IsStarted)
        {
            Cleanup();
        }
    }
}