using System.Collections.Generic;

namespace Turbo.Plugins
{
    public interface IPlayerAvoidInfo
    {
        List<IAvoid> AvoidsFar { get; }
        List<IAvoid> AvoidsFarOnScreen { get; }
        List<IAvoid> AvoidsNear { get; }

        bool HasAvoidFar(int moreThan = 0);
        bool HasAvoidFarOnScreen(int moreThan = 0);
        bool HasAvoidNear(int moreThan = 0);
        bool IsAvoidNear(AvoidWeight avoidWeight);
        bool IsAvoidNear(AvoidType avoidType);
        bool IsAvoidOnScreen(AvoidType avoidType);
    }
}