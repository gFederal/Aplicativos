using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;

namespace FedAllChampionsUtility
{

    class Sion : Champion
    { 
        public static Spell Q;
        public static Spell E;

        public static Vector2 QCastPos = new Vector2();
        public Sion()
        {
            LoadSpells();
            LoadMenu();

            Game.PrintChat("Sion Loaded!");
            Game.OnGameUpdate += Game_OnGameUpdate;
            Game.OnGameProcessPacket += Game_OnGameProcessPacket;
            Drawing.OnDraw += Drawing_OnDraw;
            Obj_AI_Hero.OnProcessSpellCast += ObjAiHeroOnOnProcessSpellCast;

            PluginLoaded();

        }

        private void LoadMenu()
        {
            Program.Menu.AddSubMenu(new Menu("Combo", "Combo"));
            Program.Menu.SubMenu("Combo").AddItem(new MenuItem("UseQCombo", "Use Q").SetValue(true));
            Program.Menu.SubMenu("Combo").AddItem(new MenuItem("UseWCombo", "Use W").SetValue(true));
            Program.Menu.SubMenu("Combo").AddItem(new MenuItem("UseECombo", "Use E").SetValue(true));
            Program.Menu.SubMenu("Combo").AddItem(new MenuItem("ComboActive", "Combo!").SetValue(new KeyBind(32, KeyBindType.Press)));
            
            Program.Menu.AddSubMenu(new Menu("R", "R"));
            Program.Menu.SubMenu("R").AddItem(new MenuItem("AntiCamLock", "Avoid locking camera").SetValue(true));
            Program.Menu.SubMenu("R").AddItem(new MenuItem("MoveToMouse", "Move to mouse (Exploit)").SetValue(true));//Disabled by default since its not legit Keepo

        }

        private void LoadSpells()
        {
            Q = new Spell(SpellSlot.Q, 1050);
            Q.SetSkillshot(0.6f, 100f, float.MaxValue, false, SkillshotType.SkillshotLine);
            Q.SetCharged("SionQ", "SionQ", 500, 720, 0.5f);

            E = new Spell(SpellSlot.E, 800);
            E.SetSkillshot(0.25f, 80f, 1800, false, SkillshotType.SkillshotLine);
        }

        private static void ObjAiHeroOnOnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (sender.IsMe && args.SData.Name == "SionQ")
            {
                QCastPos = args.End.To2D();
            }
        }

        static void Drawing_OnDraw(EventArgs args)
        {
            Utility.DrawCircle(ObjectManager.Player.Position, Q.Range, System.Drawing.Color.White);
        }

        static void Game_OnGameProcessPacket(GamePacketEventArgs args)
        {
            if (args.PacketData[0] == 0xFE && Program.Menu.Item("AntiCamLock").GetValue<bool>())
            {
                var p = new GamePacket(args.PacketData);
                if (p.ReadInteger(1) == ObjectManager.Player.NetworkId && p.Size() > 9)
                {
                    args.Process = false;
                }
            }
        }

        static void Game_OnGameUpdate(EventArgs args)
        {
            //Casting R
            if (ObjectManager.Player.HasBuff("SionR"))
            {
                if (Program.Menu.Item("MoveToMouse").GetValue<bool>())
                {
                    var p = ObjectManager.Player.Position.To2D().Extend(Game.CursorPos.To2D(), 500);
                    ObjectManager.Player.IssueOrder(GameObjectOrder.MoveTo, p.To3D());
                }
                return;
            }

            if (Program.Menu.Item("ComboActive").GetValue<KeyBind>().Active)
            {
                var qTarget = SimpleTs.GetTarget(!Q.IsCharging ? Q.ChargedMaxRange / 2 : Q.ChargedMaxRange, SimpleTs.DamageType.Physical);

                var eTarget = SimpleTs.GetTarget(E.Range, SimpleTs.DamageType.Physical);

                if (qTarget != null && Program.Menu.Item("UseQCombo").GetValue<bool>())
                {
                    if (Q.IsCharging)
                    {
                        var start = ObjectManager.Player.ServerPosition.To2D();
                        var end = start.Extend(QCastPos, Q.Range);
                        var direction = (end - start).Normalized();
                        var normal = direction.Perpendicular();

                        var points = new List<Vector2>();
                        var hitBox = qTarget.BoundingRadius;
                        points.Add(start + normal * (Q.Width + hitBox));
                        points.Add(start - normal * (Q.Width + hitBox));
                        points.Add(end + Q.ChargedMaxRange * direction - normal * (Q.Width + hitBox));
                        points.Add(end + Q.ChargedMaxRange * direction + normal * (Q.Width + hitBox));

                        for (int i = 0; i <= points.Count - 1; i++)
                        {
                            var A = points[i];
                            var B = points[i == points.Count - 1 ? 0 : i + 1];

                            if (qTarget.ServerPosition.To2D().Distance(A, B, true, true) < 50 * 50)
                            {
                                Packet.C2S.ChargedCast.Encoded(new Packet.C2S.ChargedCast.Struct((SpellSlot)((byte)Q.Slot), Game.CursorPos.X, Game.CursorPos.X, Game.CursorPos.X)).Send();
                            }
                        }
                        return;
                    }
                    
                    if(Q.IsReady())
                    {
                        Q.StartCharging(qTarget.ServerPosition);
                    }
                }

                if (qTarget != null && Program.Menu.Item("UseWCombo").GetValue<bool>())
                {
                    ObjectManager.Player.Spellbook.CastSpell(SpellSlot.W, ObjectManager.Player);
                }

                if (eTarget != null && Program.Menu.Item("UseECombo").GetValue<bool>())
                {
                    E.Cast(eTarget);
                }
            }
        }
    }
}
