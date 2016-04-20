﻿using System;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Threading;

namespace Meridian59.Ogre.Patcher
{
    /// <summary>
    /// An instance of this runs in a dedicated thread and procsses items.
    /// </summary>
    public class Worker
    {      
        protected readonly ConcurrentQueue<PatchFile> queue;
        protected readonly Thread thread;
        protected readonly string baseFilePath;
        protected readonly string baseUrl;
        protected readonly WebClient webClient;
        protected readonly MD5 md5;
        protected readonly SynchronizationContext eventContext;

        protected volatile bool isDownloading;
        protected volatile bool IsRunning;

        public event EventHandler<PatchFile.EventArgs> FileFinishedOK;
        public event EventHandler<PatchFile.EventArgs> FileFinishedError;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="BaseFilePath"></param>
        /// <param name="BaseUrl"></param>
        /// <param name="InputQueue"></param>
        /// <param name="EventContext"></param>
        public Worker(
            string BaseFilePath, 
            string BaseUrl,
            ConcurrentQueue<PatchFile> InputQueue,
            SynchronizationContext EventContext)
        {
            // keep references
            baseFilePath = BaseFilePath;
            baseUrl = BaseUrl;
            queue = InputQueue;
            eventContext = EventContext;

            // get MD5 creator for this worker
            md5 = MD5.Create();
            md5.Initialize();

            // create webclient for downloads
            webClient = new WebClient();
            webClient.DownloadProgressChanged += OnWebClientDownloadProgressChanged;
            webClient.DownloadFileCompleted += OnWebClientDownloadFileCompleted;
            
            // create thread
            thread = new Thread(ThreadProc);
            thread.IsBackground = true;
        }

        /// <summary>
        /// Starts the workerthread and processes items from the InputQueue.
        /// </summary>
        public void Start()
        {
            if (IsRunning)
                return;

            IsRunning = true;
            thread.Start();      
        }

        /// <summary>
        /// Stops the workerthread.
        /// </summary>
        public void Stop()
        {
            IsRunning = false;
        }

        /// <summary>
        /// Internal thread loop
        /// </summary>
        protected void ThreadProc()
        {       
            PatchFile file;

            while (IsRunning)
            {
                // just sleep until async download is done
                if (isDownloading)          
                    Thread.Sleep(16);
               
                // try get next task
                else if (queue.TryDequeue(out file))
                {
                    // CASE 1: File on disk has equal hash, skip it
                    if (IsDiskFileEqual(file))
                    {
                        file.LengthDone = file.Length;
                        RaiseFileFinishedOK(file);
                    }

                    // CASE 2: File must be downloaded
                    else
                    {
                        // build full url and filepath
                        string fullUrl = baseUrl + file.Basepath + file.Filename;
                        string fullDir = baseFilePath + file.Basepath;
                        string fullFile = fullDir + file.Filename;

                        // possibly create directory structure
                        Directory.CreateDirectory(fullDir);

                        // start download it
                        isDownloading = true;
                        webClient.DownloadFileAsync(new Uri(fullUrl), fullFile, file);
                    }
                }

                // not downloading and no tasks
                else
                    Thread.Sleep(100);
            }

            // make sure to cancel async tasks
            webClient.CancelAsync();
        }

        /// <summary>
        /// Compares the MD5 hash of the file on disk and the parameter.
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        protected bool IsDiskFileEqual(PatchFile file)
        {
            // build expected path on disk
            string filePath = baseFilePath + file.Basepath + file.Filename;

            // not equal if not existant
            if (!File.Exists(filePath))
                return false;

            // create filestream
            FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read);

            // compute and compare md5
            byte[] md5Fil = md5.ComputeHash(fs);
            byte[] md5Onl = StringToByteArray(file.MyHash);
            bool areEqual = md5Fil.SequenceEqual<byte>(md5Onl);

            // close filestream
            fs.Close();
            fs.Dispose();

            return areEqual; 
        }

        /// <summary>
        /// Raises FileFinishedOK event in other thread.
        /// </summary>
        /// <param name="File"></param>
        protected void RaiseFileFinishedOK(PatchFile File)
        {
            eventContext.Post(new SendOrPostCallback((o) =>
            {
                if (FileFinishedOK != null)
                    FileFinishedOK(this, new PatchFile.EventArgs(File));

            }), null);
        }

        /// <summary>
        /// Raises FileFinishedError in other thread.
        /// </summary>
        /// <param name="File"></param>
        protected void RaiseFileFinishedError(PatchFile File)
        {
            eventContext.Post(new SendOrPostCallback((o) =>
            {
                if (FileFinishedError != null)
                    FileFinishedError(this, new PatchFile.EventArgs(File));

            }), null);
        }

        /// <summary>
        /// Raised by the WebClient object when download progresses.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void OnWebClientDownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            if (e.UserState is PatchFile)
            {
                PatchFile f  = (PatchFile)e.UserState;
                f.LengthDone = e.BytesReceived;
            }           
        }

        /// <summary>
        /// Raised by the WebClient object when download completed (successful or not).
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void OnWebClientDownloadFileCompleted(object sender, AsyncCompletedEventArgs e)
        {
            isDownloading = false;

            if (e.UserState is PatchFile)
            {
                PatchFile f = (PatchFile)e.UserState;

                if (e.Error != null)
                    RaiseFileFinishedError(f);

                else
                    RaiseFileFinishedOK(f);
            }            
        }

        /// <summary>
        /// Helper function to convert a hexadecimal string into a byte array.
        /// </summary>
        /// <param name="hex"></param>
        /// <returns></returns>
        protected static byte[] StringToByteArray(string hex)
        {
            return Enumerable.Range(0, hex.Length)
                             .Where(x => x % 2 == 0)
                             .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                             .ToArray();
        }
    }
}
