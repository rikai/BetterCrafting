using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewValley;
using StardewValley.Menus;
using StardewValley.Objects;
using StardewModdingAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using static BetterCrafting.CategoryManager;

namespace BetterCrafting
{
    internal class BetterCraftingPage : CraftingPage
    {
        private const int WIDTH = 800;
        private const string AVAILABLE = "a";
        private const string UNAVAILABLE = "u";
        private const string UNKNOWN = "k";
        private const int ROWS = 2;

        private int pageX;
        private int pageY;

        private ModEntry betterCrafting;

        private CategoryManager categoryManager;

        private Dictionary<ItemCategory, List<Dictionary<ClickableTextureComponent, CraftingRecipe>>> recipes;
        private Dictionary<ClickableComponent, ItemCategory> categories;

        private ClickableComponent[] selectables;

        private ItemCategory selectedCategory;
        private int recipePage;

        private ClickableComponent throwComp;

        private ClickableTextureComponent oldButton;

        private CraftingRecipe hoverRecipe;

        private string hoverTitle;
        private string hoverText;
        private Item heldItem;
        private Item hoverItem;

        private string categoryText;

        private int maxItemsInRow;
        private int totalIconSize;

        private int snappedId = 0;
        private int snappedSection = 1;

        public ClickableTextureComponent junimoNoteIcon;

        public new List<Chest> _materialContainers;

        public BetterCraftingPage(ModEntry betterCrafting, CategoryData categoryData, Nullable<ItemCategory> defaultCategory, List<Chest>material_containers = null)
            : base(Game1.activeClickableMenu.xPositionOnScreen, Game1.activeClickableMenu.yPositionOnScreen, Game1.activeClickableMenu.width, Game1.activeClickableMenu.height)
        {
            _materialContainers = material_containers;
            _ = _materialContainers;

            this.betterCrafting = betterCrafting;
            this.categoryManager = new CategoryManager(betterCrafting.Monitor, categoryData);

            this.inventory = new InventoryMenu(this.xPositionOnScreen + IClickableMenu.spaceToClearSideBorder + IClickableMenu.borderWidth,
                   this.yPositionOnScreen + IClickableMenu.spaceToClearTopBorder + IClickableMenu.borderWidth + Game1.tileSize * 5 - Game1.tileSize / 4,
                false);
            this.inventory.showGrayedOutSlots = true;

            this.pageX = this.xPositionOnScreen + IClickableMenu.spaceToClearSideBorder + IClickableMenu.borderWidth - Game1.tileSize / 4;
            this.pageY = this.yPositionOnScreen + IClickableMenu.spaceToClearTopBorder + IClickableMenu.borderWidth - Game1.tileSize / 4;

            if (defaultCategory.HasValue)
            {
                this.selectedCategory = defaultCategory.Value;
            }
            else
            {
                this.selectedCategory = this.categoryManager.GetDefaultItemCategory();
            }
            this.recipePage = 0;

            this.recipes = new Dictionary<ItemCategory, List<Dictionary<ClickableTextureComponent, CraftingRecipe>>>();
            this.categories = new Dictionary<ClickableComponent, ItemCategory>();

            int catIndex = 0;

            var categorySpacing = Game1.tileSize / 6;
            var tabPad = Game1.tileSize + Game1.tileSize / 4;

            int id = 0;

            foreach (ItemCategory category in this.categoryManager.GetItemCategories())
            {
                this.recipes.Add(category, new List<Dictionary<ClickableTextureComponent, CraftingRecipe>>());

                var catName = this.categoryManager.GetItemCategoryName(category);

                var nameSize = Game1.smallFont.MeasureString(catName);

                var width = nameSize.X + Game1.tileSize / 2;
                var height = nameSize.Y + Game1.tileSize / 4;

                var x = this.xPositionOnScreen - width;
                var y = this.yPositionOnScreen + tabPad + catIndex * (height + categorySpacing);

                var c = new ClickableComponent(
                    new Rectangle((int)x, (int)y, (int)width, (int)height),
                    category.Equals(this.selectedCategory) ? UNAVAILABLE : AVAILABLE, catName);
                c.myID = id;
                c.upNeighborID = id - 1;
                c.downNeighborID = id + 1;

                this.categories.Add(c, category);

                catIndex += 1;
                id += 1;
            }

            if (ShouldShowJunimoNoteIcon())
            {
                junimoNoteIcon = new ClickableTextureComponent("", new Rectangle(xPositionOnScreen + width, yPositionOnScreen + 96, 64, 64), "", Game1.content.LoadString("Strings\\UI:GameMenu_JunimoNote_Hover"), Game1.mouseCursors, new Rectangle(331, 374, 15, 14), 4f)
                {
                    myID = 898,
                    leftNeighborID = 11,
                    downNeighborID = 106
                };
            }

            this.trashCan = new ClickableTextureComponent(
                new Rectangle(
                    this.xPositionOnScreen + width + 4,
                    this.yPositionOnScreen + height - Game1.tileSize * 3 - Game1.tileSize / 2 - IClickableMenu.borderWidth - 104,
                    Game1.tileSize, 104),
                Game1.mouseCursors,
                new Rectangle(669, 261, 16, 26),
                Game1.pixelZoom, false);

            this.throwComp = new ClickableComponent(
                new Rectangle(
                    this.xPositionOnScreen + width + 4,
                    this.yPositionOnScreen + height - Game1.tileSize * 3 - IClickableMenu.borderWidth,
                    Game1.tileSize, Game1.tileSize),
                "");

            this.oldButton = new ClickableTextureComponent("",
                new Rectangle(
                    this.xPositionOnScreen + width,
                    this.yPositionOnScreen + height / 3 - Game1.tileSize + Game1.pixelZoom * 2,
                    Game1.tileSize,
                    Game1.tileSize),
                "",
                "Old Crafting Menu",
                Game1.mouseCursors,
                new Rectangle(162, 440, 16, 16),
                Game1.pixelZoom, false);
        }

