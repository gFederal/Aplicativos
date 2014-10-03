using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;

using Color = System.Drawing.Color;

namespace FedAllChampionsUtility
{  

    internal class Revealer
    {
        public static Dictionary<String, String> dict;
        public static Obj_AI_Base player = ObjectManager.Player;
        public static Spell E;        
        public static int VISION_WARD = 2043;
        public static int TRINKET_RED = 3364;
        public static float wardrange = 600f;
        public static float trinket_range = 600f;
        public static bool debug = false;

        public Revealer()
        {            
            Program.Menu.AddSubMenu(new Menu("Reveal", "Reveal"));
            Program.Menu.SubMenu("Reveal").AddItem(new MenuItem("tb_sep0", "====== Settings"));
            Program.Menu.SubMenu("Reveal").AddItem(new MenuItem("doRev", "Reveal").SetValue(true));
            Program.Menu.SubMenu("Reveal").AddItem(new MenuItem("revDesc1", "Priority:"));
            Program.Menu.SubMenu("Reveal").AddItem(new MenuItem("prior", "ON: Pink | OFF: Trinket").SetValue(true));
            if (player.BaseSkinName == "LeeSin")
            {
                Program.Menu.SubMenu("Reveal").AddItem(new MenuItem("leeE", "Lee Sin: Use E").SetValue(true));
                E = new Spell(SpellSlot.E, 350f);
            }
            Program.Menu.SubMenu("Reveal").AddItem(new MenuItem("tb_sep1", "========="));
            
            fillDict();           
            
            Game.OnGameUpdate += Game_GameUpdate;
        } 

        private void Game_GameUpdate(EventArgs args)
        {
            if (!isEn("doRev")) return;
            foreach (var enemy in ObjectManager.Get<Obj_AI_Hero>().Where(hero => hero.IsEnemy))
            {
                if (enemy.HasBuffOfType(BuffType.Invisibility) && !(enemy.BaseSkinName == "Evelynn"))
                {
                    Reveal(enemy);
                }
            }

        }

        private void Reveal(Obj_AI_Hero enemy)
        {
            if (player.BaseSkinName == "LeeSin" && E.IsReady() && player.Distance(enemy) <= E.Range && isEn("leeE"))
            {
                E.Cast();
            }
            else
            {
                if (isEn("prior"))
                {
                    //W
                    if (player.Distance(enemy) <= wardrange + 300f)
                    {
                        if (player.Distance(enemy) <= wardrange)
                        {
                            useItem(VISION_WARD, enemy.Position);
                        }
                        else
                        {
                            Vector3 pos1 = Vector3.Lerp(player.Position, enemy.Position, wardrange / player.Distance(enemy));
                            useItem(VISION_WARD, pos1);
                        }

                    }
                }
                else
                {
                    //Trink
                    if (player.Distance(enemy) <= trinket_range + 300f)
                    {
                        if (player.Distance(enemy) <= trinket_range)
                        {
                            useItem(TRINKET_RED, enemy.Position);
                        }
                        else
                        {
                            Vector3 pos1 = Vector3.Lerp(player.Position, enemy.Position, trinket_range / player.Distance(enemy));
                            useItem(TRINKET_RED, pos1);
                        }

                    }
                }
            }

        }
        private bool isEn(String item)
        {
            return Program.Menu.Item(item).GetValue<bool>();
        }

        private static void fillDict()
        {
            dict = new Dictionary<String, String>();
            dict.Add("Vayne", "VayneTumbleFade");
            dict.Add("Twitch", "TwitchHideInShadows");
            dict.Add("Rengar", "RengarR");
            dict.Add("MonkeyKing", "monkeykingdecoystealth");
            dict.Add("Khazix", "khazixrstealth");
            dict.Add("Talon", "talonshadowassaultbuff");
            dict.Add("Akali", "akaliwstealth");
        }
        private void useItem(int id, Vector3 position)
        {
            if (Items.HasItem(id) && Items.CanUseItem(id))
            {
                Items.UseItem(id, position);
            }
        }
    }
}
