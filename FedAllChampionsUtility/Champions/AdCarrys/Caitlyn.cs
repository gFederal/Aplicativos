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
    class Caitlyn : Champion
    {
        public static Spell Q, W, E, R;        
        public static Vector2 PingLocation;

        const float _spellQSpeed = 2500;
        const float _spellQSpeedMin = 400;
        const float _spellQFarmSpeed = 1600;
        
        public Caitlyn()
        {
            LoadMenu();
            LoadSpells();

            Game.OnGameUpdate += Game_OnGameUpdate;
            Drawing.OnDraw += Drawing_OnDraw;
            Drawing.OnEndScene += Drawing_OnEndScene;
            AntiGapcloser.OnEnemyGapcloser += AntiGapcloser_OnEnemyGapcloser;

            PluginLoaded();

        }

        private void LoadSpells()
        {
            Q = new Spell(SpellSlot.Q, 1300);
            W = new Spell(SpellSlot.W, 800);
            E = new Spell(SpellSlot.E, 950);
            R = new Spell(SpellSlot.R, 2000);

            Q.SetSkillshot(0.5f, 90f, 2200f, false, SkillshotType.SkillshotLine);
            W.SetSkillshot(0.25f, 80f, 2000f, false, SkillshotType.SkillshotCircle);
            E.SetSkillshot(0.25f, 80f, 1600f, true, SkillshotType.SkillshotLine);
            
        }

        private void LoadMenu()
        {
            Program.Menu.AddSubMenu(new Menu("Piltover", "Piltover"));
            Program.Menu.SubMenu("Piltover").AddItem(new MenuItem("UseQ", "Use Q Mode: - ToDo").SetValue(new StringList(new[] { "Combo", "Harass", "Both", "No" }, 1)));
            Program.Menu.SubMenu("Piltover").AddItem(new MenuItem("KillQ", "Auto Q Kill").SetValue(true));
            Program.Menu.SubMenu("Piltover").AddItem(new MenuItem("autoccQ", "AutoPeacemaker on CC").SetValue(true));
            Program.Menu.SubMenu("Piltover").AddItem(new MenuItem("minMinions", "Min. Minions Q LaneClear - ToDo").SetValue(new Slider(6, 0, 10)));            

            Program.Menu.AddSubMenu(new Menu("Trap", "Trap"));
            Program.Menu.SubMenu("Trap").AddItem(new MenuItem("autoccW", "AutoTrap on CC").SetValue(true));
            Program.Menu.SubMenu("Trap").AddItem(new MenuItem("AGCtrap", "AntiGapClose with W").SetValue(true));
            Program.Menu.SubMenu("Trap").AddItem(new MenuItem("casttrap", "Trap on Closest Enemy - ToDo").SetValue(new KeyBind("G".ToCharArray()[0], KeyBindType.Press)));

            Program.Menu.AddSubMenu(new Menu("90 Caliber", "90 Caliber"));
            Program.Menu.SubMenu("90 Caliber").AddItem(new MenuItem("AGConoff", "AntiGapClose with E").SetValue(true));
            Program.Menu.SubMenu("90 Caliber").AddItem(new MenuItem("KillEQ", "Auto E-Q Kill").SetValue(true));
            Program.Menu.SubMenu("90 Caliber").AddItem(new MenuItem("UseEQC", "Use E-Q Combo").SetValue(new KeyBind("Z".ToCharArray()[0], KeyBindType.Press)));
            Program.Menu.SubMenu("90 Caliber").AddItem(new MenuItem("PeelE", "Use E defensively").SetValue(true));

            Program.Menu.AddSubMenu(new Menu("Ace Hole", "Ace Hole"));
            Program.Menu.SubMenu("Ace Hole").AddItem(new MenuItem("rKill", "R Killshot").SetValue(new KeyBind("R".ToCharArray()[0], KeyBindType.Press)));
            Program.Menu.SubMenu("Ace Hole").AddItem(new MenuItem("AutoRKill", "Auto R Kill").SetValue(true));
            Program.Menu.SubMenu("Ace Hole").AddItem(new MenuItem("pingkillable", "Ping Killable Heroes").SetValue(true));

            Program.Menu.AddSubMenu(new Menu("Drawing", "Drawing"));
            Program.Menu.SubMenu("Drawing").AddItem(new MenuItem("Draw_Disabled", "Disable All").SetValue(false));
            Program.Menu.SubMenu("Drawing").AddItem(new MenuItem("Draw_Q", "Draw Q").SetValue(true));
            Program.Menu.SubMenu("Drawing").AddItem(new MenuItem("Draw_W", "Draw W").SetValue(true));
            Program.Menu.SubMenu("Drawing").AddItem(new MenuItem("Draw_E", "Draw E").SetValue(true));
            Program.Menu.SubMenu("Drawing").AddItem(new MenuItem("Draw_R", "Draw R").SetValue(true));            
            Program.Menu.SubMenu("Drawing").AddItem(new MenuItem("DrawRRangeM", "Draw R Range (Minimap)").SetValue(new Circle(true, Color.FromArgb(150, Color.DodgerBlue))));
                        
        }
        private void Game_OnGameUpdate(EventArgs args)
        {
            if (ObjectManager.Player.IsDead) return;

            if (Program.Menu.Item("rKill").GetValue<KeyBind>().Active || Program.Menu.Item("AutoRKill").GetValue<bool>())
            {
                AutoRKill();
            }

            if (Program.Menu.Item("autoccW").GetValue<bool>() || Program.Menu.Item("autoccQ").GetValue<bool>())
            {
                AutoCC();
            }

            if (Program.Menu.Item("PeelE").GetValue<bool>())
            {
                PeelE();
            }

            if (Program.Menu.Item("UseEQC").GetValue<KeyBind>().Active && E.IsReady() && Q.IsReady())
            {
                ComboEQ();
            }

            if (Program.Menu.Item("KillQ").GetValue<bool>() || Program.Menu.Item("KillEQ").GetValue<bool>())
            {
                Killer();
            }            
        }

        private float GetRRange()
        {
            return 1500 + (500 * R.Level);
        }

        private void PeelE()
        {
            foreach (var enemy in ObjectManager.Get<Obj_AI_Hero>())
            {
                if (enemy.IsValidTarget(E.Range) && enemy.Distance(ObjectManager.Player) <= enemy.BoundingRadius + enemy.AttackRange + ObjectManager.Player.BoundingRadius && enemy.IsMelee())
                {
                    E.Cast(enemy);
                }

            }
        }

        private void ComboEQ()
        {
            var vTarget = SimpleTs.GetTarget(E.Range - 50, SimpleTs.DamageType.Physical);            

            if (vTarget.IsValidTarget(E.Range))
            {
                E.Cast(vTarget);
                Vector3 predictedPos = Prediction.GetPrediction(vTarget, Q.Delay).UnitPosition;
                Q.Speed = GetDynamicQSpeed(ObjectManager.Player.Distance(predictedPos));
                Q.CastIfHitchanceEquals(vTarget, HitChance.High, Packets());                    
            }
        }

        float GetDynamicQSpeed(float distance)
        {
            float accelerationrate = Q.Range / (_spellQSpeedMin - _spellQSpeed); // = -0.476...
            return _spellQSpeed + accelerationrate * distance;
        }

        private void Killer()
        {
            var qTarget = SimpleTs.GetTarget(Q.Range - 50, SimpleTs.DamageType.Physical);
            var eTarget = SimpleTs.GetTarget(E.Range - 50, SimpleTs.DamageType.Physical);

            if (Program.Menu.Item("KillQ").GetValue<bool>() && Program.Menu.Item("KillEQ").GetValue<bool>() && Q.IsReady() && E.IsReady() && eTarget.Health < (DamageLib.getDmg(eTarget, DamageLib.SpellType.E) + DamageLib.getDmg(eTarget, DamageLib.SpellType.Q)) * 0.9)
            {
                PredictionOutput ePred = E.GetPrediction(eTarget);
                if (ePred.Hitchance >= HitChance.High)
                    E.Cast(eTarget, Packets(), true);

                Vector3 predictedPos = Prediction.GetPrediction(qTarget, Q.Delay).UnitPosition;
                Q.Speed = GetDynamicQSpeed(ObjectManager.Player.Distance(predictedPos));
                Q.CastIfHitchanceEquals(qTarget, HitChance.High, Packets());
               
            }
            else
            {
                if (Program.Menu.Item("KillQ").GetValue<bool>() && Q.IsReady() && qTarget.Health < (DamageLib.getDmg(qTarget, DamageLib.SpellType.Q) * 0.9))
                {
                    Vector3 predictedPos = Prediction.GetPrediction(qTarget, Q.Delay).UnitPosition;
                    Q.Speed = GetDynamicQSpeed(ObjectManager.Player.Distance(predictedPos));
                    Q.CastIfHitchanceEquals(qTarget, HitChance.High, Packets());
                }
                else
                {
                    if (Program.Menu.Item("KillEQ").GetValue<bool>() && !Q.IsReady() && E.IsReady() && eTarget.Health < (DamageLib.getDmg(qTarget, DamageLib.SpellType.Q) * 0.9))
                    {
                        Vector3 predictedPos = Prediction.GetPrediction(qTarget, Q.Delay).UnitPosition;
                        Q.Speed = GetDynamicQSpeed(ObjectManager.Player.Distance(predictedPos));
                        Q.CastIfHitchanceEquals(qTarget, HitChance.High, Packets());
                    }
                }
            }
        }

        private void AutoRKill()
        {
            var rTarget = SimpleTs.GetTarget(GetRRange()-100, SimpleTs.DamageType.Physical);

            if (R.IsReady() && rTarget != null && rTarget.Health < DamageLib.getDmg(rTarget, DamageLib.SpellType.R) * 0.9) 
            {
                if (ObjectManager.Player.Distance(rTarget) > Q.Range)
                R.CastOnUnit(rTarget);
            }
        }

        private void AutoCC()
        {
            List<Obj_AI_Hero> enemBuffed = getEnemiesBuffs();
            foreach (Obj_AI_Hero enem in enemBuffed)
            {
                if (W.IsReady())
                {
                    W.CastOnUnit(enem);
                }

                if (Q.IsReady())
                {
                    if (Q.GetPrediction(enem).Hitchance >= HitChance.High)
                        Q.Cast(enem, Packets());
                }
            }
        }

        public static List<Obj_AI_Hero> getEnemiesBuffs()
        {
            List<Obj_AI_Hero> enemBuffs = new List<Obj_AI_Hero>();
            foreach (Obj_AI_Hero enem in ObjectManager.Get<Obj_AI_Hero>().Where(enem => enem.IsEnemy))
            {
                foreach (BuffInstance buff in enem.Buffs)
                {
                    if (buff.Name == "zhonyasringshield" || buff.Name == "caitlynyordletrapdebuff" || buff.Name == "powerfistslow" || buff.Name == "aatroxqknockup" || buff.Name == "ahriseducedoom" ||
                        buff.Name == "CurseoftheSadMummy" || buff.Name == "braumstundebuff" || buff.Name == "braumpulselineknockup" || buff.Name == "rupturetarget" || buff.Name == "EliseHumanE" ||
                        buff.Name == "HowlingGaleSpell" || buff.Name == "jarvanivdragonstrikeph2" || buff.Name == "karmaspiritbindroot" || buff.Name == "LuxLightBindingMis" || buff.Name == "lissandrawfrozen" ||
                        buff.Name == "lissandraenemy2" || buff.Name == "unstoppableforceestun" || buff.Name == "maokaiunstablegrowthroot" || buff.Name == "monkeykingspinknockup" || buff.Name == "DarkBindingMissile" ||
                        buff.Name == "namiqdebuff" || buff.Name == "nautilusanchordragroot" || buff.Name == "RunePrison" || buff.Name == "SonaR" || buff.Name == "sejuaniglacialprison" || buff.Name == "swainshadowgrasproot" ||
                        buff.Name == "threshqfakeknockup" || buff.Name == "VeigarStun" || buff.Name == "velkozestun" || buff.Name == "virdunkstun" || buff.Name == "viktorgravitonfieldstun" || buff.Name == "yasuoq3mis" ||
                        buff.Name == "zyragraspingrootshold" || buff.Name == "zyrabramblezoneknockup" || buff.Name == "katarinarsound" || buff.Name == "lissandrarself" || buff.Name == "AlZaharNetherGrasp" || buff.Name == "Meditate" ||
                        buff.Name == "missfortunebulletsound" || buff.Name == "AbsoluteZero" || buff.Name == "pantheonesound" || buff.Name == "VelkozR" || buff.Name == "infiniteduresssound" || buff.Name == "chronorevive" || buff.Type == BuffType.Suppression)                   
                    {
                        enemBuffs.Add(enem);
                        break;
                    }
                }
            }
            return enemBuffs;
        }

        private void Drawing_OnEndScene(EventArgs args)
        {
            if (Program.Menu.Item("Draw_Disabled").GetValue<bool>())
                return;

            if (R.Level == 0) return;
            var menuItem = Program.Menu.Item("DrawRRangeM").GetValue<Circle>();
            if (menuItem.Active)
                Utility.DrawCircle(ObjectManager.Player.Position, GetRRange(), menuItem.Color, 2, 30, true);
            
        }

        private void Drawing_OnDraw(EventArgs args)
        {
            if (Program.Menu.Item("Draw_Disabled").GetValue<bool>())
                return;

            if (Program.Menu.Item("Draw_Q").GetValue<bool>())
                if (Q.Level > 0)
                    Utility.DrawCircle(ObjectManager.Player.Position, Q.Range, Q.IsReady() ? Color.Green : Color.Red);

            if (Program.Menu.Item("Draw_W").GetValue<bool>())
                if (W.Level > 0)
                    Utility.DrawCircle(ObjectManager.Player.Position, W.Range, W.IsReady() ? Color.Green : Color.Red);

            if (Program.Menu.Item("Draw_E").GetValue<bool>())
                if (E.Level > 0)
                    Utility.DrawCircle(ObjectManager.Player.Position, E.Range, E.IsReady() ? Color.Green : Color.Red);

            if (Program.Menu.Item("Draw_R").GetValue<bool>())
                if (R.Level > 0)
                    Utility.DrawCircle(ObjectManager.Player.Position, GetRRange(), R.IsReady() ? Color.Green : Color.Red);

            var victims = "";

            foreach (var target in Program.Helper.EnemyInfo.Where(x =>
             x.Player.IsVisible && x.Player.IsValidTarget(R.Range) && !x.Player.IsDead && DamageLib.getDmg(x.Player, DamageLib.SpellType.R) * 0.9 >= x.Player.Health))
            {
                victims += target.Player.ChampionName + " ";

                if (!R.IsReady() || !Program.Menu.Item("pingkillable").GetValue<bool>() ||
                    (target.LastPinged != 0 && Environment.TickCount - target.LastPinged <= 9000))
                    continue;
                if (!(ObjectManager.Player.Distance(target.Player) < GetRRange()) || !(ObjectManager.Player.Distance(target.Player) > 1200) ||
                    (!target.Player.IsVisible))
                    continue;

                Ping(target.Player.Position.To2D());
                target.LastPinged = Environment.TickCount;
            }
            
        }

        private void Ping(Vector2 position)
        {
            PingLocation = position;
            SimplePing();
            Utility.DelayAction.Add(150, SimplePing);
            Utility.DelayAction.Add(300, SimplePing);
            Utility.DelayAction.Add(400, SimplePing);
            Utility.DelayAction.Add(800, SimplePing);
        }

        private void SimplePing()
        {
            Packet.S2C.Ping.Encoded(new Packet.S2C.Ping.Struct(PingLocation.X, PingLocation.Y, 0, 0, Packet.PingType.FallbackSound)).Process();
        }

        private void AntiGapcloser_OnEnemyGapcloser(ActiveGapcloser gapcloser)
        {
            if (E.IsReady() && gapcloser.Sender.IsValidTarget(E.Range))
                E.Cast(gapcloser.Sender);
        }       

    }
}
