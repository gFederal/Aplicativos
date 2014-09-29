using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;

using Color = System.Drawing.Color;

namespace FedAllChampionsUtility
{
    class Yasuo : Champion
    {
        internal class YasWall
        {
            public Obj_SpellLineMissile pointL;
            public Obj_SpellLineMissile pointR;
            public float endtime = 0;
            public YasWall()
            {

            }

            public YasWall(Obj_SpellLineMissile L, Obj_SpellLineMissile R)
            {
                pointL = L;
                pointR = R;
                endtime = Game.Time + 4;
            }

            public void setR(Obj_SpellLineMissile R)
            {
                pointR = R;
                endtime = Game.Time + 4;
            }

            public void setL(Obj_SpellLineMissile L)
            {
                pointL = L;
                endtime = Game.Time + 4;
            }
        }
        
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

        public static Vector3 point1 = new Vector3();
        public static Vector3 point2 = new Vector3();

        public static Vector3 castFrom;
        public static bool isDashigPro = false;
        public static float startDash = 0;
        public static float time = 0;

        public static YasWall wall = new YasWall();
        
        public Yasuo()
        {
            LoadMenu();
            LoadSpells();

            Game.OnGameUpdate += OnGameUpdate;
            GameObject.OnCreate += OnCreateObject;
            GameObject.OnDelete += OnDeleteObject;
            Obj_AI_Base.OnProcessSpellCast += OnProcessSpell;

            Game.OnGameSendPacket += OnGameSendPacket;
            Game.OnGameProcessPacket += OnGameProcessPacket;

            PluginLoaded();
        }

        private void LoadMenu()
        {
            Program.Menu.AddSubMenu(new Menu("TeamFight", "TeamFight"));
            Program.Menu.SubMenu("TeamFight").AddItem(new MenuItem("smartW", "Smart W").SetValue(true));
            Program.Menu.SubMenu("TeamFight").AddItem(new MenuItem("smartR", "Smart R").SetValue(true));
            Program.Menu.SubMenu("TeamFight").AddItem(new MenuItem("useEWall", "use E to safe")).SetValue(true);
            Program.Menu.SubMenu("TeamFight").AddItem(new MenuItem("useRHit", "Use R if hit").SetValue(new Slider(3, 5, 1)));
            Program.Menu.SubMenu("TeamFight").AddItem(new MenuItem("useRHitTime", "Use R when they land")).SetValue(true);            

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

            Q.SetSkillshot(getNewQSpeed(), 50f, float.MaxValue, false, SkillshotType.SkillshotLine);
            QEmp.SetSkillshot(0.1f, 50f, 1200f, false, SkillshotType.SkillshotLine);
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

        public static float getNewQSpeed()
        {
            float ds = 0.3f;//s
            float a = 1 / ds * Yasuo.Player.AttackSpeedMod;
            return 1 / a;
        }

        public static void doCombo(Obj_AI_Hero target)
        {
            Q.SetSkillshot(getNewQSpeed(), 50f, float.MaxValue, false, SkillshotType.SkillshotLine);
            if (target == null) return;

            if (Program.Menu.Item("smartR").GetValue<bool>() && R.IsReady())
                useRSmart();

            if (Program.Menu.Item("smartW").GetValue<bool>())
                putWallBehind(target);

            if (Program.Menu.Item("useEWall").GetValue<bool>())
                eBehindWall(target);

            if (!useESmart(target))
            {
                //Console.WriteLine("test");
                List<Obj_AI_Hero> ignore = new List<Obj_AI_Hero>();
                ignore.Add(target);
                gapCloseE(target.Position.To2D(), ignore);
            }

            useQSmart(target);
            useHydra(target);
        }

        public static Vector2 getNextPos(Obj_AI_Hero target)
        {
            Vector2 dashPos = target.Position.To2D();
            if (target.IsMoving)
            {
                Vector2 tpos = target.Position.To2D();
                Vector2 path = target.Path[0].To2D() - tpos;
                path.Normalize();
                dashPos = tpos + (path * 100);
            }
            return dashPos;
        }

        public static void putWallBehind(Obj_AI_Hero target)
        {
            if (!W.IsReady() || !E.IsReady())
                return;
            Vector2 dashPos = getNextPos(target);
            PredictionOutput po = Prediction.GetPrediction(target, 0.5f);

            float dist = Player.Distance(po.UnitPosition);
            if (!target.IsMoving || Player.Distance(dashPos) <= dist + 40)
                if (dist < 330 && dist > 100 && W.IsReady())
                    W.Cast(po.UnitPosition);
        }

        public static void eBehindWall(Obj_AI_Hero target)
        {
            if (!E.IsReady() || !enemyIsJumpable(target) || target.IsMelee())
                return;
            float dist = Player.Distance(target);
            var pPos = Player.Position.To2D();
            Vector2 dashPos = target.Position.To2D();
            if (!target.IsMoving || Player.Distance(dashPos) <= dist)
            {
                foreach (Obj_AI_Base enemy in ObjectManager.Get<Obj_AI_Base>().Where(enemy => enemyIsJumpable(enemy)))
                {
                    Vector2 posAfterE = pPos + (Vector2.Normalize(enemy.Position.To2D() - pPos) * E.Range);
                    if ((target.Distance(posAfterE) < dist
                        || target.Distance(posAfterE) < Orbwalking.GetRealAutoAttackRange(target) + 100)
                        && goesThroughWall(target.Position, posAfterE.To3D()))
                    {
                        useENormal(enemy);
                    }
                }
            }

        }

        public static bool goesThroughWall(Vector3 vec1, Vector3 vec2)
        {
            if (wall.endtime < Game.Time)
                return false;
            Vector2 inter = LineIntersectionPoint(vec1.To2D(), vec2.To2D(), wall.pointL.Position.To2D(), wall.pointR.Position.To2D());
            float wallW = (300 + 50 * W.Level);
            if (wall.pointL.Position.To2D().Distance(inter) > wallW ||
                wall.pointR.Position.To2D().Distance(inter) > wallW)
                return false;
            var dist = vec1.Distance(vec2);
            if (vec1.To2D().Distance(inter) + vec2.To2D().Distance(inter) - 30 > dist)
                return false;

            return true;
        }


        public static void doLastHit(Obj_AI_Hero target)
        {
            var minions = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, Q.Range + 50);
            foreach (var minion in minions.Where(minion => minion.IsValidTarget(Q.Range)))
            {
                var totalAD = Player.FlatPhysicalDamageMod + Player.BaseAttackDamage;
                if (Player.Distance(minion) < Orbwalking.GetRealAutoAttackRange(minion) && minion.Health < Damage.CalcDamage(ObjectManager.Player, minion, Damage.DamageType.Physical, totalAD))
                    return;
                if (Program.Menu.Item("useElh").GetValue<bool>() && minion.Health < ObjectManager.Player.GetSpellDamage(minion, SpellSlot.E))
                    useENormal(minion);
                
                    if (Program.Menu.Item("useQlh").GetValue<bool>() && !isQEmpovered() && HealthPrediction.LaneClearHealthPrediction(minion, (int)(getNewQSpeed() * 1000)) < Player.GetSpellDamage(minion, SpellSlot.Q))
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
            List<Obj_AI_Base> minions = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, 800,MinionTypes.All,MinionTeam.NotAlly);
            if (Program.Menu.Item("useElc").GetValue<bool>())
                foreach (var minion in minions.Where(minion => minion.IsValidTarget(E.Range)))
                {
                    if (minion.Health < Player.GetSpellDamage(minion, SpellSlot.E)
                        || Q.IsReady() && minion.Health < (Player.GetSpellDamage(minion, SpellSlot.E) + Player.GetSpellDamage(minion, SpellSlot.Q)))
                        
                    {
                        useENormal(minion);
                    }
                }

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
                        if (minions.Count(min => min.IsValid && min.Distance(getDashEndPos()) < 250) >= Program.Menu.Item("useEmpQHit").GetValue<Slider>().Value)
                        {
                              QCir.Cast(Player.Position, false);
                       }
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
                        if (minions.Count(min => !min.IsDead && min.Distance(getDashEndPos()) < 250) > 1)
                        {
                            QCir.Cast(Player.Position, false);
                            // Console.WriteLine("Cast q circ simp");
                            return;
                        }
                    }
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

