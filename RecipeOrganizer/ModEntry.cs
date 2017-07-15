using System;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Menus;
using Microsoft.Xna.Framework.Input;

namespace RecipeOrganizer
{
    /// <summary>The mod entry point.</summary>
    public class ModEntry : Mod
    {
        ClickableTextureComponent organizeButton;

        /*********
        ** Public methods
        *********/
        /// <summary>The mod entry point, called after the mod is first loaded.</summary>
        /// <param name="helper">Provides simplified APIs for writing mods.</param>
        public override void Entry(IModHelper helper)
        {
            SaveEvents.AfterLoad += SaveEvents_AfterLoad;
            ControlEvents.MouseChanged += ControlEvents_MouseChanged;
            GraphicsEvents.Resize += GraphicsEvents_Resize;
            GraphicsEvents.OnPreRenderGuiEvent += GraphicsEvents_OnPreRenderGuiEvent;
        }
        /*********
        ** Private methods
        *********/
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
        /// 
        private void SaveEvents_AfterLoad(object sender, EventArgs e)
        {
            InitializeButton();
        }

        private void ControlEvents_MouseChanged(object sender, EventArgsMouseStateChanged e)
        {
            if (isInRecipeMenu())
            {
                if (e.NewState.LeftButton == ButtonState.Pressed && e.PriorState.LeftButton == ButtonState.Released)
                {
                    if (organizeButton.bounds.Contains(e.NewPosition.X, e.NewPosition.Y))
                        this.Monitor.Log("I'm clicking");
                }
            }
        }

        private void InitializeButton()
        {
            int x = Game1.viewport.Width / 2 - (800 + IClickableMenu.borderWidth * 2) / 2;
            int y = Game1.viewport.Height / 2 - (600 + IClickableMenu.borderWidth * 2) / 2;
            int width = 800 + IClickableMenu.borderWidth * 2;
            int height = 600 + IClickableMenu.borderWidth * 2;

            Rectangle bounds = new Rectangle(x + width, y + height / 3 - Game1.tileSize, Game1.tileSize, Game1.tileSize);
            Rectangle sourceRect = new Rectangle(162, 440, 16, 16);

            organizeButton = new ClickableTextureComponent("", bounds, "", Game1.content.LoadString("Strings\\UI:ItemGrab_Organize"), Game1.mouseCursors, sourceRect, (float)Game1.pixelZoom, false);
        }

        //Won't do anything until a new SMAPI update (which should fix it)
        //https://github.com/Pathoschild/SMAPI/issues/328
        private void GraphicsEvents_Resize(object sender, EventArgs e)
        {
            InitializeButton();
        }

        private void GraphicsEvents_OnPreRenderGuiEvent(object sender, EventArgs e)
        {
            if(isInRecipeMenu())
                organizeButton.draw(Game1.spriteBatch);
        }

        private bool isInRecipeMenu()
        {
            IClickableMenu menu = Game1.activeClickableMenu;
            if (Game1.activeClickableMenu != null && menu is CraftingPage)
            {
                //if it is the recipe menu
                return this.Helper.Reflection.GetPrivateValue<bool>(menu, "cooking");
            }
            return false;
        }
    }
}