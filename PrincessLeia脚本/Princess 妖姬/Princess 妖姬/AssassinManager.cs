﻿using System;
using System.Drawing;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;

namespace Princess_LeBlanc
{
    internal class AssassinManager
    {
        public AssassinManager()
        {
            Load();
        }

        private static void Load()
        {
            Program.LeBlancConfig.AddSubMenu(new Menu("Assassin Manager", "MenuAssassin"));
            Program.LeBlancConfig.SubMenu("MenuAssassin").AddItem(new MenuItem("AssassinActive", "Active").SetValue(true));
            Program.LeBlancConfig.SubMenu("MenuAssassin")
                .AddItem(
                    new MenuItem("AssassinSelectOption", "Set: ").SetValue(
                        new StringList(new[] { "Single Select", "Multi Select" })));
            Program.LeBlancConfig.SubMenu("MenuAssassin")
                .AddItem(new MenuItem("AssassinSetClick", "Add/Remove with click").SetValue(true));
            Program.LeBlancConfig.SubMenu("MenuAssassin")
                .AddItem(
                    new MenuItem("AssassinReset", "Reset List").SetValue(new KeyBind("T".ToCharArray()[0],
                        KeyBindType.Press)));

            Program.LeBlancConfig.SubMenu("MenuAssassin").AddSubMenu(new Menu("Draw:", "Draw"));

            Program.LeBlancConfig.SubMenu("MenuAssassin")
                .SubMenu("Draw")
                .AddItem(new MenuItem("DrawSearch", "Search Range").SetValue(new Circle(true, Color.GreenYellow)));
            Program.LeBlancConfig.SubMenu("MenuAssassin")
                .SubMenu("Draw")
                .AddItem(new MenuItem("DrawActive", "Active Enemy").SetValue(new Circle(true, Color.GreenYellow)));
            Program.LeBlancConfig.SubMenu("MenuAssassin")
                .SubMenu("Draw")
                .AddItem(new MenuItem("DrawNearest", "Nearest Enemy").SetValue(new Circle(true, Color.DarkSeaGreen)));
            Program.LeBlancConfig.SubMenu("MenuAssassin")
                .SubMenu("Draw")
                .AddItem(new MenuItem("DrawStatus", "Show Status").SetValue(true));


            Program.LeBlancConfig.SubMenu("MenuAssassin").AddSubMenu(new Menu("Assassin List:", "AssassinMode"));
            foreach (
                var enemy in ObjectManager.Get<Obj_AI_Hero>().Where(enemy => enemy.Team != ObjectManager.Player.Team))
            {
                Program.LeBlancConfig.SubMenu("MenuAssassin")
                    .SubMenu("AssassinMode")
                    .AddItem(
                        new MenuItem("Assassin" + enemy.ChampionName, enemy.ChampionName).SetValue(
                            TargetSelector.GetPriority(enemy) > 3));
            }
            Program.LeBlancConfig.SubMenu("MenuAssassin")
                .AddItem(new MenuItem("AssassinSearchRange", "Search Range")).SetValue(new Slider(1000, 2000));

            Game.OnGameUpdate += OnGameUpdate;
            Drawing.OnDraw += Drawing_OnDraw;
            Game.OnWndProc += Game_OnWndProc;
        }

        static void ClearAssassinList()
        {
            foreach (
                var enemy in ObjectManager.Get<Obj_AI_Hero>().Where(enemy => enemy.Team != ObjectManager.Player.Team))
            {
                Program.LeBlancConfig.Item("Assassin" + enemy.ChampionName).SetValue(false);
            }
        }
        private static void OnGameUpdate(EventArgs args)
        {
        }

