using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using LeagueSharp;
using SharpDX;
using Color = System.Drawing.Color;

namespace FedAllChampionsUtility
{
    internal enum jJungleCampState
    {
        Unknown,
        Dead,
        Alive
    }
    internal class jCallOnce
    {
        public Action A(Action action)
        {
            var context = new Context();
            Action ret = () =>
            {
                if (!context.AlreadyCalled)
                {
                    action();
                    context.AlreadyCalled = true;
                }
            };

            return ret;
        }

        private class Context
        {
            public bool AlreadyCalled;
        }
    }

    internal class jJungleCamp
    {
        public TimeSpan SpawnTime { get; set; }
        public TimeSpan RespawnTimer { get; set; }
        public Vector3 Position { get; set; }
        public List<jJungleMinion> Minions { get; set; }
        public jJungleCampState State { get; set; }
        public float ClearTick { get; set; }
    }

    internal class jJungleMinion
    {
        public jJungleMinion(string name)
        {
            Name = name;
        }

        public string Name { get; set; }
        public bool Dead { get; set; }
        public GameObject Unit { get; set; }
    }

    internal class jJungleTimers
    {
        private readonly List<jJungleCamp> _jungleCamps = new List<jJungleCamp>
        {
            new jJungleCamp //Baron
            {
                SpawnTime = TimeSpan.FromSeconds(900),
                RespawnTimer = TimeSpan.FromSeconds(420),
                Position = new Vector3(4549.126f, 10126.66f, -63.11666f),
                Minions = new List<jJungleMinion>
                {
                    new jJungleMinion("Worm12.1.1")
                }
            },
            new jJungleCamp //Dragon
            {
                SpawnTime = TimeSpan.FromSeconds(150),
                RespawnTimer = TimeSpan.FromSeconds(360),
                Position = new Vector3(9606.835f, 4210.494f, -60.30991f),
                Minions = new List<jJungleMinion>
                {
                    new jJungleMinion("Dragon6.1.1")
                }
            },
            //Order
            new jJungleCamp //Wight
            {
                SpawnTime = TimeSpan.FromSeconds(125),
                RespawnTimer = TimeSpan.FromSeconds(50),
                Position = new Vector3(1859.131f, 8246.272f, 54.92376f),
                Minions = new List<jJungleMinion>
                {
                    new jJungleMinion("GreatWraith13.1.1")
                }
            },
            new jJungleCamp //Blue
            {
                SpawnTime = TimeSpan.FromSeconds(115),
                RespawnTimer = TimeSpan.FromSeconds(300),
                Position = new Vector3(3388.156f, 7697.175f, 55.21874f),
                Minions = new List<jJungleMinion>
                {
                    new jJungleMinion("AncientGolem1.1.1"),
                    new jJungleMinion("YoungLizard1.1.2"),
                    new jJungleMinion("YoungLizard1.1.3")
                }
            },
            new jJungleCamp //Wolfs
            {
                SpawnTime = TimeSpan.FromSeconds(125),
                RespawnTimer = TimeSpan.FromSeconds(50),
                Position = new Vector3(3415.77f, 6269.637f, 55.60973f),
                Minions = new List<jJungleMinion>
                {
                    new jJungleMinion("GiantWolf2.1.1"),
                    new jJungleMinion("Wolf2.1.2"),
                    new jJungleMinion("Wolf2.1.3")
                }
            },
            new jJungleCamp //Wraith
            {
                SpawnTime = TimeSpan.FromSeconds(125),
                RespawnTimer = TimeSpan.FromSeconds(50),
                Position = new Vector3(6447.0f, 5384.0f, 60.0f),
                Minions = new List<jJungleMinion>
                {
                    new jJungleMinion("Wraith3.1.1"),
                    new jJungleMinion("LesserWraith3.1.2"),
                    new jJungleMinion("LesserWraith3.1.3"),
                    new jJungleMinion("LesserWraith3.1.4")
                }
            },
            new jJungleCamp //Red
            {
                SpawnTime = TimeSpan.FromSeconds(115),
                RespawnTimer = TimeSpan.FromSeconds(300),
                Position = new Vector3(7509.412f, 3977.053f, 56.867f),
                Minions = new List<jJungleMinion>
                {
                    new jJungleMinion("LizardElder4.1.1"),
                    new jJungleMinion("YoungLizard4.1.2"),
                    new jJungleMinion("YoungLizard4.1.3")
                }
            },
            new jJungleCamp //Golems
            {
                SpawnTime = TimeSpan.FromSeconds(125),
                RespawnTimer = TimeSpan.FromSeconds(50),
                Position = new Vector3(8042.148f, 2274.269f, 54.2764f),
                Minions = new List<jJungleMinion>
                {
                    new jJungleMinion("Golem5.1.2"),
                    new jJungleMinion("SmallGolem5.1.1")
                }
            },
            //Chaos
            new jJungleCamp //Golems
            {
                SpawnTime = TimeSpan.FromSeconds(125),
                RespawnTimer = TimeSpan.FromSeconds(50),
                Position = new Vector3(6005.0f, 12055.0f, 39.62551f),
                Minions = new List<jJungleMinion>
                {
                    new jJungleMinion("Golem11.1.2"),
                    new jJungleMinion("SmallGolem11.1.1")
                }
            },
            new jJungleCamp //Red
            {
                SpawnTime = TimeSpan.FromSeconds(115),
                RespawnTimer = TimeSpan.FromSeconds(300),
                Position = new Vector3(6558.157f, 10524.92f, 54.63499f),
                Minions = new List<jJungleMinion>
                {
                    new jJungleMinion("LizardElder10.1.1"),
                    new jJungleMinion("YoungLizard10.1.2"),
                    new jJungleMinion("YoungLizard10.1.3")
                }
            },
            new jJungleCamp //Wraith
            {
                SpawnTime = TimeSpan.FromSeconds(125),
                RespawnTimer = TimeSpan.FromSeconds(50),
                Position = new Vector3(7534.319f, 9226.513f, 55.50048f),
                Minions = new List<jJungleMinion>
                {
                    new jJungleMinion("Wraith9.1.1"),
                    new jJungleMinion("LesserWraith9.1.2"),
                    new jJungleMinion("LesserWraith9.1.3"),
                    new jJungleMinion("LesserWraith9.1.4")
                }
            },
            new jJungleCamp //Wolfs
            {
                SpawnTime = TimeSpan.FromSeconds(125),
                RespawnTimer = TimeSpan.FromSeconds(50),
                Position = new Vector3(10575.0f, 8083.0f, 65.5235f),
                Minions = new List<jJungleMinion>
                {
                    new jJungleMinion("GiantWolf8.1.1"),
                    new jJungleMinion("Wolf8.1.2"),
                    new jJungleMinion("Wolf8.1.3")
                }
            },
            new jJungleCamp //Blue
            {
                SpawnTime = TimeSpan.FromSeconds(115),
                RespawnTimer = TimeSpan.FromSeconds(300),
                Position = new Vector3(10439.95f, 6717.918f, 54.8691f),
                Minions = new List<jJungleMinion>
                {
                    new jJungleMinion("AncientGolem7.1.1"),
                    new jJungleMinion("YoungLizard7.1.2"),
                    new jJungleMinion("YoungLizard7.1.3")
                }
            },
            new jJungleCamp //Wight
            {
                SpawnTime = TimeSpan.FromSeconds(125),
                RespawnTimer = TimeSpan.FromSeconds(50),
                Position = new Vector3(12287.0f, 6205.0f, 54.84151f),
                Minions = new List<jJungleMinion>
                {
                    new jJungleMinion("GreatWraith14.1.1")
                }
            }
        };

