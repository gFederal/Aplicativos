#region

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using Color = System.Drawing.Color;

#endregion

namespace FedAllChampionsUtility
{
    class Maokai : Champion
    {
        public static List<Spell> SpellList = new List<Spell>();

        public static Spell Q;
        public static Spell W;
        public static Spell E;
        public static Spell R;
                
        private SpellSlot SmiteSlot;
        private SpellSlot IgniteSlot;

        private Obj_AI_Hero Player = ObjectManager.Player;
        public Maokai()
        {
            LoadMenu();
            LoadSpells();

            Game.OnGameUpdate += Game_OnGameUpdate;
            Drawing.OnDraw += Drawing_OnDraw;
            AntiGapcloser.OnEnemyGapcloser += AntiGapcloser_OnEnemyGapcloser;
            Interrupter.OnPossibleToInterrupt += Interrupter_OnPossibleToInterrupt;

            PluginLoaded();
        }

        private void LoadSpells()
        {
            Q = new Spell(SpellSlot.Q, 600);
            W = new Spell(SpellSlot.W, 525);
            E = new Spell(SpellSlot.E, 1100);
            R = new Spell(SpellSlot.R, 625);
            
            SmiteSlot = Player.GetSpellSlot("SummonerSmite");
            IgniteSlot = Player.GetSpellSlot("SummonerDot");

            Q.SetSkillshot(0.25f, 110f, 1200f, false, SkillshotType.SkillshotLine);
            E.SetSkillshot(0.25f, 100f, 1750f, false, SkillshotType.SkillshotCircle);

            SpellList.AddRange(new[] { Q, W, E, R });
        }

