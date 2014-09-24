using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using Color = System.Drawing.Color;

namespace FedAllChampionsUtility
{
    class LasthitMarker
    {
        private static Menu _menu;

        private const int MaxMinionDistance = 1000;
        private List<Obj_AI_Minion> _killableMinions = new List<Obj_AI_Minion>();

        public LasthitMarker()
        {
            _menu = Program.Menu.AddSubMenu(new Menu("Lasthit Marker", "LasthitMarker"));
            _menu.AddItem(new MenuItem("LasthitMarker", "Ativar Lasthit Marker").SetValue(new Circle(true, Color.FromArgb(255, 255, 255, 255))));
            _menu.AddItem(new MenuItem("CircleLag", "Lag Free Circles").SetValue(true));
            _menu.AddItem(new MenuItem("CircleQuality", "Circles Quality").SetValue(new Slider(30, 100, 10)));
            _menu.AddItem(new MenuItem("CircleThickness", "Circles Thickness").SetValue(new Slider(2, 10, 1)));

            Game.OnGameUpdate += OnGameUpdate;
            Drawing.OnDraw += Drawing_OnDraw;

        }

        private void Drawing_OnDraw(EventArgs args)
        {
            try
            {
                foreach (Obj_AI_Minion minion in _killableMinions)
                {
                    var menuItem = _menu.Item("LasthitMarker").GetValue<Circle>();

                    if (_menu.Item("CircleLag").GetValue<bool>())
                    {
                        Utility.DrawCircle(minion.Position, minion.BoundingRadius + 30, menuItem.Color,
                            _menu.Item("CircleThickness").GetValue<Slider>().Value,
                            _menu.Item("CircleQuality").GetValue<Slider>().Value);
                    }
                    else
                    {
                        Drawing.DrawCircle(minion.Position, minion.BoundingRadius + 30, menuItem.Color);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        private void OnGameUpdate(EventArgs args)
        {
            try
            {
                _killableMinions = (from minion in ObjectManager.Get<Obj_AI_Minion>()
                                    where minion.IsValid && minion.IsVisible && minion.IsEnemy && !minion.IsDead
                                    where Vector3.Distance(ObjectManager.Player.Position, minion.Position) <= MaxMinionDistance
                                    where minion.Health <= DamageCalculator.Calculate(ObjectManager.Player, minion)
                                    select minion).ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }
    }
}
