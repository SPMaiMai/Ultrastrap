namespace Bloxstrap.Models.SettingTasks
{
    public class ShortcutTask : BoolBaseTask
    {
        private readonly string _shortcutPath;
        private readonly string? _startMenuPath;
        private readonly string _exeFlags;
        private readonly string _description;

        public ShortcutTask(string name, string lnkFolder, string lnkName, string exeFlags = "", bool alsoStartMenu = false) : base("Shortcut", name)
        {
            _shortcutPath = Path.Combine(lnkFolder, lnkName);
            _exeFlags = exeFlags;
            _description = Path.GetFileNameWithoutExtension(lnkName);

            if (alsoStartMenu)
                _startMenuPath = Path.Combine(Paths.WindowsStartMenuRoot, lnkName);

            OriginalState = File.Exists(_shortcutPath) || (_startMenuPath is not null && File.Exists(_startMenuPath));
        }

        public override void Execute()
        {
            if (NewState)
            {
                Shortcut.Create(Paths.Application, _exeFlags, _shortcutPath, _description);

                if (_startMenuPath is not null)
                    Shortcut.Create(Paths.Application, _exeFlags, _startMenuPath, _description);
            }
            else
            {
                Shortcut.Delete(_shortcutPath);

                if (_startMenuPath is not null)
                    Shortcut.Delete(_startMenuPath);
            }

            OriginalState = NewState;
        }
    }
}