        public static bool ShouldShowJunimoNoteIcon()
        {
            if (Game1.player.hasOrWillReceiveMail("canReadJunimoText") && !Game1.player.hasOrWillReceiveMail("JojaMember"))
            {
                if (Game1.MasterPlayer.hasCompletedCommunityCenter())
                {
                    if (Game1.player.hasOrWillReceiveMail("hasSeenAbandonedJunimoNote"))
                    {
                        return !Game1.MasterPlayer.hasOrWillReceiveMail("ccMovieTheater");
                    }
                    return false;
                }
                return true;
            }
            return false;
        }

        public void UpdateInventory()
        {
            foreach (var category in this.recipes.Keys)
            {
                this.recipes[category].Clear();
                this.recipes[category].Add(new Dictionary<ClickableTextureComponent, CraftingRecipe>());
            }

            var indexMap = new Dictionary<ItemCategory, int>();
            var pageMap = new Dictionary<ItemCategory, int>();

            var spaceBetweenCraftingIcons = Game1.tileSize / 4;
            this.totalIconSize = Game1.tileSize + spaceBetweenCraftingIcons;
            this.maxItemsInRow = (this.width - IClickableMenu.spaceToClearSideBorder - IClickableMenu.borderWidth) / this.totalIconSize - 1;
            var xPad = Game1.tileSize / 8;

            this.selectables = new ClickableComponent[maxItemsInRow * ROWS];

            int id = 200;

            for (int row = 0; row < ROWS; row++)
            {
                for (int column = 0; column < maxItemsInRow; column++)
                {
                    var x = this.pageX + xPad + column * (Game1.tileSize + spaceBetweenCraftingIcons);
                    var y = this.pageY + row * (Game1.tileSize * 2 + spaceBetweenCraftingIcons);

                    var c = new ClickableComponent(new Rectangle(x, y, Game1.tileSize, Game1.tileSize), "");
                    c.myID = id;
                    c.upNeighborID = id - maxItemsInRow;
                    c.rightNeighborID = id + 1;
                    c.downNeighborID = id + maxItemsInRow;
                    c.leftNeighborID = id - 1;

                    this.selectables[column + row * maxItemsInRow] = c;

                    id += 1;
                }
            }

            foreach (var recipeName in CraftingRecipe.craftingRecipes.Keys)
            {
                var recipe = new CraftingRecipe(recipeName, false);
                var item = recipe.createItem();

                var category = this.categoryManager.GetItemCategory(item);

                if (!indexMap.ContainsKey(category))
                {
                    indexMap.Add(category, 0);
                }

                if (!pageMap.ContainsKey(category))
                {
                    pageMap.Add(category, 0);
                }

                if (indexMap[category] >= maxItemsInRow * ROWS)
                {
                    pageMap[category] += 1;
                    indexMap[category] = 0;

                    this.recipes[category].Add(new Dictionary<ClickableTextureComponent, CraftingRecipe>());
                }

                var column = indexMap[category] % maxItemsInRow;
                var row = indexMap[category] / maxItemsInRow;

                var x = this.pageX + xPad + column * (Game1.tileSize + spaceBetweenCraftingIcons);
                var y = this.pageY + row * (Game1.tileSize * 2 + spaceBetweenCraftingIcons);

                var hoverText = Game1.player.craftingRecipes.ContainsKey(recipeName) ? (
                    recipe.doesFarmerHaveIngredientsInInventory() ? AVAILABLE : UNAVAILABLE)
                    : UNKNOWN;

                var c = new ClickableTextureComponent("",
                    new Rectangle(x, y, Game1.tileSize, recipe.bigCraftable ? (Game1.tileSize * 2) : Game1.tileSize),
                    null,
                    hoverText,
                    recipe.bigCraftable ? Game1.bigCraftableSpriteSheet : Game1.objectSpriteSheet,
                    recipe.bigCraftable ? Game1.getArbitrarySourceRect(
                        Game1.bigCraftableSpriteSheet,
                        16, 32,
                        recipe.getIndexOfMenuView())
                    : Game1.getSourceRectForStandardTileSheet(
                        Game1.objectSpriteSheet,
                        recipe.getIndexOfMenuView(), 16, 16),
                    Game1.pixelZoom,
                    false);

                this.recipes[category][pageMap[category]].Add(c, recipe);

                indexMap[category] += 1;
            }

            this.UpdateScrollButtons();
        }

