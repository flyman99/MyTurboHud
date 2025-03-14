using System;

namespace Turbo.Plugins
{
    public interface ITextLogController
    {
        int ExceptionCount { get; }

        DateTime Log(string fileName, string text, bool appendTimeStamp = true, bool append = true);
        void LogParagonLevel();

        void Delete(string fileName);
    }
}