using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;




namespace Kennen
{
    internal class Program
    {
        //      {
        private const string ChampName = "Kennen";
        public static Orbwalking.Orbwalker Orbwalker;
        public static List<Spell> Spells = new List<Spell>();
        public static Spell Q;
        public static Spell W;
        public static Spell E;
        public static Spell R;
        public static SpellSlot IgniteSlot;
        private static Obj_AI_Hero _player;

        public static Menu Config;

        private static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }


        private static void Game_OnGameLoad(EventArgs args)
        {
            _player = ObjectManager.Player;
            if (_player.BaseSkinName != ChampName)
                return;

            Q = new Spell(SpellSlot.Q, 1050);
            Q.SetSkillshot(0.25f, 60f, 2000f, true, SkillshotType.SkillshotLine);

            W = new Spell(SpellSlot.W, 770);
            E = new Spell(SpellSlot.E, 0);
            R = new Spell(SpellSlot.R, 470);

            IgniteSlot = ObjectManager.Player.GetSpellSlot("SummonerDot");

            Spells.Add(Q);
            Spells.Add(W);
            Spells.Add(E);
            Spells.Add(R);

            Config = new Menu(ChampName, ChampName, true);

            var tsMenu = new Menu("Target Selector", "Target Selector");
            TargetSelector.AddToMenu(tsMenu);
            Config.AddSubMenu(tsMenu);
            Config.AddSubMenu(new Menu("Orbwalking", "Orbwalking"));
            Orbwalker = new Orbwalking.Orbwalker(Config.SubMenu("Orbwalking"));


            Config.AddSubMenu(new Menu("Combo Settings", "combo"));
            Config.SubMenu("combo").AddItem(new MenuItem("comboKey", "Full Combo Key").SetValue(new KeyBind(32, KeyBindType.Press)));
            Config.SubMenu("combo").AddItem(new MenuItem("UseE", "Use E in combo").SetValue(false));
            Config.SubMenu("combo").AddItem(new MenuItem("autoIgnite", "Use Ignite in combo").SetValue(true));

            Config.AddSubMenu(new Menu("Harass Settings", "harass"));
            Config.SubMenu("harass").AddItem(new MenuItem("autoQ", "Auto-Q when Target in Range").SetValue(new KeyBind("Z".ToCharArray()[0], KeyBindType.Toggle)));
            Config.SubMenu("harass").AddItem(new MenuItem("autoW", "Auto-W when Target in Range").SetValue(new KeyBind("J".ToCharArray()[0], KeyBindType.Toggle)));
            Config.SubMenu("harass").AddItem(new MenuItem("harassKey", "Q Harass Key").SetValue(new KeyBind("T".ToCharArray()[0], KeyBindType.Press)));

            Config.AddSubMenu(new Menu("Misc Settings", "misc"));
            
            Config.SubMenu("misc").AddItem(new MenuItem("useRhit", "Use R if hit").SetValue(new Slider(2, 5, 0)));
            Config.SubMenu("misc").AddItem(new MenuItem("autoR", "Auto Ulti").SetValue(new KeyBind("U".ToCharArray()[0], KeyBindType.Toggle)));
            Config.SubMenu("misc").AddItem(new MenuItem("autozh", "Auto Zhonya").SetValue(new KeyBind("Y".ToCharArray()[0], KeyBindType.Toggle)));
            Config.SubMenu("misc").AddItem(new MenuItem("autozhhp", "Min Percentage of HP to use Zhonya").SetValue(new Slider(40, 1)));




            Config.AddSubMenu(new Menu("Draw Settings", "drawing"));
            Config.SubMenu("drawing").AddItem(new MenuItem("mDraw", "Disable All Range Draws").SetValue(false));
            Config.SubMenu("drawing").AddItem(new MenuItem("Target", "Draw Circle on Target").SetValue(new Circle(true, Color.FromArgb(255, 255, 0, 0))));
            Config.SubMenu("drawing").AddItem(new MenuItem("QDraw", "Draw (Q) Range").SetValue(new Circle(true, Color.FromArgb(255, 178, 0, 0))));
            Config.SubMenu("drawing").AddItem(new MenuItem("WDraw", "Draw (W) Range").SetValue(new Circle(true, Color.FromArgb(80, 4, 177, 204))));
            Config.SubMenu("drawing").AddItem(new MenuItem("RDraw", "Draw (R) Range").SetValue(new Circle(true, Color.FromArgb(80, 4, 21, 204))));

            Config.AddToMainMenu();

            Drawing.OnDraw += Drawing_OnDraw;
            Game.OnUpdate += Game_OnGameUpdate;


            Game.PrintChat("<font color='#000080'>Kennen Reloaded By</font> <font color='#B22222'>ReloadBot</font>");

        }

