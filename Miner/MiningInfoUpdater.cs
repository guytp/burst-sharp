using Newtonsoft.Json;
using System;
using System.ComponentModel;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Guytp.BurstSharp.Miner
{
    /// <summary>
    /// Checks for updates to current mining state and stores the most recent information in the MiningInfo property.
    /// </summary>
    public class MiningInfoUpdater : IDisposable, INotifyPropertyChanged
    {
        #region Declarations
        /// <summary>
        /// Defines if the mine is alive.
        /// </summary>
        private bool _isAlive;

        /// <summary>
        /// Defines the thread updating mining info.
        /// </summary>
        private Thread _miningInfoThread;

        /// <summary>
        /// Defines the last JSON that was parsed.
        /// </summary>
        private string _lastJson;

        /// <summary>
        /// Defines the HTTP client we are using.
        /// </summary>
        private HttpClient _client;

        /// <summary>
        /// Defines the information about the current mining state of the network.
        /// </summary>
        private MiningInfo _miningInfo;
        #endregion

        #region Properties
        /// <summary>
        /// Gets the information about current mining state on the network.
        /// </summary>
        public MiningInfo MiningInfo
        {
            get { return _miningInfo; }
            set
            {
                if (_miningInfo == value)
                    return;
                _miningInfo = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("MiningInfo"));
            }
        }
        #endregion

        #region Events
        /// <summary>
        /// Fired whenever a property is updated on this object.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;
        #endregion

        #region Constructors
        /// <summary>
        /// Create a new instance of this class.
        /// </summary>
        public MiningInfoUpdater()
        {
            _client = new HttpClient();
            _client.Timeout = TimeSpan.FromSeconds(2);
            _client.BaseAddress = new Uri(Configuration.PoolApiUrl);
            _isAlive = true;
            _miningInfoThread = new Thread(MiningInfoThread) { Name = "Mining Info", IsBackground = true };
            _miningInfoThread.Start();
        }
        #endregion

        #region Thread Entry Points
        /// <summary>
        /// The main thread entry point that continually checks for changes to mining state in the background.
        /// </summary>
        private void MiningInfoThread()
        {
            while (_isAlive)
            {
                try
                {
                    string stringResponse;
#if STUB
                    if (string.IsNullOrEmpty(Configuration.StubJson))
                    {
#endif
                        HttpResponseMessage response = _client.GetAsync("/burst?requestType=getMiningInfo").Result;
                        response.EnsureSuccessStatusCode();
                        stringResponse = response.Content.ReadAsStringAsync().Result;
#if STUB
                    }
                    else
                        stringResponse = Configuration.StubJson;
#endif
                    if (stringResponse != _lastJson)
                    {
                        MiningInfo = JsonConvert.DeserializeObject<MiningInfo>(stringResponse);
                        _lastJson = stringResponse;
                    }
                }
                catch (AggregateException ex)
                {
                    if (ex.InnerExceptions.Count == 1 && ex.InnerException is TaskCanceledException)
                        Logger.Error("Timeout requesting current block status");
                    else
                        throw;
                }
                catch (Exception ex)
                {
                    Logger.Error("Failed to update mining info", ex);
                }
                Thread.Sleep(1000);
            }
        }
        #endregion

        #region IDisposable Implementation
        /// <summary>
        /// Free up any used resources.
        /// </summary>
        public void Dispose()
        {
            if (_isAlive)
            {
                // Wait for threads to terminate
                _isAlive = false;
                _miningInfoThread?.Join();
                _miningInfoThread = null;
            }
            if (_client != null)
            {
                _client.Dispose();
                _client = null;
            }
        }
        #endregion
    }
}