        private void LoadMenu()
        {
            Program.Menu.AddSubMenu(new Menu("Combo", "Combo"));
            Program.Menu.SubMenu("Combo").AddItem(new MenuItem("UseQCombo", "Use Q").SetValue(true));
            Program.Menu.SubMenu("Combo").AddItem(new MenuItem("UseWCombo", "Use W").SetValue(true));
            Program.Menu.SubMenu("Combo").AddItem(new MenuItem("UseECombo", "Use E").SetValue(true));
            Program.Menu.SubMenu("Combo").AddItem(new MenuItem("UseRCombo", "Use R").SetValue(true));
            Program.Menu.SubMenu("Combo").AddItem(new MenuItem("MinR", "Min Enemys for (R)").SetValue<Slider>(new Slider(3, 1, 5)));
            Program.Menu.SubMenu("Combo").AddItem(new MenuItem("ManaR", "Turn off R at % Mana").SetValue(new Slider(30, 100, 0)));
            Program.Menu.SubMenu("Combo").AddItem(new MenuItem("ComboActive", "Combo!").SetValue(new KeyBind(32, KeyBindType.Press)));

            Program.Menu.AddSubMenu(new Menu("Ganks", "Ganks"));
            Program.Menu.SubMenu("Ganks").AddItem(new MenuItem("UseQGank", "Use Q").SetValue(true));
            Program.Menu.SubMenu("Ganks").AddItem(new MenuItem("UseWGank", "Use W").SetValue(true));
            Program.Menu.SubMenu("Ganks").AddItem(new MenuItem("UseEGank", "Use E").SetValue(true));
            Program.Menu.SubMenu("Ganks").AddItem(new MenuItem("GanksActive", "Ganks!").SetValue(new KeyBind("X".ToCharArray()[0], KeyBindType.Press)));

            Program.Menu.AddSubMenu(new Menu("Harass", "Harass"));
            Program.Menu.SubMenu("Harass").AddItem(new MenuItem("UseQHarass", "Use Q").SetValue(true));
            Program.Menu.SubMenu("Harass").AddItem(new MenuItem("UseEHarass", "Use E").SetValue(true));
            Program.Menu.SubMenu("Harass").AddItem(new MenuItem("ManaHarass", "Dont Harass if mana < %").SetValue(new Slider(40, 100, 0)));
            Program.Menu.SubMenu("Harass").AddItem(new MenuItem("harassToggle", "Use Harass (toggle)").SetValue<KeyBind>(new KeyBind('T', KeyBindType.Toggle)));
            Program.Menu.SubMenu("Harass").AddItem(new MenuItem("HarassActive", "Harass!").SetValue(new KeyBind("C".ToCharArray()[0], KeyBindType.Press)));

            Program.Menu.AddSubMenu(new Menu("Killsteal", "Killsteal"));
            Program.Menu.SubMenu("Killsteal").AddItem(new MenuItem("killstealQ", "Use Q-Spell to Killsteal").SetValue(true));
            Program.Menu.SubMenu("Killsteal").AddItem(new MenuItem("killstealE", "Use E-Spell to Killsteal").SetValue(true));
            Program.Menu.SubMenu("Killsteal").AddItem(new MenuItem("killstealR", "Use R-Spell to Killsteal").SetValue(true));

            Program.Menu.AddSubMenu(new Menu("Farm", "Farm"));
            Program.Menu.SubMenu("Farm").AddItem(new MenuItem("UseQFarm", "Use Q Farm").SetValue(true));
            Program.Menu.SubMenu("Farm").AddItem(new MenuItem("UseEFarm", "Use E Farm").SetValue(true));
            Program.Menu.SubMenu("Farm").AddItem(new MenuItem("ManaFarm", "Min Mana").SetValue(new Slider(60, 100, 0)));           
            Program.Menu.SubMenu("Farm").AddItem(new MenuItem("LaneClearActive", "LaneClear!").SetValue(new KeyBind("V".ToCharArray()[0], KeyBindType.Press)));

            Program.Menu.AddSubMenu(new Menu("JungleFarm", "JungleFarm"));
            Program.Menu.SubMenu("JungleFarm").AddItem(new MenuItem("UseQJFarm", "Use Q").SetValue(true));
            Program.Menu.SubMenu("JungleFarm").AddItem(new MenuItem("UseWJFarm", "Use W").SetValue(true));
            Program.Menu.SubMenu("JungleFarm").AddItem(new MenuItem("UseEJFarm", "Use E").SetValue(true));
            Program.Menu.SubMenu("JungleFarm").AddItem(new MenuItem("AutoSmite", "Auto Smite!").SetValue<KeyBind>(new KeyBind('J', KeyBindType.Toggle)));
            Program.Menu.SubMenu("JungleFarm").AddItem(new MenuItem("JungleFarmActive", "JungleFarm!").SetValue(new KeyBind("V".ToCharArray()[0], KeyBindType.Press)));

            Program.Menu.AddSubMenu(new Menu("Misc", "Misc")); 
            Program.Menu.SubMenu("Misc").AddItem(new MenuItem("AutoW", "Auto W under Turrets").SetValue(false));
            Program.Menu.SubMenu("Misc").AddItem(new MenuItem("gapClose", "Auto-Knockback Gapclosers").SetValue(true));
            Program.Menu.SubMenu("Misc").AddItem(new MenuItem("stun", "Auto-Interrupt Important Spells").SetValue(true));

            var dmgAfterComboItem = new MenuItem("DamageAfterCombo", "Draw damage after a rotation").SetValue(true);
            Utility.HpBarDamageIndicator.DamageToUnit += hero => (float)(ObjectManager.Player.GetSpellDamage(hero, SpellSlot.Q) + ObjectManager.Player.GetSpellDamage(hero, SpellSlot.W) + ObjectManager.Player.GetSpellDamage(hero, SpellSlot.E) + ObjectManager.Player.GetSpellDamage(hero, SpellSlot.R));
            Utility.HpBarDamageIndicator.Enabled = dmgAfterComboItem.GetValue<bool>();
            dmgAfterComboItem.ValueChanged += delegate(object sender, OnValueChangeEventArgs eventArgs)
            {
                Utility.HpBarDamageIndicator.Enabled = eventArgs.GetNewValue<bool>();
            };

            Program.Menu.AddSubMenu(new Menu("Drawing", "Drawing"));
            Program.Menu.SubMenu("Drawing").AddItem(new MenuItem("Draw_Disabled", "Disable All").SetValue(false));
            Program.Menu.SubMenu("Drawing").AddItem(new MenuItem("Draw_Q", "Draw Q").SetValue(true));
            Program.Menu.SubMenu("Drawing").AddItem(new MenuItem("Draw_W", "Draw W").SetValue(true));
            Program.Menu.SubMenu("Drawing").AddItem(new MenuItem("Draw_E", "Draw E").SetValue(true));
            Program.Menu.SubMenu("Drawing").AddItem(dmgAfterComboItem);
        }

