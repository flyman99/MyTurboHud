using System;
using System.Collections.Generic;

namespace Turbo.Plugins
{
    public interface IController
    {
        ITextureController Texture { get; }
        IRenderController Render { get; }
        ISceneRevealController SceneReveal { get; }
        IStatController Stat { get; }
        IInventoryController Inventory { get; }
        IQueueController Queue { get; }
        IInputController Input { get; }
        IWindow Window { get; }
        IGameController Game { get; }
        ISnoController Sno { get; }
        ISoundController Sound { get; }
        ITrackerController Tracker { get; }
        ITextLogController TextLog { get; }
        ITimeController Time { get; }

        string MyBattleTag { get; }
        IEnumerable<IHero> AccountHeroes { get; }

        T GetPlugin<T>() where T : class, IPlugin;
        void RunOnPlugin<T>(Action<T> action) where T : class, IPlugin;
        void TogglePlugin<T>(bool enabled) where T : class, IPlugin;
        IEnumerable<IPlugin> AllPlugins { get; }

        void Debug(string text);

        IInteractionController Interaction { get; }
        IAvoidanceController Avoidance { get; }

        void Wait(int interval, int randomOffset = -1);
        bool WaitFor(int timeout, int sleepBetweenTests, int stabilityTimeout, Func<bool> func);

        void ReCollect();
        Language CurrentLanguage { get; }
    }
}