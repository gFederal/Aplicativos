#region

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;

#endregion

namespace FedAllChampionsUtility
{
    class Leblanc : Champion
    {
        public static List<Spell> SpellList = new List<Spell>();

        public static Spell Q;
        public static Spell W;
        public static Spell E;
        public static Spell R;

        public static SpellSlot IgniteSlot;
        public static Items.Item DFG;        

        public Leblanc()
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
            Q = new Spell(SpellSlot.Q, 700);
            W = new Spell(SpellSlot.W, 600);
            E = new Spell(SpellSlot.E, 950);
            R = new Spell(SpellSlot.R, 700);

            IgniteSlot = ObjectManager.Player.GetSpellSlot("SummonerDot");
            DFG = Utility.Map.GetMap()._MapType == Utility.Map.MapType.TwistedTreeline ? new Items.Item(3188, 750) : new Items.Item(3128, 750);

            W.SetSkillshot(0.5f, 220, 1300, false, SkillshotType.SkillshotCircle);
            E.SetSkillshot(0.25f, 95, 1600, true, SkillshotType.SkillshotLine);

            SpellList.AddRange(new[] { Q, W, E, R });
        }

        private void LoadMenu()
        {
            Program.Menu.AddSubMenu(new Menu("Combo", "Combo"));            
            Program.Menu.SubMenu("Combo").AddItem(new MenuItem("UseQCombo", "Use Q").SetValue(true));
            Program.Menu.SubMenu("Combo").AddItem(new MenuItem("UseWCombo", "Use W").SetValue(true));
            Program.Menu.SubMenu("Combo").AddItem(new MenuItem("UseECombo", "Use E").SetValue(true));
            Program.Menu.SubMenu("Combo").AddItem(new MenuItem("UseRCombo", "Use R").SetValue(true));
            Program.Menu.SubMenu("Combo").AddItem(new MenuItem("UseDFGCombo", "Use DFG").SetValue(true));
            Program.Menu.SubMenu("Combo").AddItem(new MenuItem("BackCombo", "Back W LowHP/MP or delay").SetValue(true));
            Program.Menu.SubMenu("Combo").AddItem(new MenuItem("UseIgniteCombo", "Use Ignite").SetValue(true));
            Program.Menu.SubMenu("Combo").AddItem(new MenuItem("ComboActive", "Combo Q+R+W+E!").SetValue(new KeyBind(32, KeyBindType.Press)));
            Program.Menu.SubMenu("Combo").AddItem(new MenuItem("ComboActive2", "Combo Q+W+R+E!").SetValue(new KeyBind("Z".ToCharArray()[0], KeyBindType.Press)));

            Program.Menu.AddSubMenu(new Menu("Harass", "Harass"));
            Program.Menu.SubMenu("Harass").AddItem(new MenuItem("UseQHarass", "Use Q").SetValue(true));
            Program.Menu.SubMenu("Harass").AddItem(new MenuItem("UseWHarass", "Use W").SetValue(true));
            Program.Menu.SubMenu("Harass").AddItem(new MenuItem("UseEHarass", "Use E").SetValue(false));
            Program.Menu.SubMenu("Harass").AddItem(new MenuItem("UseRHarass", "Use R").SetValue(false));
            Program.Menu.SubMenu("Harass").AddItem(new MenuItem("UseWQHarass", "Use W+Q Out Range").SetValue(true));
            Program.Menu.SubMenu("Harass").AddItem(new MenuItem("BackHarass", "Back W end Harass").SetValue(true));
            Program.Menu.SubMenu("Harass").AddItem(new MenuItem("harassToggleQ", "Use Q (toggle)").SetValue<KeyBind>(new KeyBind('T', KeyBindType.Toggle)));
            Program.Menu.SubMenu("Harass").AddItem(new MenuItem("HarassActive", "Harass!").SetValue(new KeyBind("C".ToCharArray()[0], KeyBindType.Press)));

            Program.Menu.AddSubMenu(new Menu("Farm", "Farm"));
            Program.Menu.SubMenu("Farm").AddItem(new MenuItem("UseQFarm", "Use Q").SetValue(new StringList(new[] { "Freeze", "LaneClear", "Both", "No" }, 2)));
            Program.Menu.SubMenu("Farm").AddItem(new MenuItem("UseWFarm", "Use W").SetValue(new StringList(new[] { "Freeze", "LaneClear", "Both", "No" }, 3)));
            Program.Menu.SubMenu("Farm").AddItem(new MenuItem("UseEFarm", "Use E").SetValue(new StringList(new[] { "Freeze", "LaneClear", "Both", "No" }, 3)));
            Program.Menu.SubMenu("Farm").AddItem(new MenuItem("ManaFarm", "Min Mana").SetValue(new Slider(50, 100, 0)));
            Program.Menu.SubMenu("Farm").AddItem(new MenuItem("waveNumW", "Minions to hit with W").SetValue<Slider>(new Slider(4, 1, 10)));
            Program.Menu.SubMenu("Farm").AddItem(new MenuItem("FreezeActive", "Freeze!").SetValue(new KeyBind("C".ToCharArray()[0], KeyBindType.Press)));
            Program.Menu.SubMenu("Farm").AddItem(new MenuItem("LaneClearActive", "LaneClear!").SetValue(new KeyBind("V".ToCharArray()[0], KeyBindType.Press)));

            Program.Menu.AddSubMenu(new Menu("JungleFarm", "JungleFarm"));
            Program.Menu.SubMenu("JungleFarm").AddItem(new MenuItem("UseQJFarm", "Use Q").SetValue(true));
            Program.Menu.SubMenu("JungleFarm").AddItem(new MenuItem("UseWJFarm", "Use W").SetValue(true));
            Program.Menu.SubMenu("JungleFarm").AddItem(new MenuItem("UseEJFarm", "Use E").SetValue(true));
            Program.Menu.SubMenu("JungleFarm").AddItem(new MenuItem("JungleFarmActive", "JungleFarm!").SetValue(new KeyBind("V".ToCharArray()[0], KeyBindType.Press)));

            var dmgAfterComboItem = new MenuItem("DamageAfterCombo", "Draw damage after a rotation").SetValue(true);
            Utility.HpBarDamageIndicator.DamageToUnit = GetComboDamage;
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
            Program.Menu.SubMenu("Drawing").AddItem(new MenuItem("WQRange", "WQ range").SetValue(new Circle(false, Color.FromArgb(150, Color.DodgerBlue))));            
            Program.Menu.SubMenu("Drawing").AddItem(dmgAfterComboItem);

        }