        private void Drawing_OnDraw(EventArgs args)
        {
            if (Program.Menu.Item("Draw_Disabled").GetValue<bool>())
                return;            

            if (Program.Menu.Item("Draw_Q").GetValue<bool>())
                if (Q.Level > 0)
                    Utility.DrawCircle(ObjectManager.Player.Position, Q.Range, Q.IsReady() ? Color.Green : Color.Red);

            if (Program.Menu.Item("Draw_E").GetValue<bool>())
                if (E.Level > 0)
                    Utility.DrawCircle(ObjectManager.Player.Position, E.Range, E.IsReady() ? Color.Green : Color.Red);

            if (Program.Menu.Item("Draw_W").GetValue<bool>())
                if (W.Level > 0)
                    Utility.DrawCircle(ObjectManager.Player.Position, W.Range, W.IsReady() ? Color.Green : Color.Red);
        }

        private void Game_OnGameUpdate(EventArgs args)
        {
            if (Player.IsDead) return;

            if (Program.Menu.Item("ComboActive").GetValue<KeyBind>().Active)
            {
                Combo();
            }
            else
            {
                if (Program.Menu.Item("HarassActive").GetValue<KeyBind>().Active)
                    Harass();

                if (Program.Menu.Item("GanksActive").GetValue<KeyBind>().Active)
                    Ganks();

                if (Program.Menu.Item("harassToggle").GetValue<KeyBind>().Active)
                    ToggleHarass();

                if (Program.Menu.Item("LaneClearActive").GetValue<KeyBind>().Active)
                    LaneClear();

                if (Program.Menu.Item("JungleFarmActive").GetValue<KeyBind>().Active)
                    JungleFarm();

                if (Program.Menu.Item("AutoW").GetValue<bool>())
                    AutoUnderTower();

                if (Program.Menu.Item("AutoSmite").GetValue<KeyBind>().Active)
                    AutoSmite();                
            }
        }      

        private void AutoSmite()
        {
            if (Program.Menu.Item("AutoSmite").GetValue<KeyBind>().Active)
            {
                float[] SmiteDmg = { 20 * Player.Level + 370, 30 * Player.Level + 330, 40 * Player.Level + 240, 50 * Player.Level + 100 };
                string[] MonsterNames = { "LizardElder", "AncientGolem", "Worm", "Dragon" };
                var vMinions = MinionManager.GetMinions(Player.ServerPosition, Player.SummonerSpellbook.Spells.FirstOrDefault(
                    spell => spell.Name.Contains("smite")).SData.CastRange[0], MinionTypes.All, MinionTeam.NotAlly, MinionOrderTypes.Health);
                foreach (var vMinion in vMinions)
                {
                    if (vMinion != null
                        && !vMinion.IsDead
                        && !Player.IsDead
                        && !Player.IsStunned
                        && SmiteSlot != SpellSlot.Unknown
                        && Player.SummonerSpellbook.CanUseSpell(SmiteSlot) == SpellState.Ready)
                    {
                        if ((vMinion.Health < SmiteDmg.Max()) && (MonsterNames.Any(name => vMinion.BaseSkinName.StartsWith(name))))
                        {
                            Player.SummonerSpellbook.CastSpell(SmiteSlot, vMinion);                          

                        }
                    }
                }
            }
        }

        private void AutoUlt()
        {
            int inimigos = Utility.CountEnemysInRange(650);

            var RMana = Program.Menu.Item("ManaR").GetValue<Slider>().Value;
            var MPercentR = Player.Mana * 100 / Player.MaxMana;

            if (Program.Menu.Item("MinR").GetValue<Slider>().Value <= inimigos && MPercentR >= RMana)
            {
                R.Cast();
            }

        }

        private void AutoUnderTower()
        {
            var wTarget = SimpleTs.GetTarget(W.Range + W.Width, SimpleTs.DamageType.Magical);

            if (Utility.UnderTurret(wTarget, false) && W.IsReady())
            {
                W.CastOnUnit(wTarget, Packets());                
            }
        }