        public static void useHydra(Obj_AI_Base target)
        {

            if ((Items.CanUseItem(3074) || Items.CanUseItem(3074)) && target.Distance(Player.ServerPosition) < (400 + target.BoundingRadius - 20))
            {
                Items.UseItem(3074, target);
                Items.UseItem(3077, target);
            }
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
            return isDashigPro;
        }

        public static bool canCastFarQ()
        {
            return !isDashigPro;
        }

        public static bool canCastCircQ()
        {
            return isDashigPro;
        }

        public static List<Obj_AI_Hero> getKockUpEnemies(ref float lessKnockTime)
        {
            List<Obj_AI_Hero> enemKonck = new List<Obj_AI_Hero>();
            foreach (Obj_AI_Hero enem in ObjectManager.Get<Obj_AI_Hero>().Where(enem => enem.IsEnemy))
            {
                foreach (BuffInstance buff in enem.Buffs)
                {
                    if (buff.Type == BuffType.Knockback || buff.Type == BuffType.Knockup)
                    {
                        if (buff.Type == BuffType.Knockup)
                            lessKnockTime = (buff.EndTime - Game.Time) < lessKnockTime
                                ? (buff.EndTime - Game.Time)
                                : lessKnockTime;
                        enemKonck.Add(enem);
                        break;
                    }
                }
            }

            if (!Program.Menu.Item("useRHitTime").GetValue<bool>())
                lessKnockTime = 0;

            return enemKonck;
        }

