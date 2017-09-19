using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Input;
using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Newtonsoft.Json;
using Debugger = System.Diagnostics.Debugger;

namespace Techmatic.ShortcutCommander {
    /// <summary>
    ///     This is the class that implements the package exposed by this assembly.
    ///     The minimum requirement for a class to be considered a valid package for Visual Studio
    ///     is to implement the IVsPackage interface and register itself with the shell.
    ///     This package uses the helper classes defined inside the Managed Package Framework (MPF)
    ///     to do it: it derives from the Package class that provides the implementation of the
    ///     IVsPackage interface and uses the registration attributes defined in the framework to
    ///     register itself and its components with the shell.
    /// </summary>
    // This attribute tells the PkgDef creation utility (CreatePkgDef.exe) that this class is
    // a package.
    [PackageRegistration(UseManagedResourcesOnly = true)]
    // This attribute is used to register the information needed to show this package
    // in the Help/About dialog of Visual Studio.
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)]
    [Guid(GuidList.guidShortcutCommanderPkgString)]
    [ProvideAutoLoad("ADFC4E64-0397-11D1-9F4E-00A0C911004F")]
    public sealed class ShortcutCommanderPackage : Package {
        /// <summary>
        ///     Default constructor of the package.
        ///     Inside this method you can place any initialization code that does not require
        ///     any Visual Studio service because at this point the package object is created but
        ///     not sited yet inside Visual Studio environment. The place to do all the other
        ///     initialization is the Initialize method.
        /// </summary>
        public ShortcutCommanderPackage() {
            Debug.WriteLine(string.Format(CultureInfo.CurrentCulture, "Entering constructor for: {0}", ToString()));
        }

        [ContextStatic] private static CommandEvents commandEvents;
        [ContextStatic] private DTEEvents dteEvents;

        [ContextStatic] private HashSet<HotkeyRegistration> _statistics = new HashSet<HotkeyRegistration>();

        /// <summary>
        ///     Borrowed from Mads Kristensen's wonderful Visual Studio Shortcuts tool (http://visualstudioshortcuts.com).
        /// </summary>
        /// <param name="bindings"></param>
        /// <returns></returns>
        private static string[] GetBindings(IEnumerable<object> bindings) {
            var result = bindings.Select(binding => binding.ToString().IndexOf("::") >= 0
                                             ? binding.ToString().Substring(binding.ToString().IndexOf("::") + 2)
                                             : binding.ToString()).Distinct();

            return result.ToArray();
        }

        /////////////////////////////////////////////////////////////////////////////
        // Overridden Package Implementation

        #region Package Members

        /// <summary>
        ///     Initialization of the package; this method is called right after the package is sited, so this is the place
        ///     where you can put all the initialization code that rely on services provided by VisualStudio.
        /// </summary>
        protected override void Initialize() {
            Debug.WriteLine(string.Format(CultureInfo.CurrentCulture, "Entering Initialize() of: {0}", ToString()));
            base.Initialize();

            ManageStatistics();
            

            var m_objDTE = (DTE) GetService(typeof(DTE));
            commandEvents = m_objDTE.Events.CommandEvents;
            dteEvents = m_objDTE.Events.DTEEvents;
            dteEvents.OnBeginShutdown += DTEEvents_OnBeginShutdown;

            commandEvents.BeforeExecute += delegate(string Guid, int ID, object CustomIn, object CustomOut, ref bool CancelDefault) {
                                               OnCommand(m_objDTE, Guid, ID);
                                           };
        }

        protected override void Dispose(bool disposing) {
            _t.Dispose();
            _timerDisposed = true;
            base.Dispose(disposing);
        }

        readonly Stopwatch _sw = new Stopwatch();
        private void OnCommand(DTE m_objDTE, string Guid, int ID) {
            
            _sw.Start();
            var objCommand = m_objDTE.Commands.Item(Guid, ID);
            var bindings = objCommand?.Bindings as object[];
            if (bindings != null && bindings.Any()) {
                var shortcuts = GetBindings(bindings);
                if (shortcuts.Any())
                    lock (typeof(ShortcutCommanderPackage)) {
                        foreach (var shortcut in shortcuts) {
                            /*var shortcutSequences = shortcut.Split(new[] {", "}, StringSplitOptions.RemoveEmptyEntries);
                            var hotkeys = new List<Key>();
                            foreach (var shortcutSequence in shortcutSequences) {
                                var shortcutKeys = shortcutSequence.Split('+');
                                foreach (var shortcutKey in shortcutKeys) {
                                    var key = GetKey(shortcutKey);
                                    hotkeys.Add(key);
                                }
                            }
                            var hotkeyWasPressed = true;
                            foreach (var hotkey in hotkeys) {
                                hotkeyWasPressed = Keyboard.IsKeyDown(hotkey) && hotkeyWasPressed;
                            }
                            if (hotkeyWasPressed) */
                            UpdateStatistics(shortcut, objCommand.LocalizedName);
                        }
                    }
            }
            _sw.Stop();
        }

        private static Key GetKey(string shortcutKey) {
            Key key;
            switch (shortcutKey) {
                case "Ctrl":
                    key = Key.LeftCtrl;
                    break;

                case "Alt":
                    key = Key.LeftAlt;
                    break;

                case "Shift":
                    key = Key.LeftShift;
                    break;

                case "Bkspce":
                    key = Key.Back;
                    break;

                case "Del":
                    key = Key.Delete;
                    break;

                case "Ins":
                    key = Key.Insert;
                    break;

                case "PgDn":
                    key = Key.PageDown;
                    break;

                case "PgUp":
                    key = Key.PageUp;
                    break;

                case "Down Arrow":
                    key = Key.Down;
                    break;

                case "Up Arrow":
                    key = Key.Up;
                    break;

                case "Left Arrow":
                    key = Key.Left;
                    break;

                case "Right Arrow":
                    key = Key.Right;
                    break;

                case "Esc":
                    key = Key.Escape;
                    break;

                case "`":
                    key = Key.Oem3;
                    break;

                default:
                    if (!Enum.TryParse(shortcutKey, out key)) {
                        if (Debugger.IsAttached) {
                            Debugger.Break();
                        }
                    }
                    break;
            }
            return key;
        }

        private void DTEEvents_OnBeginShutdown() {
            SaveStatistics();
        }

        private void UpdateStatistics(string shortcut, string commandName) {
            var existingStatistic = _statistics.FirstOrDefault(x => x.Hotkey == shortcut);
            if (existingStatistic == null) {
                var newHotkey = new HotkeyRegistration(shortcut, commandName);
                _statistics.Add(newHotkey);
            }
            else {
                existingStatistic.IncreaseUsageCount();
            }
        }

        private readonly string _path = Path.Combine(Environment.GetFolderPath(
            Environment.SpecialFolder.MyDocuments), "ShortcutCommander", "statistics.dat");

        private Timer _t;
        private bool _timerDisposed;

        private const int SaveIntervalInSeconds = 300;

        private void ManageStatistics() {
            if (File.Exists(_path)) {
                LoadStatistics();
            }
            else {
                Directory.CreateDirectory(Path.GetDirectoryName(_path));
                File.Create(_path).Dispose();
            }
            _t = new Timer(ScheduleNextSave, null, 0, -1);
        }

        private void ScheduleNextSave(object state) {
            SaveStatistics();
            if (_timerDisposed)
                return;
            _t.Change((long)TimeSpan.FromSeconds(SaveIntervalInSeconds).TotalMilliseconds, -1);
        }

        private void LoadStatistics() {
            var res = File.ReadAllText(_path);
            _statistics = JsonConvert.DeserializeObject<HashSet<HotkeyRegistration>>(res);
        }

        private void SaveStatistics() {
            var res = JsonConvert.SerializeObject(_statistics);
            File.WriteAllText(_path, res);
        }

        #endregion
    }
}