        private void Combo()
        {            
            var wTarget = SimpleTs.GetTarget(W.Range + W.Width, SimpleTs.DamageType.Magical);
            
            if (wTarget != null && Program.Menu.Item("UseWCombo").GetValue<bool>() && W.IsReady())
            {
                W.CastOnUnit(wTarget, Packets());
            }
            if (Program.Menu.Item("UseQCombo").GetValue<bool>())
            {
                Cast_BasicLineSkillshot_Enemy(Q);
            }
            if (Program.Menu.Item("UseECombo").GetValue<bool>())
            {
                Cast_BasicCircleSkillshot_Enemy(E);
            }
            if (Program.Menu.Item("UseRCombo").GetValue<bool>() && R.IsReady())
            {
                AutoUlt();
            }
        }

        private void Ganks()
        {            
            var wTarget = SimpleTs.GetTarget(W.Range + W.Width, SimpleTs.DamageType.Magical);
            
            if (Program.Menu.Item("UseQGank").GetValue<bool>())
            {
                Cast_BasicLineSkillshot_Enemy(Q);
            }
            if (wTarget != null && Program.Menu.Item("UseWGank").GetValue<bool>() && W.IsReady())
            {
                W.CastOnUnit(wTarget, Packets());
            }
            if (Program.Menu.Item("UseEGank").GetValue<bool>())
            {
                Cast_BasicCircleSkillshot_Enemy(E);
            }
        }

        private void Harass()
        { 
            if (Program.Menu.Item("UseQHarass").GetValue<bool>())
            {
                Cast_BasicLineSkillshot_Enemy(Q);
            }
            if (Program.Menu.Item("UseEHarass").GetValue<bool>())
            {
                Cast_BasicCircleSkillshot_Enemy(E);
            }
        }

        private void ToggleHarass()
        {  
            if (Program.Menu.Item("UseQHarass").GetValue<bool>())
            {
                Cast_BasicLineSkillshot_Enemy(Q);
            }
            if (Program.Menu.Item("UseEHarass").GetValue<bool>())
            {
                Cast_BasicCircleSkillshot_Enemy(E);
            }
        }

        private void LaneClear()
        {
            var FMana = Program.Menu.Item("ManaFarm").GetValue<Slider>().Value;
            var MPercent = Player.Mana * 100 / Player.MaxMana;            

            if (Program.Menu.Item("UseQFarm").GetValue<bool>() && MPercent >= FMana)
            {
                Cast_BasicLineSkillshot_AOE_Farm(Q);
            }
            if (Program.Menu.Item("UseEFarm").GetValue<bool>() && MPercent >= FMana)
            {
                Cast_BasicCircleSkillshot_AOE_Farm(E, 150);
            }
        }

        private void JungleFarm()
        {
            var mobs = MinionManager.GetMinions(Player.ServerPosition, Q.Range,
                MinionTypes.All,
                MinionTeam.Neutral, MinionOrderTypes.MaxHealth);
            if (mobs.Count > 0)
            {
                var mob = mobs[0];
                W.CastOnUnit(mob, Packets());
                Q.Cast(mob, Packets());
                E.Cast(mob, Packets());
            }
        }

        private void AntiGapcloser_OnEnemyGapcloser(ActiveGapcloser gapcloser)
        {
            if (!Program.Menu.Item("gapClose").GetValue<bool>()) return;

            if (gapcloser.Sender.IsValidTarget(400f))
            {
                Q.Cast(gapcloser.Sender);
            }
        }

        private void Interrupter_OnPossibleToInterrupt(Obj_AI_Base unit, InterruptableSpell spell)
        {
            if (!Program.Menu.Item("stun").GetValue<bool>()) return;

            if (unit.IsValidTarget(600f) && (spell.DangerLevel != InterruptableDangerLevel.Low))
            {
                if (Q.IsReady())
                {
                    Q.Cast(unit);
                }

                if (!Q.IsReady() && unit.IsValidTarget(W.Range))
                {
                    W.CastOnUnit(unit);
                }                
            }
        }

    }
}