        private void UpdateScrollButtons()
        {
            this.upButton = null;
            this.downButton = null;

            if (this.recipePage > 0)
            {
                this.upButton = new ClickableTextureComponent(
                    new Rectangle(
                        this.xPositionOnScreen + this.maxItemsInRow * this.totalIconSize + Game1.tileSize,
                        this.pageY,
                        Game1.tileSize,
                        Game1.tileSize),
                    Game1.mouseCursors,
                    Game1.getSourceRectForStandardTileSheet(Game1.mouseCursors, 12, -1, -1),
                    0.8f);
            }

            if (this.recipePage < this.recipes[this.selectedCategory].Count - 1)
            {
                this.downButton = new ClickableTextureComponent(
                    new Rectangle(
                        this.xPositionOnScreen + this.maxItemsInRow * this.totalIconSize + Game1.tileSize,
                        this.pageY + Game1.tileSize * 3 + Game1.tileSize / 2,
                        Game1.tileSize,
                        Game1.tileSize),
                    Game1.mouseCursors,
                    Game1.getSourceRectForStandardTileSheet(Game1.mouseCursors, 11, -1, -1),
                    0.8f);
            }
        }

        private Dictionary<ClickableTextureComponent, CraftingRecipe> GetCurrentPage()
        {
            var craftingPages = this.recipes[this.selectedCategory];
            if (this.recipePage >= craftingPages.Count || this.recipePage < 0)
            {
                this.recipePage = 0;
            }

            return craftingPages[this.recipePage];
        }

        private void SetCategory(ClickableComponent c)
        {
            if (!this.categories.Keys.Contains(c))
            {
                return;
            }

            if (!this.selectedCategory.Equals(this.categories[c]))
            {
                Game1.playSound("smallSelect");
            }

            foreach (var c2 in this.categories.Keys)
            {
                c2.name = AVAILABLE;
            }

            c.name = UNAVAILABLE;

            this.selectedCategory = this.categories[c];
            this.betterCrafting.lastCategory = this.selectedCategory;

            this.recipePage = 0;
            this.UpdateScrollButtons();
        }

        private void ScrollUp()
        {
            if (this.recipePage <= 0)
            {
                foreach (var c in this.categories.Keys)
                {
                    if (this.categories[c].Equals(this.selectedCategory))
                    {
                        var id = c.myID;
                        if (id > 0)
                        {
                            this.SetCategory(this.categories.Keys.ToArray()[id - 1]);
                        }
                        break;
                    }
                }
            }
            else
            {
                this.recipePage -= 1;

                Game1.playSound("shwip");
            }

            this.UpdateScrollButtons();
        }

