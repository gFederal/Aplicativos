using System;
using System.Collections.Generic;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;

using Color = System.Drawing.Color;

namespace FedAllChampionsUtility
{
    class Yasuo : Champion
    {
        public static Obj_AI_Hero Player = ObjectManager.Player;
        public static List<Obj_SpellMissile> skillShots = new List<Obj_SpellMissile>();

        public static string lastSpell = "";
        public static int afterDash = 0;
        public static List<string> WIgnore = null;

        public static Spellbook sBook = Player.Spellbook;
        public static SpellDataInst Qdata = sBook.GetSpell(SpellSlot.Q);
        public static SpellDataInst Wdata = sBook.GetSpell(SpellSlot.W);
        public static SpellDataInst Edata = sBook.GetSpell(SpellSlot.E);
        public static SpellDataInst Rdata = sBook.GetSpell(SpellSlot.R);

        public static Spell Q, QEmp, QCir, W, E, R;
        public static List<Spell> SpellList = new List<Spell>();
        
        public Yasuo()
        {
            LoadMenu();
            LoadSpells();

            Game.OnGameUpdate += OnGameUpdate;
            GameObject.OnCreate += OnCreateObject;
            GameObject.OnDelete += OnDeleteObject;
            Obj_AI_Base.OnProcessSpellCast += OnProcessSpell;

            PluginLoaded();
        }

        private void LoadMenu()
        {
            Program.Menu.AddSubMenu(new Menu("TeamFight", "TeamFight"));
            Program.Menu.SubMenu("TeamFight").AddItem(new MenuItem("smartW", "Smart W").SetValue(true));
            Program.Menu.SubMenu("TeamFight").AddItem(new MenuItem("smartR", "Smart R").SetValue(true));
            Program.Menu.SubMenu("TeamFight").AddItem(new MenuItem("useRHit", "Use R if hit").SetValue(new Slider(3, 5, 1)));

            Program.Menu.AddSubMenu(new Menu("Harass", "Harass"));
            Program.Menu.SubMenu("Harass").AddItem(new MenuItem("harassTower", "Harass under tower").SetValue(false));
            Program.Menu.SubMenu("Harass").AddItem(new MenuItem("harassOn", "Harass enemies").SetValue(true));
            Program.Menu.SubMenu("Harass").AddItem(new MenuItem("harQ3Only", "Use only Q3").SetValue(true));

            Program.Menu.AddSubMenu(new Menu("LaneClear", "LaneClear"));
            Program.Menu.SubMenu("LaneClear").AddItem(new MenuItem("useQlc", "Use Q").SetValue(true));
            Program.Menu.SubMenu("LaneClear").AddItem(new MenuItem("useEmpQHit", "Emp Q Min hit")).SetValue(new Slider(3, 6, 1));
            Program.Menu.SubMenu("LaneClear").AddItem(new MenuItem("useElc", "Use E").SetValue(true));

            Program.Menu.AddSubMenu(new Menu("Passive", "Passive"));
            Program.Menu.SubMenu("Passive").AddItem(new MenuItem("djTur", "Dont Jump turrets").SetValue(true));            
            Program.Menu.SubMenu("Passive").AddItem(new MenuItem("useR_KS", "Use R for KS").SetValue(true));
            Program.Menu.SubMenu("Passive").AddItem(new MenuItem("flee", "Flee E").SetValue(new KeyBind('Z', KeyBindType.Press, false)));            
            
        }

        private void LoadSpells()
        {
            Q = new Spell(SpellSlot.Q, 475f);
            QEmp = new Spell(SpellSlot.Q, 900f);
            QCir = new Spell(SpellSlot.Q, 320f);

            W = new Spell(SpellSlot.W, 400f);
            E = new Spell(SpellSlot.E, 475f);
            R = new Spell(SpellSlot.R, 1200f);

            Q.SetSkillshot(0.25f, 50f, float.MaxValue, false, SkillshotType.SkillshotLine);
            QEmp.SetSkillshot(0.25f, 50f, 1200f, false, SkillshotType.SkillshotLine);
            QCir.SetSkillshot(0f, 375f, float.MaxValue, false, SkillshotType.SkillshotCircle);

            // Add to spell list
            SpellList.AddRange(new[] { Q, QEmp, QCir, W, E, R });
        }

