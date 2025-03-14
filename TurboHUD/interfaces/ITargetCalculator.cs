namespace Turbo.Plugins
{
    public interface ITargetCalculator
    {
        bool MonsterIsTarget(IMonster monster);
        double CalculateMonsterPriority(IMonster monster);
    }
}