        private void ScrollDown()
        {
            if (this.recipePage >= this.recipes[this.selectedCategory].Count - 1)
            {
                foreach (var c in this.categories.Keys)
                {
                    if (this.categories[c].Equals(this.selectedCategory))
                    {
                        var id = c.myID;
                        if (id < this.categories.Count - 1)
                        {
                            this.SetCategory(this.categories.Keys.ToArray()[id + 1]);
                        }
                        break;
                    }
                }
            }
            else
            {
                this.recipePage += 1;

                Game1.playSound("shwip");
            }

            this.UpdateScrollButtons();
        }

        public override void applyMovementKey(int direction)
        {
            Game1.playSound("shiny4");

            if (this.snappedSection == 0)
            {
                this.currentlySnappedComponent = this.categories.Keys.ToArray()[snappedId];

                if (direction == 0)
                {
                    if (this.snappedId > 0)
                    {
                        this.snappedId -= 1;
                        this.currentlySnappedComponent = this.categories.Keys.ToArray()[snappedId];
                    }
                    else
                    {
                        this.snappedId = GameMenu.craftingTab;
                        this.snappedSection = 2;

                        var gameMenu = (GameMenu)Game1.activeClickableMenu;
                        var tabs = this.betterCrafting.Helper.Reflection.GetFieldValue<List<ClickableComponent>>(gameMenu, "tabs");
                        Game1.setMousePosition(tabs[this.snappedId].bounds.Center);
                        return;
                    }
                }
                else if (direction == 1)
                {
                    if (this.snappedId <= this.categories.Keys.Count / 2)
                    {
                        this.snappedSection = 1;
                        this.snappedId = 0;
                        this.currentlySnappedComponent = this.selectables[snappedId];
                    }
                    else
                    {
                        this.snappedSection = 3;
                        this.snappedId = 0;
                        this.inventory.currentlySnappedComponent = this.inventory.inventory[0];
                        this.inventory.snapCursorToCurrentSnappedComponent();
                        return;
                    }
                }
                else if (direction == 2)
                {
                    if (this.snappedId < this.categories.Keys.Count - 1)
                    {
                        this.snappedId += 1;
                        this.currentlySnappedComponent = this.categories.Keys.ToArray()[snappedId];
                    }
                }
            }
            else if (this.snappedSection == 1)
            {
                var column = this.snappedId % this.maxItemsInRow;
                var row = this.snappedId / this.maxItemsInRow;

                if (direction == 0)
                {
                    if (row > 0)
                    {
                        this.snappedId -= this.maxItemsInRow;
                        this.currentlySnappedComponent = this.selectables[column + (row - 1) * this.maxItemsInRow];
                    }
                    else
                    {
                        this.snappedId = GameMenu.craftingTab;
                        this.snappedSection = 2;
                        var gameMenu = (GameMenu)Game1.activeClickableMenu;
                        var tabs = this.betterCrafting.Helper.Reflection.GetFieldValue<List<ClickableComponent>>(gameMenu, "tabs");
                        Game1.setMousePosition(tabs[this.snappedId].bounds.Center);
                        return;
                    }
                }
                else if (direction == 1)
                {
                    if (column < this.maxItemsInRow - 1)
                    {
                        this.snappedId += 1;
                        this.currentlySnappedComponent = this.selectables[column + 1 + row * this.maxItemsInRow];
                    }
                    else
                    {
                        if (row == 0)
                        {
                            if (this.upButton == null)
                            {
                                this.snappedId = 1;
                                this.snappedSection = 4;
                                this.applyMovementKey(0);
                            }
                            else
                            {
                                this.snappedSection = 5;
                                this.snappedId = 1;
                                this.applyMovementKey(0);
                            }
                        }
                        else
                        {
                            if (this.downButton == null)
                            {
                                this.snappedId = 1;
                                this.snappedSection = 4;
                                this.applyMovementKey(2);
                            }
                            else
                            {
                                this.snappedSection = 5;
                                this.snappedId = 0;
                                this.applyMovementKey(2);
                            }
                        }
                    }
                }
                else if (direction == 2)
                {
                    if (row < 1)
                    {
                        this.snappedId += this.maxItemsInRow;
                        this.currentlySnappedComponent = this.selectables[column + (row + 1) * this.maxItemsInRow];
                    }
                    else
                    {
                        this.snappedSection = 3;
                        this.snappedId = 0;
                        this.inventory.currentlySnappedComponent = this.inventory.inventory[column];
                        this.inventory.snapCursorToCurrentSnappedComponent();
                        return;
                    }
                }
                else if (direction == 3)
                {
                    if (column > 0)
                    {
                        this.snappedId -= 1;
                        this.currentlySnappedComponent = this.selectables[column - 1 + row * this.maxItemsInRow];
                    }
                    else
                    {
                        this.snappedSection = 0;
                        this.snappedId = 0;
                        this.currentlySnappedComponent = this.categories.Keys.First();
                    }
                }
            }
            else if (this.snappedSection == 2)
            {
                this.currentlySnappedComponent = null;

                if (direction == 1)
                {
                    var gameMenu = (GameMenu)Game1.activeClickableMenu;
                    var tabs = this.betterCrafting.Helper.Reflection.GetFieldValue<List<ClickableComponent>>(gameMenu, "tabs");

                    if (this.snappedId < tabs.Count - 1)
                    {
                        this.snappedId += 1;
                        Game1.setMousePosition(tabs[this.snappedId].bounds.Center);
                    }

                    return;
                }
                else if (direction == 2)
                {
                    this.snapToDefaultClickableComponent();
                }
                else if (direction == 3)
                {
                    if (this.snappedId > 0)
                    {
                        this.snappedId -= 1;

                        var gameMenu = (GameMenu)Game1.activeClickableMenu;
                        var tabs = this.betterCrafting.Helper.Reflection.GetFieldValue<List<ClickableComponent>>(gameMenu, "tabs");
                        Game1.setMousePosition(tabs[this.snappedId].bounds.Center);
                    }

                    return;
                }
            }
            else if (snappedSection == 3)
            {
                if (direction == 0 && this.inventory.currentlySnappedComponent.myID < this.inventory.capacity / this.inventory.rows)
                {
                    if (this.inventory.currentlySnappedComponent.myID == this.inventory.capacity / this.inventory.rows - 1)
                    {
                        this.snappedSection = 5;
                        this.snappedId = 0;
                        this.applyMovementKey(2);
                    }
                    else
                    {
                        this.snappedSection = 1;
                        this.snappedId = Math.Min(Math.Max(1, this.inventory.currentlySnappedComponent.myID), this.maxItemsInRow - 1) + this.maxItemsInRow;
                        this.applyMovementKey(3);
                    }
                }
                else if (direction == 1 && (this.inventory.currentlySnappedComponent.myID + 1) % (this.inventory.capacity / this.inventory.rows) == 0)
                {
                    this.snappedSection = 4;
                    this.snappedId = 2;
                    this.applyMovementKey(2);
                }
                else if (direction == 3 && (this.inventory.currentlySnappedComponent.myID) % (this.inventory.capacity / this.inventory.rows) == 0)
                {
                    this.snappedId = this.categories.Keys.Count - 2;
                    this.snappedSection = 0;
                    this.applyMovementKey(2);
                }
                else
                {
                    this.inventory.applyMovementKey(direction);
                }

                return;
            }
            else if (snappedSection == 4)
            {
                if (direction == 0 && snappedId > 0)
                {
                    this.snappedId -= 1;

                    if (snappedId == 0)
                    {
                        var gameMenu = (GameMenu)Game1.activeClickableMenu;
                        if (junimoNoteIcon != null)
                        {
                            this.currentlySnappedComponent = junimoNoteIcon;
                        }
                        else
                        {
                            this.snappedId = 2;
                            this.applyMovementKey(0);
                        }
                    }
                    else if (snappedId == 1)
                    {
                        this.currentlySnappedComponent = this.oldButton;
                    }
                    else if (snappedId == 2)
                    {
                        this.currentlySnappedComponent = this.trashCan;
                    }
                }
                else if (direction == 2 && snappedId < 3)
                {
                    this.snappedId += 1;
                    if (snappedId == 1)
                    {
                        this.currentlySnappedComponent = this.oldButton;
                    }
                    else if (snappedId == 2)
                    {
                        this.currentlySnappedComponent = this.trashCan;
                    }
                    else if (snappedId == 3)
                    {
                        this.currentlySnappedComponent = this.throwComp;
                    }
                }
                else if (direction == 3)
                {
                    if (this.snappedId < 2)
                    {
                        if (this.upButton != null)
                        {
                            this.snappedSection = 5;
                            this.snappedId = 1;
                            this.applyMovementKey(0);
                        }
                        else
                        {
                            this.snappedSection = 5;
                            this.snappedId = 0;
                            this.applyMovementKey(3);
                        }
                    }
                    else if (this.snappedId == 2)
                    {
                        if (this.downButton != null)
                        {
                            this.snappedSection = 5;
                            this.snappedId = 0;
                            this.applyMovementKey(2);
                        }
                        else
                        {
                            this.snappedSection = 5;
                            this.snappedId = 1;
                            this.applyMovementKey(3);
                        }
                    }
                    else
                    {
                        this.snappedSection = 3;
                        this.snappedId = 0;
                        this.inventory.currentlySnappedComponent = this.inventory.inventory[this.inventory.capacity / this.inventory.rows - 1];
                        this.inventory.snapCursorToCurrentSnappedComponent();
                        return;
                    }
                }
            }
            else if (snappedSection == 5)
            {
                if (direction == 0)
                {
                    if (snappedId == 1)
                    {
                        snappedId = 0;
                        this.currentlySnappedComponent = this.upButton;
                    }
                    else if (snappedId == 0)
                    {
                        this.snappedSection = 1;
                        this.snappedId = 0;
                        this.applyMovementKey(0);
                    }
                }
                else if (direction == 1)
                {
                    if (snappedId == 0)
                    {
                        this.snappedSection = 4;
                        this.snappedId = 1;
                        this.applyMovementKey(0);
                    }
                    else
                    {
                        this.snappedSection = 4;
                        this.snappedId = 1;
                        this.applyMovementKey(3);
                    }
                }
                else if (direction == 2)
                {
                    if (snappedId == 0)
                    {
                        this.snappedId = 1;
                        this.currentlySnappedComponent = this.downButton;
                    }
                    else
                    {
                        this.snappedSection = 3;
                        this.snappedId = 0;
                        this.inventory.currentlySnappedComponent = this.inventory.inventory[this.inventory.capacity / this.inventory.rows - 1];
                        this.inventory.snapCursorToCurrentSnappedComponent();
                        return;
                    }
                }
                else if (direction == 3)
                {
                    if (snappedId == 0)
                    {
                        this.snappedSection = 1;
                        this.snappedId = this.maxItemsInRow - 2;
                        this.applyMovementKey(1);
                    }
                    else if (snappedId == 1)
                    {
                        this.snappedSection = 1;
                        this.snappedId = this.maxItemsInRow * ROWS - 2;
                        this.applyMovementKey(1);
                    }
                }
            }

            this.snapCursorToCurrentSnappedComponent();
        }

