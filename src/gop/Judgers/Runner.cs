using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;

namespace gop.Judgers
{
    public enum RunnerState
    {
        Pending,
        Running,
        Ended,
        OutOfMemory,
        OutOfTime,
    }

    public class Runner : IDisposable
    {
        long MaximumPagedMemorySize64 { get; set; }

        long MaximumPeakPagedMemorySize64 { get; set; }

        ProcessStartInfo StartInfo { get; set; }

        public string[] Output { get; private set; }

        public string[] Error { get; private set; }

        public string Input { get; set; }

        public long? MemoryLimit { get; set; }

        public TimeSpan TimeLimit { get; set; }

        public long MaximumMemory => Math.Max(MaximumPagedMemorySize64, MaximumPeakPagedMemorySize64);

        public TimeSpan RunningTime => StartTime - EndTime;

        public DateTimeOffset StartTime { get; private set; }

        public DateTimeOffset EndTime { get; private set; }

        public int ExitCode { get; private set; }

        /// <summary>
        /// Gets if the app is running
        /// </summary>
        public RunnerState State { get; private set; }

        /// <summary>
        /// Gets the internal process
        /// </summary>
        public Process Process { get; private set; }

        BackgroundWorker bwMemory;

        public Runner(ProcessStartInfo startInfo)
        {
            StartInfo = startInfo;
            State = RunnerState.Pending;
        }

        public void Run()
        {
            StartInfo.UseShellExecute = false;
            StartInfo.RedirectStandardError = true;
            StartInfo.RedirectStandardInput = true;
            StartInfo.RedirectStandardOutput = true;

            Process = new Process
            {
                StartInfo = StartInfo
            };

            Process.EnableRaisingEvents = true;
            if (bwMemory != null) bwMemory.Dispose();
            bwMemory = new BackgroundWorker { WorkerSupportsCancellation = true };
            bwMemory.DoWork += (sender, e) =>
            {
                MaximumPagedMemorySize64 = MaximumPeakPagedMemorySize64 = 0;
                while (State == RunnerState.Running && !e.Cancel)
                {
                    try
                    {
                        MaximumPagedMemorySize64 = Math.Max(MaximumPagedMemorySize64, Process.PagedMemorySize64);
                        MaximumPeakPagedMemorySize64 = Math.Max(MaximumPeakPagedMemorySize64, Process.PeakPagedMemorySize64);
                        if (MemoryLimit.HasValue && MaximumMemory > MemoryLimit)
                        {
                            State = RunnerState.OutOfMemory;
                            Process.Kill();
                        }
                        Thread.Sleep(5);
                    }
                    catch
                    {

                    }
                }
            };
            Process.Exited += Process_Exited;

            State = RunnerState.Running;

            StartTime = DateTimeOffset.Now;

            Process.Start();
            Process.StandardInput.WriteLine(Input);
            Process.StandardInput.Close();

            bwMemory.RunWorkerAsync();

            if (Process.WaitForExit((int)Math.Ceiling(TimeLimit.TotalMilliseconds)))
            {
                State = RunnerState.Ended;
            }
            else
            {
                State = RunnerState.OutOfTime;
                Process.Kill();
                Process.WaitForExit();
            }
        }

        private void Process_Exited(object sender, EventArgs e)
        {
            try
            {
                ExitCode = Process.ExitCode;
                if (bwMemory?.IsBusy == true) bwMemory.CancelAsync();
                Process.WaitForExit();
                EndTime = DateTimeOffset.Now;

                var output = new List<string>();
                while (!Process.StandardOutput.EndOfStream)
                    output.Add(Process.StandardOutput.ReadLine());
                Output = output.ToArray();
                var error = new List<string>();
                while (!Process.StandardError.EndOfStream)
                    error.Add(Process.StandardError.ReadLine());
                Error = error.ToArray();
            }
            catch
            {

            }
        }


        /// <summary>
        /// Dispose resources
        /// </summary>
        public void Dispose()
        {
            ((IDisposable)bwMemory)?.Dispose();
            ((IDisposable)Process)?.Dispose();
        }
    }
}
