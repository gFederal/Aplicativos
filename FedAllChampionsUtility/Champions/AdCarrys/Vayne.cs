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
    class Vayne : Champion
    {        
        public Spell E, QE, Q, R;        

        public Vayne()
        {
            LoadMenu();
            LoadSpells();

            Drawing.OnDraw += Drawing_OnDraw;            
            Game.OnGameUpdate += Game_OnGameUpdate;
            Orbwalking.AfterAttack += Orbwalking_AfterAttack;
            PluginLoaded();
        }

        private void LoadSpells()
        {
            Q = new Spell(SpellSlot.Q, 300f);
            E = new Spell(SpellSlot.E, 550f);
            QE = new Spell(SpellSlot.Q, 800f);
            R = new Spell(SpellSlot.R, 0f);

            E.SetTargetted(0.25f, 2200f);
            QE.SetTargetted(0.50f, 2200f);
            
        } 
        
        private void LoadMenu()
         {

             Program.Menu.AddSubMenu(new Menu("TeamFight", "TeamFight"));
             Program.Menu.SubMenu("TeamFight").AddItem(new MenuItem("UseQC", "Use Q").SetValue(true));
             Program.Menu.SubMenu("TeamFight").AddItem(new MenuItem("UseEC", "Use E").SetValue(true));
             Program.Menu.SubMenu("TeamFight").AddItem(new MenuItem("UseQEC", "Use QE if not in E range").SetValue(true));
             Program.Menu.SubMenu("TeamFight").AddItem(new MenuItem("UseET", "Use E (Toggle)").SetValue(new KeyBind("T".ToCharArray()[0], KeyBindType.Toggle)));

             Program.Menu.AddSubMenu(new Menu("Passive", "Passive"));
             Program.Menu.SubMenu("Passive").AddItem(new MenuItem("UseEInterrupt", "Use E To Interrupt").SetValue(true));
             Program.Menu.SubMenu("Passive").AddItem(new MenuItem("PushDistance", "E Push Distance").SetValue(new Slider(450, 475, 300)));
             Program.Menu.SubMenu("Passive").AddItem(new MenuItem("AutoR", "Auto R").SetValue(true));
             Program.Menu.SubMenu("Passive").AddItem(new MenuItem("MinR", "Min Enemys for (R)").SetValue<Slider>(new Slider(3, 1, 5)));              

             Program.Menu.AddSubMenu(new Menu("Drawing", "Drawing"));
             Program.Menu.SubMenu("Drawing").AddItem(new MenuItem("Draw_Disabled", "Disable All").SetValue(false));
             Program.Menu.SubMenu("Drawing").AddItem(new MenuItem("Draw_Q", "Draw Q + Attack").SetValue(true));             
             Program.Menu.SubMenu("Drawing").AddItem(new MenuItem("Draw_E", "Draw E").SetValue(false));
             
         }

        public void Game_OnGameUpdate(EventArgs args)
        {
            if ((!E.IsReady()) ||
                ((Program.Orbwalker.ActiveMode != Orbwalking.OrbwalkingMode.Combo || !Program.Menu.Item("UseQEC").GetValue<bool>()) &&
                 !Program.Menu.Item("UseET").GetValue<KeyBind>().Active))
            {
                return;
            }

            var normalTarget = SimpleTs.GetTarget(E.Range, SimpleTs.DamageType.Physical);
            var qeTarget = SimpleTs.GetTarget(200 + E.Range, SimpleTs.DamageType.Physical);
            if (normalTarget.IsValid)
            {
                if (IsStunable(normalTarget))
                {
                    E.Cast(normalTarget);
                }
            }
            else if (qeTarget.IsValid)
            {
                if (!IsStunable(qeTarget) || !Program.Menu.Item("UseQEC").GetValue<bool>() || !Q.IsReady())
                {
                    return;
                }
                Q.Cast(qeTarget);
                Utility.DelayAction.Add(250, () => E.Cast(qeTarget));
            }

            if (Program.Menu.Item("AutoR").GetValue<bool>())
            {
                AutoUlt();
            }
        }

        private void AutoUlt()
        {
            int inimigos = Utility.CountEnemysInRange(650);            

            if (Program.Menu.Item("MinR").GetValue<Slider>().Value <= inimigos)
            {
                R.Cast();
            }

        }

        private void Drawing_OnDraw(EventArgs args)
        {
            if (Program.Menu.Item("Draw_Disabled").GetValue<bool>())
                return;
            
            var myRange = Orbwalking.GetRealAutoAttackRange(null);
            
            if (Program.Menu.Item("Draw_Q").GetValue<bool>())
                if (Q.Level > 0)
                    Utility.DrawCircle(ObjectManager.Player.Position, Q.Range + myRange + 65, Q.IsReady() ? Color.Green : Color.Red);            

            if (Program.Menu.Item("Draw_E").GetValue<bool>())
                if (E.Level > 0)
                    Utility.DrawCircle(ObjectManager.Player.Position, E.Range+107, E.IsReady() ? Color.Green : Color.Red);
            
        }

        public void AntiGapcloser_OnEnemyGapcloser(ActiveGapcloser gapcloser)
        {
            if (E.IsReady() && gapcloser.Sender.IsValidTarget(E.Range))
            {
                E.CastOnUnit(gapcloser.Sender);
            }
        }

        public void Interrupter_OnPossibleToInterrupt(Obj_AI_Base unit, InterruptableSpell spell)
        {
            if (Program.Menu.Item("UseEInterrupt").GetValue<bool>() && unit.IsValidTarget(550f))
            {
                E.Cast(unit);
            }
        }

        public void Orbwalking_AfterAttack(Obj_AI_Base unit, Obj_AI_Base target)
        {
            if (unit.IsMe && Program.Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo && Program.Menu.Item("UseQC").GetValue<bool>())
            {
                CastTumble((Obj_AI_Hero)target);
            }
        }

        public bool IsStunable(Obj_AI_Hero enemy)
        {
            var prediction =
                Vector3.DistanceSquared(enemy.ServerPosition, ObjectManager.Player.ServerPosition) > 550 * 550
                    ? QE.GetPrediction(enemy)
                    : E.GetPrediction(enemy);

            if (prediction.Hitchance.Equals(HitChance.OutOfRange))
            {
                return false;
            }

            for (var i = 0; i < Program.Menu.Item("PushDistance").GetValue<Slider>().Value; i += (int)enemy.BoundingRadius)
            {
                if (
                    NavMesh.GetCollisionFlags(
                        prediction.UnitPosition.To2D().Extend(ObjectManager.Player.ServerPosition.To2D(), -i).To3D())
                        .HasFlag(CollisionFlags.Wall))
                {
                    return true;
                }
            }
            return false;
        }

        public void CastTumble(Obj_AI_Hero target)
        {
            var posAfterTumble =
                ObjectManager.Player.ServerPosition.To2D().Extend(Game.CursorPos.To2D(), 300).To3D();
            var distanceAfterTumble = Vector3.DistanceSquared(posAfterTumble, target.ServerPosition);
            if (distanceAfterTumble < 550 * 550 && distanceAfterTumble > 100 * 100)
            {
                Q.Cast(Game.CursorPos);
            }
        }        
        
    }
}