        public override void snapToDefaultClickableComponent()
        {
            this.snappedId = 1;
            this.snappedSection = 1;

            this.applyMovementKey(3);
        }

        public override bool readyToClose()
        {
            return this.heldItem == null;
        }

        public override void receiveScrollWheelAction(int direction)
        {

            if (direction > 0)
            {
                this.ScrollUp();
            }
            else if (direction < 0)
            {
                this.ScrollDown();
            }
        }

        public override void performHoverAction(int x, int y)
        {
            this.hoverTitle = "";
            this.hoverText = "";
            this.hoverRecipe = null;
            this.hoverItem = this.inventory.hover(x, y, this.hoverItem);

            if (this.hoverItem != null)
            {
                this.hoverTitle = this.inventory.hoverTitle;
                this.hoverText = this.inventory.hoverText;
            }

            var currentPage = this.GetCurrentPage();

            foreach (var c in currentPage.Keys)
            {
                if (c.containsPoint(x, y))
                {
                    if (c.hoverText.Equals(UNKNOWN))
                    {
                        this.hoverText = currentPage[c].name + " (not yet learned)";
                    }
                    else
                    {
                        this.hoverRecipe = currentPage[c];
                    }

                    if (c.hoverText.Equals(AVAILABLE))
                    {
                        c.scale = Math.Min(c.scale + 0.02f, c.baseScale + 0.2f);
                    }
                }
                else
                {
                    c.scale = Math.Max(c.scale - 0.02f, c.baseScale);
                }
            }

            this.categoryText = null;

            foreach (var c in this.categories.Keys)
            {
                if (c.containsPoint(x, y))
                {
                    this.categoryText = c.label;
                }
            }

            if (this.upButton != null)
            {
                if (this.upButton.containsPoint(x, y))
                {
                    this.upButton.scale = Math.Min(this.upButton.scale + 0.02f, this.upButton.baseScale + 0.1f);
                }
                else
                {
                    this.upButton.scale = Math.Max(this.upButton.scale - 0.02f, this.upButton.baseScale);
                }
            }

            if (this.downButton != null)
            {
                if (this.downButton.containsPoint(x, y))
                {
                    this.downButton.scale = Math.Min(this.downButton.scale + 0.02f, this.downButton.baseScale + 0.1f);
                }
                else
                {
                    this.downButton.scale = Math.Max(this.downButton.scale - 0.02f, this.downButton.baseScale);
                }
            }

            this.oldButton.tryHover(x, y);
            if (this.oldButton.containsPoint(x, y))
            {
                this.hoverText = oldButton.hoverText;
            }

            if (this.trashCan.containsPoint(x, y))
            {
                if (this.trashCanLidRotation <= 0f)
                {
                    Game1.playSound("trashcanlid");
                }
                this.trashCanLidRotation = Math.Min(this.trashCanLidRotation + 0.06544985f, 1.57079637f);
                return;
            }

            this.trashCanLidRotation = Math.Max(this.trashCanLidRotation - 0.06544985f, 0f);
        }

