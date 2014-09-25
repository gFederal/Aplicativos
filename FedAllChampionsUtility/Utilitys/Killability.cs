using System;
using System.Collections.Generic;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using FedAllChampionsUtility.Properties;

namespace FedAllChampionsUtility
{
    internal class Killability
    {

        public static List<Spell> spells;
        public static List<Items.Item> items;

        private static SpellSlot ignite;  

        public Killability()
        {
            LoadMenu();
            LoadSpells();            
        }

        private void LoadSpells()
        {
            ignite = ObjectManager.Player.GetSpellSlot("SummonerDot");

            items = new List<Items.Item>
            {
                new Items.Item(3128, 750), // Deathfire Grasp
                new Items.Item(3077, 400), // Tiamat
                new Items.Item(3074, 400), // Ravenous Hydra
                new Items.Item(3146, 700), // Hextech Gunblade
                new Items.Item(3144, 450), // Bilgewater Cutlass
                new Items.Item(3153, 450)  // Blade of the Ruined King
            };

            spells = new List<Spell>
            {
                new Spell(SpellSlot.Q),
                new Spell(SpellSlot.W),
                new Spell(SpellSlot.E),
                new Spell(SpellSlot.R)
            };
        }

        private void LoadMenu()
        {
            Program.Menu.AddSubMenu(new Menu("Killability", "Killability"));
            Program.Menu.SubMenu("Killability").AddItem(new MenuItem("icon", "Show Icon").SetValue(true));
            Program.Menu.SubMenu("Killability").AddItem(new MenuItem("text", "Show Text").SetValue(true));
        }        
    }
}