        private static void OnGameUpdate(EventArgs args)
        {
            if (Program.Orbwalker.ActiveMode.ToString() == "Combo")
            {
                Obj_AI_Hero target = SimpleTs.GetTarget(1250, SimpleTs.DamageType.Physical);
                doCombo(target);
            }

            if (Program.Orbwalker.ActiveMode.ToString() == "LastHit")
            {
                Obj_AI_Hero target = SimpleTs.GetTarget(1250, SimpleTs.DamageType.Physical);
                doLastHit(target);
                useQSmart(target);
            }

            if (Program.Orbwalker.ActiveMode.ToString() == "Mixed")
            {
                Obj_AI_Hero target = SimpleTs.GetTarget(1250, SimpleTs.DamageType.Physical);
                doLastHit(target);
                useQSmart(target);
            }

            if (Program.Orbwalker.ActiveMode.ToString() == "LaneClear")
            {
                Obj_AI_Hero target = SimpleTs.GetTarget(1250, SimpleTs.DamageType.Physical);
                doLaneClear(target);
            }

            if (Program.Menu.Item("flee").GetValue<KeyBind>().Active)
            {
                Player.IssueOrder(GameObjectOrder.MoveTo, Game.CursorPos);
                gapCloseE(Game.CursorPos.To2D());
            }

            if (Program.Menu.Item("useR_KS").GetValue<bool>())
            {
                useRKill();
            }  

            if (Program.Menu.Item("harassOn").GetValue<bool>() && Program.Orbwalker.ActiveMode.ToString() == "None")
            {
                Obj_AI_Hero target = SimpleTs.GetTarget(1000, SimpleTs.DamageType.Physical);
                useQSmart(target, Program.Menu.Item("harQ3Only").GetValue<bool>());
            }

            if (Program.Menu.Item("smartW").GetValue<bool>())
                foreach (Obj_SpellMissile mis in skillShots)
                {
                    if (mis.IsValid)
                        useWSmart(mis);
                }
        }

        public static void doCombo(Obj_AI_Hero target)
        {
            if (target == null) return;

            if (Program.Menu.Item("smartR").GetValue<bool>() && R.IsReady())
                useRSmart();

            if (!useESmart(target))
            {
                //Console.WriteLine("test");
                List<Obj_AI_Hero> ignore = new List<Obj_AI_Hero>();
                ignore.Add(target);
                gapCloseE(target.Position.To2D(), ignore);
            }

            useQSmart(target);            
        }

        public static void doLastHit(Obj_AI_Hero target)
        {
            var minions = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, Q.Range + 50);
            foreach (var minion in minions.Where(minion => minion.IsValidTarget(Q.Range)))
            {
                if (Player.Distance(minion) < Orbwalking.GetRealAutoAttackRange(minion) && minion.Health < DamageLib.CalcPhysicalMinionDmg((double)(Player.BaseAttackDamage + Player.FlatPhysicalDamageMod), (Obj_AI_Minion)minion, true))
                    return;
                if (Program.Menu.Item("useElh").GetValue<bool>() && minion.Health < DamageLib.getDmg(minion, DamageLib.SpellType.E))
                    useENormal(minion);

                if (Program.Menu.Item("useQlh").GetValue<bool>() && !isQEmpovered() && minion.Health < DamageLib.getDmg(minion, DamageLib.SpellType.Q))
                    if (!(target != null && isQEmpovered() && Player.Distance(target) < 1050))
                    {
                        if (canCastFarQ())
                            Q.Cast(minion);
                        else
                            QCir.CastIfHitchanceEquals(minion, HitChance.High);
                    }
            }
        }

        public static void doLaneClear(Obj_AI_Hero target)
        {
            List<Obj_AI_Base> minions = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, Q.Range + 50, MinionTypes.All, MinionTeam.NotAlly);