        private static void AntiGapcloser_OnEnemyGapcloser(ActiveGapcloser gapcloser)
        {
            PredictionOutput ePred = E.GetPrediction(gapcloser.Sender);
            if (ePred.Hitchance >= HitChance.High)
                E.Cast(ePred.CastPosition);
        }
        private static void Interrupter_OnPossibleToInterrupt(Obj_AI_Base unit, InterruptableSpell spell)
        {
            PredictionOutput ePred = E.GetPrediction(unit);
            if (ePred.Hitchance >= HitChance.High)
                E.Cast(ePred.CastPosition);
        }

        private static void Drawing_OnDraw(EventArgs args)
        {
            if (Program.Menu.Item("Draw_Disabled").GetValue<bool>())
                return;
            
            //Draw the ranges of the spells.
            var menuItem = Program.Menu.Item("WQRange").GetValue<Circle>();
            if (menuItem.Active) Utility.DrawCircle(ObjectManager.Player.Position, W.Range + Q.Range, menuItem.Color);            

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

        private static void Game_OnGameUpdate(EventArgs args)
        {
            if (ObjectManager.Player.IsDead) return;

            if (Program.Menu.Item("ComboActive").GetValue<KeyBind>().Active)
            {
                Combo1();
            }
            else
            {
                if (Program.Menu.Item("ComboActive2").GetValue<KeyBind>().Active)
                {
                    Combo2();
                }
                
                if (Program.Menu.Item("HarassActive").GetValue<KeyBind>().Active)
                    Harass();

                if (Program.Menu.Item("harassToggleQ").GetValue<KeyBind>().Active)
                    ToggleHarass();

                var lc = Program.Menu.Item("LaneClearActive").GetValue<KeyBind>().Active;
                if (lc || Program.Menu.Item("FreezeActive").GetValue<KeyBind>().Active)
                    Farm(lc);

                if (Program.Menu.Item("JungleFarmActive").GetValue<KeyBind>().Active)
                    JungleFarm();
            }

        }

        private static float GetComboDamage(Obj_AI_Base enemy)
        {
            var damage = 0d;

            if (Q.IsReady())
                damage += ObjectManager.Player.GetSpellDamage(enemy, SpellSlot.Q);

            if (DFG.IsReady())
                damage += ObjectManager.Player.GetItemDamage(enemy, Damage.DamageItems.Dfg) / 1.2;

            if (W.IsReady())
                damage += ObjectManager.Player.GetSpellDamage(enemy, SpellSlot.W);

            if (E.IsReady())
                damage += ObjectManager.Player.GetSpellDamage(enemy, SpellSlot.E);

            if (IgniteSlot != SpellSlot.Unknown && ObjectManager.Player.SummonerSpellbook.CanUseSpell(IgniteSlot) == SpellState.Ready)
                damage += ObjectManager.Player.GetSummonerSpellDamage(enemy, Damage.SummonerSpell.Ignite);

            if (R.IsReady())
                damage += ObjectManager.Player.GetSpellDamage(enemy, SpellSlot.Q);

            return (float)damage * (DFG.IsReady() ? 1.2f : 1);
        }        

        private static void Combo1() // Q+R+W+E
        {
            var qTarget = SimpleTs.GetTarget(Q.Range, SimpleTs.DamageType.Magical);
            var wTarget = SimpleTs.GetTarget(W.Range, SimpleTs.DamageType.Magical);
            var rTarget = SimpleTs.GetTarget(Q.Range, SimpleTs.DamageType.Magical);
            var eTarget = SimpleTs.GetTarget(E.Range, SimpleTs.DamageType.Magical);
            var comboDamage = wTarget != null ? GetComboDamage(wTarget) : 0;

            var useQ = Program.Menu.Item("UseQCombo").GetValue<bool>();
            var useW = Program.Menu.Item("UseWCombo").GetValue<bool>();
            var useE = Program.Menu.Item("UseECombo").GetValue<bool>();
            var useR = Program.Menu.Item("UseRCombo").GetValue<bool>();
            var userDFG = Program.Menu.Item("UseDFGCombo").GetValue<bool>();
            var UseIgniteCombo = Program.Menu.Item("UseIgniteCombo").GetValue<bool>();

            if (Q.IsReady() && R.IsReady() && useQ && useR)
            {
                if (qTarget != null)
                {
                    Q.CastOnUnit(qTarget);

                    if (userDFG && wTarget != null && comboDamage > wTarget.Health && DFG.IsReady() && W.IsReady() && R.IsReady())
                    {
                        DFG.Cast(wTarget);
                    }

                    if (ObjectManager.Player.Spellbook.GetSpell(SpellSlot.R).Name.Contains("LeblancChaos"))
                    {
                        R.CastOnUnit(qTarget);
                    }
                }
            }
            else
            {
                if (useW && wTarget != null && W.IsReady() && !LeblancPulo())
                {
                    W.CastOnUnit(wTarget);
                }

                if (useE && eTarget != null && E.IsReady())
                {
                    PredictionOutput ePred = E.GetPrediction(eTarget);
                    if (ePred.Hitchance >= HitChance.High)
                        E.Cast(ePred.CastPosition);
                }

                if (useQ && qTarget != null && Q.IsReady())
                {
                    Q.CastOnUnit(qTarget);
                }

                if (useR && qTarget != null && R.IsReady() && ObjectManager.Player.Spellbook.GetSpell(SpellSlot.R).Name.Contains("LeblancChaos"))
                {
                    R.Cast(qTarget);
                }

                if (Program.Menu.Item("BackCombo").GetValue<bool>() && LeblancPulo() && (qTarget == null ||
                    !W.IsReady() && !Q.IsReady() && !R.IsReady() ||
                    GetHPPercent() < 15 ||
                    GetMPPercent() < 15))
                {
                    W.Cast();
                }
            }

            if (wTarget != null && IgniteSlot != SpellSlot.Unknown &&
                        ObjectManager.Player.SummonerSpellbook.CanUseSpell(IgniteSlot) == SpellState.Ready)
            {
                if (ObjectManager.Player.Distance(wTarget) < 650 && comboDamage > wTarget.Health && UseIgniteCombo)
                {
                    ObjectManager.Player.SummonerSpellbook.CastSpell(IgniteSlot, wTarget);
                }
            }
        }

        private static void Combo2() // Q+W+R+E
        {
            var qTarget = SimpleTs.GetTarget(Q.Range, SimpleTs.DamageType.Magical);
            var wTarget = SimpleTs.GetTarget(W.Range, SimpleTs.DamageType.Magical);
            var rTarget = SimpleTs.GetTarget(Q.Range, SimpleTs.DamageType.Magical);
            var eTarget = SimpleTs.GetTarget(E.Range, SimpleTs.DamageType.Magical);
            var comboDamage = wTarget != null ? GetComboDamage(wTarget) : 0;

            var useQ = Program.Menu.Item("UseQCombo").GetValue<bool>();
            var useW = Program.Menu.Item("UseWCombo").GetValue<bool>();
            var useE = Program.Menu.Item("UseECombo").GetValue<bool>();
            var useR = Program.Menu.Item("UseRCombo").GetValue<bool>();
            var userDFG = Program.Menu.Item("UseDFGCombo").GetValue<bool>();
            var UseIgniteCombo = Program.Menu.Item("UseIgniteCombo").GetValue<bool>();

            ObjectManager.Player.IssueOrder(GameObjectOrder.MoveTo, Game.CursorPos);

            if (Q.IsReady() && W.IsReady() && R.IsReady() && useQ && useW && useR)
            {
                if (Q.IsReady() && qTarget != null)
                {
                    Q.CastOnUnit(qTarget);
                }

                if (userDFG && wTarget != null && comboDamage > wTarget.Health && DFG.IsReady() && W.IsReady() && R.IsReady())
                {
                    DFG.Cast(wTarget);
                }

                if (wTarget != null && ObjectManager.Player.Spellbook.GetSpell(SpellSlot.R).Name.Contains("LeblancChaos") && !LeblancPulo())
                {
                    W.CastOnUnit(wTarget);
                }

                if (rTarget != null && LeblancPulo())
                {
                    R.CastOnUnit(rTarget);
                }
            }
            else
            {
                if (rTarget != null && LeblancPulo())
                {
                    R.CastOnUnit(qTarget);
                }

                if (useE && eTarget != null && E.IsReady())
                {
                    PredictionOutput ePred = E.GetPrediction(eTarget);
                    if (ePred.Hitchance >= HitChance.High)
                        E.Cast(ePred.CastPosition);
                }

                if (useW && wTarget != null && W.IsReady() && !LeblancPulo())
                {
                    W.CastOnUnit(wTarget);
                }

                if (useQ && qTarget != null && Q.IsReady())
                {
                    Q.CastOnUnit(qTarget);
                }                

                if (Program.Menu.Item("BackCombo").GetValue<bool>() && LeblancPulo() && (qTarget == null && wTarget == null && eTarget == null && rTarget == null ||
                !W.IsReady() && !Q.IsReady() && !R.IsReady() && !E.IsReady() ||
                GetHPPercent() < 15 ||
                GetMPPercent() < 10))
                {
                    W.Cast();
                }
            }
            

            if (wTarget != null && IgniteSlot != SpellSlot.Unknown &&
                        ObjectManager.Player.SummonerSpellbook.CanUseSpell(IgniteSlot) == SpellState.Ready)
            {
                if (ObjectManager.Player.Distance(wTarget) < 650 && comboDamage > wTarget.Health && UseIgniteCombo)
                {
                    ObjectManager.Player.SummonerSpellbook.CastSpell(IgniteSlot, wTarget);
                }
            }
        }        

        private static float GetHPPercent()
        {
            return (ObjectManager.Player.Health / ObjectManager.Player.MaxHealth) * 100f;
        }
        private static float GetMPPercent()
        {
            return (ObjectManager.Player.Mana / ObjectManager.Player.MaxMana) * 100f;
        }

        private static void ToggleHarass()
        {
            var target = SimpleTs.GetTarget(Q.Range, SimpleTs.DamageType.Magical);

            if (target != null && Q.IsReady())
            {
                Q.CastOnUnit(target);
            }

        }

        private static bool LeblancPulo()
        {
            if (!W.IsReady() || ObjectManager.Player.Spellbook.GetSpell(SpellSlot.W).Name == "leblancslidereturn")
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        
        private static void Harass()
        {
            var target = SimpleTs.GetTarget(Q.Range, SimpleTs.DamageType.Magical);
            var rtarget = SimpleTs.GetTarget(W.Range + Q.Range, SimpleTs.DamageType.Magical);

            if (Program.Menu.Item("UseWQHarass").GetValue<bool>() && ObjectManager.Player.Distance(rtarget) > Q.Range && ObjectManager.Player.Distance(rtarget) <= W.Range + Q.Range)
            {
                if (W.IsReady() && Q.IsReady())
                {
                    W.Cast(rtarget.ServerPosition);
                }

                if (target != null && Program.Menu.Item("UseQHarass").GetValue<bool>() && Q.IsReady() && ObjectManager.Player.Spellbook.GetSpell(SpellSlot.R).Name.Contains("LeblancSlideM"))
                {
                    Q.CastOnUnit(target);
                }

            }
            else
            {
                if (target != null && Program.Menu.Item("UseRHarass").GetValue<bool>() && R.IsReady() && ObjectManager.Player.Spellbook.GetSpell(SpellSlot.R).Name.Contains("LeblancChaos"))
                {
                    R.CastOnUnit(target);
                }

                if (target != null && Program.Menu.Item("UseQHarass").GetValue<bool>() && Q.IsReady())
                {
                    Q.CastOnUnit(target);
                }

                if (target != null && Program.Menu.Item("UseWHarass").GetValue<bool>() && !LeblancPulo())
                {
                    W.CastOnUnit(target);
                }

                if (target != null && Program.Menu.Item("UseEHarass").GetValue<bool>())
                {
                    PredictionOutput ePred = E.GetPrediction(target);
                    if (ePred.Hitchance >= HitChance.High)
                        E.Cast(ePred.CastPosition);
                }

                if (Program.Menu.Item("UseWQHarass").GetValue<bool>() && Program.Menu.Item("BackHarass").GetValue<bool>() && LeblancPulo() && !Q.IsReady())
                {
                    if (Program.Menu.Item("UseRHarass").GetValue<bool>() && R.IsReady()) return;
                    if (Program.Menu.Item("UseEHarass").GetValue<bool>() && E.IsReady()) return;

                    W.Cast();
                }
            }
        }

        private static void Farm(bool laneClear)
        {
            if (!Orbwalking.CanMove(40)) return;

            var rangedMinionsW = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, W.Range + W.Width + 30,
                MinionTypes.Ranged);
            var allMinionsW = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, W.Range + W.Width + 30,
                MinionTypes.All);

            var allMinions = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, Q.Range + Q.Width + 30, MinionTypes.All);

