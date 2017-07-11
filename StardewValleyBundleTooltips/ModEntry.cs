using System;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using System.Collections.Generic;
using System.Linq;
using StardewValley.Menus;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

//Created by Musbah Sinno

//Resources:Got GetHoveredItemFromMenu and DrawHoverTextbox from a CJB mod and modified them to suit my needs.
//          They also inspired me to make GetHoveredItemFromToolbar, so thank you CJB
//https://github.com/CJBok/SDV-Mods/blob/master/CJBShowItemSellPrice/StardewCJB.cs

namespace StardewValleyBundleTooltips
{
    /// <summary>The mod entry point.</summary>
    public class ModEntry : Mod
    {
        //Needed to make sure essential variables are loaded before running what needs them
        Boolean isLoaded = false;

        Item toolbarItem;
        List<int> itemsInBundles;
        SerializableDictionary<int, int[][]> bundles;
        SerializableDictionary<int, String[]> bundleNamesAndSubNames;

        /*********
        ** Public methods
        *********/
        /// <summary>The mod entry point, called after the mod is first loaded.</summary>
        /// <param name="helper">Provides simplified APIs for writing mods.</param>
        public override void Entry(IModHelper helper)
        {
            SaveEvents.AfterLoad += this.SaveEvents_AfterLoad;
            GraphicsEvents.OnPostRenderGuiEvent += GraphicsEvents_OnPostRenderGuiEvent;
            GraphicsEvents.OnPreRenderHudEvent += GraphicsEvents_OnPreRenderHudEvent;
            GraphicsEvents.OnPostRenderHudEvent += GraphicsEvents_OnPostRenderHudEvent;
            PlayerEvents.InventoryChanged += this.PlayerEvents_InventoryChanged;

        }

        /*********
        ** Private methods
        *********/
        /// <summary>The method invoked when the player presses a keyboard button.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
        /// 
        private void GraphicsEvents_OnPreRenderHudEvent(object sender, EventArgs e)
        {
            //I have to get it on preRendering because it gets set to null post
            toolbarItem = GetHoveredItemFromToolbar();
        }

        private void GraphicsEvents_OnPostRenderHudEvent(object sender, EventArgs e)
        {
            if (isLoaded && Game1.activeClickableMenu == null && toolbarItem != null)
            {
                PopulateHoverTextBoxAndDraw(toolbarItem);
                toolbarItem = null;
            }
        }

        private void GraphicsEvents_OnPostRenderGuiEvent(object sender, EventArgs e)
        {
            if (isLoaded && Game1.activeClickableMenu != null)
            {
                Item item = this.GetHoveredItemFromMenu(Game1.activeClickableMenu);
                if (item != null)
                    PopulateHoverTextBoxAndDraw(item);
            }
        }

        private void PopulateHoverTextBoxAndDraw(Item item)
        {
            StardewValley.Locations.CommunityCenter communityCenter = Game1.getLocationFromName("CommunityCenter") as StardewValley.Locations.CommunityCenter;

            String desc = "";
            int count = 0;

            foreach (int itemInBundles in itemsInBundles)
            {
                if (item.parentSheetIndex == itemInBundles)
                {
                    foreach (KeyValuePair<int, int[][]> bundle in bundles)
                    {
                        for (int i = 0; i < bundle.Value.Length; i++)
                        {
                            var isItemInBundleSlot = communityCenter.bundles[bundle.Key][i*3];
                            if (bundle.Value[i] != null && bundle.Value[i][0] == item.parentSheetIndex && bundle.Value[i][2] == ((StardewValley.Object)item).quality)
                            {
                                if(!isItemInBundleSlot)
                                {
                                    if (count > 0)
                                        desc += "\n";

                                    desc += bundleNamesAndSubNames[bundle.Key][0] + " | " + bundleNamesAndSubNames[bundle.Key][1] + " | Quantity:" + bundle.Value[i][1];
                                    count++;
                                }
                            }
                        }
                    }
                }
            }

            if(desc != "")
                this.DrawHoverTextBox(Game1.smallFont, desc);
        }

        private Item GetHoveredItemFromMenu(IClickableMenu menu)
        {
            // game menu
            if (menu is GameMenu gameMenu)
            {
                IClickableMenu page = this.Helper.Reflection.GetPrivateValue<List<IClickableMenu>>(gameMenu, "pages")[gameMenu.currentTab];
                if (page is InventoryPage)
                    return this.Helper.Reflection.GetPrivateValue<Item>(page, "hoveredItem");
            }
            // from inventory UI (so things like shops and so on)
            else if (menu is MenuWithInventory inventoryMenu)
            {
                return inventoryMenu.hoveredItem;
            }

            return null;
        }

        private Item GetHoveredItemFromToolbar()
        {
            foreach (IClickableMenu menu in Game1.onScreenMenus)
            {
                if (menu is Toolbar toolbar)
                {
                    return this.Helper.Reflection.GetPrivateValue<Item>(menu, "hoverItem");
                }
            }

            return null;
        }