            if (Q.IsReady() && Program.Menu.Item("useQlc").GetValue<bool>())
            {
                if (isQEmpovered() && !(target != null && Player.Distance(target) < 1050))
                {
                    if (canCastFarQ())
                    {
                        List<Vector2> minionPs = MinionManager.GetMinionsPredictedPositions(minions, 0.25f, 50f, 1200f, Player.ServerPosition, 900f, false, SkillshotType.SkillshotLine);
                        MinionManager.FarmLocation farm = QEmp.GetLineFarmLocation(minionPs); //MinionManager.GetBestLineFarmLocation(minionPs, 50f, 900f);
                        if (farm.MinionsHit >= Program.Menu.Item("useEmpQHit").GetValue<Slider>().Value)
                        {
                            // Console.WriteLine("Cast q simp Emp");
                            QEmp.Cast(farm.Position, false);
                            return;
                        }
                    }
                    else
                    {
                        List<Vector2> minionPs = MinionManager.GetMinionsPredictedPositions(minions, 0.5f, 270f, float.MaxValue, getDashEndPos(), 0, false, SkillshotType.SkillshotCircle, getDashEndPos());
                        MinionManager.FarmLocation farm = QCir.GetCircularFarmLocation(minionPs); //MinionManager.GetBestLineFarmLocation(minionPs, 50f, 900f);
                        //if (farm.MinionsHit >= YasuoSharp.Config.Item("useEmpQHit").GetValue<Slider>().Value)
                        // QCir.Cast(farm.Position, false);
                    }
                }
                else
                {
                    if (canCastFarQ())
                    {
                        List<Vector2> minionPs = MinionManager.GetMinionsPredictedPositions(minions, 0.25f, 30f, 1800f, Player.ServerPosition, 475, false, SkillshotType.SkillshotLine);
                        Vector2 clos = Geometry.Closest(Player.ServerPosition.To2D(), minionPs);
                        if (Player.Distance(clos) < 475)
                        {
                            // Console.WriteLine("Cast q simp");
                            Q.Cast(clos, false);
                            return;
                        }
                    }
                    else
                    {
                        List<Vector2> minionPs = MinionManager.GetMinionsPredictedPositions(minions, 0.5f, 270f, float.MaxValue, getDashEndPos(), 0, false, SkillshotType.SkillshotCircle, getDashEndPos());
                        MinionManager.FarmLocation farm = QCir.GetCircularFarmLocation(minionPs); //MinionManager.GetBestLineFarmLocation(minionPs, 50f, 900f);
                        if (farm.MinionsHit > 2)
                        {
                            QCir.Cast(farm.Position, false);
                            // Console.WriteLine("Cast q circ simp");
                            return;
                        }
                    }
                }
            }
            if (Program.Menu.Item("useElc").GetValue<bool>())
                foreach (var minion in minions.Where(minion => minion.IsValidTarget(Q.Range)))
                {
                    if (minion.Health < DamageLib.getDmg(minion, DamageLib.SpellType.E)
                        || ((minion.Health < (DamageLib.getDmg(minion, DamageLib.SpellType.E) + DamageLib.getDmg(minion, DamageLib.SpellType.AD) - 40))
                        && (minion.Health > (DamageLib.getDmg(minion, DamageLib.SpellType.E) + 80))))
                    {
                        useENormal(minion);
                        return;
                    }
                }
        }

        public static void doHarass(Obj_AI_Hero target)
        {
            if (!inTowerRange(Player.ServerPosition.To2D()) || Program.Menu.Item("harassTower").GetValue<bool>())
                useQSmart(target);
        }

        public static void doFastGetTo()
        {
            List<Obj_AI_Base> jumpies = ObjectManager.Get<Obj_AI_Base>().Where(enemy => enemyIsJumpable(enemy)).ToList();
        }        

        public static bool inTowerRange(Vector2 pos)
        {            
            foreach (Obj_AI_Turret tur in ObjectManager.Get<Obj_AI_Turret>().Where(tur => tur.IsEnemy && tur.Health > 0))
            {
                if (pos.Distance(tur.Position.To2D()) < (850 + Player.BoundingRadius))
                    return true;
            }
            return false;
        }

        public static Vector3 getDashEndPos()
        {
            Vector2 dashPos2 = Player.GetDashInfo().EndPos;
            return new Vector3(dashPos2, Player.ServerPosition.Z);
        }

        public static bool isQEmpovered()
        {
            return Player.HasBuff("yasuoq3w", true);
        }

        public static bool isDashing()
        {
            return Player.IsDashing();
        }

        public static bool canCastFarQ()
        {
            return !Player.IsDashing();
        }

        public static bool canCastCircQ()
        {
            return Player.IsDashing();
        }

        public static List<Obj_AI_Hero> getKockUpEnemies()
        {
            List<Obj_AI_Hero> enemKonck = new List<Obj_AI_Hero>();
            foreach (Obj_AI_Hero enem in ObjectManager.Get<Obj_AI_Hero>().Where(enem => enem.IsEnemy))
            {
                foreach (BuffInstance buff in enem.Buffs)
                {
                    if (buff.Type == BuffType.Knockback || buff.Type == BuffType.Knockup)
                    {
                        enemKonck.Add(enem);
                        break;
                    }
                }
            }
            return enemKonck;
        }

        public static void useQSmart(Obj_AI_Hero target, bool onlyEmp = false)
        {
            if (!Q.IsReady())
                return;
            if (isQEmpovered())
            {
                if (canCastFarQ())
                {
                    PredictionOutput po = QEmp.GetPrediction(target); //QEmp.GetPrediction(target, true);
                    if (po.Hitchance >= HitChance.High && Player.Distance(po.CastPosition) < 900)
                    {
                        Console.WriteLine("Cast q emp champ");
                        QEmp.Cast(po.CastPosition);
                    }
                }
                else//dashing
                {
                    float trueRange = QCir.Range - 10;
                    Vector3 endPos = getDashEndPos();
                    if (Player.Distance(endPos) < 10 && target.Distance(endPos) < 270)
                    {
                        Console.WriteLine("Cast q emp cir champ");
                        QCir.Cast(target.Position);
                    }
                }
            }
            else if (!onlyEmp)
            {
                if (canCastFarQ())
                {
                    PredictionOutput po = Q.GetPrediction(target);
                    if (po.Hitchance == HitChance.High)
                        Q.Cast(po.CastPosition);
                    //Console.WriteLine("Cast q champ");
                    // Console.WriteLine("test QQ");

                }
                else//dashing
                {
                    float trueRange = QCir.Range - 10;
                    Vector3 endPos = getDashEndPos();
                    if (Player.Distance(endPos) < 5 && target.Distance(endPos) < 270)
                    {
                        Console.WriteLine("Cast q cir champ");
                        QCir.Cast(target.Position);
                    }
                }
            }
        }

        public static void useWSmart(Obj_SpellMissile missle)
        {
            if (!W.IsReady())
                return;
            if (missle.SpellCaster is Obj_AI_Hero && missle.IsEnemy)
            {
                Obj_AI_Base enemHero = missle.SpellCaster;
                float dmg = (enemHero.BaseAttackDamage + enemHero.FlatPhysicalDamageMod);
                if (missle.SData.Name.Contains("Crit"))
                    dmg *= 2;
                if (!missle.SData.Name.Contains("Attack") || (enemHero.CombatType == GameObjectCombatType.Ranged && dmg > Player.MaxHealth / 8))
                {
                    if (missle.Target.IsMe || (DistanceFromPointToLine(missle.SpellCaster.Position.To2D(), missle.EndPosition.To2D(), Player.ServerPosition.To2D()) < (Player.BoundingRadius + missle.SData.LineWidth)))
                    {
                        Vector3 blockWhere = missle.Position;//Player.ServerPosition + Vector3.Normalize(missle.Position - Player.ServerPosition)*30; // missle.Position; 
                        if (Player.Distance(missle.Position) < 420)
                        {
                            if (missle.Target.IsMe || isMissileCommingAtMe(missle))
                            {
                                Console.WriteLine(missle.BoundingRadius);
                                Console.WriteLine(missle.SData.LineWidth);
                                lastSpell = missle.SData.Name;
                                W.Cast(blockWhere, true);
                                skillShots.Clear();
                            }
                        }
                    }
                }
            }
        }

        public static void useENormal(Obj_AI_Base target)
        {
            if (!E.IsReady())
                return;
            //Console.WriteLine("gapcloseer?");
            Vector2 pPos = Player.ServerPosition.To2D();
            Vector2 posAfterE = pPos + (Vector2.Normalize(target.Position.To2D() - pPos) * E.Range);
            if ((!inTowerRange(posAfterE) || !Program.Menu.Item("djTur").GetValue<bool>()) && !Player.IsChanneling)
                E.Cast(target, false);
        }

        public static bool useESmart(Obj_AI_Hero target, List<Obj_AI_Hero> ignore = null)
        {
            if (!E.IsReady())
                return false;
            float trueAARange = Player.AttackRange + target.BoundingRadius;
            float trueERange = target.BoundingRadius + E.Range;

            float dist = Player.Distance(target);
            Vector2 dashPos = new Vector2();
            if (target.IsMoving)
            {
                Vector2 tpos = target.Position.To2D();
                Vector2 path = target.Path[0].To2D() - tpos;
                path.Normalize();
                dashPos = tpos + (path * 100);
            }
            float targ_ms = (target.IsMoving && Player.Distance(dashPos) > dist) ? target.MoveSpeed : 0;
            float msDif = (Player.MoveSpeed - targ_ms) == 0 ? 0.0001f : (Player.MoveSpeed - targ_ms);
            float timeToReach = (dist - trueAARange) / msDif;
            //Console.WriteLine(timeToReach);
            if (dist > trueAARange && dist < trueERange)
            {
                if (timeToReach > 1.7f || timeToReach < 0.0f)
                {
                    //  Console.WriteLine("test2");
                    useENormal(target);
                    return true;
                }
            }
            return false;
        }

        public static void gapCloseE(Vector2 pos, List<Obj_AI_Hero> ignore = null)
        {
            if (!E.IsReady())
                return;

            // Console.WriteLine("gapcloseer?");
            //Player.IssueOrder(GameObjectOrder.MoveTo, pos.To3D());
            Vector2 pPos = Player.ServerPosition.To2D();
            Obj_AI_Base bestEnem = null;
            //Check if can go through wall
            // foreach (Obj_AI_Base enemy in ObjectManager.Get<Obj_AI_Base>().Where(enemy => enemyIsJumpable(enemy, ignore)))
            // {
            //
            //}



            Vector2 bestLoc = pPos + (Vector2.Normalize(pos - pPos) * (Player.MoveSpeed * 0.35f));
            float bestDist = pos.Distance(bestLoc);
            foreach (Obj_AI_Base enemy in ObjectManager.Get<Obj_AI_Base>().Where(enemy => enemyIsJumpable(enemy, ignore)))
            {

                float trueRange = E.Range + enemy.BoundingRadius;
                float distToEnem = Player.Distance(enemy);
                if (distToEnem < trueRange && distToEnem > 15)
                {
                    Vector2 posAfterE = pPos + (Vector2.Normalize(enemy.Position.To2D() - pPos) * E.Range);
                    float distE = pos.Distance(posAfterE);
                    if (distE < bestDist)
                    {
                        bestLoc = posAfterE;
                        bestDist = distE;
                        bestEnem = enemy;
                    }
                }
            }
            if (bestEnem != null)
                useENormal(bestEnem);

        }

        public static void useRSmart()
        {
            List<Obj_AI_Hero> enemInAir = getKockUpEnemies();
            foreach (Obj_AI_Hero enem in enemInAir)
            {
                int aroundAir = 0;
                foreach (Obj_AI_Hero enem2 in enemInAir)
                {
                    if (Vector3.DistanceSquared(enem.ServerPosition, enem2.ServerPosition) < 400 * 400)
                        aroundAir++;
                }
                if (aroundAir >= Program.Menu.Item("useRHit").GetValue<Slider>().Value)
                    R.Cast(enem);
            }
        }

        public static void useRKill()
        {
            List<Obj_AI_Hero> enemInAir = getKockUpEnemies();
            foreach (Obj_AI_Hero enem in enemInAir)
            {
                if (enem.Health < DamageLib.getDmg(enem, DamageLib.SpellType.R) && R.IsReady())
                {
                    R.Cast(enem);
                }
            }
        }

        public void setWIgnore()
        {
            WIgnore.Add("LuxPrismaticWaveMissile");
        }

        public static bool isMissileCommingAtMe(Obj_SpellMissile missle)
        {
            Vector3 step = missle.StartPosition + Vector3.Normalize(missle.StartPosition - missle.EndPosition) * 10;
            return (Player.Distance(step) < Player.Distance(missle.StartPosition)) ? false : true;
        }

        public static bool enemyIsJumpable(Obj_AI_Base enemy, List<Obj_AI_Hero> ignore = null)
        {
            //Console.WriteLine("gapcloseerawfawfaw23125?");
            if (enemy.IsValid && enemy.IsEnemy && !enemy.IsInvulnerable && !enemy.MagicImmune && !enemy.IsDead)
            {
                if (ignore != null)
                    foreach (Obj_AI_Hero ign in ignore)
                    {
                        if (ign.NetworkId == enemy.NetworkId)
                            return false;
                    }

                foreach (BuffInstance buff in enemy.Buffs)
                {
                    if (buff.Name == "YasuoDashWrapper")
                        return false;
                }
                return true;
            }
            return false;
        }

        public static float getSpellCastTime(Spell spell)
        {
            return sBook.GetSpell(spell.Slot).SData.SpellCastTime;
        }

        public static float getSpellCastTime(SpellSlot slot)
        {
            return sBook.GetSpell(slot).SData.SpellCastTime;
        }

        public static bool interact(Vector2 p1, Vector2 p2, Vector2 pC, float radius)
        {

            Vector2 p3 = new Vector2();
            p3.X = pC.X + radius;
            p3.Y = pC.Y + radius;
            float m = ((p2.Y - p1.Y) / (p2.X - p1.X));
            float Constant = (m * p1.X) - p1.Y;

            float b = -(2f * ((m * Constant) + p3.X + (m * p3.Y)));
            float a = (1 + (m * m));
            float c = ((p3.X * p3.X) + (p3.Y * p3.Y) - (radius * radius) + (2f * Constant * p3.Y) + (Constant * Constant));
            float D = ((b * b) - (4f * a * c));
            if (D > 0)
            {
                return true;
            }
            else
                return false;

        }

        public static float DistanceFromPointToLine(Vector2 l1, Vector2 l2, Vector2 point)
        {
            return Math.Abs((l2.X - l1.X) * (l1.Y - point.Y) - (l1.X - point.X) * (l2.Y - l1.Y)) /
                    (float)Math.Sqrt(Math.Pow(l2.X - l1.X, 2) + Math.Pow(l2.Y - l1.Y, 2));
        }

        private static void OnCreateObject(GameObject sender, EventArgs args)
        {
            if (sender is Obj_SpellMissile && sender.IsEnemy)
            {
                Obj_SpellMissile missle = (Obj_SpellMissile)sender;
                if(WIgnore.Contains(missle.SData.Name)) return;
                skillShots.Add(missle);
            }
        }

        private static void OnDeleteObject(GameObject sender, EventArgs args)
        {
            int i = 0;
            foreach (var lho in skillShots)
            {
                if (lho.NetworkId == sender.NetworkId)
                {
                    skillShots.RemoveAt(i);
                    return;
                }
                i++;
            }
        }

        public static void OnProcessSpell(LeagueSharp.Obj_AI_Base obj, LeagueSharp.GameObjectProcessSpellCastEventArgs arg)
        {
            if (obj.Name.Contains("Turret") || obj.Name.Contains("Minion"))
                return;
        }        

    }
}