        private readonly Action _onLoadAction;

        public jJungleTimers()
        {
            _onLoadAction = new jCallOnce().A(OnLoad);
            Game.OnGameUpdate += OnGameUpdate;
        }

        private void OnLoad()
        {
            GameObject.OnCreate += ObjectOnCreate;
            GameObject.OnDelete += ObjectOnDelete;
            Drawing.OnDraw += OnDraw;           
        }

        private void OnGameUpdate(EventArgs args)
        {
            try
            {
                _onLoadAction();
                UpdateCamps();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        private void OnDraw(EventArgs args)
        {
            try
            {
                foreach (jJungleCamp minionCamp in _jungleCamps)
                {
                    if (minionCamp.State == jJungleCampState.Dead)
                    {
                        float delta = Game.Time - minionCamp.ClearTick;
                        if (delta < minionCamp.RespawnTimer.TotalSeconds)
                        {
                            TimeSpan time = TimeSpan.FromSeconds(minionCamp.RespawnTimer.TotalSeconds - delta);
                            Vector2 pos = Drawing.WorldToMinimap(minionCamp.Position);
                            string display = string.Format("{0}:{1:D2}", time.Minutes, time.Seconds);
                            Drawing.DrawText(pos.X - display.Length * 3, pos.Y - 5, Color.LimeGreen, display);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        private void ObjectOnDelete(GameObject sender, EventArgs args)
        {
            try
            {
                if (sender.Type != GameObjectType.obj_AI_Minion)
                    return;

                var neutral = (Obj_AI_Minion)sender;
                if (neutral.Name.Contains("Minion") || !neutral.IsValid)
                    return;

                foreach (
                    jJungleMinion minion in
                        from camp in _jungleCamps
                        from minion in camp.Minions
                        where minion.Name == neutral.Name
                        select minion)
                {
                    minion.Dead = neutral.IsDead;
                    minion.Unit = null;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        private void ObjectOnCreate(GameObject sender, EventArgs args)
        {
            try
            {
                if (sender.Type != GameObjectType.obj_AI_Minion)
                    return;

                var neutral = (Obj_AI_Minion)sender;

                if (neutral.Name.Contains("Minion") || !neutral.IsValid)
                    return;

                foreach (
                    jJungleMinion minion in
                        from camp in _jungleCamps
                        from minion in camp.Minions
                        where minion.Name == neutral.Name
                        select minion)
                {
                    minion.Unit = neutral;
                    minion.Dead = neutral.IsDead;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        private void UpdateCamps()
        {
            foreach (jJungleCamp camp in _jungleCamps)
            {
                bool allAlive = true;
                bool allDead = true;

                foreach (jJungleMinion minion in camp.Minions)
                {
                    if (minion.Unit != null)
                        minion.Dead = minion.Unit.IsDead;

                    if (minion.Dead)
                        allAlive = false;
                    else
                        allDead = false;
                }

                switch (camp.State)
                {
                    case jJungleCampState.Unknown:
                        if (allAlive)
                        {
                            camp.State = jJungleCampState.Alive;
                            camp.ClearTick = 0.0f;
                        }
                        break;
                    case jJungleCampState.Dead:
                        if (allAlive)
                        {
                            camp.State = jJungleCampState.Alive;
                            camp.ClearTick = 0.0f;
                        }
                        break;
                    case jJungleCampState.Alive:
                        if (allDead)
                        {
                            camp.State = jJungleCampState.Dead;
                            camp.ClearTick = Game.Time;
                        }
                        break;
                }
            }
        }
    }




}

