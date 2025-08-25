using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace WitShells.ThreadingJob.Tests
{
    // 1. Normal Execute Job
    public class SyncCalculationJob : ThreadJob<int>
    {
        private readonly int _input;
        private readonly int _multiplier;

        public SyncCalculationJob(int input, int multiplier = 2)
        {
            _input = input;
            _multiplier = multiplier;
            IsAsync = false;
            IsStreaming = false;
        }

        public override int Execute()
        {
            try
            {
                // Simulate heavy calculation
                Thread.Sleep(1000);

                int result = _input * _multiplier;
                Result = result;
                return result;
            }
            catch (Exception ex)
            {
                Exception = ex;
                throw;
            }
        }
    }

    // 2. Async Execute Job
    public class AsyncWebRequestJob : ThreadJob<string>
    {
        private readonly string _url;

        public AsyncWebRequestJob(string url)
        {
            _url = url;
            IsAsync = true;
            IsStreaming = false;
        }

        public override async Task<string> ExecuteAsync()
        {
            try
            {
                // Simulate async web request
                await Task.Delay(2000);

                // Simulate different responses based on URL
                string result = _url.Contains("error") ?
                    throw new InvalidOperationException("Simulated network error") :
                    $"Response from {_url} at {DateTime.Now}";

                Result = result;
                return result;
            }
            catch (Exception ex)
            {
                Exception = ex;
                throw;
            }
        }
    }

    // 3. Streaming Job (Sync)
    public class FileProcessingJob : ThreadJob<string>
    {
        private readonly string[] _files;
        private readonly int _processingDelayMs;

        public FileProcessingJob(string[] files, int processingDelayMs = 500)
        {
            _files = files ?? throw new ArgumentNullException(nameof(files));
            _processingDelayMs = processingDelayMs;
            IsAsync = false;
            IsStreaming = true;
        }

        public override void ExecuteStreaming(Action<string> onProgress, Action onComplete = null)
        {
            try
            {
                for (int i = 0; i < _files.Length; i++)
                {
                    // Simulate file processing
                    Thread.Sleep(_processingDelayMs);

                    string processedFile = $"Processed: {_files[i]} ({i + 1}/{_files.Length})";
                    onProgress?.Invoke(processedFile);
                }

                Result = $"Completed processing {_files.Length} files";
                onComplete?.Invoke();
            }
            catch (Exception ex)
            {
                Exception = ex;
                throw;
            }
        }
    }

    // 4. Streaming Async Job
    public class AsyncBatchDownloadJob : ThreadJob<DownloadResult>
    {
        private readonly string[] _urls;
        private readonly int _delayMs;

        public AsyncBatchDownloadJob(string[] urls, int delayMs = 800)
        {
            _urls = urls ?? throw new ArgumentNullException(nameof(urls));
            _delayMs = delayMs;
            IsAsync = true;
            IsStreaming = true;
        }

        public override async Task ExecuteStreamingAsync(Action<DownloadResult> onProgress, Action onComplete = null)
        {
            try
            {
                for (int i = 0; i < _urls.Length; i++)
                {
                    // Simulate async download with progress
                    await Task.Delay(_delayMs);

                    var downloadResult = new DownloadResult
                    {
                        Url = _urls[i],
                        Status = _urls[i].Contains("fail") ? "Failed" : "Success",
                        Data = $"Data from {_urls[i]}",
                        Progress = (float)(i + 1) / _urls.Length,
                        CompletedAt = DateTime.Now
                    };

                    onProgress?.Invoke(downloadResult);
                }

                onComplete?.Invoke();
            }
            catch (Exception ex)
            {
                Exception = ex;
                throw;
            }
        }
    }

    // 5. Complex Streaming Job with Multiple Progress Types
    public class ComplexDataProcessingJob : ThreadJob<ProcessingUpdate>
    {
        private readonly int _totalItems;
        private readonly bool _shouldFail;

        public ComplexDataProcessingJob(int totalItems, bool shouldFail = false)
        {
            _totalItems = totalItems;
            _shouldFail = shouldFail;
            IsAsync = true;
            IsStreaming = true;
        }

        public override async Task ExecuteStreamingAsync(Action<ProcessingUpdate> onProgress, Action onComplete = null)
        {
            try
            {
                // Phase 1: Initialization
                onProgress?.Invoke(new ProcessingUpdate
                {
                    Phase = "Initialization",
                    Progress = 0f,
                    Message = "Starting process..."
                });

                await Task.Delay(500);

                // Phase 2: Processing
                for (int i = 0; i < _totalItems; i++)
                {
                    if (_shouldFail && i == _totalItems / 2)
                        throw new InvalidOperationException("Simulated processing error");

                    await Task.Delay(100);

                    onProgress?.Invoke(new ProcessingUpdate
                    {
                        Phase = "Processing",
                        Progress = (float)i / _totalItems,
                        Message = $"Processing item {i + 1} of {_totalItems}",
                        CurrentItem = i + 1,
                        TotalItems = _totalItems
                    });
                }

                // Phase 3: Finalization
                onProgress?.Invoke(new ProcessingUpdate
                {
                    Phase = "Finalization",
                    Progress = 1f,
                    Message = "Finalizing..."
                });

                await Task.Delay(300);

                onComplete?.Invoke();
            }
            catch (Exception ex)
            {
                Exception = ex;
                throw;
            }
        }
    }

    // Supporting data classes
    [System.Serializable]
    public class DownloadResult
    {
        public string Url;
        public string Status;
        public string Data;
        public float Progress;
        public DateTime CompletedAt;

        public override string ToString()
        {
            return $"{Status}: {Url} ({Progress:P0})";
        }
    }

    [System.Serializable]
    public class ProcessingUpdate
    {
        public string Phase;
        public float Progress;
        public string Message;
        public int CurrentItem;
        public int TotalItems;

        public override string ToString()
        {
            return $"[{Phase}] {Message} ({Progress:P0})";
        }
    }
}