            var FMana = Program.Menu.Item("ManaFarm").GetValue<Slider>().Value;
            var MPercent = ObjectManager.Player.Mana * 100 / ObjectManager.Player.MaxMana;

            // Spell usage
            var useQi = Program.Menu.Item("UseQFarm").GetValue<StringList>().SelectedIndex;
            var useWi = Program.Menu.Item("UseWFarm").GetValue<StringList>().SelectedIndex;
            var useEi = Program.Menu.Item("UseEFarm").GetValue<StringList>().SelectedIndex;
            var useQ = (laneClear && MPercent >= FMana && (useQi == 1 || useQi == 2)) || (!laneClear && MPercent >= FMana && (useQi == 0 || useQi == 2));
            var useW = (laneClear && MPercent >= FMana && (useWi == 1 || useWi == 2)) || (!laneClear && MPercent >= FMana && (useWi == 0 || useWi == 2));
            var useE = (laneClear && MPercent >= FMana && (useEi == 1 || useEi == 2)) || (!laneClear && MPercent >= FMana && (useEi == 0 || useEi == 2));

            var fl1 = W.GetCircularFarmLocation(rangedMinionsW, W.Width);
            var fl2 = W.GetCircularFarmLocation(allMinionsW, W.Width);

            if (useQ)
            {
                foreach (var minion in allMinions)
                {
                    if (minion.IsValidTarget() &&
                        minion.Health < 0.75 * ObjectManager.Player.GetSpellDamage(minion, SpellSlot.Q))
                    {
                        Q.CastOnUnit(minion);
                    }
                }
            }
            else if (useW)
            {
                if (fl1.MinionsHit >= Program.Menu.Item("waveNumW").GetValue<Slider>().Value && fl1.MinionsHit >= 3)
                {
                    W.Cast(fl1.Position);
                }
            }
            else if (useE)
            {
                foreach (var minion in allMinions)
                {
                    if (minion.IsValidTarget(E.Range) &&
                        minion.Health < 0.80 * ObjectManager.Player.GetSpellDamage(minion, SpellSlot.E))
                    {
                        E.Cast(minion);
                    }
                }
            }
            if (laneClear)
            {
                foreach (var minion in allMinions)
                {
                    if (useQ && minion.IsValidTarget() &&
                        minion.Health < 0.80 * ObjectManager.Player.GetSpellDamage(minion, SpellSlot.Q))
                        Q.CastOnUnit(minion);

                    if (useW)

                        if (fl1.MinionsHit >= Program.Menu.Item("waveNumW").GetValue<Slider>().Value && fl1.MinionsHit >= 3)
                        {
                            W.Cast(fl1.Position);
                        }

                    if (useE)
                        E.Cast(minion);
                }
            }
        }

        private static void JungleFarm()
        {
            var mobs = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, W.Range,
                MinionTypes.All,
                MinionTeam.Neutral, MinionOrderTypes.MaxHealth);
            if (mobs.Count > 0)
            {
                var mob = mobs[0];
                Q.Cast(mob);
                W.Cast(mob);
                E.Cast(mob);
            }
        }       
    }
}