        private void DrawHoverTextBox(SpriteFont font, String description)
        {
            Vector2 stringLength = font.MeasureString(description);
            int width = (int)stringLength.X + Game1.tileSize / 2 + 40;
            int height = (int)stringLength.Y + Game1.tileSize / 3 + 5;

            int x = (int)(Mouse.GetState().X / Game1.options.zoomLevel) - Game1.tileSize / 2 - width;
            int y = (int)(Mouse.GetState().Y / Game1.options.zoomLevel) + Game1.tileSize / 2;

            if (x < 0)
                x = 0;

            if (y + height > Game1.graphics.GraphicsDevice.Viewport.Height)
                y = Game1.graphics.GraphicsDevice.Viewport.Height - height;

            IClickableMenu.drawTextureBox(Game1.spriteBatch, Game1.menuTexture, new Rectangle(0, 256, 60, 60), x, y, width, height, Color.White);
            Utility.drawTextWithShadow(Game1.spriteBatch, description, font, new Vector2(x + Game1.tileSize / 4, y + Game1.tileSize / 4), Game1.textColor);
        }


        private void PlayerEvents_InventoryChanged(object sender, EventArgsInventoryChanged e)
        {
            
        }

        private void SaveEvents_AfterLoad(object sender, EventArgs e)
        {
            //This will be filled with the itemIDs of every item in every bundle (for a fast search without details)
            itemsInBundles = new List<int>();
            bundles = getBundles();

            //remove duplicates
            itemsInBundles = new HashSet<int>(itemsInBundles).ToList();

            isLoaded = true;

            //Game1.objectInformation contains the raw items data

            //this.Monitor.Log(Game1.player.hasItemInInventory(0, 1));

            //List<int> playerItemsInBundle = new List<int>();

            //first check player inventory for bundle items
            //for (int i = 0; i < Game1.player.items.Count; i++)
            //{
            //    //parentSheetIndex -1 means that these are tools and not items
            //    if (Game1.player.items[i] != null && Game1.player.items[i].parentSheetIndex != -1)
            //    {
            //        foreach (int itemInBundles in itemsInBundles)
            //        {
            //            if (Game1.player.items[i].parentSheetIndex == itemInBundles)
            //                playerItemsInBundle.Add(itemInBundles);
            //        }
            //    }
            //}

            //remove the duplicate items
            //playerItemsInBundle = new HashSet<int>(playerItemsInBundle).ToList();
            //output the items that are needed for community center, if we have any in inventory
            //foreach (KeyValuePair<int, int[][]> bundle in bundles)
            //{
            //    for (int i = 0; i < bundle.Value.Length; i++)
            //    {
            //        for (int j = 0; j < playerItemsInBundle.Count; j++)
            //        {
            //            if (bundle.Value[i] != null && bundle.Value[i][0] == playerItemsInBundle[j])
            //            {
            //                this.Monitor.Log("BundleId:" + bundle.Key + " itemId:" + bundle.Value[i][0] + " itemQuantity:" + bundle.Value[i][1] + " itemQuality:" + bundle.Value[i][2]);
            //            }
            //        }
            //    }
            //}
        }

        public SerializableDictionary<int, int[][]> getBundles()
        {
            Dictionary<string, string> dictionary = Game1.content.Load<Dictionary<string, string>>("Data\\Bundles");
            SerializableDictionary<int, int[][]> bundles = new SerializableDictionary<int, int[][]>();
            bundleNamesAndSubNames = new SerializableDictionary<int, string[]>();

            foreach (KeyValuePair<string, string> keyValuePair in dictionary)
            {
                //format of the values are itemID itemAmount itemQuality

                //if bundleIndex is between 23 and 26, then they're vault bundles so don't add to dictionary

                String[] split = keyValuePair.Key.Split('/');
                String bundleName = split[0];
                String bundleSubName = keyValuePair.Value.Split('/')[0];
                int bundleIndex = Convert.ToInt32(split[1]);
                if (!(bundleIndex >= 23 && bundleIndex <= 26))
                {
                    //creating an array for the bundle names
                    String[] bundleNames = new String[] {bundleName,bundleSubName} ;

                    //creating an array of items[i][j] , i is the item index, j=0 itemId, j=1 itemAmount, j=2 itemQuality
                    String[] allItems = keyValuePair.Value.Split('/')[2].Split(' ');
                    int allItemsLength = allItems.Length / 3;
                    
                    int[][] items = new int[allItemsLength][];

                    int j = 0;
                    int i = 0;
                    while(j< allItemsLength)
                    {
                        items[j] = new int[3];
                        items[j][0] = Convert.ToInt32(allItems[0 + i]);
                        items[j][1] = Convert.ToInt32(allItems[1 + i]);
                        items[j][2] = Convert.ToInt32(allItems[2 + i]);

                        itemsInBundles.Add(items[j][0]);
                        i = i + 3;
                        j++;
                    }

                    bundles.Add(bundleIndex, items);
                    bundleNamesAndSubNames.Add(bundleIndex, bundleNames);
                }
            }

            return bundles;
        }
    }
}