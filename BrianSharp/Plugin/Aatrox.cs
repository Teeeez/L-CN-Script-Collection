using System;
using System.Linq;
using Color = System.Drawing.Color;

using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;

using Orbwalk = BrianSharp.Common.Orbwalker;

namespace BrianSharp.Plugin
{
    class Aatrox : Common.Helper
    {
        public Aatrox()
        {
            Q = new Spell(SpellSlot.Q, 720);
            W = new Spell(SpellSlot.W);
            E = new Spell(SpellSlot.E, 1000, TargetSelector.DamageType.Magical);
            R = new Spell(SpellSlot.R, 550, TargetSelector.DamageType.Magical);
            Q.SetSkillshot(0.5f, 285, float.MaxValue, false, SkillshotType.SkillshotCircle);
            E.SetSkillshot(0.5f, 80, 1200, false, SkillshotType.SkillshotLine);
            R.SetSkillshot(0.5f, 550, 800, false, SkillshotType.SkillshotCircle);

            var ChampMenu = new Menu("脚本插件", PlayerName + "_Plugin");
            {
                var ComboMenu = new Menu("连招", "Combo");
                {
                    AddItem(ComboMenu, "Q", "使用 Q");
                    AddItem(ComboMenu, "W", "使用 W");
                    AddItem(ComboMenu, "WHpU", "-> 如果血量低于转换治疗", 50);
                    AddItem(ComboMenu, "E", "使用 E");
                    AddItem(ComboMenu, "R", "使用 R");
                    AddItem(ComboMenu, "RHpU", "-> 如果敌人血量低于", 60);
                    AddItem(ComboMenu, "RCountA", "-> 如果敌人超过", 2, 1, 4);
                    ChampMenu.AddSubMenu(ComboMenu);
                }
                var HarassMenu = new Menu("骚扰", "Harass");
                {
                    AddItem(HarassMenu, "AutoE", "使用 E", "H", KeyBindType.Toggle);
                    AddItem(HarassMenu, "AutoEHpA", "-> 如果血量超过", 50);
                    AddItem(HarassMenu, "Q", "使用 Q");
                    AddItem(HarassMenu, "QHpA", "-> 如果血量超过", 20);
                    AddItem(HarassMenu, "E", "使用 E");
                    ChampMenu.AddSubMenu(HarassMenu);
                }
                var ClearMenu = new Menu("清线/清野", "Clear");
                {
                    var SmiteMob = new Menu("惩戒能击杀野怪", "SmiteMob");
                    {
                        AddItem(SmiteMob, "Smite", "使用 惩戒");
                        AddItem(SmiteMob, "Baron", "-> 大龙");
                        AddItem(SmiteMob, "Dragon", "-> 小龙");
                        AddItem(SmiteMob, "Red", "-> 红爸爸");
                        AddItem(SmiteMob, "Blue", "-> 蓝爸爸");
                        AddItem(SmiteMob, "Krug", "-> Ancient Krug");
                        AddItem(SmiteMob, "Gromp", "-> Gromp");
                        AddItem(SmiteMob, "Raptor", "-> Crimson Raptor");
                        AddItem(SmiteMob, "Wolf", "-> Greater Murk Wolf");
                        ClearMenu.AddSubMenu(SmiteMob);
                    }
                    AddItem(ClearMenu, "Q", "使用 Q");
                    AddItem(ClearMenu, "W", "使用 W");
                    AddItem(ClearMenu, "WPriority", "-> 优先治疗");
                    AddItem(ClearMenu, "WHpU", "-> 如果血量低于转换治疗", 50);
                    AddItem(ClearMenu, "E", "使用 E");
                    ChampMenu.AddSubMenu(ClearMenu);
                }
                var FleeMenu = new Menu("逃跑", "Flee");
                {
                    AddItem(FleeMenu, "Q", "使用e Q");
                    AddItem(FleeMenu, "E", "使用E减缓敌人");
                    ChampMenu.AddSubMenu(FleeMenu);
                }
                var MiscMenu = new Menu("额外选项", "Misc");
                {
                    var KillStealMenu = new Menu("抢人头", "KillSteal");
                    {
                        AddItem(KillStealMenu, "Q", "使用 Q");
                        AddItem(KillStealMenu, "E", "使用 E");
                        AddItem(KillStealMenu, "Ignite", "使用 点燃");
                        MiscMenu.AddSubMenu(KillStealMenu);
                    }
                    var AntiGapMenu = new Menu("反突进", "AntiGap");
                    {
                        AddItem(AntiGapMenu, "Q", "使用 Q");
                        foreach (var Obj in ObjectManager.Get<Obj_AI_Hero>().Where(i => i.IsEnemy))
                        {
                            foreach (var Spell in AntiGapcloser.Spells.Where(i => i.ChampionName == Obj.ChampionName)) AddItem(AntiGapMenu, Obj.ChampionName + "_" + Spell.Slot.ToString(), "-> Skill " + Spell.Slot.ToString() + " Of " + Obj.ChampionName);
                        }
                        MiscMenu.AddSubMenu(AntiGapMenu);
                    }
                    var InterruptMenu = new Menu("打断", "Interrupt");
                    {
                        AddItem(InterruptMenu, "Q", "A使用 Q");
                        foreach (var Obj in ObjectManager.Get<Obj_AI_Hero>().Where(i => i.IsEnemy))
                        {
                            foreach (var Spell in Interrupter.Spells.Where(i => i.ChampionName == Obj.ChampionName)) AddItem(InterruptMenu, Obj.ChampionName + "_" + Spell.Slot.ToString(), "-> Skill " + Spell.Slot.ToString() + " Of " + Obj.ChampionName);
                        }
                        MiscMenu.AddSubMenu(InterruptMenu);
                    }
                    ChampMenu.AddSubMenu(MiscMenu);
                }
                var DrawMenu = new Menu("显示范围", "Draw");
                {
                    AddItem(DrawMenu, "Q", "Q 范围", false);
                    AddItem(DrawMenu, "E", "E 范围", false);
                    AddItem(DrawMenu, "R", "R 范围", false);
                    ChampMenu.AddSubMenu(DrawMenu);
                }
                MainMenu.AddSubMenu(ChampMenu);
            }
            Game.OnGameUpdate += OnGameUpdate;
            Drawing.OnDraw += OnDraw;
            AntiGapcloser.OnEnemyGapcloser += OnEnemyGapcloser;
            Interrupter.OnPossibleToInterrupt += OnPossibleToInterrupt;
        }

