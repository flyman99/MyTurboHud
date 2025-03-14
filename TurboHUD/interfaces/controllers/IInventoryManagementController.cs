namespace Turbo.Plugins
{
    public interface IInventoryManagementController
    {
        bool Working { get; }
        IWatch LastInventoryCleanupOn { get; }

        ActionResult InventoryCleanup(int timeout, bool close = true);

        bool DropLegendaryPotionsEnabled { get; set; } // default false
        ActionResult DropLegendaryPotions(int timeout = 60000);
    }
}
