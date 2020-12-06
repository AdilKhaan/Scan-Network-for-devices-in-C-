using System;
using System.Collections.Generic; //to get list of tasks
using System.Net;
using System.Net.NetworkInformation;
using System.Threading.Tasks; //we gonna do some multiple threading
using System.Diagnostics; //using for stop watch timer
namespace MultiThreadedPing
{
    public class NetPinger
    {
        #region initialize 
        public string BaseIP = "192.168.0.";
        public int StartIP = 1;
        public int StopIP = 255;
        public int timeout = 100; //timeout for pinging
        public int nFound = 0; // keep trace of how many success ping
        
        static object lockObj = new object();
        Stopwatch stopWatch = new Stopwatch();
        public TimeSpan ts; // how long the ping took

        // Event Stuff
        // when we are done with all the ip address
        // we gonna generate this event and print result

        public event EventHandler<string> PingEvent;
        public string ip;
        public IPHostEntry host;
        public List<HostData> hosts = new List<HostData>();
        #endregion
        #region Ping Methods
        public async void RunPingSweep_Async()
        {
            // Ping'ing of each of the 255 IP addresses will be an individual 
            // task/thread and those Tasks will be stored in a List<Task>
            var tasks = new List<Task>();
            stopWatch.Restart();
            nFound = 0;

            for(int i=StartIP; i <=StopIP; i++)
            {
              
                //Construct the full IP address for each Task to ping
                ip = BaseIP + i.ToString();

                //Make a new Ping object for each IP address to be ping'ed
                Ping p = new Ping();
                var task = PingAndUpdateAsync(p, ip);
                tasks.Add(task);
            }
            await Task.WhenAll(tasks).ContinueWith(t =>
            {
                stopWatch.Stop();
                ts = stopWatch.Elapsed;
            });

            PingEvent?.Invoke(this, ts.ToString());
        
        }

        private async Task PingAndUpdateAsync(Ping ping, string ip)
        {
            //Do the actual Ping'ing to each IPaddress using System.Net.Ping.dll;
            //the "ConfigureAwait(false)" allows any thread other than the main UI
            //thread to continue the method when the SendPingAsync is done. this
            //frees the UI thread.
            var reply = await ping.SendPingAsync(ip, timeout).ConfigureAwait(false);

            if (reply.Status == IPStatus.Success)
            {
                //If a device ("host") was found, get its host properties (name, etc.)
                host = Dns.GetHostEntry(ip);
                hosts.Add(new HostData(host, ip));

                //Synchronizes access to the privat "nFound" field by 
                //locking on a dedicated "lockObj" instanc.This ensures
                //that the nFound field cannot be updated simultaneously
                //by two threads attempting to call the Ping methods
                //simultanously. Instead one will get access while the 
                //other waits its turn.

                lock (lockObj)
                {
                    nFound++;
                }




            }
        }
        #endregion

    }
}