        private void OnGameUpdate(EventArgs args)
        {
            if (Player.IsDead || MenuGUI.IsChatOpen || Player.IsRecalling()) return;
            if (Orbwalk.CurrentMode == Orbwalk.Mode.Combo || Orbwalk.CurrentMode == Orbwalk.Mode.Harass)
            {
                NormalCombo(Orbwalk.CurrentMode.ToString());
            }
            else if (Orbwalk.CurrentMode == Orbwalk.Mode.LaneClear)
            {
                LaneJungClear();
            }
            else if (Orbwalk.CurrentMode == Orbwalk.Mode.Flee) Flee();
            if (GetValue<KeyBind>("Harass", "AutoE").Active) AutoE();
            KillSteal();
        }

        private void OnDraw(EventArgs args)
        {
            if (Player.IsDead) return;
            if (GetValue<bool>("Draw", "Q") && Q.Level > 0) Render.Circle.DrawCircle(Player.Position, Q.Range, Q.IsReady() ? Color.Green : Color.Red);
            if (GetValue<bool>("Draw", "E") && E.Level > 0) Render.Circle.DrawCircle(Player.Position, E.Range, E.IsReady() ? Color.Green : Color.Red);
            if (GetValue<bool>("Draw", "R") && R.Level > 0) Render.Circle.DrawCircle(Player.Position, R.Range, R.IsReady() ? Color.Green : Color.Red);
        }

        private void OnEnemyGapcloser(ActiveGapcloser gapcloser)
        {
            if (Player.IsDead || !GetValue<bool>("AntiGap", "Q") || !GetValue<bool>("AntiGap", gapcloser.Sender.ChampionName + "_" + gapcloser.Slot.ToString()) || !Q.IsReady() || !Orbwalk.InAutoAttackRange(gapcloser.Sender)) return;
            if (Q.Cast(gapcloser.Sender, PacketCast, true) == Spell.CastStates.SuccessfullyCasted) return;
        }

        private void OnPossibleToInterrupt(Obj_AI_Hero unit, InterruptableSpell spell)
        {
            if (Player.IsDead || !GetValue<bool>("Interrupt", "Q") || !GetValue<bool>("Interrupt", unit.ChampionName + "_" + spell.Slot.ToString()) || !Q.CanCast(unit)) return;
            if (Q.Cast(unit, PacketCast, true) == Spell.CastStates.SuccessfullyCasted) return;
        }

