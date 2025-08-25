using System.Collections.Generic;
using UnityEngine;
using WitShells.ThreadingJob;
using WitShells.ThreadingJob.Tests;

public partial class ThreadManagerEditor
{
    [Header("Test Results")]
    [SerializeField] private bool showDebugLogs = true;
    [SerializeField] private List<string> testResults = new List<string>();

    public void ShowAllTests()
    {
        if (GUILayout.Button("Run All Tests"))
        {
            RunAllTests();
        }

        if (GUILayout.Button("Sync Execution"))
        {
            TestSyncExecution();
        }

        if (GUILayout.Button("Async Execution"))
        {
            TestAsyncExecution();
        }

        if (GUILayout.Button("Sync Streaming"))
        {
            TestSyncStreaming();
        }

        if (GUILayout.Button("Async Streaming"))
        {
            TestAsyncStreaming();
        }

        if (GUILayout.Button("Complex Streaming"))
        {
            TestComplexStreaming();
        }

        if (GUILayout.Button("Error Handling"))
        {
            TestErrorHandling();
        }
    }

    [ContextMenu("Run All Tests")]
    public void RunAllTests()
    {
        testResults.Clear();
        LogTest("=== Starting ThreadJob Tests ===");

        TestSyncExecution();
        TestAsyncExecution();
        TestSyncStreaming();
        TestAsyncStreaming();
        TestComplexStreaming();
        TestErrorHandling();

        LogTest("=== All Tests Completed ===");
    }

    // Test 1: Normal Synchronous Execution
    [ContextMenu("Test Sync Execution")]
    public void TestSyncExecution()
    {
        LogTest("--- Testing Sync Execution ---");

        var job = new SyncCalculationJob(10, 5);

        ThreadManager.Instance.EnqueueJob(
            job,
            onComplete: (result) =>
            {
                LogTest($"✅ Sync job completed: {result}");
                LogTest($"Expected: 50, Got: {result}, Match: {result == 50}");
            },
            onError: (error) =>
            {
                LogTest($"❌ Sync job failed: {error.Message}");
            }
        );
    }

    // Test 2: Async Execution
    [ContextMenu("Test Async Execution")]
    public void TestAsyncExecution()
    {
        LogTest("--- Testing Async Execution ---");

        var job = new AsyncWebRequestJob("https://api.example.com/data");

        ThreadManager.Instance.EnqueueJob(
            job,
            onComplete: (result) =>
            {
                LogTest($"✅ Async job completed: {result}");
            },
            onError: (error) =>
            {
                LogTest($"❌ Async job failed: {error.Message}");
            }
        );

        // Test error scenario
        var errorJob = new AsyncWebRequestJob("https://api.error.com/fail");
        ThreadManager.Instance.EnqueueJob(
            errorJob,
            onComplete: (result) =>
            {
                LogTest($"❌ Error job should not complete: {result}");
            },
            onError: (error) =>
            {
                LogTest($"✅ Error job failed as expected: {error.Message}");
            }
        );
    }

    // Test 3: Sync Streaming
    [ContextMenu("Test Sync Streaming")]
    public void TestSyncStreaming()
    {
        LogTest("--- Testing Sync Streaming ---");

        string[] files = { "file1.txt", "file2.txt", "file3.txt", "file4.txt" };
        var job = new FileProcessingJob(files, 300);

        ThreadManager.Instance.EnqueueStreamingJob(
            job,
            onProgress: (progress) =>
            {
                LogTest($"📁 File progress: {progress}");
            },
            onComplete: () =>
            {
                LogTest("✅ Sync streaming completed");
            },
            onError: (error) =>
            {
                LogTest($"❌ Sync streaming failed: {error.Message}");
            }
        );
    }