        public override void receiveLeftClick(int x, int y, bool playSound = true)
        {

            this.heldItem = this.inventory.leftClick(x, y, this.heldItem);

            foreach (var c in this.categories.Keys)
            {
                if (c.containsPoint(x, y))
                {
                    this.SetCategory(c);
                }
            }

            var currentPage = this.GetCurrentPage();

            foreach (var c in currentPage.Keys)
            {
                int num = Game1.oldKBState.IsKeyDown(Keys.LeftShift) ? 5 : 1;
                for (int index = 0; index < num; ++index)
                {
                    if (c.containsPoint(x, y)
                        && c.hoverText.Equals(AVAILABLE)
                        && currentPage[c].doesFarmerHaveIngredientsInInventory())
                    {
                        this.clickCraftingRecipe(c, index == 0 ? true : false);
                    }
                }
            }

            if (this.upButton != null && this.upButton.containsPoint(x, y))
            {
                this.ScrollUp();
            }

            if (this.downButton != null && this.downButton.containsPoint(x, y))
            {
                this.ScrollDown();
            }

            if (this.oldButton.containsPoint(x, y) && this.readyToClose())
            {
                Game1.playSound("select");

                GameMenu gameMenu = (GameMenu)Game1.activeClickableMenu;
                ModEntry.oldMenu = true;
                Game1.activeClickableMenu = new GameMenu(gameMenu.currentTab);
                return;
            }

            if (this.trashCan != null && this.trashCan.containsPoint(x, y) && (this.heldItem != null && this.heldItem.canBeTrashed()))
            {
                if (this.heldItem is StardewValley.Object && Game1.player.specialItems.Contains((this.heldItem as StardewValley.Object).ParentSheetIndex))
                    Game1.player.specialItems.Remove((this.heldItem as StardewValley.Object).ParentSheetIndex);
                this.heldItem = (Item)null;
                Game1.playSound("trashcan");
            }
            else
            {
                if (this.heldItem == null || this.isWithinBounds(x, y) || !this.heldItem.canBeTrashed())
                    return;
                Game1.playSound("throwDownITem");
                Game1.createItemDebris(this.heldItem, Game1.player.getStandingPosition(), Game1.player.FacingDirection, (GameLocation)null, -1);
                this.heldItem = (Item)null;
            }
        }

