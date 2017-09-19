namespace Techmatic.ShortcutCommander {
    public class HotkeyRegistration {
        public HotkeyRegistration(string hotkey, string commandName) {
            Hotkey = hotkey;
            CommandName = commandName;
            UsageCount = 1;
        }

        public string Hotkey { get; set; }

        public string CommandName { get; set; }

        public long UsageCount { get; set; }

        public void IncreaseUsageCount() {
            UsageCount++;
        }
    }
}