    // Test 4: Async Streaming
    [ContextMenu("Test Async Streaming")]
    public void TestAsyncStreaming()
    {
        LogTest("--- Testing Async Streaming ---");

        string[] urls = {
                "https://api1.com",
                "https://api2.com",
                "https://api3.fail.com",
                "https://api4.com"
            };

        var job = new AsyncBatchDownloadJob(urls, 400);

        ThreadManager.Instance.EnqueueStreamingJob(
            job,
            onProgress: (downloadResult) =>
            {
                LogTest($"📥 Download progress: {downloadResult}");
            },
            onComplete: () =>
            {
                LogTest("✅ Async streaming completed");
            },
            onError: (error) =>
            {
                LogTest($"❌ Async streaming failed: {error.Message}");
            }
        );
    }

    // Test 5: Complex Streaming with Multiple Progress Types
    [ContextMenu("Test Complex Streaming")]
    public void TestComplexStreaming()
    {
        LogTest("--- Testing Complex Streaming ---");

        var job = new ComplexDataProcessingJob(20, false);

        ThreadManager.Instance.EnqueueStreamingJob(
            job,
            onProgress: (update) =>
            {
                LogTest($"🔄 Processing: {update}");
            },
            onComplete: () =>
            {
                LogTest("✅ Complex streaming completed");
            },
            onError: (error) =>
            {
                LogTest($"❌ Complex streaming failed: {error.Message}");
            }
        );
    }

    // Test 6: Error Handling
    [ContextMenu("Test Error Handling")]
    public void TestErrorHandling()
    {
        LogTest("--- Testing Error Handling ---");

        // Test sync job with division by zero
        var errorSyncJob = new SyncCalculationJob(10, 0);
        ThreadManager.Instance.EnqueueJob(
            errorSyncJob,
            onComplete: (result) =>
            {
                LogTest($"❌ Error sync should not complete: {result}");
            },
            onError: (error) =>
            {
                LogTest($"✅ Sync error handled: {error.GetType().Name}");
            }
        );

        // Test streaming job with error
        var errorStreamingJob = new ComplexDataProcessingJob(10, true);
        ThreadManager.Instance.EnqueueStreamingJob(
            errorStreamingJob,
            onProgress: (update) =>
            {
                LogTest($"🔄 Error streaming progress: {update}");
            },
            onComplete: () =>
            {
                LogTest("❌ Error streaming should not complete");
            },
            onError: (error) =>
            {
                LogTest($"✅ Streaming error handled: {error.Message}");
            }
        );
    }

    // Test 7: Batch Operations
    [ContextMenu("Test Batch Operations")]
    public void TestBatchOperations()
    {
        LogTest("--- Testing Batch Operations ---");

        // Queue multiple jobs at once
        for (int i = 0; i < 5; i++)
        {
            int index = i; // Capture for closure
            var job = new SyncCalculationJob(index, 2);

            ThreadManager.Instance.EnqueueJob(
                job,
                onComplete: (result) =>
                {
                    LogTest($"✅ Batch job {index} completed: {result}");
                },
                onError: (error) =>
                {
                    LogTest($"❌ Batch job {index} failed: {error.Message}");
                }
            );
        }
    }

    // Test 8: Mixed Job Types
    [ContextMenu("Test Mixed Job Types")]
    public void TestMixedJobTypes()
    {
        LogTest("--- Testing Mixed Job Types ---");

        // Mix sync, async, and streaming jobs
        TestSyncExecution();

        System.Threading.Thread.Sleep(100);
        TestAsyncExecution();

        System.Threading.Thread.Sleep(100);
        TestSyncStreaming();
    }

    private void LogTest(string message)
    {
        testResults.Add($"[{System.DateTime.Now:HH:mm:ss}] {message}");

        if (showDebugLogs)
            Debug.Log(message);

        // Keep only last 50 results
        if (testResults.Count > 50)
            testResults.RemoveAt(0);
    }

    // Utility method to clear test results
    [ContextMenu("Clear Test Results")]
    public void ClearTestResults()
    {
        testResults.Clear();
        LogTest("Test results cleared");
    }
}