        private void clickCraftingRecipe(ClickableTextureComponent c, bool playSound)
        {
            CraftingRecipe recipe = this.GetCurrentPage()[c];
            Item crafted = recipe.createItem();

            Game1.player.checkForQuestComplete(null, -1, -1, crafted, null, 2, -1);

            if (this.heldItem == null)
            {
                recipe.consumeIngredients(_materialContainers);
                this.heldItem = crafted;

                if (playSound)
                {
                    Game1.playSound("coin");
                }
            }
            else if (this.heldItem.Name.Equals(crafted.Name) && this.heldItem.Stack + recipe.numberProducedPerCraft - 1 < this.heldItem.maximumStackSize())
            {
                recipe.consumeIngredients(_materialContainers);
                this.heldItem.Stack += recipe.numberProducedPerCraft;

                if (playSound)
                {
                    Game1.playSound("coin");
                }
            }

            Game1.player.craftingRecipes[recipe.name] += recipe.numberProducedPerCraft;

            Game1.stats.checkForCraftingAchievements();

            if (Game1.options.gamepadControls && this.heldItem != null && Game1.player.couldInventoryAcceptThisItem(this.heldItem))
            {
                Game1.player.addItemToInventoryBool(this.heldItem);
                this.heldItem = null;
            }
        }

