using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Collections.Concurrent;


namespace SCUtil
{

    public struct ProcessSettings
    {
        public string args;
        public string workingDirectory;

        public CMDProcess ToCMDProcess(Action<CMDProcess> onComplete = null)
        {
            return new CMDProcess(args, workingDirectory, onComplete);
        }

    }

    public class CMDProcess
    {
        public Thread t;

        string args;
        string workingDirectory;
        Action<CMDProcess> onComplete;

        public CMDProcess(string args, string workingDirectory, Action<CMDProcess> onComplete = null)
        {
            this.args = args;
            this.workingDirectory = workingDirectory;
            this.onComplete = onComplete;
        }

        public void run()
        {

            FileUtil.RunShellCMDFromPath(args, workingDirectory, false);
            if(onComplete != null)
            {
                onComplete(this);
            }
        }

        public Thread start()
        {
            Thread t = new Thread(new ThreadStart(run));
            t.Start();
            return t;
        }

        
    }

    public class CMDProcessPool
    {
        ConcurrentQueue<ProcessSettings> jobs = new ConcurrentQueue<ProcessSettings>();

        private int poolSize;

        public CMDProcessPool(int poolSize = 6)
        {
            this.poolSize = poolSize;
        }

        public void Add(ProcessSettings cp)
        {
            jobs.Enqueue(cp);
        }

        public void run()
        {
            if (jobs.Count == 0) return;

            Thread[] consumers = new Thread[poolSize];
            for(int i = 0; i < poolSize; ++i)
            {
                consumers[i] = new Thread(() =>
                {
                    while (true)
                    {
                        if(jobs.Count == 0)
                        {
                            break;
                        }
                        ProcessSettings ps;
                        if (jobs.TryDequeue(out ps))
                        {
                            CMDProcess cp = ps.ToCMDProcess();
                            Thread t = cp.start();
                            t.Join();
                        }

                    }

                });
                consumers[i].Start();

            }

            for(int i = 0; i < poolSize; ++i)
            {
                consumers[i].Join();
            }
        }

    }
}