        private static void Game_OnWndProc(WndEventArgs args)
        {

            if (Program.LeBlancConfig.Item("AssassinReset").GetValue<KeyBind>().Active && args.Msg == 257)
            {
                ClearAssassinList();
                Game.PrintChat(
                    "<font color='#FFFFFF'>Reset Assassin List is Complete! Click on the enemy for Add/Remove.</font>");
            }

            if (args.Msg != (uint)WindowsMessages.WM_LBUTTONDOWN)
            {
                return;
            }

            if (Program.LeBlancConfig.Item("AssassinSetClick").GetValue<bool>())
            {
                foreach (var objAiHero in from hero in ObjectManager.Get<Obj_AI_Hero>()
                                          where hero.IsValidTarget()
                                          select hero
                                              into h
                                              orderby h.Distance(Game.CursorPos) descending
                                              select h
                                                  into enemy
                                                  where enemy.Distance(Game.CursorPos) < 150f
                                                  select enemy)
                {
                    if (objAiHero != null && objAiHero.IsVisible && !objAiHero.IsDead)
                    {
                        var xSelect =
                            Program.LeBlancConfig.Item("AssassinSelectOption").GetValue<StringList>().SelectedIndex;

                        switch (xSelect)
                        {
                            case 0:
                                ClearAssassinList();
                                Program.LeBlancConfig.Item("Assassin" + objAiHero.ChampionName).SetValue(true);
                                Game.PrintChat(
                                    string.Format(
                                        "<font color='FFFFFF'>Added to Assassin List</font> <font color='#09F000'>{0} ({1})</font>",
                                        objAiHero.Name, objAiHero.ChampionName));
                                break;
                            case 1:
                                var menuStatus =
                                    Program.LeBlancConfig.Item("Assassin" + objAiHero.ChampionName)
                                        .GetValue<bool>();
                                Program.LeBlancConfig.Item("Assassin" + objAiHero.ChampionName)
                                    .SetValue(!menuStatus);
                                Game.PrintChat(
                                    string.Format("<font color='{0}'>{1}</font> <font color='#09F000'>{2} ({3})</font>",
                                        !menuStatus ? "#FFFFFF" : "#FF8877",
                                        !menuStatus ? "Added to Assassin List:" : "Removed from Assassin List:",
                                        objAiHero.Name, objAiHero.ChampionName));
                                break;
                        }
                    }
                }
            }
        }
        private static void Drawing_OnDraw(EventArgs args)
        {
            if (!Program.LeBlancConfig.Item("AssassinActive").GetValue<bool>())
                return;

            if (Program.LeBlancConfig.Item("DrawStatus").GetValue<bool>())
            {
                var enemies = ObjectManager.Get<Obj_AI_Hero>().Where(xEnemy => xEnemy.IsEnemy);
                var objAiHeroes = enemies as Obj_AI_Hero[] ?? enemies.ToArray();
                Drawing.DrawText(Drawing.Width * 0.90f, Drawing.Height * 0.58f, Color.GreenYellow, "Assassin Status");
                Drawing.DrawText(Drawing.Width * 0.90f, Drawing.Height * 0.58f, Color.GhostWhite, "_____________");
                for (int i = 0; i < objAiHeroes.Count(); i++)
                {
                    var xCaption = objAiHeroes[i].ChampionName;
                    var xWidth = Drawing.Width * 0.90f;
                    if (Program.LeBlancConfig.Item("Assassin" + objAiHeroes[i].ChampionName).GetValue<bool>())
                    {
                        xCaption = "+ " + xCaption;
                        xWidth = Drawing.Width * 0.8910f;
                    }
                    Drawing.DrawText(xWidth, Drawing.Height * 0.58f + (float)(i + 1) * 15, Color.Gainsboro,
                        xCaption);
                }
            }

            var drawSearch = Program.LeBlancConfig.Item("DrawSearch").GetValue<Circle>();
            var drawActive = Program.LeBlancConfig.Item("DrawActive").GetValue<Circle>();
            var drawNearest = Program.LeBlancConfig.Item("DrawNearest").GetValue<Circle>();

            var drawSearchRange = Program.LeBlancConfig.Item("AssassinSearchRange").GetValue<Slider>().Value;
            if (drawSearch.Active)
            {
                Render.Circle.DrawCircle(ObjectManager.Player.Position, drawSearchRange, drawSearch.Color);
            }

            foreach (
                var enemy in
                    ObjectManager.Get<Obj_AI_Hero>()
                        .Where(enemy => enemy.Team != ObjectManager.Player.Team)
                        .Where(
                            enemy =>
                                enemy.IsVisible &&
                                Program.LeBlancConfig.Item("Assassin" + enemy.ChampionName) != null &&
                                !enemy.IsDead)
                        .Where(
                            enemy => Program.LeBlancConfig.Item("Assassin" + enemy.ChampionName).GetValue<bool>()))
            {
                if (ObjectManager.Player.Distance(enemy) < drawSearchRange)
                {
                    if (drawActive.Active)
                        Render.Circle.DrawCircle(enemy.Position, 85f, drawActive.Color);
                }
                else if (ObjectManager.Player.Distance(enemy) > drawSearchRange &&
                         ObjectManager.Player.Distance(enemy) < drawSearchRange + 400)
                {
                    if (drawNearest.Active)
                        Render.Circle.DrawCircle(enemy.Position, 85f, drawNearest.Color);
                }
            }
        }
    }
}