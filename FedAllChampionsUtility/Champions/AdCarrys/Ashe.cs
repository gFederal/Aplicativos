#region

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;

#endregion

namespace FedAllChampionsUtility
{
    class Ashe : Champion
    {
        private static readonly Obj_AI_Hero player = ObjectManager.Player;        
        private static Spell Q, W, R;
        private static bool hasQ = false; 
        private const double WAngle = 57.5 * Math.PI / 180;

        public Ashe()
        {
            LoadMenu();
            LoadSpells();

            Game.OnGameSendPacket += OnSendPacket;
            Game.OnGameUpdate += Game_OnGameUpdate;
            AntiGapcloser.OnEnemyGapcloser += AntiGapcloser_OnEnemyGapcloser;
            Interrupter.OnPossibleToInterrupt += Interrupter_OnPossibleToInterrupt;

            PluginLoaded();            
        }

        private void LoadSpells()
        {
            Q = new Spell(SpellSlot.Q);
            W = new Spell(SpellSlot.W, 1200);
            R = new Spell(SpellSlot.R);

            W.SetSkillshot(0.5f, (float)WAngle, 2000f, false, SkillshotType.SkillshotCone);
            R.SetSkillshot(0.3f, 250f, 1600f, false, SkillshotType.SkillshotLine);
        }

        private void LoadMenu()
        {
            Program.Menu.AddSubMenu(new Menu("Combo", "Combo"));
            Program.Menu.SubMenu("Combo").AddItem(new MenuItem("UseQ", "Use Q").SetValue(true));
            Program.Menu.SubMenu("Combo").AddItem(new MenuItem("UseW", "Use W").SetValue(true));
            Program.Menu.SubMenu("Combo").AddItem(new MenuItem("UseR", "Use R").SetValue(true));
            
            Program.Menu.AddSubMenu(new Menu("Harass", "Harass"));            
            Program.Menu.SubMenu("Harass").AddItem(new MenuItem("UseW", "Use W").SetValue(true));

            Program.Menu.AddSubMenu(new Menu("Misc", "Misc"));
            Program.Menu.SubMenu("Misc").AddItem(new MenuItem("interrupt", "Interrupt spells").SetValue(true));
            Program.Menu.SubMenu("Misc").AddItem(new MenuItem("antigapcloser", "Anti-Gapscloser").SetValue(true));           
        }

        private static void AntiGapcloser_OnEnemyGapcloser(ActiveGapcloser gapcloser)
        {
            if (R.IsReady() && Program.Menu.Item("antigapcloser").GetValue<bool>() && Vector3.Distance(gapcloser.Sender.Position, player.Position) < 1000)
            {
                R.Cast(gapcloser.End, true);
            }
        }       
        private static void Interrupter_OnPossibleToInterrupt(Obj_AI_Base unit, InterruptableSpell spell)
        {
            if (R.IsReady() &&
                Vector3.Distance(player.Position, unit.Position) < 1000 &&
                Program.Menu.Item("interrupt").GetValue<bool>() &&
                spell.DangerLevel >= InterruptableDangerLevel.Medium)
            {
                R.Cast(unit.Position, true);
            }
        }
        private static void Game_OnGameUpdate(EventArgs args)
        {
            // Combo
            if (Program.Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo)
                Combo();

            // Harass
            if (Program.Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Mixed)
                Harass();
        }
        private static void Combo()
        {
            bool useW = W.IsReady() && Program.Menu.Item("UseW").GetValue<bool>();
            bool useR = R.IsReady() && Program.Menu.Item("UseR").GetValue<bool>();
            Obj_AI_Hero targetW = SimpleTs.GetTarget(W.Range, SimpleTs.DamageType.Physical);
            Obj_AI_Hero targetR = SimpleTs.GetTarget(700, SimpleTs.DamageType.Magical);
            if (useW)
            {
                W.CastIfHitchanceEquals(targetW, HitChance.Medium);
            }
            if (useR)
            {
                R.CastIfHitchanceEquals(targetR, HitChance.High);
            }
        }
        private static void Harass()
        {
            bool useW = W.IsReady() && Program.Menu.Item("UseW").GetValue<bool>();
            Obj_AI_Hero targetW = SimpleTs.GetTarget(W.Range, SimpleTs.DamageType.Physical);
            if (useW)
            {
                W.CastIfHitchanceEquals(targetW, HitChance.Medium);
            }
        }
        private static void OnSendPacket(GamePacketEventArgs args)
        {
            if (!Program.Menu.Item("UseQ").GetValue<bool>()) return;
            if (args.PacketData[0] == Packet.C2S.Move.Header && Packet.C2S.Move.Decoded(args.PacketData).SourceNetworkId == player.NetworkId && Packet.C2S.Move.Decoded(args.PacketData).MoveType == 3)
            {
                bool heroFound;
                foreach (BuffInstance buff in player.Buffs)
                    if (buff.Name == "FrostShot") hasQ = true;
                heroFound = false;
                foreach (Obj_AI_Hero hero in ObjectManager.Get<Obj_AI_Hero>())
                    if (hero.NetworkId == Packet.C2S.Move.Decoded(args.PacketData).TargetNetworkId)
                        heroFound = true;
                if (heroFound)
                {
                    if (!hasQ) Q.Cast();
                    hasQ = true;
                }
                else
                {
                    if (hasQ) Q.Cast();
                    hasQ = false;
                }
            }
        }       

    }
}
