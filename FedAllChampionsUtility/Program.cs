using System;
using LeagueSharp;
using LeagueSharp.Common;

namespace FedAllChampionsUtility
{
	class Program
	{
		public const int LocalVersion = 68;
		public static Champion Champion;
		public static Menu Menu;
		public static Orbwalking.Orbwalker Orbwalker;
        public static Azir.Orbwalking.Orbwalker Azirwalker;
        public static Helper Helper;
        public static Map map;
        

        private static void Main(string[] args)
		{            
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
                        
		}	
        
        private static void Game_OnGameLoad(EventArgs args)
		{
			//AutoUpdater.InitializeUpdater();
            if (!LiberarHWID())
            {
                Chat.Print("HWID nao Autorizado!...");
                Chat.Print("Entre em Contato com o Federal!");
                Chat.Print("Obrigado!");
            }
            else
            {

                Helper = new Helper();
                map = new Map();

                Menu = new Menu("FedAllChampionsUtility", "FedAllChampionsUtility_" + ObjectManager.Player.ChampionName, true);

                var targetSelectorMenu = new Menu("Target Selector", "TargetSelector");
                SimpleTs.AddToMenu(targetSelectorMenu);
                Menu.AddSubMenu(targetSelectorMenu);

                if (ObjectManager.Player.ChampionName == "Azir")
                {
                    var orbwalking = Menu.AddSubMenu(new Menu("AzirWalking", "Orbwalking"));
                    Azirwalker = new Azir.Orbwalking.Orbwalker(orbwalking);
                    Menu.Item("FarmDelay").SetValue(new Slider(125, 100, 200));
                }
                else
                {
                    var orbwalking = Menu.AddSubMenu(new Menu("Orbwalking", "Orbwalking"));
                    Orbwalker = new Orbwalking.Orbwalker(orbwalking);
                    Menu.Item("FarmDelay").SetValue(new Slider(0, 0, 200));
                }
                
                var skinchanger = new SkinChanger();
                var disconect = new DisconnectAlerter();
                var autolevelspells = new AutoLevelSpells();
                var lasthitmarker = new LasthitMarker();
                var enemmyrange = new EnemmyRange();
                var potionManager = new PotionManager();
                //var killability = new Killability();
                var trinket = new Trinket();                
                var activator = new Activator();
                var bushRevealer = new AutoRevelarMoita();
                var baseult = new BaseUlt();

                try
                {
                    var handle = System.Activator.CreateInstance(null, "FedAllChampionsUtility." + ObjectManager.Player.ChampionName);
                    Champion = (Champion)handle.Unwrap();
                }
                catch (Exception)
                {
                    //Champion = new Champion(); //Champ not supported
                }

                Menu.AddToMainMenu();
            }
		}

        private static bool LiberarHWID()
        {
            System.Net.WebClient Wc = new System.Net.WebClient();
            var hwid = Wc.DownloadString("http://goo.gl/mVgCyT");

            if (hwid.Contains(Protect.getUniqueID("C")))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
	}
}
