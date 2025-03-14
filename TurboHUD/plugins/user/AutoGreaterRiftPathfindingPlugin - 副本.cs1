namespace Turbo.Plugins.User
{
    using Turbo.Plugins.Default;
    using System.Collections.Generic;
    using System.Linq;
    using System;
    using SharpDX;
    using System.Windows.Forms;
    using System.IO;

    public class AutoGreaterRiftPathfindingPlugin : BasePlugin, IInGameWorldPainter, IAfterCollectHandler
    {
        public bool Enabled { get; set; }
        public bool ShowPath { get; set; }
        public bool AutoNavigate { get; set; }
        public List<Vector2> PathPoints { get; set; }
        public Vector2 CurrentTarget { get; set; }
        public IMonster CurrentTarget_Monster { get; set; }
        public IActor CurrentTarget_ShrineOrPylon { get; set; }
        private float PathUpdateTimer { get; set; }
        private float PathUpdateInterval => 0.5f;
        private Vector2 NextWaypoint { get; set; }
        public Vector2 LastPosition { get; set; }
        private IWatch StuckTimer { get; set; }
        private float StuckThreshold => 2.0f;
        private int StuckTimeout => 3000;
        private HashSet<Vector2> ExploredPoints { get; set; }
        private HashSet<int> ExploredSceneIds { get; set; }
        public IWorldDecorator PathDecorator { get; private set; }
        public IWorldDecorator TargetDecorator { get; private set; }
        public IWorldDecorator NextPointDecorator { get; private set; }
        public float HealthThreshold { get; set; }
        public bool AutoUsePotion { get; set; }
        
        // 技能施放相关属性
        private IWatch SkillCooldownTimer { get; set; }
        private int SkillCooldown => 200; // 技能冷却时间（毫秒）
        private int CurrentSkillIndex { get; set; } // 当前技能索引
        
        // 路障检测相关属性
        private bool IsBreakingObstacle { get; set; }
        private IWatch ObstacleBreakTimer { get; set; }
        private int ObstacleBreakTimeout => 1000; // 打破路障超时时间（毫秒）

        private List<string> DebugMessages = new List<string>();
        public IFont DebugFont { get; private set; }
        private string LogFilePath => Path.Combine(Directory.GetCurrentDirectory(), "TurboHUD", "logs", "AutoGreaterRiftPathfindingPlugin_Log.txt");
        
        public AutoGreaterRiftPathfindingPlugin()
        {
            Enabled = true;
            ShowPath = true;
            AutoNavigate = false;
            PathPoints = new List<Vector2>();
            CurrentTarget = default(Vector2);
            CurrentTarget_Monster = null;
            CurrentTarget_ShrineOrPylon = null;
            PathUpdateTimer = 0;
            NextWaypoint = default(Vector2);
            LastPosition = default(Vector2);
            ExploredPoints = new HashSet<Vector2>();
            ExploredSceneIds = new HashSet<int>();
            HealthThreshold = 0.3f;
            AutoUsePotion = true;
        }
        
        public override void Load(IController hud)
        {
            base.Load(hud);
            
            // 初始化计时器
            StuckTimer = Hud.Time.CreateWatch();
            StuckTimer.Start();
            
            // 初始化技能冷却计时器
            SkillCooldownTimer = Hud.Time.CreateWatch();
            SkillCooldownTimer.Start();
            CurrentSkillIndex = 0;
            
            // 初始化路障打破计时器
            ObstacleBreakTimer = Hud.Time.CreateWatch();
            ObstacleBreakTimer.Start();
            IsBreakingObstacle = false;
            
            PathDecorator = new WorldDecoratorCollection(
                new MapShapeDecorator(Hud)
                {
                    ShapePainter = new CircleShapePainter(Hud),
                    Brush = Hud.Render.CreateBrush(255, 0, 255, 0, 2),
                    Radius = 2.0f
                }
            ).Decorators.First();
            TargetDecorator = new WorldDecoratorCollection(
                new MapShapeDecorator(Hud)
                {
                    ShapePainter = new CircleShapePainter(Hud),
                    Brush = Hud.Render.CreateBrush(255, 255, 0, 0, 2),
                    Radius = 5.0f
                }
            ).Decorators.First();
            NextPointDecorator = new WorldDecoratorCollection(
                new MapShapeDecorator(Hud)
                {
                    ShapePainter = new CircleShapePainter(Hud),
                    Brush = Hud.Render.CreateBrush(255, 0, 0, 255, 2),
                    Radius = 3.0f
                }
            ).Decorators.First();
            DebugFont = Hud.Render.CreateFont("tahoma", 8, 255, 0, 255, 0, true, false, 255, 0, 0, 0, true);
            if (File.Exists(LogFilePath)) File.Delete(LogFilePath);
        }

        private void AddDebugMessage(string message)
        {
            string timestampedMessage = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [{Hud.Game.CurrentGameTick}] {message}";
            DebugMessages.Add(timestampedMessage);
            if (DebugMessages.Count > 15) DebugMessages.RemoveAt(0);
            try
            {
                File.AppendAllText(LogFilePath, timestampedMessage + Environment.NewLine);
            }
            catch (Exception ex)
            {
                Hud.Sound.Speak("日志写入失败: " + ex.Message);
            }
        }

        public void PaintWorld(WorldLayer layer)
        {
            if (layer != WorldLayer.Ground || !Enabled) return;

            var uiElement = Hud.Render.GetUiElement("Root.NormalLayer.minimap_dialog_backgroundScreen.minimap_dialog_pve.BoostWrapper");
            var uiRect = uiElement != null ? ToSharpDXRectangleF(uiElement.Rectangle) : new SharpDX.RectangleF(0, 0, Hud.Window.Size.Width, Hud.Window.Size.Height);
            
            DebugFont.DrawText($"自动导航: {(AutoNavigate ? "开启" : "关闭")}", uiRect.Left + 20, uiRect.Top + 250);
            DebugFont.DrawText($"显示路径: {(ShowPath ? "开启" : "关闭")}", uiRect.Left + 20, uiRect.Top + 270);
            DebugFont.DrawText($"自动喝药: {(AutoUsePotion ? "开启" : "关闭")} (阈值: {HealthThreshold*100}%)", uiRect.Left + 20, uiRect.Top + 290);
            
            if (CurrentTarget != default(Vector2))
                DebugFont.DrawText($"当前目标: {CurrentTarget.X:F1}, {CurrentTarget.Y:F1}", uiRect.Left + 20, uiRect.Top + 310);
            else if (CurrentTarget_Monster != null)
                DebugFont.DrawText($"当前目标: 怪物 ({CurrentTarget_Monster.FloorCoordinate.X:F1}, {CurrentTarget_Monster.FloorCoordinate.Y:F1})", uiRect.Left + 20, uiRect.Top + 310);
            else if (CurrentTarget_ShrineOrPylon != null)
                DebugFont.DrawText($"当前目标: 神龛/塔 ({CurrentTarget_ShrineOrPylon.FloorCoordinate.X:F1}, {CurrentTarget_ShrineOrPylon.FloorCoordinate.Y:F1})", uiRect.Left + 20, uiRect.Top + 310);
            
            // 检查 PathPoints 是否为 null，避免空引用异常
            if (PathPoints != null)
                DebugFont.DrawText($"路径点数量: {PathPoints.Count}", uiRect.Left + 20, uiRect.Top + 330);
            else
                DebugFont.DrawText("路径点数量: 0", uiRect.Left + 20, uiRect.Top + 330);
            
            for (int i = 0; i < DebugMessages.Count; i++)
            {
                DebugFont.DrawText(DebugMessages[i], uiRect.Left + 20, uiRect.Top + 350 + i * 20);
            }
            
            // 绘制路径
            if (ShowPath && PathPoints != null && PathPoints.Count > 0)
            {
                for (int i = 0; i < PathPoints.Count; i++)
                {
                    var coord = Hud.Window.CreateWorldCoordinate(PathPoints[i].X, PathPoints[i].Y, Hud.Game.Me.FloorCoordinate.Z);
                    ((IWorldDecorator)PathDecorator).Paint(null, coord, null);
                }
            }
            
            // 绘制目标点
            if (CurrentTarget != default(Vector2))
            {
                var coord = Hud.Window.CreateWorldCoordinate(CurrentTarget.X, CurrentTarget.Y, Hud.Game.Me.FloorCoordinate.Z);
                ((IWorldDecorator)TargetDecorator).Paint(null, coord, null);
            }
            
            // 绘制下一个路径点
            if (NextWaypoint != default(Vector2))
            {
                var coord = Hud.Window.CreateWorldCoordinate(NextWaypoint.X, NextWaypoint.Y, Hud.Game.Me.FloorCoordinate.Z);
                ((IWorldDecorator)NextPointDecorator).Paint(null, coord, null);
            }
        }

        private SharpDX.RectangleF ToSharpDXRectangleF(System.Drawing.RectangleF rect)
        {
            return new SharpDX.RectangleF(rect.X, rect.Y, rect.Width, rect.Height);
        }

        public void AfterCollect()
        {
            Update(Hud);
        }

        public void Update(IController hud)
        {
            if (!Enabled || !Hud.Game.IsInGame)
            {
                AddDebugMessage("Update 未执行：条件不满足");
                return;
            }
            
            var playerPos = new Vector2(Hud.Game.Me.FloorCoordinate.X, Hud.Game.Me.FloorCoordinate.Y);
            AddDebugMessage($"玩家位置: ({playerPos.X:F1}, {playerPos.Y:F1}), 生命值: {Hud.Game.Me.Defense.HealthPct:P0}");
            
            if (AutoUsePotion && Hud.Game.Me.Defense.HealthPct < HealthThreshold)
            {
                AddDebugMessage($"生命值低于阈值 {HealthThreshold*100}%，尝试使用药水");
                if (!Hud.Game.Me.Powers.HealthPotionSkill.IsOnCooldown)
                {
                    var cursorX = Hud.Window.CursorX;
                    var cursorY = Hud.Window.CursorY;
                    
                    Hud.Interaction.DoAction(ActionKey.Heal);
                    
                    Hud.Interaction.MouseMove(cursorX, cursorY);
                    
                    AddDebugMessage("使用药水");
                }
                else
                {
                    AddDebugMessage("药水冷却中");
                }
            }
            
            PathUpdateTimer += 0.1f;
            if (PathUpdateTimer >= 2.0f)
            {
                PathUpdateTimer = 0;
                UpdatePath();
            }
            if (AutoNavigate)
            {
                AddDebugMessage("AutoNavigate 为 true，调用 MoveToTarget");
                MoveToTarget();
            }
            else
            {
                AddDebugMessage("AutoNavigate 为 false");
            }
        }

        public void MoveToTarget()
        {
            if (!Enabled || !AutoNavigate || !Hud.Game.IsInGame) 
            {
                AddDebugMessage("MoveToTarget 未执行：条件不满足");
                return;
            }

            AddDebugMessage("MoveToTarget 已调用");
            var playerPos = new Vector2(Hud.Game.Me.FloorCoordinate.X, Hud.Game.Me.FloorCoordinate.Y);

            // 检查是否卡住
            if (LastPosition != null && Vector2.Distance(playerPos, LastPosition) < StuckThreshold)
            {
                if (StuckTimer.ElapsedMilliseconds > StuckTimeout)
                {
                    AddDebugMessage($"检测到卡住：当前位置 ({playerPos.X:F1}, {playerPos.Y:F1})，上次位置 ({LastPosition.X:F1}, {LastPosition.Y:F1})，距离 {Vector2.Distance(playerPos, LastPosition):F1}，时间 {StuckTimer.ElapsedMilliseconds}ms");
                    HandleStuck(playerPos);
                    return;
                }
                else
                {
                    AddDebugMessage($"可能卡住：当前位置 ({playerPos.X:F1}, {playerPos.Y:F1})，上次位置 ({LastPosition.X:F1}, {LastPosition.Y:F1})，距离 {Vector2.Distance(playerPos, LastPosition):F1}，时间 {StuckTimer.ElapsedMilliseconds}ms");
                }
            }
            else
            {
                // 如果移动了，重置卡住计时器
                StuckTimer.Restart();
            }
            
            // 更新上次位置
            LastPosition = playerPos;
            
            // 检查路径上是否有路障
            CheckAndBreakObstacles(playerPos);
            if (IsBreakingObstacle)
            {
                AddDebugMessage("正在打破路障，暂停其他操作");
                return;
            }
            
            // 检查附近是否有怪物，优先攻击怪物
            var nearbyMonsters = FindNearbyMonsters(playerPos);
            if (nearbyMonsters.Any())
            {
                // 更新当前目标为最近的怪物
                CurrentTarget_Monster = nearbyMonsters.First();
                var monsterPos = new Vector2(CurrentTarget_Monster.FloorCoordinate.X, CurrentTarget_Monster.FloorCoordinate.Y);
                var distanceToMonster = Vector2.Distance(playerPos, monsterPos);
                
                // 只有当怪物距离小于15.0f时才攻击，避免频繁改变方向
                if (distanceToMonster < 15.0f)
                {
                    AddDebugMessage($"发现附近怪物，优先攻击：{CurrentTarget_Monster.SnoMonster.NameLocalized}，距离: {distanceToMonster:F1}");
                    
                    // 如果在攻击范围内，施放技能攻击怪物
                    if (distanceToMonster < 10.0f) // 减小攻击范围，避免频繁改变方向
                    {
                        AddDebugMessage($"在攻击范围内，攻击怪物，距离: {distanceToMonster:F1}");
                        try
                        {
                            // 使用屏幕坐标点击
                            var screenCoord = CurrentTarget_Monster.ScreenCoordinate;
                            AddDebugMessage($"怪物屏幕坐标: ({screenCoord.X}, {screenCoord.Y})");
                            
                            if (screenCoord.X >= 0 && screenCoord.X <= Hud.Window.Size.Width &&
                                screenCoord.Y >= 0 && screenCoord.Y <= Hud.Window.Size.Height)
                            {
                                // 移动鼠标到怪物位置
                                Hud.Interaction.MouseMove(screenCoord.X, screenCoord.Y);
                                System.Threading.Thread.Sleep(50);
                                
                                // 施放技能
                                CastSkills();
                            }
                            else
                            {
                                AddDebugMessage("怪物屏幕坐标超出窗口范围");
                                
                                // 尝试使用世界坐标
                                Hud.Interaction.MouseMove(CurrentTarget_Monster.FloorCoordinate.X, CurrentTarget_Monster.FloorCoordinate.Y, CurrentTarget_Monster.FloorCoordinate.Z);
                                System.Threading.Thread.Sleep(50);
                                
                                // 施放技能
                                CastSkills();
                            }
                        }
                        catch (Exception ex)
                        {
                            AddDebugMessage($"攻击怪物出错: {ex.Message}");
                        }
                        return;
                    }
                    else
                    {
                        // 如果不在攻击范围内，移动到怪物位置
                        AddDebugMessage($"移动到怪物位置: ({monsterPos.X:F1}, {monsterPos.Y:F1})");
                        MoveToPosition(monsterPos);
                        return;
                    }
                }
            }
            
            // 如果没有下一个路径点，更新路径
            if (NextWaypoint == default(Vector2))
            {
                AddDebugMessage("没有下一个路径点，更新路径");
                UpdatePath();
                
                // 如果更新后仍然没有路径点，返回
                if (NextWaypoint == default(Vector2))
                {
                    AddDebugMessage("更新路径后仍然没有下一个路径点，无法移动");
                    return;
                }
            }
            
            // 移动到下一个路径点
            AddDebugMessage($"移动到下一个路径点: ({NextWaypoint.X:F1}, {NextWaypoint.Y:F1})，距离: {Vector2.Distance(playerPos, NextWaypoint):F1}");
            MoveToPosition(NextWaypoint);
            
            // 如果已经很接近下一个路径点，更新路径点
            if (Vector2.Distance(playerPos, NextWaypoint) < 5.0f)
            {
                UpdateNextWaypoint(playerPos);
            }
        }
        
        // 使用空格键移动到指定位置
        private void MoveToPosition(Vector2 position)
        {
            try
            {
                // 获取玩家当前位置
                var playerPos = new Vector2(Hud.Game.Me.FloorCoordinate.X, Hud.Game.Me.FloorCoordinate.Y);
                
                // 计算到目标的方向向量
                var direction = new Vector2(position.X - playerPos.X, position.Y - playerPos.Y);
                var distance = (float)Math.Sqrt(direction.X * direction.X + direction.Y * direction.Y);
                
                // 如果已经非常接近目标，直接停止移动
                if (distance < 5.0f)
                {
                    AddDebugMessage($"已接近目标，距离: {distance:F1}");
                    return;
                }
                
                // 归一化方向向量
                if (distance > 0)
                {
                    direction.X /= distance;
                    direction.Y /= distance;
                }
                else
                {
                    // 如果距离为0，使用默认方向
                    direction.X = 1;
                    direction.Y = 0;
                }
                
                // 动态调整点击距离，根据玩家到目标的距离
                float clickDistance;
                if (distance < 30.0f)
                {
                    // 当接近目标时，使用更短的点击距离，提高精度
                    clickDistance = Math.Min(distance * 0.8f, 15.0f);
                }
                else
                {
                    // 远离目标时使用固定距离，保持稳定
                    clickDistance = 20.0f;
                }
                
                // 计算点击位置，确保在玩家前方
                var clickPos = new Vector2(
                    playerPos.X + direction.X * clickDistance,
                    playerPos.Y + direction.Y * clickDistance
                );
                
                // 将世界坐标转换为屏幕坐标
                var worldCoord = Hud.Window.CreateWorldCoordinate(clickPos.X, clickPos.Y, Hud.Game.Me.FloorCoordinate.Z);
                var screenCoord = worldCoord.ToScreenCoordinate();
                
                // 检查点击位置是否在屏幕内及其有效性
                if (screenCoord.X >= 0 && screenCoord.X <= Hud.Window.Size.Width &&
                    screenCoord.Y >= 0 && screenCoord.Y <= Hud.Window.Size.Height)
                {
                    // 尝试检查该点是否真的可以点击（例如，是否被UI元素遮挡）
                    bool canClick = true;
                    
                    // 如果可以点击，执行点击操作
                    if (canClick)
                    {
                        // 移动鼠标到点击位置
                        Hud.Interaction.MouseMove(screenCoord.X, screenCoord.Y);
                        System.Threading.Thread.Sleep(30); // 稍微减少延迟，让移动更流畅
                        
                        // 使用鼠标点击移动
                        Hud.Interaction.MouseDown(System.Windows.Forms.MouseButtons.Left);
                        System.Threading.Thread.Sleep(30);
                        Hud.Interaction.MouseUp(System.Windows.Forms.MouseButtons.Left);
                        
                        AddDebugMessage($"点击移动: ({clickPos.X:F1}, {clickPos.Y:F1})，方向: ({direction.X:F2}, {direction.Y:F2})，距离: {clickDistance:F1}");
                        return; // 成功点击后返回
                    }
                }
                
                // 如果点击位置不在屏幕内或无法点击，尝试使用屏幕中心点击
                AddDebugMessage("尝试使用屏幕中心方向点击");
                
                // 计算屏幕中心位置
                var screenCenterX = Hud.Window.Size.Width / 2;
                var screenCenterY = Hud.Window.Size.Height / 2;
                
                // 计算方向矢量在屏幕上的投影
                float screenDirectionX = 0, screenDirectionY = 0;
                
                // 首先计算屏幕中心点的世界坐标
                var centerScreenCoord = new System.Drawing.PointF(screenCenterX, screenCenterY);
                var screenWidth = Hud.Window.Size.Width;
                var screenHeight = Hud.Window.Size.Height;
                
                // 然后将世界方向向量转换为屏幕方向向量
                // 这是一个简化的方法，可能需要根据具体游戏调整
                screenDirectionX = direction.X * screenWidth / 100;
                screenDirectionY = -direction.Y * screenHeight / 100; // Y坐标可能需要反转
                
                // 归一化屏幕方向向量
                float screenDirLength = (float)Math.Sqrt(screenDirectionX * screenDirectionX + screenDirectionY * screenDirectionY);
                if (screenDirLength > 0.001f)
                {
                    screenDirectionX /= screenDirLength;
                    screenDirectionY /= screenDirLength;
                    
                    // 计算屏幕上的点击位置
                    float screenClickDistance = Math.Min(100.0f, screenWidth / 4); // 屏幕上的点击距离，不要太远
                    float clickX = screenCenterX + screenDirectionX * screenClickDistance;
                    float clickY = screenCenterY + screenDirectionY * screenClickDistance;
                    
                    // 确保点击位置在屏幕内
                    clickX = Math.Max(10, Math.Min(clickX, screenWidth - 10));
                    clickY = Math.Max(10, Math.Min(clickY, screenHeight - 10));
                    
                    // 尝试点击
                    Hud.Interaction.MouseMove(clickX, clickY);
                    System.Threading.Thread.Sleep(30);
                    Hud.Interaction.MouseDown(System.Windows.Forms.MouseButtons.Left);
                    System.Threading.Thread.Sleep(30);
                    Hud.Interaction.MouseUp(System.Windows.Forms.MouseButtons.Left);
                    
                    AddDebugMessage($"使用屏幕方向点击: ({clickX:F1}, {clickY:F1})，屏幕方向: ({screenDirectionX:F2}, {screenDirectionY:F2})");
                }
                else
                {
                    // 如果无法计算屏幕方向，直接点击屏幕中心稍前方
                    float clickY = screenCenterY - screenHeight / 8; // 点击屏幕中心偏上位置
                    Hud.Interaction.MouseMove(screenCenterX, clickY);
                    System.Threading.Thread.Sleep(30);
                    Hud.Interaction.MouseDown(System.Windows.Forms.MouseButtons.Left);
                    System.Threading.Thread.Sleep(30);
                    Hud.Interaction.MouseUp(System.Windows.Forms.MouseButtons.Left);
                    
                    AddDebugMessage("无法计算屏幕方向，点击屏幕中心偏上位置");
                }
            }
            catch (Exception ex)
            {
                AddDebugMessage($"移动到位置出错: {ex.Message}");
                PathPoints = null;
            }
        }
        
        // 查找附近的怪物
        private List<IMonster> FindNearbyMonsters(Vector2 playerPos)
        {
            var monsters = new List<IMonster>();
            
            // 优先查找精英怪物
            var eliteMonsters = Hud.Game.AliveMonsters
                .Where(m => (m.Rarity == ActorRarity.Champion || m.Rarity == ActorRarity.Rare || m.Rarity == ActorRarity.Unique) &&
                       Vector2.Distance(playerPos, new Vector2(m.FloorCoordinate.X, m.FloorCoordinate.Y)) < 60.0f)
                .OrderBy(m => Vector2.Distance(playerPos, new Vector2(m.FloorCoordinate.X, m.FloorCoordinate.Y)))
                .ToList();
            
            if (eliteMonsters.Any())
            {
                monsters.AddRange(eliteMonsters);
            }
            
            // 如果没有精英怪物，查找普通怪物
            if (!monsters.Any())
            {
                var normalMonsters = Hud.Game.AliveMonsters
                    .Where(m => Vector2.Distance(playerPos, new Vector2(m.FloorCoordinate.X, m.FloorCoordinate.Y)) < 40.0f)
                    .OrderBy(m => Vector2.Distance(playerPos, new Vector2(m.FloorCoordinate.X, m.FloorCoordinate.Y)))
                    .ToList();
                
                if (normalMonsters.Any())
                {
                    monsters.AddRange(normalMonsters);
                }
            }
            
            return monsters;
        }
        
        // 检查并打破路径上的路障
        private void CheckAndBreakObstacles(Vector2 playerPos)
        {
            // 如果正在打破路障，检查是否超时
            if (IsBreakingObstacle)
            {
                if (ObstacleBreakTimer.ElapsedMilliseconds > ObstacleBreakTimeout)
                {
                    AddDebugMessage("打破路障超时，继续移动");
                    IsBreakingObstacle = false;
                    ObstacleBreakTimer.Restart();
                    return;
                }
                
                // 继续打破路障
                BreakObstacle();
                return;
            }
            
            // 查找附近的可破坏物体
            var obstacles = Hud.Game.Actors
                .Where(a => (a.SnoActor.Kind == ActorKind.Chest || a.SnoActor.Kind == ActorKind.Obstacle || a.SnoActor.Kind == ActorKind.WeaponRack || a.SnoActor.Kind == ActorKind.ArmorRack) &&
                       !a.IsDisabled &&
                       Vector2.Distance(playerPos, new Vector2(a.FloorCoordinate.X, a.FloorCoordinate.Y)) < 20.0f)
                .OrderBy(a => Vector2.Distance(playerPos, new Vector2(a.FloorCoordinate.X, a.FloorCoordinate.Y)))
                .ToList();
            
            if (obstacles.Any())
            {
                var obstacle = obstacles.First();
                var obstaclePos = new Vector2(obstacle.FloorCoordinate.X, obstacle.FloorCoordinate.Y);
                var distance = Vector2.Distance(playerPos, obstaclePos);
                
                // 如果路障在路径上，开始打破它
                if (IsObstacleInPath(obstacle))
                {
                    AddDebugMessage($"发现路径上的路障: {obstacle.SnoActor.NameLocalized}，距离: {distance:F1}");
                    IsBreakingObstacle = true;
                    ObstacleBreakTimer.Restart();
                    
                    // 移动到路障位置
                    try
                    {
                        var screenCoord = obstacle.ScreenCoordinate;
                        
                        if (screenCoord.X >= 0 && screenCoord.X <= Hud.Window.Size.Width &&
                            screenCoord.Y >= 0 && screenCoord.Y <= Hud.Window.Size.Height)
                        {
                            Hud.Interaction.MouseMove(screenCoord.X, screenCoord.Y);
                            System.Threading.Thread.Sleep(50);
                            
                            // 打破路障
                            BreakObstacle();
                        }
                    }
                    catch (Exception ex)
                    {
                        AddDebugMessage($"移动到路障位置出错: {ex.Message}");
                        IsBreakingObstacle = false;
                    }
                }
            }
        }
        
        // 判断路障是否在路径上
        private bool IsObstacleInPath(IActor obstacle)
        {
            if (PathPoints == null || PathPoints.Count == 0 || NextWaypoint == default(Vector2))
                return false;
            
            var obstaclePos = new Vector2(obstacle.FloorCoordinate.X, obstacle.FloorCoordinate.Y);
            var playerPos = new Vector2(Hud.Game.Me.FloorCoordinate.X, Hud.Game.Me.FloorCoordinate.Y);
            
            // 检查路障是否在玩家和下一个路径点之间
            var direction = Vector2.Normalize(NextWaypoint - playerPos);
            var obstacleDirection = obstaclePos - playerPos;
            var distance = Vector2.Distance(playerPos, obstaclePos);
            
            // 计算路障到路径的距离
            var projection = Vector2.Dot(obstacleDirection, direction);
            var perpendicular = Vector2.Distance(obstacleDirection, direction * projection);
            
            // 如果路障在路径前方且距离路径较近，认为它在路径上
            return projection > 0 && projection < Vector2.Distance(playerPos, NextWaypoint) && perpendicular < 10.0f;
        }
        
        // 打破路障
        private void BreakObstacle()
        {
            AddDebugMessage("正在打破路障");
            
            // 使用左键点击打破路障
            Hud.Interaction.MouseDown(MouseButtons.Left);
            System.Threading.Thread.Sleep(50);
            Hud.Interaction.MouseUp(MouseButtons.Left);
            System.Threading.Thread.Sleep(100);
        }

        public void UpdatePath()
        {
            try
            {
                AddDebugMessage("更新路径");
                
                // 记录当前位置为已探索点
                Vector2 playerPos = new Vector2(Hud.Game.Me.FloorCoordinate.X, Hud.Game.Me.FloorCoordinate.Y);
                RecordExploredArea(playerPos);
                
                // 检查是否有奖励神龛或塔
                if (FindAndTargetShrine())
                {
                    AddDebugMessage("找到神龛或塔，更新目标");
                    return;
                }
                
                // 检查是否有怪物
                if (FindAndTargetMonster())
                {
                    AddDebugMessage("找到怪物，更新目标");
                    return;
                }
                
                // 检查是否有大秘境出口
                var riftExit = FindRiftExit();
                if (riftExit != null)
                {
                    AddDebugMessage($"找到大秘境出口: {riftExit.SnoActor.NameLocalized}");
                    CurrentTarget = new Vector2(riftExit.FloorCoordinate.X, riftExit.FloorCoordinate.Y);
                    CurrentTarget_Monster = null;
                    CurrentTarget_ShrineOrPylon = null;
                    
                    // 计算到出口的路径
                    PathPoints = FindPathAStar(playerPos, CurrentTarget);
                    if (PathPoints.Count > 0)
                    {
                        UpdateNextWaypoint(playerPos);
                        AddDebugMessage($"到大秘境出口的路径已更新，路径点数量: {PathPoints.Count}");
                        return;
                    }
                }
                
                // 如果当前有目标并且不是很远，继续前往该目标
                if (CurrentTarget != default(Vector2) && Vector2.Distance(playerPos, CurrentTarget) < 150.0f)
                {
                    AddDebugMessage($"继续前往当前目标: ({CurrentTarget.X:F1}, {CurrentTarget.Y:F1}), 距离: {Vector2.Distance(playerPos, CurrentTarget):F1}");
                    PathPoints = FindPathAStar(playerPos, CurrentTarget);
                    if (PathPoints.Count > 0)
                    {
                        UpdateNextWaypoint(playerPos);
                        return;
                    }
                }
                
                // 如果没有目标或当前目标无法到达，寻找新的探索目标
                AddDebugMessage("没有目标或已到达目标，寻找新的探索目标");
                FindNewExplorationTarget();
                
                // 如果找到了新目标，计算路径
                if (CurrentTarget != default(Vector2))
                {
                    PathPoints = FindPathAStar(playerPos, CurrentTarget);
                    if (PathPoints.Count > 0)
                    {
                        UpdateNextWaypoint(playerPos);
                        AddDebugMessage($"找到新探索目标: ({CurrentTarget.X:F1}, {CurrentTarget.Y:F1})，路径点数量: {PathPoints.Count}");
                        return;
                    }
                }
                
                // 如果找不到可行路径，随机选择一个方向
                AddDebugMessage("未找到新探索目标，随机选择一个方向");
                FindRandomDirection(playerPos);
            }
            catch (Exception ex)
            {
                AddDebugMessage($"更新路径出错: {ex.Message}");
            }
        }

        private bool FindAndTargetObelisk()
        {
            AddDebugMessage("开始寻找方尖碑");
            
            // 记录所有可见的 Actor
            var allActors = Hud.Game.Actors.ToList();
            AddDebugMessage($"场景中共有 {allActors.Count} 个 Actor");
            
            // 方尖碑的 SNO 值
            var obeliskSnoIds = new List<uint> 
            { 
                (uint)ActorSnoEnum._x1_openworld_lootrunobelisk_b, // 大秘境方尖碑
                430075, // 可能的其他方尖碑 ID
                430076  // 可能的其他方尖碑 ID
            };
            
            // 需要排除的 Actor 名称
            string[] excludeNames = new string[] 
            { 
                "米瑞姆", "Miriam", "Myriam", 
                "欧瑞克", "Orek", 
                "卡德拉", "Kadala", 
                "库尔", "Kulle"
            };
            
            // 尝试找到方尖碑
            var obelisks = Hud.Game.Actors.Where(a => 
                obeliskSnoIds.Contains((uint)a.SnoActor.Sno) &&
                !a.IsDisabled && 
                !a.IsOperated
            ).ToList();
            
            AddDebugMessage($"通过 SNO ID 找到 {obelisks.Count} 个方尖碑");
            
            // 如果没有找到方尖碑，尝试扩大搜索范围
            if (obelisks.Count == 0)
            {
                AddDebugMessage("尝试通过名称寻找方尖碑");
                obelisks = Hud.Game.Actors.Where(a => 
                    a.SnoActor.NameLocalized != null && 
                    (a.SnoActor.NameLocalized.Contains("方尖碑") || 
                     a.SnoActor.NameLocalized.Contains("Obelisk") ||
                     a.SnoActor.NameLocalized.Contains("奈非天") ||
                     a.SnoActor.NameLocalized.Contains("Nephalem")) &&
                    !excludeNames.Any(name => a.SnoActor.NameLocalized.Contains(name)) &&
                    !a.IsDisabled && 
                    !a.IsOperated
                ).ToList();
                
                AddDebugMessage($"通过名称找到 {obelisks.Count} 个方尖碑");
                
                // 如果仍然没有找到，尝试查找所有可能的方尖碑
                if (obelisks.Count == 0)
                {
                    AddDebugMessage("尝试查找所有可能的方尖碑");
                    var possibleObelisks = Hud.Game.Actors.Where(a => 
                        !a.IsDisabled && 
                        !a.IsOperated &&
                        a.SnoActor.NameLocalized != null &&
                        !excludeNames.Any(name => a.SnoActor.NameLocalized.Contains(name))
                    ).ToList();
                    
                    if (possibleObelisks.Any())
                    {
                        AddDebugMessage("可能的方尖碑列表:");
                        foreach (var actor in possibleObelisks.Take(10))
                        {
                            AddDebugMessage($"Actor: {actor.SnoActor.Sno} - {actor.SnoActor.NameLocalized}, 距离: {actor.FloorCoordinate.XYDistanceTo(Hud.Game.Me.FloorCoordinate)}");
                        }
                    }
                    else
                    {
                        AddDebugMessage("未找到任何可能的方尖碑");
                    }
                }
            }
            
            // 按距离排序
            var nearestObelisk = obelisks.OrderBy(a => a.FloorCoordinate.XYDistanceTo(Hud.Game.Me.FloorCoordinate)).FirstOrDefault();
            
            if (nearestObelisk != null)
            {
                CurrentTarget_ShrineOrPylon = nearestObelisk;
                CurrentTarget = new Vector2(nearestObelisk.FloorCoordinate.X, nearestObelisk.FloorCoordinate.Y);
                AddDebugMessage($"找到方尖碑，SNO ID: {nearestObelisk.SnoActor.Sno}, 名称: {nearestObelisk.SnoActor.NameLocalized}，位置: ({CurrentTarget.X:F1}, {CurrentTarget.Y:F1}), 距离: {nearestObelisk.FloorCoordinate.XYDistanceTo(Hud.Game.Me.FloorCoordinate):F1}");
                return true;
            }
            
            AddDebugMessage("未找到方尖碑");
            return false;
        }

        private bool FindAndTargetShrine()
        {
            AddDebugMessage("开始寻找神龛或塔");
            var shrine = Hud.Game.Actors.Where(a => a.SnoActor.Kind == ActorKind.Shrine && !a.IsOperated && !a.IsDisabled)
                .OrderBy(a => a.FloorCoordinate.XYDistanceTo(Hud.Game.Me.FloorCoordinate))
                .FirstOrDefault();
            if (shrine != null)
            {
                CurrentTarget_ShrineOrPylon = shrine;
                CurrentTarget = new Vector2(shrine.FloorCoordinate.X, shrine.FloorCoordinate.Y);
                AddDebugMessage($"找到神龛或塔，类型: {shrine.SnoActor.NameLocalized}，位置: ({CurrentTarget.X:F1}, {CurrentTarget.Y:F1}), 距离: {shrine.FloorCoordinate.XYDistanceTo(Hud.Game.Me.FloorCoordinate):F1}");
                return true;
            }
            CurrentTarget_ShrineOrPylon = null;
            AddDebugMessage("未找到神龛或塔");
            return false;
        }
        
        private bool FindAndTargetMonster()
        {
            AddDebugMessage("开始寻找怪物");
            var monster = Hud.Game.AliveMonsters.Where(m => m.Rarity != ActorRarity.Normal)
                .OrderBy(m => m.FloorCoordinate.XYDistanceTo(Hud.Game.Me.FloorCoordinate))
                .FirstOrDefault();
            if (monster != null)
            {
                CurrentTarget_Monster = monster;
                CurrentTarget = new Vector2(monster.FloorCoordinate.X, monster.FloorCoordinate.Y);
                AddDebugMessage($"找到怪物，类型: {monster.SnoMonster.NameLocalized}，稀有度: {monster.Rarity}，位置: ({CurrentTarget.X:F1}, {CurrentTarget.Y:F1}), 距离: {monster.FloorCoordinate.XYDistanceTo(Hud.Game.Me.FloorCoordinate):F1}");
                return true;
            }
            CurrentTarget_Monster = null;
            AddDebugMessage("未找到怪物");
            return false;
        }
        
        private IActor FindRiftExit()
        {
            var exit = Hud.Game.Actors.FirstOrDefault(a => a.SnoActor.Kind == ActorKind.Portal && a.SnoActor.Code.Contains("RiftPortal"));
            if (exit != null)
            {
                AddDebugMessage($"找到秘境出口，距离: {exit.FloorCoordinate.XYDistanceTo(Hud.Game.Me.FloorCoordinate)}");
            }
            return exit;
        }

        private void FindNewExplorationTarget()
        {
            try
            {
                Vector2 playerPos = new Vector2(Hud.Game.Me.FloorCoordinate.X, Hud.Game.Me.FloorCoordinate.Y);
                
                // 1. 首先尝试根据场景和导航网格查找未探索区域
                // 由于可能存在NavMesh不可用的情况，这里使用一个更简单的方法：边界扩展
                
                // 检查已探索点的边界，找出边界点
                if (ExploredPoints.Count > 10)
                {
                    AddDebugMessage("基于已探索区域边界寻找新目标");
                    
                    // 建立网格来表示探索状态
                    var grid = BuildExplorationGrid();
                    
                    // 寻找边界点
                    var boundaryPoints = FindBoundaryPoints(grid);
                    
                    if (boundaryPoints.Count > 0)
                    {
                        // 选择一个最有潜力的边界点（远离已探索区域中心，但不要太远）
                        var exploredCenter = GetExploredCenter();
                        
                        // 按照与已探索中心的距离排序边界点
                        var sortedBoundaryPoints = boundaryPoints
                            .OrderByDescending(p => Vector2.Distance(p, exploredCenter))
                            .Take(5) // 取前5个最远的点
                            .ToList();
                        
                        // 从这些点中选择一个与玩家距离适中的点
                        var chosenTarget = sortedBoundaryPoints
                            .OrderBy(p => Math.Abs(Vector2.Distance(p, playerPos) - 100.0f)) // 尝试找距离玩家约100单位的点
                            .FirstOrDefault();
                        
                        if (chosenTarget != default(Vector2))
                        {
                            CurrentTarget = chosenTarget;
                            CurrentTarget_Monster = null;
                            CurrentTarget_ShrineOrPylon = null;
                            AddDebugMessage($"选择边界点作为新目标: ({CurrentTarget.X:F1}, {CurrentTarget.Y:F1})");
                            return;
                        }
                    }
                }
                
                // 2. 如果没有找到合适的边界点，尝试使用游戏区域中尚未探索的区域
                var mapBounds = GetMapBounds();
                var unexploredAreas = FindUnexploredAreas(mapBounds);
                
                if (unexploredAreas.Count > 0)
                {
                    // 按照与玩家的距离排序，优先选择更近的区域
                    var target = unexploredAreas
                        .OrderBy(p => Vector2.Distance(p, playerPos))
                        .FirstOrDefault();
                    
                    CurrentTarget = target;
                    CurrentTarget_Monster = null;
                    CurrentTarget_ShrineOrPylon = null;
                    AddDebugMessage($"选择未探索区域作为新目标: ({CurrentTarget.X:F1}, {CurrentTarget.Y:F1})");
                    return;
                }
                
                // 3. 如果以上方法都失败，回到最初探索的区域（可能有漏掉的地方）
                if (ExploredPoints.Count > 0)
                {
                    var startPoint = ExploredPoints.First();
                    if (Vector2.Distance(playerPos, startPoint) > 100.0f)
                    {
                        CurrentTarget = startPoint;
                        CurrentTarget_Monster = null;
                        CurrentTarget_ShrineOrPylon = null;
                        AddDebugMessage($"返回到起始区域: ({CurrentTarget.X:F1}, {CurrentTarget.Y:F1})");
                        return;
                    }
                }
                
                // 如果所有方法都失败，将保持当前目标不变
                AddDebugMessage("无新探索目标（所有方法都失败）");
            }
            catch (Exception ex)
            {
                AddDebugMessage($"寻找新探索目标出错: {ex.Message}");
            }
        }

        // 构建一个表示已探索区域的网格
        private Dictionary<(int, int), bool> BuildExplorationGrid()
        {
            var grid = new Dictionary<(int, int), bool>();
            int gridSize = 10; // 网格单元格大小
            
            foreach (var point in ExploredPoints)
            {
                int gridX = (int)(point.X / gridSize);
                int gridY = (int)(point.Y / gridSize);
                grid[(gridX, gridY)] = true;
            }
            
            return grid;
        }

        // 寻找已探索区域的边界点
        private List<Vector2> FindBoundaryPoints(Dictionary<(int, int), bool> grid)
        {
            var boundaryPoints = new List<Vector2>();
            int gridSize = 10;
            
            // 检查每个已探索的网格单元，查看其邻居是否包含未探索区域
            foreach (var cell in grid.Keys)
            {
                int x = cell.Item1;
                int y = cell.Item2;
                
                // 检查8个相邻单元格
                bool isBoundary = false;
                for (int dx = -1; dx <= 1; dx++)
                {
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        if (dx == 0 && dy == 0) continue;
                        
                        if (!grid.ContainsKey((x + dx, y + dy)))
                        {
                            isBoundary = true;
                            break;
                        }
                    }
                    if (isBoundary) break;
                }
                
                if (isBoundary)
                {
                    // 如果是边界单元格，添加其中心点作为边界点
                    boundaryPoints.Add(new Vector2((x + 0.5f) * gridSize, (y + 0.5f) * gridSize));
                }
            }
            
            return boundaryPoints;
        }

        // 获取已探索区域的中心
        private Vector2 GetExploredCenter()
        {
            if (ExploredPoints.Count == 0)
                return new Vector2(Hud.Game.Me.FloorCoordinate.X, Hud.Game.Me.FloorCoordinate.Y);
            
            float sumX = 0, sumY = 0;
            foreach (var point in ExploredPoints)
            {
                sumX += point.X;
                sumY += point.Y;
            }
            
            return new Vector2(sumX / ExploredPoints.Count, sumY / ExploredPoints.Count);
        }

        // 查找地图中未探索的区域
        private List<Vector2> FindUnexploredAreas((float MinX, float MaxX, float MinY, float MaxY) mapBounds)
        {
            var unexploredAreas = new List<Vector2>();
            int gridSize = 50; // 较大的网格单元用于查找未探索区域
            
            // 构建已探索区域的网格
            var exploredGrid = new HashSet<(int, int)>();
            foreach (var point in ExploredPoints)
            {
                int gridX = (int)(point.X / gridSize);
                int gridY = (int)(point.Y / gridSize);
                exploredGrid.Add((gridX, gridY));
            }
            
            // 在地图边界内随机选择若干点，检查是否在已探索网格中
            var random = new Random();
            int attempts = 50;
            
            for (int i = 0; i < attempts; i++)
            {
                float x = mapBounds.MinX + (float)random.NextDouble() * (mapBounds.MaxX - mapBounds.MinX);
                float y = mapBounds.MinY + (float)random.NextDouble() * (mapBounds.MaxY - mapBounds.MinY);
                
                int gridX = (int)(x / gridSize);
                int gridY = (int)(y / gridSize);
                
                // 如果该区域未被探索过
                if (!exploredGrid.Contains((gridX, gridY)))
                {
                    unexploredAreas.Add(new Vector2(x, y));
                }
            }
            
            return unexploredAreas;
        }

        // 随机选择一个方向作为目标
        private void FindRandomDirection(Vector2 playerPos)
        {
            try
            {
                var random = new Random();
                
                // 创建8个可能的方向
                var directions = new List<Vector2>
                {
                    new Vector2(1, 0),      // 东
                    new Vector2(0.7f, 0.7f), // 东南
                    new Vector2(0, 1),      // 南
                    new Vector2(-0.7f, 0.7f), // 西南
                    new Vector2(-1, 0),     // 西
                    new Vector2(-0.7f, -0.7f), // 西北
                    new Vector2(0, -1),     // 北
                    new Vector2(0.7f, -0.7f)  // 东北
                };
                
                // 随机打乱方向列表，但优先考虑尚未探索过的方向
                directions = directions.OrderBy(d => 
                {
                    Vector2 targetPos = playerPos + d * 100.0f;
                    return ExploredPoints.Any(p => Vector2.Distance(p, targetPos) < 30.0f) ? 1 : 0;
                }).ThenBy(d => random.Next()).ToList();
                
                // 尝试每个方向，直到找到一个可行的目标
                foreach (var direction in directions)
                {
                    float distance = 90.0f + random.Next(0, 20); // 90-110的随机距离
                    Vector2 targetPos = new Vector2(
                        playerPos.X + direction.X * distance,
                        playerPos.Y + direction.Y * distance
                    );
                    
                    // 检查该目标是否已在已探索区域
                    if (!ExploredPoints.Any(p => Vector2.Distance(p, targetPos) < 20.0f))
                    {
                        CurrentTarget = targetPos;
                        CurrentTarget_Monster = null;
                        CurrentTarget_ShrineOrPylon = null;
                        
                        PathPoints = GenerateStraightLinePath(playerPos, CurrentTarget);
                        if (PathPoints.Count > 0)
                        {
                            UpdateNextWaypoint(playerPos);
                            AddDebugMessage($"随机选择方向: ({direction.X:F2}, {direction.Y:F2})，距离: {distance:F1}，计算路径，路径点数量: {PathPoints.Count}");
                            return;
                        }
                    }
                }
                
                // 如果所有方向都失败，选择一个默认方向（向东）
                CurrentTarget = new Vector2(playerPos.X + 100.0f, playerPos.Y);
                PathPoints = GenerateStraightLinePath(playerPos, CurrentTarget);
                if (PathPoints.Count > 0)
                {
                    UpdateNextWaypoint(playerPos);
                    AddDebugMessage($"所有随机方向都失败，选择默认方向（向东），路径点数量: {PathPoints.Count}");
                }
            }
            catch (Exception ex)
            {
                AddDebugMessage($"选择随机方向出错: {ex.Message}");
            }
        }

        // 生成直线路径
        private List<Vector2> GenerateStraightLinePath(Vector2 start, Vector2 goal)
        {
            var path = new List<Vector2>();
            var direction = goal - start;
            var distance = direction.Length();
            
            if (distance > 0)
            {
                direction = Vector2.Normalize(direction);
            }
            else
            {
                // 如果距离为0，使用默认方向（向东）
                direction = new Vector2(1, 0);
                distance = 10.0f; // 设置一个默认距离
            }
            
            // 每 10 单位生成一个路径点
            var step = 10.0f;
            var numSteps = (int)(distance / step);
            
            path.Add(start); // 添加起点
            
            for (int i = 1; i <= numSteps; i++)
            {
                var point = start + direction * (i * step);
                path.Add(point);
            }
            
            // 确保目标点在路径中
            if (path.Count == 0 || Vector2.Distance(path[path.Count - 1], goal) > 1.0f)
            {
                path.Add(goal);
            }
            
            return path;
        }

        // 获取当前地图边界
        private (float MinX, float MaxX, float MinY, float MaxY) GetMapBounds()
        {
            try
            {
                // 尝试获取游戏地图边界，如果有此功能
                var actors = Hud.Game.Actors.Where(a => a.DisplayOnOverlay && a.FloorCoordinate != null).ToList();
                
                if (!actors.Any())
                    return (float.MinValue, float.MaxValue, float.MinValue, float.MaxValue);
                
                float minX = actors.Min(a => a.FloorCoordinate.X) - 100;
                float maxX = actors.Max(a => a.FloorCoordinate.X) + 100;
                float minY = actors.Min(a => a.FloorCoordinate.Y) - 100;
                float maxY = actors.Max(a => a.FloorCoordinate.Y) + 100;
                
                return (minX, maxX, minY, maxY);
            }
            catch (Exception ex)
            {
                AddDebugMessage($"获取地图边界出错: {ex.Message}");
                return (float.MinValue, float.MaxValue, float.MinValue, float.MaxValue);
            }
        }

        private List<Vector2> ReconstructPath(Dictionary<Vector2, Vector2> cameFrom, Vector2 current)
        {
            var path = new List<Vector2> { current };
            while (cameFrom.ContainsKey(current))
            {
                current = cameFrom[current];
                path.Add(current);
            }
            path.Reverse();
            return path;
        }

        private void UpdateNextWaypoint(Vector2 playerPos)
        {
            try
            {
                // 如果没有路径点，没有什么可更新的
                if (PathPoints == null || PathPoints.Count == 0)
                {
                    NextWaypoint = default(Vector2);
                    AddDebugMessage("无路径点，无法更新下一个路径点");
                    return;
                }
                
                // 找出当前路径中离玩家最近的点
                int closestPointIndex = -1;
                float closestDistance = float.MaxValue;
                
                for (int i = 0; i < PathPoints.Count; i++)
                {
                    float distance = Vector2.Distance(playerPos, PathPoints[i]);
                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        closestPointIndex = i;
                    }
                }
                
                // 如果最近的点是最后一个点，那么目标就是最后一个点
                if (closestPointIndex == PathPoints.Count - 1)
                {
                    NextWaypoint = PathPoints[closestPointIndex];
                    AddDebugMessage($"已到达最后一个路径点: ({NextWaypoint.X:F1}, {NextWaypoint.Y:F1})");
                    return;
                }
                
                // 否则，选择最近点之后的点作为下一个路径点
                // 但我们要保证这个点在玩家的前方，而不是后方
                
                // 首先尝试获取下一个点
                int nextPointIndex = closestPointIndex + 1;
                if (nextPointIndex < PathPoints.Count)
                {
                    // 计算从玩家到下一个点的向量
                    Vector2 dirToNext = new Vector2(
                        PathPoints[nextPointIndex].X - playerPos.X,
                        PathPoints[nextPointIndex].Y - playerPos.Y
                    );
                    
                    // 检查是否已经接近下一个点
                    float distToNext = dirToNext.Length();
                    if (distToNext < 10.0f && nextPointIndex < PathPoints.Count - 1)
                    {
                        // 如果已经非常接近下一个点，并且不是最后一个点，则使用下下个点
                        NextWaypoint = PathPoints[nextPointIndex + 1];
                        AddDebugMessage($"已接近下一个点，选择下下个点: ({NextWaypoint.X:F1}, {NextWaypoint.Y:F1})");
                    }
                    else
                    {
                        // 使用下一个点
                        NextWaypoint = PathPoints[nextPointIndex];
                        AddDebugMessage($"选择下一个路径点: ({NextWaypoint.X:F1}, {NextWaypoint.Y:F1})，索引: {nextPointIndex}，距离: {distToNext:F1}");
                    }
                }
                else
                {
                    // 如果没有下一个点（应该不会发生），使用最后一个点
                    NextWaypoint = PathPoints[PathPoints.Count - 1];
                    AddDebugMessage($"没有下一个点，使用最后一个点: ({NextWaypoint.X:F1}, {NextWaypoint.Y:F1})");
                }
            }
            catch (Exception ex)
            {
                AddDebugMessage($"更新下一个路径点出错: {ex.Message}");
                NextWaypoint = default(Vector2);
            }
        }

        private void RecordExploredArea(Vector2 playerPos)
        {
            ExploredPoints.Add(new Vector2((float)Math.Round(playerPos.X / 10) * 10, (float)Math.Round(playerPos.Y / 10) * 10));
            // 临时禁用 Scenes 相关功能
            // var currentScene = Hud.Game.Scenes.FirstOrDefault(s => s.NavMesh != null && s.NavMesh.IsInMesh(playerPos.X, playerPos.Y));
            // if (currentScene != null)
            //     ExploredSceneIds.Add(currentScene.SceneHash);
            AddDebugMessage("RecordExploredArea 跳过 Scenes 检查");
        }

        private void HandleStuck(Vector2 playerPos)
        {
            AddDebugMessage("检测到卡住，尝试处理");
            
            // 记录卡住的位置，以避免将来再次尝试相同的路径
            if (!ExploredPoints.Contains(playerPos))
            {
                ExploredPoints.Add(playerPos);
                AddDebugMessage($"将卡住位置 ({playerPos.X:F1}, {playerPos.Y:F1}) 添加到已探索点集合中");
            }
            
            // 标记周围区域为已探索，避免再次尝试
            float avoidRadius = 10.0f;
            for (float x = playerPos.X - avoidRadius; x <= playerPos.X + avoidRadius; x += 5.0f)
            {
                for (float y = playerPos.Y - avoidRadius; y <= playerPos.Y + avoidRadius; y += 5.0f)
                {
                    Vector2 point = new Vector2(x, y);
                    if (!ExploredPoints.Contains(point))
                    {
                        ExploredPoints.Add(point);
                    }
                }
            }
            
            // 首先尝试使用移动技能逃离
            bool escapeSuccessful = false;
            try
            {
                // 尝试使用移动技能（例如：瞬移、冲锋等）
                AddDebugMessage("尝试使用技能脱离卡住状态");
                Hud.Interaction.DoAction(ActionKey.Skill1);
                System.Threading.Thread.Sleep(200);
                
                // 检查是否脱离卡住
                var newPos = new Vector2(Hud.Game.Me.FloorCoordinate.X, Hud.Game.Me.FloorCoordinate.Y);
                if (Vector2.Distance(newPos, playerPos) > 5.0f)
                {
                    AddDebugMessage($"成功使用技能脱离卡住，新位置: ({newPos.X:F1}, {newPos.Y:F1})");
                    escapeSuccessful = true;
                }
            }
            catch (Exception ex)
            {
                AddDebugMessage($"使用技能脱离卡住出错: {ex.Message}");
            }
            
            // 如果技能方式失败，尝试更智能的随机方向移动
            if (!escapeSuccessful)
            {
                AddDebugMessage("技能脱离失败，尝试智能方向移动");
                
                // 创建四个主要方向（N, E, S, W）和四个对角方向（NE, SE, SW, NW）
                var directions = new List<Vector2>
                {
                    new Vector2(0, -1),    // 北
                    new Vector2(1, 0),     // 东
                    new Vector2(0, 1),     // 南
                    new Vector2(-1, 0),    // 西
                    new Vector2(0.7f, -0.7f),  // 东北
                    new Vector2(0.7f, 0.7f),   // 东南
                    new Vector2(-0.7f, 0.7f),  // 西南
                    new Vector2(-0.7f, -0.7f)  // 西北
                };
                
                // 按照比玩家位置到目标的方向优先排序，避免向后移动
                if (CurrentTarget != default(Vector2))
                {
                    var dirToTarget = new Vector2(CurrentTarget.X - playerPos.X, CurrentTarget.Y - playerPos.Y);
                    if (dirToTarget.Length() > 0)
                    {
                        dirToTarget = Vector2.Normalize(dirToTarget);
                        directions = directions.OrderByDescending(d => Vector2.Dot(d, dirToTarget)).ToList();
                        AddDebugMessage($"按照接近目标方向排序: ({dirToTarget.X:F2}, {dirToTarget.Y:F2})");
                    }
                }
                
                // 尝试每个方向，直到找到可行走的位置
                foreach (var dir in directions)
                {
                    // 尝试三个不同的距离
                    float[] distances = new float[] { 20.0f, 40.0f, 60.0f };
                    
                    foreach (var distance in distances)
                    {
                        var targetX = playerPos.X + dir.X * distance;
                        var targetY = playerPos.Y + dir.Y * distance;
                        
                        // 检查此位置是否已经尝试过（在已探索点集合中）
                        Vector2 targetPos = new Vector2(targetX, targetY);
                        if (ExploredPoints.Any(p => Vector2.Distance(p, targetPos) < 5.0f))
                        {
                            continue;
                        }
                        
                        try
                        {
                            AddDebugMessage($"尝试向 ({dir.X:F2}, {dir.Y:F2}) 方向移动 {distance:F1} 单位");
                            
                            // 创建世界坐标和屏幕坐标
                            var worldCoord = Hud.Window.CreateWorldCoordinate(targetX, targetY, Hud.Game.Me.FloorCoordinate.Z);
                            var screenCoord = worldCoord.ToScreenCoordinate();
                            
                            // 检查屏幕坐标是否在窗口范围内
                            if (screenCoord.X >= 0 && screenCoord.X <= Hud.Window.Size.Width &&
                                screenCoord.Y >= 0 && screenCoord.Y <= Hud.Window.Size.Height)
                            {
                                // 使用鼠标移动并点击
                                Hud.Interaction.MouseMove(screenCoord.X, screenCoord.Y);
                                System.Threading.Thread.Sleep(50);
                                Hud.Interaction.MouseDown(MouseButtons.Left);
                                System.Threading.Thread.Sleep(50);
                                Hud.Interaction.MouseUp(MouseButtons.Left);
                                
                                // 重置路径和目标
                                PathPoints = null;
                                CurrentTarget = targetPos;
                                
                                AddDebugMessage($"设置新目标位置: ({targetX:F1}, {targetY:F1})");
                                escapeSuccessful = true;
                                break;
                            }
                            else
                            {
                                AddDebugMessage($"屏幕坐标 ({screenCoord.X:F1}, {screenCoord.Y:F1}) 超出窗口范围");
                            }
                        }
                        catch (Exception ex)
                        {
                            AddDebugMessage($"尝试移动出错: {ex.Message}");
                        }
                    }
                    
                    if (escapeSuccessful)
                        break;
                }
            }
            
            // 如果所有尝试都失败，尝试更激进的措施
            if (!escapeSuccessful)
            {
                AddDebugMessage("所有方向尝试都失败，尝试更激进的措施");
                
                try
                {
                    // 尝试使用传送回城，然后再传送回来（紧急情况）
                    Hud.Interaction.DoAction(ActionKey.Move);
                    System.Threading.Thread.Sleep(300);
                    
                    // 尝试使用其他所有技能按钮
                    Hud.Interaction.DoAction(ActionKey.Skill1);
                    System.Threading.Thread.Sleep(100);
                    Hud.Interaction.DoAction(ActionKey.Skill2);
                    System.Threading.Thread.Sleep(100);
                    Hud.Interaction.DoAction(ActionKey.Skill3);
                    System.Threading.Thread.Sleep(100);
                    Hud.Interaction.DoAction(ActionKey.Skill4);
                    System.Threading.Thread.Sleep(100);
                    
                    // 重置路径状态
                    PathPoints = null;
                    NextWaypoint = default(Vector2);
                    AddDebugMessage("重置路径状态");
                }
                catch (Exception ex)
                {
                    AddDebugMessage($"紧急措施失败: {ex.Message}");
                }
            }
            
            // 重置卡住计时器
            StuckTimer.Restart();
        }

        public void ToggleAutoPotion()
        {
            AutoUsePotion = !AutoUsePotion;
            AddDebugMessage($"AutoUsePotion 切换为: {AutoUsePotion}");
        }

        public void SetHealthThreshold(float threshold)
        {
            HealthThreshold = threshold;
            AddDebugMessage($"HealthThreshold 设置为: {threshold}");
        }

        public void ResetClickedShrines()
        {
            AddDebugMessage("ResetClickedShrines 被调用（功能未实现）");
        }

        // 按顺序施放技能：左键，右键，1，3，4
        private void CastSkills()
        {
            // 检查技能冷却
            if (SkillCooldownTimer.ElapsedMilliseconds < SkillCooldown)
            {
                return;
            }
            
            // 根据当前技能索引施放对应技能
            switch (CurrentSkillIndex)
            {
                case 0: // 左键
                    AddDebugMessage("施放左键技能");
                    Hud.Interaction.MouseDown(MouseButtons.Left);
                    System.Threading.Thread.Sleep(50);
                    Hud.Interaction.MouseUp(MouseButtons.Left);
                    break;
                case 1: // 右键
                    AddDebugMessage("施放右键技能");
                    Hud.Interaction.MouseDown(MouseButtons.Right);
                    System.Threading.Thread.Sleep(50);
                    Hud.Interaction.MouseUp(MouseButtons.Right);
                    break;
                case 2: // 1键
                    AddDebugMessage("施放1号技能");
                    Hud.Interaction.DoAction(ActionKey.Skill1);
                    break;
                case 3: // 3键
                    AddDebugMessage("施放3号技能");
                    Hud.Interaction.DoAction(ActionKey.Skill3);
                    break;
                case 4: // 4键
                    AddDebugMessage("施放4号技能");
                    Hud.Interaction.DoAction(ActionKey.Skill4);
                    break;
            }
            
            // 更新技能索引，循环使用技能
            CurrentSkillIndex = (CurrentSkillIndex + 1) % 5;
            
            // 重置技能冷却计时器
            SkillCooldownTimer.Restart();
            
            // 添加额外延迟，确保技能正确施放
            System.Threading.Thread.Sleep(200);
        }

        private class PriorityQueue<T>
        {
            private List<(T item, float priority)> elements = new List<(T, float)>();

            public int Count => elements.Count;

            public void Enqueue(T item, float priority)
            {
                elements.Add((item, priority));
                elements.Sort((a, b) => a.priority.CompareTo(b.priority));
            }

            public T Dequeue()
            {
                var item = elements[0].item;
                elements.RemoveAt(0);
                return item;
            }

            public bool Contains(T item)
            {
                return elements.Any(e => EqualityComparer<T>.Default.Equals(e.item, item));
            }
        }

        private float Heuristic(Vector2 a, Vector2 b)
        {
            return Vector2.Distance(a, b);
        }

        private List<Vector2> GetNeighbors(Vector2 point)
        {
            var neighbors = new List<Vector2>();
            float step = 10.0f;
            
            // 8个方向的邻居
            neighbors.Add(new Vector2(point.X + step, point.Y));       // 东
            neighbors.Add(new Vector2(point.X + step, point.Y + step)); // 东南
            neighbors.Add(new Vector2(point.X, point.Y + step));       // 南
            neighbors.Add(new Vector2(point.X - step, point.Y + step)); // 西南
            neighbors.Add(new Vector2(point.X - step, point.Y));       // 西
            neighbors.Add(new Vector2(point.X - step, point.Y - step)); // 西北
            neighbors.Add(new Vector2(point.X, point.Y - step));       // 北
            neighbors.Add(new Vector2(point.X + step, point.Y - step)); // 东北
            
            return neighbors;
        }

        public List<Vector2> FindPathAStar(Vector2 start, Vector2 goal)
        {
            AddDebugMessage($"计算从 ({start.X:F1}, {start.Y:F1}) 到 ({goal.X:F1}, {goal.Y:F1}) 的路径");
            
            // 检查起点和终点是否太近，如果太近直接返回直线路径
            if (Vector2.Distance(start, goal) < 20.0f)
            {
                AddDebugMessage("起点和终点距离很近，返回直线路径");
                return GenerateStraightLinePath(start, goal);
            }
            
            // 检查目标点是否已知不可行走
            if (ExploredPoints.Any(p => Vector2.Distance(p, goal) < 10.0f))
            {
                AddDebugMessage("目标点附近区域已知不可行走，尝试找替代目标");
                // 在目标周围寻找一个可行走的点
                var alternativeGoal = FindAlternativeGoal(goal);
                if (alternativeGoal != goal)
                {
                    AddDebugMessage($"找到替代目标点: ({alternativeGoal.X:F1}, {alternativeGoal.Y:F1})");
                    goal = alternativeGoal;
                }
            }
            
            var openSet = new PriorityQueue<Vector2>();
            var cameFrom = new Dictionary<Vector2, Vector2>();
            var gScore = new Dictionary<Vector2, float>();
            var fScore = new Dictionary<Vector2, float>();
            var closedSet = new HashSet<Vector2>(); // 记录已经检查过的点

            openSet.Enqueue(start, 0);
            gScore[start] = 0;
            fScore[start] = Heuristic(start, goal);

            int iterations = 0;
            int maxIterations = 1000; // 防止无限循环
            
            while (openSet.Count > 0 && iterations < maxIterations)
            {
                iterations++;
                var current = openSet.Dequeue();
                
                // 如果到达目标附近，返回路径
                if (Vector2.Distance(current, goal) < 10.0f)
                {
                    var path = ReconstructPath(cameFrom, current);
                    AddDebugMessage($"找到路径，路径点数量: {path.Count}，迭代次数: {iterations}");
                    return OptimizePath(path); // 优化路径，减少不必要的拐点
                }
                
                closedSet.Add(current);

                foreach (var neighbor in GetNeighbors(current))
                {
                    // 如果邻居节点已经在闭集中，跳过
                    if (closedSet.Contains(neighbor))
                        continue;
                    
                    // 检查是否可行走 - 包括游戏障碍物和已知卡住点检查
                    if (!IsWalkable(neighbor) || ExploredPoints.Contains(neighbor)) 
                    {
                        closedSet.Add(neighbor); // 将不可行走的点加入闭集
                        continue;
                    }

                    var tentativeGScore = gScore[current] + Vector2.Distance(current, neighbor);
                    
                    // 如果这是一个更好的路径，或者邻居节点是新发现的
                    if (!gScore.ContainsKey(neighbor) || tentativeGScore < gScore[neighbor])
                    {
                        cameFrom[neighbor] = current;
                        gScore[neighbor] = tentativeGScore;
                        fScore[neighbor] = gScore[neighbor] + Heuristic(neighbor, goal);
                        
                        // 如果邻居不在开集中，添加它
                        if (!openSet.Contains(neighbor))
                            openSet.Enqueue(neighbor, fScore[neighbor]);
                    }
                }
            }
            
            if (iterations >= maxIterations)
            {
                AddDebugMessage($"路径搜索达到最大迭代次数 {maxIterations}，返回直线路径");
                return GenerateStraightLinePath(start, goal);
            }
            
            AddDebugMessage("未找到路径，返回直线路径");
            return GenerateStraightLinePath(start, goal);
        }

        // 优化路径，移除不必要的拐点
        private List<Vector2> OptimizePath(List<Vector2> path)
        {
            if (path.Count <= 2)
                return path;
            
            var optimizedPath = new List<Vector2>();
            optimizedPath.Add(path[0]); // 添加起点
            
            int i = 0;
            while (i < path.Count - 2)
            {
                Vector2 current = path[i];
                // 检查当前点到后面第二个点的直线是否可行走
                if (IsDirectPathClear(current, path[i + 2]))
                {
                    // 如果可以直接走到第二个点，跳过中间的点
                    i += 1;
                }
                else
                {
                    // 否则添加下一个点
                    optimizedPath.Add(path[i + 1]);
                    i += 1;
                }
            }
            
            optimizedPath.Add(path[path.Count - 1]); // 添加终点
            
            AddDebugMessage($"路径优化：原路径点数 {path.Count}，优化后点数 {optimizedPath.Count}");
            return optimizedPath;
        }

        // 检查两点之间的直线路径是否通畅
        private bool IsDirectPathClear(Vector2 start, Vector2 end)
        {
            float distance = Vector2.Distance(start, end);
            int steps = (int)Math.Ceiling(distance / 5.0f); // 每5个单位检查一次
            
            for (int i = 1; i < steps; i++)
            {
                float t = i / (float)steps;
                Vector2 point = new Vector2(
                    start.X + t * (end.X - start.X),
                    start.Y + t * (end.Y - start.Y)
                );
                
                if (!IsWalkable(point) || ExploredPoints.Contains(point))
                    return false;
            }
            
            return true;
        }

        // 在目标周围找一个可行走的替代点
        private Vector2 FindAlternativeGoal(Vector2 goal)
        {
            // 在目标周围搜索多个距离的圆环
            float[] distances = new float[] { 20.0f, 40.0f, 60.0f };
            int numDirections = 16; // 在每个圆环上检查16个方向
            
            foreach (var distance in distances)
            {
                for (int i = 0; i < numDirections; i++)
                {
                    double angle = 2 * Math.PI * i / numDirections;
                    float x = goal.X + distance * (float)Math.Cos(angle);
                    float y = goal.Y + distance * (float)Math.Sin(angle);
                    
                    Vector2 candidate = new Vector2(x, y);
                    
                    // 检查这个点是否已知不可行走
                    if (!ExploredPoints.Any(p => Vector2.Distance(p, candidate) < 10.0f) && IsWalkable(candidate))
                    {
                        return candidate;
                    }
                }
            }
            
            // 如果找不到替代点，返回原目标
            return goal;
        }

        private bool IsWalkable(Vector2 point)
        {
            try
            {
                // 首先检查该点是否在我们标记的不可行走区域
                if (ExploredPoints.Contains(point))
                    return false;
                
                // 检查点是否在游戏区域内
                var mapBounds = GetMapBounds();
                if (point.X < mapBounds.MinX || point.X > mapBounds.MaxX || 
                    point.Y < mapBounds.MinY || point.Y > mapBounds.MaxY)
                {
                    return false;
                }
                
                // 检查点周围是否有障碍物
                var obstacles = Hud.Game.Actors.Where(a => 
                    a.DisplayOnOverlay &&
                    a.FloorCoordinate != null &&
                    Vector2.Distance(new Vector2(a.FloorCoordinate.X, a.FloorCoordinate.Y), point) < 5.0f &&
                    (a.SnoActor.Kind == ActorKind.Obstacle || 
                     a.SnoActor.Kind == ActorKind.NoWalk || 
                     a.SnoActor.Kind == ActorKind.Chest || 
                     a.SnoActor.Kind == ActorKind.Stash || 
                     a.SnoActor.Kind == ActorKind.WeaponRack || 
                     a.SnoActor.Kind == ActorKind.ArmorRack || 
                     a.SnoActor.Kind == ActorKind.Banner) &&
                    !(a is IMonster) // 确保不把怪物当作障碍物
                ).ToList();
                
                if (obstacles.Any())
                {
                    return false;
                }
                
                // 查询此点的导航网格信息（如果TurboHUD提供）
                // 注意：这部分取决于TurboHUD是否提供此功能，如果不可用可以忽略
                
                return true;
            }
            catch (Exception ex)
            {
                AddDebugMessage($"IsWalkable检查出错: {ex.Message}");
                return true; // 出错时默认可行走
            }
        }
    }
} 