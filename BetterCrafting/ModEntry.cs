using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;
using System.Collections.Generic;
using static BetterCrafting.CategoryManager;

namespace BetterCrafting
{
    /// <summary>The mod entry class loaded by SMAPI.</summary>
    public class ModEntry : Mod
    {
        public static bool oldMenu = false;
        private CategoryData categoryData;
        public ItemCategory? lastCategory;

        /// <summary>The mod entry point, called after the mod is first loaded.</summary>
        /// <param name="helper">Provides simplified APIs for writing mods.</param>
        public override void Entry(IModHelper helper)
        {
            this.categoryData = this.Helper.Data.ReadJsonFile<CategoryData>("categories.json");
            if (this.categoryData == null)
            {
                this.Monitor.Log("Failed to read category data from 'categories.json'", LogLevel.Error);
                this.Monitor.Log("All crafting recipes will be in the 'Misc' tab", LogLevel.Warn);

                this.categoryData = new CategoryData();
            }

            helper.Events.Display.MenuChanged += OnMenuChanged;
            helper.Events.Player.InventoryChanged += OnInventoryChanged;
        }

        /// <summary>Raised after a game menu is opened, closed, or replaced.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
        private void OnMenuChanged(object sender, MenuChangedEventArgs e)
        {
            switch (e.NewMenu)
            {
                case null:
                    oldMenu = false;
                    break;

                case GameMenu gameMenu when !oldMenu:
                    {
                        var craftingTabNum = gameMenu.getTabNumberFromName("crafting");
                        var pages = this.Helper.Reflection.GetFieldValue<List<IClickableMenu>>(gameMenu, "pages");
                        pages[craftingTabNum] = new BetterCraftingPage(this, this.categoryData, this.lastCategory);
                        break;
                    }
            }
        }

        /// <summary>Raised after items are added or removed to a player's inventory.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
        private void OnInventoryChanged(object sender, InventoryChangedEventArgs e)
        {
            if (e.IsLocalPlayer && Game1.activeClickableMenu is GameMenu gameMenu && !oldMenu)
            {
                int craftingTabNum = gameMenu.getTabNumberFromName("crafting");
                if (gameMenu.currentTab == craftingTabNum)
                {
                    List<IClickableMenu> pages = this.Helper.Reflection.GetFieldValue<List<IClickableMenu>>(gameMenu, "pages");
                    ((BetterCraftingPage)pages[craftingTabNum]).UpdateInventory();
                }
            }
        }
    }
}