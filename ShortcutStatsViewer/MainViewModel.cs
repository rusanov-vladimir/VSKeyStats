using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using Newtonsoft.Json;
using Registration;

namespace ShortcutStatsViewer {
    public class MainViewModel : ViewModelBase {
        public MainViewModel() {
            LoadedCommand = new RelayCommand(OnLoaded);
        }

        private readonly string _path = Path.Combine(Environment.GetFolderPath(
            Environment.SpecialFolder.MyDocuments), "ShortcutCommander", "statistics.dat");

        private List<HotkeyRegistration> _statistics;

        public RelayCommand LoadedCommand { get; set; }

        public List<HotkeyRegistration> Statistics {
            get {
                return _statistics;
            }
            set {
                _statistics = value;
                RaisePropertyChanged();
            }
        }

        private void OnLoaded() {
            var res = File.ReadAllText(_path);
            Statistics = JsonConvert.DeserializeObject<HashSet<HotkeyRegistration>>(res).OrderByDescending(x => x.UsageCount).ToList();
            var afterAnalysis = Analysis.Analyze(Statistics);
            Statistics = afterAnalysis;

        }
    }
}