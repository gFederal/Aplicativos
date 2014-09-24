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
    class Twitch : Champion
    {
        public static Spell Q, W, E, R;
        public int ExpungeBuffStacks = 0;
        
        public Twitch()
        {
            LoadMenu();
            LoadSpells();

            Drawing.OnDraw += Drawing_OnDraw;
            Game.OnGameUpdate += Game_OnGameUpdate;
            
            PluginLoaded();
        }

        private void LoadSpells()
        {
            Q = new Spell(SpellSlot.Q, 0f);
            W = new Spell(SpellSlot.W, 950f);
            E = new Spell(SpellSlot.E, 1200f);
            R = new Spell(SpellSlot.R, 850f);

            W.SetSkillshot(0.25f, 150f, 1400f, false, SkillshotType.SkillshotCircle);
            
        }

        private void LoadMenu()
        {
            Program.Menu.AddSubMenu(new Menu("TeamFight", "TeamFight"));
            Program.Menu.SubMenu("TeamFight").AddItem(new MenuItem("UseWCombo", "Use W").SetValue(true));
            Program.Menu.SubMenu("TeamFight").AddItem(new MenuItem("UseECombo", "Use E").SetValue(true));            
            Program.Menu.SubMenu("TeamFight").AddItem(new MenuItem("UseRCombo", "Use R").SetValue(true));
            Program.Menu.SubMenu("TeamFight").AddItem(new MenuItem("UseRHit", "Use R if hit").SetValue(new Slider(3, 5, 1)));

            Program.Menu.AddSubMenu(new Menu("Misc", "Misc"));
            Program.Menu.SubMenu("Misc").AddItem(new MenuItem("AutoW", "Auto W").SetValue(true));
            Program.Menu.SubMenu("Misc").AddItem(new MenuItem("AutoWMode", "W Mode: ").SetValue(new StringList(new[] { "Low HP", "Multi-Targets", "Both", "Ready" }, 2)));
            Program.Menu.SubMenu("Misc").AddItem(new MenuItem("UseEStacks", "Expunge at Stacks").SetValue(new Slider(6, 6, 1)));
            Program.Menu.SubMenu("Misc").AddItem(new MenuItem("UseEKS", "Expunge for Kills").SetValue(true));

            Program.Menu.AddSubMenu(new Menu("Drawing", "Drawing"));
            Program.Menu.SubMenu("Drawing").AddItem(new MenuItem("Draw_Disabled", "Disable All").SetValue(false));           
            Program.Menu.SubMenu("Drawing").AddItem(new MenuItem("Draw_W", "Draw W").SetValue(true));
            Program.Menu.SubMenu("Drawing").AddItem(new MenuItem("Draw_E", "Draw E").SetValue(true));
            Program.Menu.SubMenu("Drawing").AddItem(new MenuItem("Draw_R", "Draw R").SetValue(true));
        }

        public void Game_OnGameUpdate(EventArgs args)
        {
            if (Program.Orbwalker.ActiveMode.ToString() == "Combo")
            {
                Combo();
            }
            else
            {

                if (Program.Orbwalker.ActiveMode.ToString() == "Mixed")
                {
                    CastW();
                }
            }

            if (Program.Menu.Item("AutoW").GetValue<bool>())
            {
                CastW();
            }

            if (Program.Menu.Item("UseEKS").GetValue<bool>())
            {
                CastE();
            }

        }

        private void Combo()
        {
            if (Program.Menu.Item("UseWCombo").GetValue<bool>() && W.IsReady())
            {
                CastW();
            }

            if (Program.Menu.Item("UseECombo").GetValue<bool>() && E.IsReady())
            {
                CastE();
            }

            if (Program.Menu.Item("UseRCombo").GetValue<bool>() && R.IsReady())
            {
                CastR();
            }
        }

        private void CastR()
        {
            int inimigos = Utility.CountEnemysInRange(800);

            if (Program.Menu.Item("UseRHit").GetValue<Slider>().Value <= inimigos)
            {
                R.Cast();
            }
        }

        private void CastW()
        {
            var wTarget = SimpleTs.GetTarget(W.Range, SimpleTs.DamageType.Physical);
            var wMode = Program.Menu.Item("AutoWMode").GetValue<StringList>().SelectedIndex;

            switch (wMode)
            {
                case 0:
                    if (IsEnemyHealthLow(wTarget))
                        W.Cast(wTarget);
                    break;
                case 1:
                    W.CastIfWillHit(wTarget, 2);
                    break;
                case 2:
                    if (IsEnemyHealthLow(wTarget))
                    {
                        W.Cast(wTarget);
                    }
                    else
                    {
                        W.CastIfWillHit(wTarget, 2);
                    }
                    break;
                case 3:
                    W.Cast(wTarget);
                    break;
            }          

            
        }

        private void CastE()
        {
            var eTarget = SimpleTs.GetTarget(E.Range, SimpleTs.DamageType.Physical);
            var eStacks = Program.Menu.Item("UseEStacks").GetValue<Slider>().Value;
            var AutoEKS = Program.Menu.Item("UseEKS").GetValue<bool>();

            if (eTarget.IsValidTarget() && eTarget.HasBuff("TwitchDeadlyVenom"))
                ExpungeBuffStacks = (from buff in eTarget.Buffs
                                     where buff.DisplayName.ToLower() == "twitchdeadlyvenom"
                                     select buff.Count).FirstOrDefault();
            if (!eTarget.IsMinion && (ExpungeBuffStacks >= eStacks || (AutoEKS && eTarget.HasBuff("TwitchDeadlyVenom") && eTarget.Health < DamageLib.getDmg(eTarget, DamageLib.SpellType.E) - 15)))
                E.Cast();
        }

        private bool IsEnemyHealthLow(Obj_AI_Hero enemy)
        {
            var enemyLowHealth = 0.40;

            if (enemy != null && !enemy.IsDead && enemy.Health < (enemy.MaxHealth * enemyLowHealth))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private void Drawing_OnDraw(EventArgs args)
        {
            if (Program.Menu.Item("Draw_Disabled").GetValue<bool>())
                return;

            if (Program.Menu.Item("Draw_W").GetValue<bool>())
                if (W.Level > 0)
                    Utility.DrawCircle(ObjectManager.Player.Position, W.Range, W.IsReady() ? Color.Green : Color.Red);

            if (Program.Menu.Item("Draw_E").GetValue<bool>())
                if (E.Level > 0)
                    Utility.DrawCircle(ObjectManager.Player.Position, E.Range, E.IsReady() ? Color.Green : Color.Red);

            if (Program.Menu.Item("Draw_R").GetValue<bool>())
                if (R.Level > 0)
                    Utility.DrawCircle(ObjectManager.Player.Position, R.Range, R.IsReady() ? Color.Green : Color.Red);

           
        }
        
    }
}