        private void NormalCombo(string Mode)
        {
            if (GetValue<bool>(Mode, "Q") && (Mode == "Combo" || (Mode == "Harass" && Player.HealthPercentage() >= GetValue<Slider>(Mode, "QHpA").Value)) && Q.CastOnBestTarget(Q.Width / 2, PacketCast, true) == Spell.CastStates.SuccessfullyCasted) return;
            if (GetValue<bool>(Mode, "E") && E.CastOnBestTarget(0, PacketCast) == Spell.CastStates.SuccessfullyCasted) return;
            if (Mode == "Combo")
            {
                if (GetValue<bool>(Mode, "W") && W.IsReady())
                {
                    if (Player.HealthPercentage() >= GetValue<Slider>("Clear", "WHpU").Value)
                    {
                        if (!Player.HasBuff("AatroxWPower") && W.Cast(PacketCast)) return;
                    }
                    else if (Player.HasBuff("AatroxWPower") && W.Cast(PacketCast)) return;
                }
                if (GetValue<bool>(Mode, "R") && R.IsReady())
                {
                    if (Player.CountEnemysInRange(R.Range) == 1)
                    {
                        var Target = R.GetTarget();
                        if (Target != null && Target.HealthPercentage() < GetValue<Slider>(Mode, "RHpU").Value && R.Cast(PacketCast)) return;
                    }
                    else
                    {
                        var Target = ObjectManager.Get<Obj_AI_Hero>().FindAll(i => i.IsValidTarget(R.Range));
                        if (Target.Count > 0 && ((Target.Count >= 2 && Target.Count(i => CanKill(i, R)) >= 1) || Target.Count >= GetValue<Slider>(Mode, "RCountA").Value || Target.Count(i => i.HealthPercentage() < GetValue<Slider>(Mode, "RHpU").Value) >= 2) && R.Cast(PacketCast)) return;
                    }
                }
            }
        }

        private void LaneJungClear()
        {
            var minionObj = MinionManager.GetMinions(E.Range, MinionTypes.All, MinionTeam.NotAlly, MinionOrderTypes.MaxHealth);
            if (minionObj.Count == 0) return;
            if (GetValue<bool>("SmiteMob", "Smite") && minionObj.Any(i => i.Team == GameObjectTeam.Neutral && CanSmiteMob(i.Name) && CastSmite(i))) return;
            if (GetValue<bool>("Clear", "Q") && Q.IsReady())
            {
                var Pos = Q.GetCircularFarmLocation(minionObj.FindAll(i => Player.Distance(i, true) <= Q.RangeSqr + Q.WidthSqr / 2), Q.Width - 30);
                if (Pos.MinionsHit > 0 && Q.Cast(Pos.Position, PacketCast)) return;
            }
            if ((!GetValue<bool>("Clear", "Q") || (GetValue<bool>("Clear", "Q") && !Q.IsReady())) && GetValue<bool>("Clear", "E") && E.IsReady())
            {
                var Pos = E.GetLineFarmLocation(minionObj);
                if (Pos.MinionsHit > 0 && E.Cast(Pos.Position, PacketCast)) return;
            }
            if (GetValue<bool>("Clear", "W") && W.IsReady())
            {
                if (Player.HealthPercentage() >= (GetValue<bool>("Clear", "WPriority") ? 85 : GetValue<Slider>("Clear", "WHpU").Value))
                {
                    if (!Player.HasBuff("AatroxWPower") && W.Cast(PacketCast)) return;
                }
                else if (Player.HasBuff("AatroxWPower") && W.Cast(PacketCast)) return;
            }
        }

        private void Flee()
        {
            if (GetValue<bool>("Flee", "Q") && Q.IsReady() && Q.Cast(Game.CursorPos, PacketCast)) return;
            if (GetValue<bool>("Flee", "E") && E.CastOnBestTarget(0, PacketCast) == Spell.CastStates.SuccessfullyCasted) return;
        }

        private void AutoE()
        {
            if (Player.HealthPercentage() < GetValue<Slider>("Harass", "AutoEHpA").Value || E.CastOnBestTarget(0, PacketCast) == Spell.CastStates.SuccessfullyCasted) return;
        }

        private void KillSteal()
        {
            if (GetValue<bool>("KillSteal", "Ignite") && Ignite.IsReady())
            {
                var Target = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.True);
                if (Target != null && CastIgnite(Target)) return;
            }
            if (GetValue<bool>("KillSteal", "Q") && Q.IsReady())
            {
                var Target = Q.GetTarget(Q.Width / 2);
                if (Target != null && CanKill(Target, Q) && Q.Cast(Target, PacketCast, true) == Spell.CastStates.SuccessfullyCasted) return;
            }
            if (GetValue<bool>("KillSteal", "E") && E.IsReady())
            {
                var Target = E.GetTarget();
                if (Target != null && CanKill(Target, E) && E.Cast(Target, PacketCast) == Spell.CastStates.SuccessfullyCasted) return;
            }
        }
    }
}