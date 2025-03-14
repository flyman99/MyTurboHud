namespace Turbo.Plugins.User
{
    using Turbo.Plugins.Default;
    using SharpDX.DirectInput;
    using System.Windows.Forms;
    using System.Collections.Generic;
    using System.Linq;
    using System;
    using SharpDX;
    using System.IO;

    public class AutoGreaterRiftPlugin : BasePlugin, IInGameTopPainter, IKeyEventHandler, INewAreaHandler, IAfterCollectHandler
    {
        public bool Running { get; set; }
        public IFont HeaderFont { get; private set; }
        public IFont InfoFont { get; private set; }
        public string StatusHeader { get; set; }
        public string CurrentAction { get; set; }
        public IWatch DelayTimer { get; private set; }
        public IUiElement uiGRmainPage => Hud.Render.GetUiElement("Root.NormalLayer.riftmenu_main");
        public IUiElement uiOnGreaterRift => Hud.Render.GetUiElement("Root.NormalLayer.riftmenu_main.LayoutRoot.greaterRift");
        public IUiElement uiAcceptButton => Hud.Render.GetUiElement("Root.NormalLayer.riftmenu_main.LayoutRoot.Accept_Layout.AcceptBtn");

        private bool IsInTown => Hud.Game.Me.IsInTown;
        private bool IsInRift => Hud.Game.SpecialArea == SpecialArea.GreaterRift;

        public enum RiftStep
        {
            OpenRift,
            ClearRift,
            CollectDrops,
            TalkToOrek,
            UpgradeGem,
            CloseRift
        }

        public RiftStep CurrentStep { get; set; }

        public IKeyEvent ToggleKeyEvent { get; private set; }

        public IFont DebugFont { get; private set; }
        private List<string> DebugMessages = new List<string>();
        private AutoGreaterRiftPathfindingPlugin PathfindingPlugin { get; set; }
        private string LogFilePath => Path.Combine(Directory.GetCurrentDirectory(), "TurboHUD", "logs", "AutoGreaterRiftPlugin_Log.txt");

        public AutoGreaterRiftPlugin()
        {
            Enabled = true;
            Running = false;
            CurrentStep = RiftStep.OpenRift;
            StatusHeader = "大秘境自动化脚本已停止";
            CurrentAction = "待机中";
        }

        public override void Load(IController hud)
        {
            base.Load(hud);
            HeaderFont = Hud.Render.CreateFont("tahoma", 9, 255, 0, 255, 255, true, false, 255, 0, 0, 0, true);
            InfoFont = Hud.Render.CreateFont("tahoma", 7, 255, 255, 255, 255, false, false, 255, 0, 0, 0, true);
            DelayTimer = Hud.Time.CreateWatch();
            ToggleKeyEvent = Hud.Input.CreateKeyEvent(true, Key.Insert, false, false, false);
            DebugFont = Hud.Render.CreateFont("tahoma", 8, 255, 0, 255, 0, true, false, 255, 0, 0, 0, true);
            PathfindingPlugin = Hud.GetPlugin<AutoGreaterRiftPathfindingPlugin>();
            
            // 初始化日志文件
            if (File.Exists(LogFilePath)) File.Delete(LogFilePath);
            
            // 初始化状态
            Running = false;
            CurrentStep = RiftStep.OpenRift;
            StatusHeader = "大秘境自动化脚本已停止";
            CurrentAction = "等待启动";
            
            AddDebugMessage("插件已加载，初始状态设置完成");
            AddDebugMessage($"Running: {Running}, CurrentStep: {CurrentStep}");
            AddDebugMessage($"PathfindingPlugin: {(PathfindingPlugin != null ? "已找到" : "未找到")}");
        }

        private void AddDebugMessage(string message)
        {
            string timestampedMessage = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [{Hud.Game.CurrentGameTick}] {message}";
            DebugMessages.Add(timestampedMessage);
            if (DebugMessages.Count > 10) DebugMessages.RemoveAt(0);
            // 写入日志文件
            try
            {
                File.AppendAllText(LogFilePath, timestampedMessage + Environment.NewLine);
            }
            catch (Exception ex)
            {
                Hud.Sound.Speak("日志写入失败: " + ex.Message);
            }
        }

        public void PaintTopInGame(ClipState clipState)
        {
            if (clipState != ClipState.BeforeClip) return;
            if (Hud.Render.UiHidden) return;

            var uiElement = Hud.Render.GetUiElement("Root.NormalLayer.minimap_dialog_backgroundScreen.minimap_dialog_pve.BoostWrapper");
            var uiRect = uiElement != null ? ToSharpDXRectangleF(uiElement.Rectangle) : new SharpDX.RectangleF(0, 0, Hud.Window.Size.Width, Hud.Window.Size.Height);

            HeaderFont.DrawText(StatusHeader ?? "大秘境自动化脚本", uiRect.Left + 20, uiRect.Top + 30);
            InfoFont.DrawText("当前步骤: " + CurrentStep.ToString(), uiRect.Left + 20, uiRect.Top + 50);
            InfoFont.DrawText("当前动作: " + (CurrentAction ?? "待机中"), uiRect.Left + 20, uiRect.Top + 70);
            InfoFont.DrawText("按Insert键开启/关闭自动化脚本", uiRect.Left + 20, uiRect.Top + 90);

            for (int i = 0; i < DebugMessages.Count; i++)
            {
                DebugFont.DrawText(DebugMessages[i], uiRect.Left + 20, uiRect.Top + 110 + i * 20);
            }
        }

        private SharpDX.RectangleF ToSharpDXRectangleF(System.Drawing.RectangleF rect)
        {
            return new SharpDX.RectangleF(rect.X, rect.Y, rect.Width, rect.Height);
        }

        public void OnKeyEvent(IKeyEvent keyEvent)
        {
            if (ToggleKeyEvent.Matches(keyEvent) && keyEvent.IsPressed)
            {
                Running = !Running;
                if (Running)
                {
                    StatusHeader = "大秘境自动化脚本已启动";
                    CurrentStep = RiftStep.OpenRift;
                    PathfindingPlugin.AutoNavigate = true;
                    AddDebugMessage("脚本启动，CurrentStep 设置为 RiftStep.OpenRift，AutoNavigate 设置为 true");
                }
                else
                {
                    StatusHeader = "大秘境自动化脚本已停止";
                    PathfindingPlugin.AutoNavigate = false;
                    AddDebugMessage("脚本停止，AutoNavigate 设置为 false");
                }
            }
        }

        public void OnNewArea(bool newGame, ISnoArea area)
        {
            AddDebugMessage($"OnNewArea 被调用，newGame: {newGame}, area: {area?.NameEnglish}, SpecialArea: {Hud.Game.SpecialArea}");
            
            if (newGame)
            {
                AddDebugMessage("新游戏开始，重置状态");
                Running = false;
                CurrentStep = RiftStep.OpenRift;
                PathfindingPlugin.AutoNavigate = false;
                StatusHeader = "大秘境自动化脚本已停止";
                CurrentAction = "等待启动";
            }
            else if (area != null)
            {
                AddDebugMessage($"进入新区域: {area.NameEnglish}, SpecialArea: {Hud.Game.SpecialArea}");
                
                // 如果进入了大秘境
                if (Hud.Game.SpecialArea == SpecialArea.GreaterRift && Running)
                {
                    AddDebugMessage("进入大秘境，设置 CurrentStep 为 ClearRift");
                    CurrentStep = RiftStep.ClearRift;
                    CurrentAction = "清理大秘境";
                    
                    // 确保寻路插件被激活
                    if (PathfindingPlugin != null)
                    {
                        PathfindingPlugin.AutoNavigate = true;
                        AddDebugMessage("已激活寻路插件，设置 AutoNavigate 为 true");
                    }
                }
                // 如果回到城镇
                else if (Hud.Game.Me.IsInTown && Running)
                {
                    AddDebugMessage("回到城镇，检查当前步骤");
                    // 如果当前步骤是清理秘境，说明可能是从秘境回到城镇，需要继续后续步骤
                    if (CurrentStep == RiftStep.ClearRift)
                    {
                        AddDebugMessage("从秘境回到城镇，设置 CurrentStep 为 TalkToOrek");
                        CurrentStep = RiftStep.TalkToOrek;
                        CurrentAction = "与奥瑞克对话";
                    }
                    else if (CurrentStep != RiftStep.OpenRift)
                    {
                        AddDebugMessage("回到城镇，但当前步骤不是 OpenRift 或 ClearRift，重置为 OpenRift");
                        CurrentStep = RiftStep.OpenRift;
                        CurrentAction = "打开大秘境";
                    }
                }
                // 如果在普通秘境中
                else if (Hud.Game.SpecialArea == SpecialArea.Rift && Running)
                {
                    AddDebugMessage("检测到在普通秘境中，而不是大秘境，重置为 OpenRift");
                    CurrentStep = RiftStep.OpenRift;
                    CurrentAction = "打开大秘境";
                }
            }
        }

        public void AfterCollect()
        {
            AddDebugMessage($"AfterCollect 被调用，Running: {Running}, CurrentStep: {CurrentStep}, IsInGame: {Hud.Game.IsInGame}, IsDead: {Hud.Game.Me.IsDead}, SpecialArea: {Hud.Game.SpecialArea}");
            
            if (!Running)
            {
                AddDebugMessage("Running 为 false，不执行任何操作");
                return;
            }
            
            if (!Hud.Game.IsInGame || Hud.Game.Me.IsDead)
            {
                AddDebugMessage("不在游戏中或角色已死亡，不执行任何操作");
                return;
            }
            
            // 检查当前区域类型，如果在普通秘境中但当前步骤是 ClearRift，则重置为 OpenRift
            if (Hud.Game.SpecialArea == SpecialArea.Rift && CurrentStep == RiftStep.ClearRift)
            {
                AddDebugMessage("检测到在普通秘境中，但当前步骤是 ClearRift，重置为 OpenRift");
                CurrentStep = RiftStep.OpenRift;
                CurrentAction = "打开大秘境";
            }
            
            // 检查当前区域类型，如果在大秘境中但当前步骤是 OpenRift，则切换到 ClearRift
            if (Hud.Game.SpecialArea == SpecialArea.GreaterRift && CurrentStep == RiftStep.OpenRift)
            {
                AddDebugMessage("检测到在大秘境中，但当前步骤是 OpenRift，切换到 ClearRift");
                CurrentStep = RiftStep.ClearRift;
                CurrentAction = "清理大秘境";
                
                // 确保寻路插件被激活
                if (PathfindingPlugin != null)
                {
                    PathfindingPlugin.AutoNavigate = true;
                    AddDebugMessage("已激活寻路插件，设置 AutoNavigate 为 true");
                }
            }
            
            AddDebugMessage($"准备执行 CurrentStep: {CurrentStep}");
            ExecuteCurrentStep();
            
            if (PathfindingPlugin != null)
            {
                AddDebugMessage("调用 PathfindingPlugin.Update");
                PathfindingPlugin.Update(Hud);
            }
            else
            {
                AddDebugMessage("PathfindingPlugin 为 null");
            }
        }

        private void ExecuteCurrentStep()
        {
            AddDebugMessage($"ExecuteCurrentStep 被调用，当前步骤: {CurrentStep}");
            
            switch (CurrentStep)
            {
                case RiftStep.OpenRift:
                    AddDebugMessage("执行 OpenGreaterRift() 方法");
                    OpenGreaterRift();
                    break;
                case RiftStep.ClearRift:
                    AddDebugMessage("执行 ClearGreaterRift() 方法");
                    ClearGreaterRift();
                    break;
                case RiftStep.CollectDrops:
                    AddDebugMessage("执行 CollectDrops() 方法");
                    CollectDrops();
                    break;
                case RiftStep.TalkToOrek:
                    AddDebugMessage("执行 TalkToOrek() 方法");
                    TalkToOrek();
                    break;
                case RiftStep.UpgradeGem:
                    AddDebugMessage("执行 UpgradeGem() 方法");
                    UpgradeGem();
                    break;
                case RiftStep.CloseRift:
                    AddDebugMessage("执行 CloseRift() 方法");
                    CloseRift();
                    break;
                default:
                    AddDebugMessage($"未知的步骤: {CurrentStep}");
                    break;
            }
        }

        private void OpenGreaterRift()
        {
            AddDebugMessage($"OpenGreaterRift 被调用，CurrentStep: {CurrentStep}, IsInTown: {Hud.Game.Me.IsInTown}, IsInRift: {Hud.Game.SpecialArea == SpecialArea.GreaterRift}, SpecialArea: {Hud.Game.SpecialArea}");
            
            if (!Hud.Game.Me.IsInTown || Hud.Game.SpecialArea == SpecialArea.GreaterRift)
            {
                AddDebugMessage("不在城镇或已在秘境中，不执行操作");
                return;
            }

            var uiGRmainPage = Hud.Render.GetUiElement("Root.NormalLayer.rift_dialog_mainPage");
            if (uiGRmainPage == null || !uiGRmainPage.Visible)
            {
                uiGRmainPage = Hud.Render.GetUiElement("Root.NormalLayer.riftmenu_main");
            }
            
            var uiGRtierPage = Hud.Render.GetUiElement("Root.NormalLayer.rift_dialog_tierPage");
            if (uiGRtierPage == null || !uiGRtierPage.Visible)
            {
                uiGRtierPage = Hud.Render.GetUiElement("Root.NormalLayer.riftmenu_tier");
            }
            
            var uiGRacceptButton = Hud.Render.GetUiElement("Root.NormalLayer.rift_dialog_mainPage.LayoutRoot.rift_dialog_c_dialog_template.LayoutRoot.ButtonContainer.button_accept");
            if (uiGRacceptButton == null || !uiGRacceptButton.Visible)
            {
                uiGRacceptButton = Hud.Render.GetUiElement("Root.NormalLayer.riftmenu_main.LayoutRoot.Accept_Layout.AcceptBtn");
            }
            
            // 尝试获取大秘境按钮
            var uiGRbutton = Hud.Render.GetUiElement("Root.NormalLayer.rift_dialog_mainPage.LayoutRoot.rift_dialog_c_dialog_template.LayoutRoot.ButtonContainer.button_greater");
            if (uiGRbutton == null || !uiGRbutton.Visible)
            {
                uiGRbutton = Hud.Render.GetUiElement("Root.NormalLayer.riftmenu_main.LayoutRoot.greaterRift");
            }
            
            // 如果已经在大秘境选择界面
            if (uiGRmainPage?.Visible == true)
            {
                AddDebugMessage("大秘境选择界面已打开");
                
                // 如果在层级选择页面
                if (uiGRtierPage?.Visible == true)
                {
                    AddDebugMessage("正在层级选择页面");
                    
                    // 选择最高层级
                    var uiGRtierUpButton = Hud.Render.GetUiElement("Root.NormalLayer.rift_dialog_tierPage.LayoutRoot.rift_dialog_c_dialog_template.LayoutRoot.TierLevelContainer.button_increaseTier");
                    if (uiGRtierUpButton == null || !uiGRtierUpButton.Visible)
                    {
                        uiGRtierUpButton = Hud.Render.GetUiElement("Root.NormalLayer.riftmenu_tier.LayoutRoot.TierLevelContainer.button_increaseTier");
                    }
                    
                    if (uiGRtierUpButton?.Visible == true)
                    {
                        AddDebugMessage("点击提高层级按钮");
                        Hud.Interaction.MouseMove(uiGRtierUpButton.Rectangle.X + uiGRtierUpButton.Rectangle.Width / 2, 
                                                 uiGRtierUpButton.Rectangle.Y + uiGRtierUpButton.Rectangle.Height / 2);
                        Hud.Interaction.MouseDown(MouseButtons.Left);
                        Hud.Interaction.MouseUp(MouseButtons.Left);
                        DelayTimer.Restart();
                        return;
                    }
                    
                    // 点击确认层级
                    var uiGRtierAcceptButton = Hud.Render.GetUiElement("Root.NormalLayer.rift_dialog_tierPage.LayoutRoot.rift_dialog_c_dialog_template.LayoutRoot.ButtonContainer.button_accept");
                    if (uiGRtierAcceptButton == null || !uiGRtierAcceptButton.Visible)
                    {
                        uiGRtierAcceptButton = Hud.Render.GetUiElement("Root.NormalLayer.riftmenu_tier.LayoutRoot.Accept_Layout.AcceptBtn");
                    }
                    
                    if (uiGRtierAcceptButton?.Visible == true)
                    {
                        AddDebugMessage("点击层级确认按钮");
                        Hud.Interaction.MouseMove(uiGRtierAcceptButton.Rectangle.X + uiGRtierAcceptButton.Rectangle.Width / 2, 
                                                 uiGRtierAcceptButton.Rectangle.Y + uiGRtierAcceptButton.Rectangle.Height / 2);
                        Hud.Interaction.MouseDown(MouseButtons.Left);
                        Hud.Interaction.MouseUp(MouseButtons.Left);
                        DelayTimer.Restart();
                        return;
                    }
                }
                
                // 点击接受按钮
                if (uiGRacceptButton?.Visible == true)
                {
                    AddDebugMessage($"点击接受按钮");
                    Hud.Interaction.MouseMove(uiGRacceptButton.Rectangle.X + uiGRacceptButton.Rectangle.Width / 2, 
                                             uiGRacceptButton.Rectangle.Y + uiGRacceptButton.Rectangle.Height / 2);
                    Hud.Interaction.MouseDown(MouseButtons.Left);
                    Hud.Interaction.MouseUp(MouseButtons.Left);
                    DelayTimer.Restart();
                    return;
                }
                
                // 点击大秘境按钮
                if (uiGRbutton?.Visible == true)
                {
                    AddDebugMessage($"点击大秘境按钮");
                    Hud.Interaction.MouseMove(uiGRbutton.Rectangle.X + uiGRbutton.Rectangle.Width / 2, 
                                             uiGRbutton.Rectangle.Y + uiGRbutton.Rectangle.Height / 2);
                    Hud.Interaction.MouseDown(MouseButtons.Left);
                    Hud.Interaction.MouseUp(MouseButtons.Left);
                    DelayTimer.Restart();
                    return;
                }
                
                // 如果找不到任何按钮，尝试点击界面上的固定位置
                AddDebugMessage("找不到任何按钮，尝试点击界面上的固定位置");
                
                // 尝试点击大秘境按钮的大致位置（左侧按钮）
                float grX = uiGRmainPage.Rectangle.X + uiGRmainPage.Rectangle.Width * 0.25f;
                float grY = uiGRmainPage.Rectangle.Y + uiGRmainPage.Rectangle.Height * 0.5f;
                Hud.Interaction.MouseMove(grX, grY);
                Hud.Interaction.MouseDown(MouseButtons.Left);
                Hud.Interaction.MouseUp(MouseButtons.Left);
                DelayTimer.Restart();
                
                // 尝试点击接受按钮的大致位置（底部中间按钮）
                float acceptX = uiGRmainPage.Rectangle.X + uiGRmainPage.Rectangle.Width * 0.5f;
                float acceptY = uiGRmainPage.Rectangle.Y + uiGRmainPage.Rectangle.Height * 0.8f;
                Hud.Interaction.MouseMove(acceptX, acceptY);
                Hud.Interaction.MouseDown(MouseButtons.Left);
                Hud.Interaction.MouseUp(MouseButtons.Left);
                DelayTimer.Restart();
                
                return;
            }

            // 如果不在大秘境选择界面，寻找并点击方尖碑
            var obelisk = Hud.Game.Actors.FirstOrDefault(a => a.SnoActor.Sno == ActorSnoEnum._x1_openworld_lootrunobelisk_b);
            if (obelisk != null)
            {
                var obeliskPos = obelisk.FloorCoordinate;
                var playerPos = Hud.Game.Me.FloorCoordinate;
                var distance = obeliskPos.XYDistanceTo(playerPos);
                
                AddDebugMessage($"找到方尖碑，距离: {distance}");
                
                if (distance > 10.0f)
                {
                    // 直接移动到方尖碑
                    AddDebugMessage("直接移动到方尖碑");
                    try
                    {
                        // 使用世界坐标移动，使用左键点击
                        Hud.Interaction.MouseMove(obeliskPos.X, obeliskPos.Y, obeliskPos.Z);
                        System.Threading.Thread.Sleep(50);
                        Hud.Interaction.MouseDown(MouseButtons.Left);
                        System.Threading.Thread.Sleep(50);
                        Hud.Interaction.MouseUp(MouseButtons.Left);
                        
                        // 如果距离较远，尝试使用寻路插件
                        if (distance > 30.0f && PathfindingPlugin != null)
                        {
                            AddDebugMessage("距离较远，使用寻路插件");
                            PathfindingPlugin.CurrentTarget = new Vector2(obeliskPos.X, obeliskPos.Y);
                            PathfindingPlugin.PathPoints = PathfindingPlugin.FindPathAStar(
                                new Vector2(playerPos.X, playerPos.Y),
                                PathfindingPlugin.CurrentTarget);
                            PathfindingPlugin.AutoNavigate = true;
                            CurrentAction = $"正在导航到方尖碑，距离: {distance}";
                        }
                    }
                    catch (Exception ex)
                    {
                        AddDebugMessage($"移动到方尖碑时出错: {ex.Message}");
                        
                        // 如果直接移动失败，尝试使用屏幕坐标
                        try
                        {
                            var screenCoord = obelisk.ScreenCoordinate;
                            AddDebugMessage($"尝试使用屏幕坐标: ({screenCoord.X}, {screenCoord.Y})");
                            
                            Hud.Interaction.MouseMove(screenCoord.X, screenCoord.Y);
                            System.Threading.Thread.Sleep(50);
                            Hud.Interaction.MouseDown(MouseButtons.Left);
                            System.Threading.Thread.Sleep(50);
                            Hud.Interaction.MouseUp(MouseButtons.Left);
                        }
                        catch (Exception ex2)
                        {
                            AddDebugMessage($"使用屏幕坐标移动时出错: {ex2.Message}");
                        }
                    }
                }
                else
                {
                    AddDebugMessage("点击方尖碑");
                    try
                    {
                        // 优先使用屏幕坐标点击
                        var screenCoord = obelisk.ScreenCoordinate;
                        AddDebugMessage($"使用屏幕坐标点击方尖碑: ({screenCoord.X}, {screenCoord.Y})");
                        
                        // 移动鼠标到方尖碑位置
                        Hud.Interaction.MouseMove(screenCoord.X, screenCoord.Y);
                        System.Threading.Thread.Sleep(200); // 增加延迟
                        
                        // 点击方尖碑
                        Hud.Interaction.MouseDown(MouseButtons.Left);
                        System.Threading.Thread.Sleep(200); // 增加延迟
                        Hud.Interaction.MouseUp(MouseButtons.Left);
                        
                        // 再次点击，确保触发
                        System.Threading.Thread.Sleep(500); // 等待500毫秒
                        Hud.Interaction.MouseDown(MouseButtons.Left);
                        System.Threading.Thread.Sleep(200);
                        Hud.Interaction.MouseUp(MouseButtons.Left);
                        
                        DelayTimer.Restart();
                        
                        // 关闭寻路
                        if (PathfindingPlugin != null)
                        {
                            PathfindingPlugin.AutoNavigate = false;
                        }
                    }
                    catch (Exception ex)
                    {
                        AddDebugMessage($"点击方尖碑时出错: {ex.Message}");
                        
                        // 如果使用屏幕坐标失败，尝试使用世界坐标
                        try
                        {
                            AddDebugMessage($"尝试使用世界坐标点击方尖碑");
                            
                            // 使用世界坐标点击
                            Hud.Interaction.MouseMove(obeliskPos.X, obeliskPos.Y, obeliskPos.Z);
                            System.Threading.Thread.Sleep(200); // 增加延迟
                            Hud.Interaction.MouseDown(MouseButtons.Left);
                            System.Threading.Thread.Sleep(200); // 增加延迟
                            Hud.Interaction.MouseUp(MouseButtons.Left);
                            
                            // 再次点击，确保触发
                            System.Threading.Thread.Sleep(500); // 等待500毫秒
                            Hud.Interaction.MouseDown(MouseButtons.Left);
                            System.Threading.Thread.Sleep(200);
                            Hud.Interaction.MouseUp(MouseButtons.Left);
                        }
                        catch (Exception ex2)
                        {
                            AddDebugMessage($"使用世界坐标点击方尖碑时出错: {ex2.Message}");
                        }
                    }
                }
            }
            else
            {
                CurrentAction = "未找到方尖碑";
                AddDebugMessage("未找到方尖碑");
                return;
            }
            
            // 检查是否进入了大秘境
            if (Hud.Game.SpecialArea == SpecialArea.GreaterRift)
            {
                AddDebugMessage("已进入大秘境，设置 CurrentStep 为 ClearRift");
                CurrentStep = RiftStep.ClearRift;
                CurrentAction = "清理大秘境";
                
                // 确保寻路插件被激活
                if (PathfindingPlugin != null)
                {
                    PathfindingPlugin.AutoNavigate = true;
                    AddDebugMessage("已激活寻路插件，设置 AutoNavigate 为 true");
                }
            }
        }

        private void ClearGreaterRift()
        {
            CurrentAction = "清理大秘境中...";
            AddDebugMessage($"ClearGreaterRift 被调用，IsInRift: {Hud.Game.SpecialArea == SpecialArea.GreaterRift}, SpecialArea: {Hud.Game.SpecialArea}");
            
            if (Hud.Game.SpecialArea != SpecialArea.GreaterRift)
            {
                AddDebugMessage("不在大秘境中，设置 CurrentStep 为 OpenRift");
                CurrentStep = RiftStep.OpenRift;
                return;
            }
            
            // 确保寻路插件被激活
            if (PathfindingPlugin != null && !PathfindingPlugin.AutoNavigate)
            {
                PathfindingPlugin.AutoNavigate = true;
                AddDebugMessage("在 ClearGreaterRift 中激活寻路插件，设置 AutoNavigate 为 true");
            }
            
            // 检查是否有怪物
            if (!Hud.Game.AliveMonsters.Any())
            {
                AddDebugMessage("没有存活的怪物，设置 CurrentStep 为 CollectDrops");
                CurrentStep = RiftStep.CollectDrops;
                return;
            }
            
            // 寻找精英怪物
            var eliteMonster = Hud.Game.AliveMonsters
                .Where(m => m.Rarity == ActorRarity.Champion || m.Rarity == ActorRarity.Rare || m.Rarity == ActorRarity.Unique)
                .OrderBy(m => m.FloorCoordinate.XYDistanceTo(Hud.Game.Me.FloorCoordinate))
                .FirstOrDefault();
            
            // 寻找普通怪物
            var normalMonster = Hud.Game.AliveMonsters
                .OrderBy(m => m.FloorCoordinate.XYDistanceTo(Hud.Game.Me.FloorCoordinate))
                .FirstOrDefault();
            
            // 寻找下一层入口
            var nextLevel = Hud.Game.Actors
                .Where(a => a.SnoActor.Kind == ActorKind.Portal && a.SnoActor.Code.Contains("RiftPortal"))
                .OrderBy(a => a.FloorCoordinate.XYDistanceTo(Hud.Game.Me.FloorCoordinate))
                .FirstOrDefault();
            
            // 优先攻击精英怪物，其次是普通怪物，最后是前往下一层
            if (eliteMonster != null)
            {
                AddDebugMessage($"找到精英怪物，距离: {eliteMonster.FloorCoordinate.XYDistanceTo(Hud.Game.Me.FloorCoordinate)}");
                var monsterPos = eliteMonster.FloorCoordinate;
                var playerPos = Hud.Game.Me.FloorCoordinate;
                
                if (PathfindingPlugin != null)
                {
                    PathfindingPlugin.CurrentTarget = new Vector2(monsterPos.X, monsterPos.Y);
                    PathfindingPlugin.PathPoints = PathfindingPlugin.FindPathAStar(
                        new Vector2(playerPos.X, playerPos.Y),
                        PathfindingPlugin.CurrentTarget);
                    PathfindingPlugin.AutoNavigate = true;
                    CurrentAction = $"正在攻击精英怪物，距离: {monsterPos.XYDistanceTo(playerPos)}";
                }
            }
            else if (normalMonster != null)
            {
                AddDebugMessage($"找到普通怪物，距离: {normalMonster.FloorCoordinate.XYDistanceTo(Hud.Game.Me.FloorCoordinate)}");
                var monsterPos = normalMonster.FloorCoordinate;
                var playerPos = Hud.Game.Me.FloorCoordinate;
                
                if (PathfindingPlugin != null)
                {
                    PathfindingPlugin.CurrentTarget = new Vector2(monsterPos.X, monsterPos.Y);
                    PathfindingPlugin.PathPoints = PathfindingPlugin.FindPathAStar(
                        new Vector2(playerPos.X, playerPos.Y),
                        PathfindingPlugin.CurrentTarget);
                    PathfindingPlugin.AutoNavigate = true;
                    CurrentAction = $"正在攻击普通怪物，距离: {monsterPos.XYDistanceTo(playerPos)}";
                }
            }
            else if (nextLevel != null)
            {
                AddDebugMessage($"找到下一层入口，距离: {nextLevel.FloorCoordinate.XYDistanceTo(Hud.Game.Me.FloorCoordinate)}");
                var nextLevelPos = nextLevel.FloorCoordinate;
                var playerPos = Hud.Game.Me.FloorCoordinate;
                
                if (PathfindingPlugin != null)
                {
                    PathfindingPlugin.CurrentTarget = new Vector2(nextLevelPos.X, nextLevelPos.Y);
                    PathfindingPlugin.PathPoints = PathfindingPlugin.FindPathAStar(
                        new Vector2(playerPos.X, playerPos.Y),
                        PathfindingPlugin.CurrentTarget);
                    PathfindingPlugin.AutoNavigate = true;
                    CurrentAction = $"正在前往下一层，距离: {nextLevelPos.XYDistanceTo(playerPos)}";
                }
            }
            else
            {
                // 如果找不到怪物和下一层入口，探索未知区域
                AddDebugMessage("找不到怪物和下一层入口，探索未知区域");
                var playerPos = Hud.Game.Me.FloorCoordinate;
                
                if (PathfindingPlugin != null)
                {
                    // 检查是否需要重新计算路径
                    if (PathfindingPlugin.PathPoints == null || PathfindingPlugin.PathPoints.Count == 0)
                    {
                        // 获取当前地图的边界
                        var mapBounds = GetMapBounds();
                        AddDebugMessage($"地图边界: X({mapBounds.MinX:F1}, {mapBounds.MaxX:F1}), Y({mapBounds.MinY:F1}, {mapBounds.MaxY:F1})");
                        
                        // 尝试找到未探索的区域
                        var unexploredTarget = FindUnexploredTarget(playerPos, mapBounds);
                        
                        if (unexploredTarget != default(Vector2))
                        {
                            AddDebugMessage($"找到未探索区域，目标: ({unexploredTarget.X:F1}, {unexploredTarget.Y:F1})");
                            PathfindingPlugin.CurrentTarget = unexploredTarget;
                            PathfindingPlugin.PathPoints = PathfindingPlugin.FindPathAStar(
                                new Vector2(playerPos.X, playerPos.Y),
                                PathfindingPlugin.CurrentTarget);
                            PathfindingPlugin.AutoNavigate = true;
                            CurrentAction = "正在探索未知区域";
                        }
                        else
                        {
                            // 如果找不到未探索区域，随机选择一个方向
                            AddDebugMessage("找不到未探索区域，随机选择一个方向");
                            Random random = new Random();
                            float angle = (float)(random.NextDouble() * Math.PI * 2);
                            float distance = 100.0f;
                            Vector2 randomTarget = new Vector2(
                                playerPos.X + (float)Math.Cos(angle) * distance,
                                playerPos.Y + (float)Math.Sin(angle) * distance
                            );
                            
                            PathfindingPlugin.CurrentTarget = randomTarget;
                            PathfindingPlugin.PathPoints = PathfindingPlugin.FindPathAStar(
                                new Vector2(playerPos.X, playerPos.Y),
                                PathfindingPlugin.CurrentTarget);
                            PathfindingPlugin.AutoNavigate = true;
                            CurrentAction = "正在随机探索";
                            AddDebugMessage($"随机选择方向: {angle}, 距离: {distance}");
                        }
                    }
                }
            }
        }
        
        // 获取当前地图的边界
        private (float MinX, float MaxX, float MinY, float MaxY) GetMapBounds()
        {
            float minX = float.MaxValue;
            float maxX = float.MinValue;
            float minY = float.MaxValue;
            float maxY = float.MinValue;
            
            // 使用所有可见的物体来确定地图边界
            foreach (var actor in Hud.Game.Actors)
            {
                var pos = actor.FloorCoordinate;
                minX = Math.Min(minX, pos.X);
                maxX = Math.Max(maxX, pos.X);
                minY = Math.Min(minY, pos.Y);
                maxY = Math.Max(maxY, pos.Y);
            }
            
            // 如果没有找到任何物体，使用玩家位置作为中心，设置一个默认范围
            if (minX == float.MaxValue)
            {
                var playerPos = Hud.Game.Me.FloorCoordinate;
                minX = playerPos.X - 200;
                maxX = playerPos.X + 200;
                minY = playerPos.Y - 200;
                maxY = playerPos.Y + 200;
            }
            
            return (minX, maxX, minY, maxY);
        }
        
        // 查找未探索的区域
        private Vector2 FindUnexploredTarget(IWorldCoordinate playerPos, (float MinX, float MaxX, float MinY, float MaxY) mapBounds)
        {
            // 将地图分成网格
            int gridSize = 50; // 网格大小
            int gridCountX = (int)((mapBounds.MaxX - mapBounds.MinX) / gridSize) + 1;
            int gridCountY = (int)((mapBounds.MaxY - mapBounds.MinY) / gridSize) + 1;
            
            // 创建已探索区域的网格
            bool[,] exploredGrid = new bool[gridCountX, gridCountY];
            
            // 标记已探索的区域
            foreach (var actor in Hud.Game.Actors)
            {
                var pos = actor.FloorCoordinate;
                int gridX = (int)((pos.X - mapBounds.MinX) / gridSize);
                int gridY = (int)((pos.Y - mapBounds.MinY) / gridSize);
                
                if (gridX >= 0 && gridX < gridCountX && gridY >= 0 && gridY < gridCountY)
                {
                    exploredGrid[gridX, gridY] = true;
                }
            }
            
            // 标记玩家当前位置为已探索
            int playerGridX = (int)((playerPos.X - mapBounds.MinX) / gridSize);
            int playerGridY = (int)((playerPos.Y - mapBounds.MinY) / gridSize);
            
            if (playerGridX >= 0 && playerGridX < gridCountX && playerGridY >= 0 && playerGridY < gridCountY)
            {
                exploredGrid[playerGridX, playerGridY] = true;
            }
            
            // 查找最近的未探索区域
            Vector2 bestTarget = default(Vector2);
            float bestDistance = float.MaxValue;
            
            for (int x = 0; x < gridCountX; x++)
            {
                for (int y = 0; y < gridCountY; y++)
                {
                    if (!exploredGrid[x, y])
                    {
                        // 计算网格中心点
                        float centerX = mapBounds.MinX + (x + 0.5f) * gridSize;
                        float centerY = mapBounds.MinY + (y + 0.5f) * gridSize;
                        
                        // 计算到玩家的距离
                        float distance = (float)Math.Sqrt(
                            Math.Pow(centerX - playerPos.X, 2) + 
                            Math.Pow(centerY - playerPos.Y, 2));
                        
                        // 如果这个区域比之前找到的更近，更新目标
                        if (distance < bestDistance)
                        {
                            bestDistance = distance;
                            bestTarget = new Vector2(centerX, centerY);
                        }
                    }
                }
            }
            
            return bestTarget;
        }

        private void CollectDrops()
        {
            CurrentAction = "拾取掉落物品...";
            var drop = Hud.Game.Items.FirstOrDefault(i => i.Quality == ItemQuality.Legendary);
            if (drop != null)
            {
                Hud.Interaction.MouseMove(drop.FloorCoordinate.X, drop.FloorCoordinate.Y, drop.FloorCoordinate.Z);
                Hud.Interaction.MouseDown(MouseButtons.Left);
                Hud.Interaction.MouseUp(MouseButtons.Left);
                DelayTimer.Start();
            }
            else
            {
                CurrentStep = RiftStep.TalkToOrek;
            }
        }

        private void TalkToOrek()
        {
            CurrentAction = "与欧瑞克对话...";
            var orek = Hud.Game.Actors.FirstOrDefault(a => a.SnoActor.Sno == (ActorSnoEnum)403012);
            if (orek != null)
            {
                Hud.Interaction.MouseMove(orek.FloorCoordinate.X, orek.FloorCoordinate.Y, orek.FloorCoordinate.Z);
                Hud.Interaction.MouseDown(MouseButtons.Left);
                Hud.Interaction.MouseUp(MouseButtons.Left);
                DelayTimer.Start();
                CurrentStep = RiftStep.UpgradeGem;
            }
        }

        private void UpgradeGem()
        {
            CurrentAction = "升级宝石中...";
            DelayTimer.Start();
            CurrentStep = RiftStep.CloseRift;
        }

        private void CloseRift()
        {
            CurrentAction = "关闭大秘境...";
            var closeButton = Hud.Render.GetUiElement("Root.NormalLayer.riftResults_main.LayoutRoot.closeButton");
            if (closeButton?.Visible == true)
            {
                Hud.Interaction.MouseMove(closeButton.Rectangle.X + closeButton.Rectangle.Width / 2, 
                                        closeButton.Rectangle.Y + closeButton.Rectangle.Height / 2);
                Hud.Interaction.MouseDown(MouseButtons.Left);
                Hud.Interaction.MouseUp(MouseButtons.Left);
                Running = false;
                CurrentStep = RiftStep.OpenRift;
                PathfindingPlugin.AutoNavigate = false;
            }
        }
    }
}