        private static void Game_OnGameUpdate(EventArgs args)
        {
            var target = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Magical);
            var comboKey = Config.Item("comboKey").GetValue<KeyBind>().Active;
            var harassKey = Config.Item("harassKey").GetValue<KeyBind>().Active;
            var ultikey = Config.Item("autoR").GetValue<KeyBind>().Active;
            var autozhkey = Config.Item("autozh").GetValue<KeyBind>().Active;
            var autoW = Config.Item("autoW").GetValue<bool>();
            var autoQ = Config.Item("autoQ").GetValue<bool>();


            if (ultikey)
                AutoR();

            if (autozhkey)
                AutoZhonya();

            if (comboKey && target != null)
                Combo(target);
            else
            {
                if (harassKey && target != null)
                    Harass(target);

                if (target == null || !target.IsValidTarget()) ;
                return;

                if (autoQ && Q.IsReady() && Q.IsInRange(target))
                {
                    Q.Cast();
                    CastBasicSkillShot(Q, Q.Range, TargetSelector.DamageType.Magical, HitChance.High);
                }
                    

                if (target == null || !target.IsValidTarget()) ;
                return;

                if (autoW && W.IsReady() && W.IsInRange(target))
                {
                    W.Cast();
                }

            }

        }


        private static void Drawing_OnDraw(EventArgs args)
        {
            if (Config.Item("mDraw").GetValue<bool>())
                return;

            foreach (var spell in Spells.Where(spell => Config.Item(spell.Slot + "Draw").GetValue<Circle>().Active))
            {
                Utility.DrawCircle(ObjectManager.Player.Position, spell.Range, Config.Item(spell.Slot + "Draw").GetValue<Circle>().Color);
            }
            var target = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Physical);
            if (Config.Item("Target").GetValue<Circle>().Active && target != null) Utility.DrawCircle(target.Position, 50, Config.Item("Target").GetValue<Circle>().Color, 1, 50);
        }


        private static void AutoZhonya()
        {

            var health = ObjectManager.Player.MaxHealth * (Config.Item("autozhhp").GetValue<Slider>().Value / 100.0);
            if (ObjectManager.Player.Health > health)
                return;
            if (Items.CanUseItem("ZhonyasHourglass"))
                Items.UseItem("ZhonyasHourglass");
        }

        public static void AutoR()
        {
            var rtarget = TargetSelector.GetTarget(R.Range, TargetSelector.DamageType.Magical);
            var RMinHit = Config.Item("useRhit").GetValue<Slider>().Value;
            var enemiesHit = ObjectManager.Player.CountEnemiesInRange(470);

            if (rtarget != null && ObjectManager.Player.Distance(rtarget,false) <= R.Range && (RMinHit <= enemiesHit) && R.IsReady())
                R.Cast();

        }



        private static void Combo(Obj_AI_Base target)
        {
            if (target == null)
                return;

            if (Q.IsReady())
                CastBasicSkillShot(Q, Q.Range, TargetSelector.DamageType.Magical, HitChance.High);

            if (target != null && Config.Item("UseE").GetValue<bool>() && E.IsReady())
                E.Cast();

            else
            {

                if (target != null && ObjectManager.Player.Distance(target,false) <= W.Range && W.IsReady())
                    W.Cast();

                if (target != null && ObjectManager.Player.Distance(target,false) <= R.Range && R.IsReady())
                    R.Cast();


                if (!Config.Item("autoIgnite").GetValue<bool>())
                    return;

                if (IgniteSlot == SpellSlot.Unknown ||
                    ObjectManager.Player.Spellbook.CanUseSpell(IgniteSlot) != SpellState.Ready)
                    return;

                if (!(ObjectManager.Player.GetSummonerSpellDamage(target, Damage.SummonerSpell.Ignite) >= target.Health))
                    return;

                ObjectManager.Player.Spellbook.CastSpell(IgniteSlot, target);
            }
        }

        private static void Harass(Obj_AI_Base target)
        {
            if (target == null)
                return;


            if (Q.IsReady())
                CastBasicSkillShot(Q, Q.Range, TargetSelector.DamageType.Magical, HitChance.High);
        }

        public static void CastBasicSkillShot(Spell spell, float range, LeagueSharp.Common.TargetSelector.DamageType type, HitChance hitChance)
        {
            var target = LeagueSharp.Common.TargetSelector.GetTarget(range, type);
            if (target == null || !spell.IsReady())
                return;
            spell.UpdateSourcePosition();
            
        }

    }
}