        public static void setUpWall()
        {
            if (wall == null)
                return;

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
                    if (po.Hitchance >= HitChance.Medium && Player.Distance(po.CastPosition) < 900)
                    {                        
                        QEmp.Cast(po.CastPosition);
                    }
                }
                else//dashing
                {
                    float trueRange = QCir.Range - 10;
                    Vector3 endPos = getDashEndPos();
                    if (Player.Distance(endPos) < 10 && target.Distance(endPos) < 270)
                    {                        
                        QCir.Cast(target.Position);
                    }
                }
            }
            else if (!onlyEmp)
            {
                if (canCastFarQ())
                {
                    PredictionOutput po = Q.GetPrediction(target);
                    if (po.Hitchance >= HitChance.Medium)
                        Q.Cast(po.CastPosition);
                    
                }
                else//dashing
                {
                    float trueRange = QCir.Range - 10;
                    Vector3 endPos = getDashEndPos();
                    if (Player.Distance(endPos) < 5 && target.Distance(endPos) < 270)
                    {                        
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
            if ((!inTowerRange(posAfterE) || !Program.Menu.Item("djTur").GetValue<bool>()))
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

            float distToPos = Player.Distance(pos);
            if (((distToPos < Q.Range)) &&
                goesThroughWall(pos.To3D(), Player.Position))
                return;

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
            float timeToLand = float.MaxValue;
            List < Obj_AI_Hero > enemInAir = getKockUpEnemies(ref timeToLand);
            foreach (Obj_AI_Hero enem in enemInAir)
            {
                int aroundAir = 0;
                foreach (Obj_AI_Hero enem2 in enemInAir)
                {
                    if (Vector3.DistanceSquared(enem.ServerPosition, enem2.ServerPosition) < 400 * 400)
                        aroundAir++;
                }
                if (aroundAir >= Program.Menu.Item("useRHit").GetValue<Slider>().Value && timeToLand < 0.1f)
                    R.Cast(enem);
            }
        }

        public static void useRKill()
        {
            float timeToLand = float.MaxValue;
            List<Obj_AI_Hero> enemInAir = getKockUpEnemies(ref timeToLand);
            foreach (Obj_AI_Hero enem in enemInAir)
            {
                if (enem.Health < ObjectManager.Player.GetSpellDamage(enem, SpellSlot.R) && R.IsReady())
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
            return (!(Player.Distance(step) < Player.Distance(missle.StartPosition)));
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

        public static Vector2 LineIntersectionPoint(Vector2 ps1, Vector2 pe1, Vector2 ps2, Vector2 pe2)
        {
            // Get A,B,C of first line - points : ps1 to pe1
            float A1 = pe1.Y - ps1.Y;
            float B1 = ps1.X - pe1.X;
            float C1 = A1 * ps1.X + B1 * ps1.Y;

            // Get A,B,C of second line - points : ps2 to pe2
            float A2 = pe2.Y - ps2.Y;
            float B2 = ps2.X - pe2.X;
            float C2 = A2 * ps2.X + B2 * ps2.Y;

            // Get delta and check if the lines are parallel
            float delta = A1 * B2 - A2 * B1;
            if (delta == 0)
                return new Vector2(-1, -1);

            // now return the Vector2 intersection point
            return new Vector2(
                (B2 * C1 - B1 * C2) / delta,
                (A1 * C2 - A2 * C1) / delta
            );
        }

        private static void OnCreateObject(GameObject sender, EventArgs args)
        {
            //wall
            if (sender is Obj_SpellLineMissile)
            {
                Obj_SpellLineMissile missle = (Obj_SpellLineMissile)sender;
                if (missle.SData.Name == "yasuowmovingwallmisl")
                {
                    Yasuo.wall.setL(missle);
                }

                if (missle.SData.Name == "yasuowmovingwallmisr")
                {
                    Yasuo.wall.setR(missle);

                }
                Console.WriteLine(missle.SData.Name);
            }

            if (sender is Obj_SpellMissile && sender.IsEnemy)
            {

                Obj_SpellMissile missle = (Obj_SpellMissile)sender;
                // if(Yasuo.WIgnore.Contains(missle.SData.Name))
                //     return;
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

        public static void OnProcessSpell(Obj_AI_Base obj, GameObjectProcessSpellCastEventArgs arg)
        {
            if (obj.IsMe)
            {
                if (arg.SData.Name == "YasuoDashWrapper")//start dash
                {
                    isDashigPro = true;
                    castFrom = Player.Position;
                    startDash = Game.Time;
                }
            }
        }

        private static void OnGameProcessPacket(GamePacketEventArgs args)
        {//28 16 176 ??184
            if (args.PacketData[0] == 41)//135no 100no 183no 34no 101 133 56yesss? 127 41yess
            {
                GamePacket gp = new GamePacket(args.PacketData);

                gp.Position = 1;
                if (gp.ReadInteger() == Yasuo.Player.NetworkId)
                {
                    Yasuo.isDashigPro = false;
                    Yasuo.time = Game.Time - Yasuo.startDash;
                }
                /* for (int i = 1; i < gp.Size() - 4; i++)
                 {
                     gp.Position = i;
                     if (gp.ReadInteger() == Yasuo.Player.NetworkId)
                     {
                         Console.WriteLine("Found: "+i);
                     }
                 }

                 Console.WriteLine("End dash");
                 Yasuo.Q.Cast(Yasuo.Player.Position);*/
            }
        }

        private static void OnGameSendPacket(GamePacketEventArgs args)
        {

        }

    }
}
