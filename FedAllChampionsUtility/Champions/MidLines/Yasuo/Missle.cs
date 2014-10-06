using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using LeagueSharp;
using LeagueSharp.Common;
using System.Drawing;
using SharpDX;

namespace FedAllChampionsUtility
{
    class Missle
    {
        GameObjectProcessSpellCastEventArgs Mis;
        Obj_AI_Base caster;
        float Damage;

        public Missle(GameObjectProcessSpellCastEventArgs missle, Obj_AI_Base obj)
        {
            Mis = missle;
            caster = obj;
            getSpell();
            Damage = calcDamage();
            Console.WriteLine(Damage + " - " + obj.Name + " - " + (int)getSpellSlot() + " - " + Mis.SData.Name);
        }

        public Vector3 getPosition()
        {
            return Mis.Start;
        }

        public void getSpell()
        {

        }

        public SpellSlot getSpellSlot()
        {
            return Utility.GetSpellSlot((Obj_AI_Hero)caster, Mis.SData.Name, false);
        }

        public Spellbook getCasterSpellBook()
        {
            return caster.Spellbook;
        }

        public float getDamage()
        {
            return Damage;
        }

        private float calcDamage()
        {
            return 0f;
        }

        public bool goesThroughUnit(Obj_AI_Base unit)
        {
            if (Yasuo.interact(Mis.Start.To2D(), Mis.End.To2D(), unit.Position.To2D(), unit.BoundingRadius + Mis.SData.LineWidth))
                return true;
            return false;
        }
    }
}