        public override void receiveRightClick(int x, int y, bool playSound = true)
        {
            this.heldItem = this.inventory.rightClick(x, y, this.heldItem);

            var currentPage = this.GetCurrentPage();

            foreach (var c in currentPage.Keys)
            {
                if (c.containsPoint(x, y)
                    && c.hoverText.Equals(AVAILABLE)
                    && currentPage[c].doesFarmerHaveIngredientsInInventory())
                {
                    this.clickCraftingRecipe(c, true);
                }
            }
        }

        public override void draw(SpriteBatch b)
        {
            this.UpdateInventory();

            var currentPage = this.GetCurrentPage();

            foreach (var c in currentPage.Keys)
            {
                if (c.hoverText.Equals(AVAILABLE))
                {
                    c.draw(b, Color.White, 0.89f);
                    if (currentPage[c].numberProducedPerCraft > 1)
                        NumberSprite.draw(currentPage[c].numberProducedPerCraft, b, new Vector2((float)(c.bounds.X + 64 - 2), (float)(c.bounds.Y + 64 - 2)), Color.Red, (float)(0.5 * ((double)c.scale / 4.0)), 0.97f, 1f, 0, 0);
                }
                else if (c.hoverText.Equals(UNKNOWN))
                {
                    c.draw(b, new Color(0f, 0f, 0f, 0.1f), 0.89f);
                }
                else
                {
                    c.draw(b, Color.Gray * 0.4f, 0.89f);
                }
            }

            foreach (var c in this.categories.Keys)
            {
                var boxColor = Color.White;
                var textColor = Game1.textColor;

                if (c.name.Equals(UNAVAILABLE))
                {
                    boxColor = Color.Gray;
                }

                IClickableMenu.drawTextureBox(b,
                    Game1.menuTexture,
                    new Rectangle(0, 256, 60, 60),
                    c.bounds.X,
                    c.bounds.Y,
                    c.bounds.Width,
                    c.bounds.Height + Game1.tileSize / 16,
                    boxColor);

                b.DrawString(Game1.smallFont,
                    c.label,
                    new Vector2(c.bounds.X + Game1.tileSize / 4, c.bounds.Y + Game1.tileSize / 4),
                    textColor);
            }

            if (this.upButton != null) this.upButton.draw(b);
            if (this.downButton != null) this.downButton.draw(b);

            this.inventory.draw(b);

            this.oldButton.draw(b, this.readyToClose() ? Color.White : Color.Gray, 0.89f);

            this.trashCan.draw(b);
            b.Draw(
                Game1.mouseCursors,
                new Vector2(this.trashCan.bounds.X + 60, this.trashCan.bounds.Y + 40),
                new Rectangle(686, 256, 18, 10),
                Color.White,
                this.trashCanLidRotation,
                new Vector2(16f, 10f),
                Game1.pixelZoom,
                SpriteEffects.None,
                0.86f);

            if (this.hoverItem != null)
            {
                IClickableMenu.drawToolTip(
                    b,
                    this.hoverText,
                    this.hoverTitle,
                    this.hoverItem,
                    this.heldItem != null);
            }
            else if (this.hoverText != null)
            {
                IClickableMenu.drawHoverText(b,
                    this.hoverText,
                    Game1.smallFont,
                    (this.heldItem != null) ? Game1.tileSize : 0,
                    (this.heldItem != null) ? Game1.tileSize : 0);
            }

            if (this.heldItem != null)
            {
                this.heldItem.drawInMenu(b,
                    new Vector2(
                        Game1.getOldMouseX() + Game1.tileSize / 4,
                        Game1.getOldMouseY() + Game1.tileSize / 4),
                    1f);
            }

            if (this.hoverRecipe != null)
            {
                IClickableMenu.drawHoverText(b,
                    " ",
                    Game1.smallFont,
                    Game1.tileSize * 3 / 4,
                    Game1.tileSize * 3 / 4,
                    -1,
                    this.hoverRecipe.name,
                    -1,
                    null,
                    null, 0, -1, -1, -1, -1, 1f, this.hoverRecipe);
            }
            else if (this.categoryText != null)
            {
                IClickableMenu.drawHoverText(b, this.categoryText, Game1.smallFont);
            }
        }
    }
}