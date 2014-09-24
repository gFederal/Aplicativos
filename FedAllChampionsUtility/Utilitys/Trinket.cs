using System;
using System.Collections.Generic;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;

namespace FedAllChampionsUtility
{
    internal class Trinket
    {        
        public static Obj_AI_Base player = ObjectManager.Player;
        static int SightStone = 2049;
        static int Orb = 3363;
        static int YellowW = 3340;
        static int TRINKET_RED = 3341;
        static int QuillCoat = 3204;
        static int Wriggle = 3154;        

        public Trinket()
        {
            LoadMenu();            
            Game.OnGameUpdate += OnTick;
        }

        public void LoadMenu()
        {
            Program.Menu.AddSubMenu(new Menu("Trinket", "Trinket"));
            Program.Menu.SubMenu("Trinket").AddItem(new MenuItem("ward", "Buy WTotem at start").SetValue(true));
            Program.Menu.SubMenu("Trinket").AddItem(new MenuItem("timer", "Buy Sw at x min").SetValue(new Slider(15, 1, 30)));
            Program.Menu.SubMenu("Trinket").AddItem(new MenuItem("orb", "Buy Orb").SetValue(true));
            Program.Menu.SubMenu("Trinket").AddItem(new MenuItem("timer2", "Buy Orb at x min").SetValue(new Slider(40, 30, 60)));
            Program.Menu.SubMenu("Trinket").AddItem(new MenuItem("sweeperS", "Buy Sw On Sightstone").SetValue(true));
            Program.Menu.SubMenu("Trinket").AddItem(new MenuItem("sweeperQ", "Buy Sw QuillCoat").SetValue(true));
            Program.Menu.SubMenu("Trinket").AddItem(new MenuItem("sweeperW", "Buy Sw on Wriggle").SetValue(true));
        }

        private static void OnTick(EventArgs args)
        {
            Obj_AI_Hero player1 = (Obj_AI_Hero)player;
            if (player.IsDead || Utility.InShopRange())
            {
                //Game.PrintChat(hasItem(YellowW).ToString());
                if (GetTimer() < 1 && !hasItem(YellowW) && isEn("ward"))
                {
                    Packet.C2S.BuyItem.Encoded(new Packet.C2S.BuyItem.Struct(YellowW, ObjectManager.Player.NetworkId)).Send();
                }
                if (hasItem(SightStone) && isEn("sweeperS") && !hasItem(TRINKET_RED))
                {
                    Packet.C2S.SellItem.Encoded(new Packet.C2S.SellItem.Struct(SpellSlot.Trinket, player.NetworkId)).Send();
                    Packet.C2S.BuyItem.Encoded(new Packet.C2S.BuyItem.Struct(TRINKET_RED, ObjectManager.Player.NetworkId)).Send();
                }
                if (hasItem(QuillCoat) && isEn("sweeperQ") && !hasItem(TRINKET_RED))
                {
                    Packet.C2S.SellItem.Encoded(new Packet.C2S.SellItem.Struct(SpellSlot.Trinket, player.NetworkId)).Send();
                    Packet.C2S.BuyItem.Encoded(new Packet.C2S.BuyItem.Struct(TRINKET_RED, ObjectManager.Player.NetworkId)).Send();
                }
                if (hasItem(Wriggle) && isEn("sweeperW") && !hasItem(TRINKET_RED))
                {
                    Packet.C2S.SellItem.Encoded(new Packet.C2S.SellItem.Struct(SpellSlot.Trinket, player.NetworkId)).Send(); player1.SellItem((int)SpellSlot.Trinket);
                    Packet.C2S.BuyItem.Encoded(new Packet.C2S.BuyItem.Struct(TRINKET_RED, ObjectManager.Player.NetworkId)).Send();
                }
                if (isEn("orb") && (GetTimer() >= Program.Menu.Item("timer2").GetValue<Slider>().Value) && !hasItem(Orb))
                {
                    Packet.C2S.SellItem.Encoded(new Packet.C2S.SellItem.Struct(SpellSlot.Trinket, player.NetworkId)).Send();
                    Packet.C2S.BuyItem.Encoded(new Packet.C2S.BuyItem.Struct(Orb, ObjectManager.Player.NetworkId)).Send();
                }
                if (hasItem(YellowW) && (GetTimer() >= Program.Menu.Item("timer").GetValue<Slider>().Value) && (GetTimer() < Program.Menu.Item("timer2").GetValue<Slider>().Value) && !hasItem(TRINKET_RED))
                {
                    // Game.PrintChat("Called");
                    Packet.C2S.SellItem.Encoded(new Packet.C2S.SellItem.Struct(SpellSlot.Trinket, player.NetworkId)).Send();
                    Packet.C2S.BuyItem.Encoded(new Packet.C2S.BuyItem.Struct(TRINKET_RED, ObjectManager.Player.NetworkId)).Send();
                }

            }
        }

        public static float GetTimer()
        {
            return Game.Time / 60;
        }
        public static bool hasItem(int id)
        {
            return Items.HasItem(id, (Obj_AI_Hero)player);
        }
        public static bool isEn(String op)
        {
            return Program.Menu.Item(op).GetValue<bool>();
        }
    }
}   

