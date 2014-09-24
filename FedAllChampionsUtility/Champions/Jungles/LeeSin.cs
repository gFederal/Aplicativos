#region

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using Color = System.Drawing.Color;

#endregion

namespace FedAllChampionsUtility
{
    class LeeSin : Champion
    {
        public static Vector2 testSpellCast;
        public static Vector2 testSpellProj;
        public static Obj_AI_Hero Player = ObjectManager.Player;        
        public static Obj_AI_Hero target;

        public static string[] testSpells = { "RelicSmallLantern", "RelicLantern", "SightWard", "wrigglelantern", "ItemGhostWard", "VisionWard",
                                              "BantamTrap", "JackInTheBox","CaitlynYordleTrap", "Bushwhack"};

        public static Spell Q, W, E, R;
        public static Spellbook sBook = Player.Spellbook;
        public static SpellDataInst Qdata = sBook.GetSpell(SpellSlot.Q);
        public static SpellDataInst Wdata = sBook.GetSpell(SpellSlot.W);
        public static SpellDataInst Edata = sBook.GetSpell(SpellSlot.E);
        public static SpellDataInst Rdata = sBook.GetSpell(SpellSlot.R);

        public static Obj_AI_Hero LockedTarget;
        public static float lastwardjump = 0;
        public static bool daInsec = false;
        public static bool Combo1 = false;
        public static bool Combo2 = false;
        public LeeSin()
        {
            LoadMenu();
            //LoadSpells();

            //Drawing.OnDraw += onDraw;
            //Game.OnGameUpdate += OnGameUpdate;            
            //Obj_AI_Base.OnProcessSpellCast += OnProcessSpell;

            PluginLoaded();
        }

        private void LoadSpells()
        {
            Q = new Spell(SpellSlot.Q, 1100);
            W = new Spell(SpellSlot.W, 700);
            E = new Spell(SpellSlot.E, 350);
            R = new Spell(SpellSlot.R, 375);
            
            Q.SetSkillshot(0.5f, 60f, 1800f, true, SkillshotType.SkillshotLine);
            E.SetSkillshot(0.5f, 350f, 0f, false, SkillshotType.SkillshotCircle);
        }

        private void LoadMenu()
        {            
            Program.Menu.AddSubMenu(new Menu("Combo", "Combo"));
            Program.Menu.SubMenu("Combo").AddItem(new MenuItem("UseQCombo", "Use Q")).SetValue(true);
            Program.Menu.SubMenu("Combo").AddItem(new MenuItem("UseECombo", "Use E")).SetValue(true);
            Program.Menu.SubMenu("Combo").AddItem(new MenuItem("UseRCombo", "Use R")).SetValue(true);
            Program.Menu.SubMenu("Combo").AddItem(new MenuItem("ActiveCombo", "Combo!").SetValue(new KeyBind(32, KeyBindType.Press)));            

            Program.Menu.AddSubMenu(new Menu("Harass", "Harass"));
            Program.Menu.SubMenu("Harass").AddItem(new MenuItem("ActiveHarass", "Harass!").SetValue((new KeyBind("C".ToCharArray()[0], KeyBindType.Press, false))));

            Program.Menu.AddSubMenu(new Menu("Insec", "Insec"));
            Program.Menu.SubMenu("Insec").AddItem(new MenuItem("ActiveInsec", "Insec!").SetValue((new KeyBind("G".ToCharArray()[0], KeyBindType.Press, false))));

            Program.Menu.AddSubMenu(new Menu("KillSteal", "KillSteal"));
            Program.Menu.SubMenu("KillSteal").AddItem(new MenuItem("UseR", "R killsteal")).SetValue(true);

            Program.Menu.AddSubMenu(new Menu("WardJump", "WardJump"));
            Program.Menu.SubMenu("WardJump").AddItem(new MenuItem("ActiveWard", "WardJump!").SetValue((new KeyBind("Z".ToCharArray()[0], KeyBindType.Press, false))));

            Program.Menu.AddSubMenu(new Menu("Drawings", "Drawings"));
            Program.Menu.SubMenu("Drawings").AddItem(new MenuItem("DrawQ", "Draw Q")).SetValue(true);
            Program.Menu.SubMenu("Drawings").AddItem(new MenuItem("DrawE", "Draw E")).SetValue(true);
            Program.Menu.SubMenu("Drawings").AddItem(new MenuItem("DrawW", "Draw W")).SetValue(true);
            Program.Menu.SubMenu("Drawings").AddItem(new MenuItem("DrawR", "Draw R")).SetValue(true);
            Program.Menu.SubMenu("Drawings").AddItem(new MenuItem("DrawInsec", "Draw Insec")).SetValue(true);            
        }
    }
}
