using System;
using System.IO;
using System.Linq;
using Rampastring.XNAUI;
using TSMapEditor.Models;
using TSMapEditor.Mutations;
using TSMapEditor.Scripts;
using TSMapEditor.Settings;
using TSMapEditor.UI.Controls;
using TSMapEditor.UI.CursorActions;
using TSMapEditor.UI.Windows;
using TSMapEditor.Models.Enums;
using Rampastring.Tools;
using System.Diagnostics;
using System.ComponentModel;
using TSMapEditor.UI.Notifications;

#if WINDOWS
using System.Windows.Forms;
#endif

namespace TSMapEditor.UI.TopBar
{
    class TopBarMenu : EditorPanel
    {
        public TopBarMenu(WindowManager windowManager, MutationManager mutationManager, MapUI mapUI, Map map, WindowController windowController,INotificationManager notificationManager) : base(windowManager)
        {
            this.mutationManager = mutationManager;
            this.mapUI = mapUI;
            this.map = map;
            this.windowController = windowController;
            this.notificationManager = notificationManager;
        }

        public event EventHandler<FileSelectedEventArgs> OnFileSelected;
        public event EventHandler InputFileReloadRequested;
        public event EventHandler MapWideOverlayLoadRequested;

        private readonly MutationManager mutationManager;
        private readonly MapUI mapUI;
        private readonly Map map;
        private readonly WindowController windowController;
        private readonly INotificationManager notificationManager;
        private MenuButton[] menuButtons;

        private DeleteTubeCursorAction deleteTunnelCursorAction;
        private PlaceTubeCursorAction placeTubeCursorAction;
        private ToggleIceGrowthCursorAction toggleIceGrowthCursorAction;
        private CheckDistanceCursorAction checkDistanceCursorAction;
        private CheckDistancePathfindingCursorAction checkDistancePathfindingCursorAction;
        private CalculateTiberiumValueCursorAction calculateTiberiumValueCursorAction;
        private ManageBaseNodesCursorAction manageBaseNodesCursorAction;
        private PlaceVeinholeMonsterCursorAction placeVeinholeMonsterCursorAction;

        private SelectBridgeWindow selectBridgeWindow;

        public override void Initialize()
        {
            Name = nameof(TopBarMenu);

            deleteTunnelCursorAction = new DeleteTubeCursorAction(mapUI);
            placeTubeCursorAction = new PlaceTubeCursorAction(mapUI);
            toggleIceGrowthCursorAction = new ToggleIceGrowthCursorAction(mapUI);
            checkDistanceCursorAction = new CheckDistanceCursorAction(mapUI);
            checkDistancePathfindingCursorAction = new CheckDistancePathfindingCursorAction(mapUI);
            calculateTiberiumValueCursorAction = new CalculateTiberiumValueCursorAction(mapUI);
            manageBaseNodesCursorAction = new ManageBaseNodesCursorAction(mapUI);
            placeVeinholeMonsterCursorAction = new PlaceVeinholeMonsterCursorAction(mapUI);

            selectBridgeWindow = new SelectBridgeWindow(WindowManager, map);
            var selectBridgeDarkeningPanel = DarkeningPanel.InitializeAndAddToParentControlWithChild(WindowManager, Parent, selectBridgeWindow);
            selectBridgeDarkeningPanel.Hidden += SelectBridgeDarkeningPanel_Hidden;

            windowController.SelectConnectedTileWindow.ObjectSelected += SelectConnectedTileWindow_ObjectSelected;

            var fileContextMenu = new EditorContextMenu(WindowManager);
            fileContextMenu.Name = nameof(fileContextMenu);
            fileContextMenu.AddItem("新建地图", () => windowController.CreateNewMapWindow.Open(), null, null, null);
            fileContextMenu.AddItem("打开地图", () => Open(), null, null, null);

            fileContextMenu.AddItem("保存", () => SaveMap());
            fileContextMenu.AddItem("另存为", () => SaveAs(), null, null, null);
            fileContextMenu.AddItem(" ", null, () => false, null, null);
            fileContextMenu.AddItem("重新加载",
                () => InputFileReloadRequested?.Invoke(this, EventArgs.Empty),
                () => !string.IsNullOrWhiteSpace(map.LoadedINI.FileName),
                null, null);
            fileContextMenu.AddItem(" ", null, () => false, null, null);
            fileContextMenu.AddItem("提取巨型地图...", () => windowController.MegamapGenerationOptionsWindow.Open(false));
            fileContextMenu.AddItem("生成地图预览图...", WriteMapPreviewConfirmation);
            fileContextMenu.AddItem(" ", null, () => false, null, null, null);
            fileContextMenu.AddItem("使用文本编辑器打开", OpenWithTextEditor, () => !string.IsNullOrWhiteSpace(map.LoadedINI.FileName));
            fileContextMenu.AddItem(" ", null, () => false, null, null);
            fileContextMenu.AddItem("退出", WindowManager.CloseGame);

            var fileButton = new MenuButton(WindowManager, fileContextMenu);
            fileButton.Name = nameof(fileButton);
            fileButton.Text = "文件";
            AddChild(fileButton);

            var editContextMenu = new EditorContextMenu(WindowManager);
            editContextMenu.Name = nameof(editContextMenu);
            editContextMenu.AddItem("配置复制的对象...", () => windowController.CopiedEntryTypesWindow.Open(), null, null, null, () => KeyboardCommands.Instance.ConfigureCopiedObjects.GetKeyDisplayString());
            editContextMenu.AddItem("复制", () => KeyboardCommands.Instance.Copy.DoTrigger(), null, null, null, () => KeyboardCommands.Instance.Copy.GetKeyDisplayString());
            editContextMenu.AddItem("复制自定义形状", () => KeyboardCommands.Instance.CopyCustomShape.DoTrigger(), null, null, null, () => KeyboardCommands.Instance.CopyCustomShape.GetKeyDisplayString());
            editContextMenu.AddItem("粘贴", () => KeyboardCommands.Instance.Paste.DoTrigger(), null, null, null, () => KeyboardCommands.Instance.Paste.GetKeyDisplayString());
            editContextMenu.AddItem(" ", null, () => false, null, null);
            editContextMenu.AddItem("撤销", () => mutationManager.Undo(), () => mutationManager.CanUndo(), null, null, () => KeyboardCommands.Instance.Undo.GetKeyDisplayString());
            editContextMenu.AddItem("重做", () => mutationManager.Redo(), () => mutationManager.CanRedo(), null, null, () => KeyboardCommands.Instance.Redo.GetKeyDisplayString());
            editContextMenu.AddItem("操作历史", () => windowController.HistoryWindow.Open());
            editContextMenu.AddItem(" ", null, () => false, null, null);
            editContextMenu.AddItem("基础设置", () => windowController.BasicSectionConfigWindow.Open(), null, null, null);
            editContextMenu.AddItem("地图大小", () => windowController.MapSizeWindow.Open(), null, null, null, null);
            editContextMenu.AddItem(" ", null, () => false, null, null);
            editContextMenu.AddItem("光照", () => windowController.LightingSettingsWindow.Open(), null, null, null);
            editContextMenu.AddItem(" ", null, () => false, null, null);
            editContextMenu.AddItem("放置隧道", () => mapUI.EditorState.CursorAction = placeTubeCursorAction, null, null, null, () => KeyboardCommands.Instance.PlaceTunnel.GetKeyDisplayString());
            editContextMenu.AddItem("删除隧道", () => mapUI.EditorState.CursorAction = deleteTunnelCursorAction, null, null, null);
            editContextMenu.AddItem(" ", null, () => false, null, null);

            int bridgeCount = map.EditorConfig.Bridges.Count;
            if (bridgeCount > 0)
            {
                var bridges = map.EditorConfig.Bridges;
                if (bridgeCount == 1 && bridges[0].Kind == BridgeKind.Low)
                {
                    editContextMenu.AddItem("绘制低桥梁", () => mapUI.EditorState.CursorAction =
                        new PlaceBridgeCursorAction(mapUI, bridges[0]), null, null, null);
                }
                else
                {
                    editContextMenu.AddItem("绘制桥梁...", SelectBridge, null, null, null);
                }
            }

            var theaterMatchingCliffs = map.EditorConfig.Cliffs.Where(cliff => cliff.AllowedTheaters.Exists(
                theaterName => theaterName.Equals(map.TheaterName, StringComparison.OrdinalIgnoreCase))).ToList();
            int cliffCount = theaterMatchingCliffs.Count;
            if (cliffCount > 0)
            {
                if (cliffCount == 1)
                {
                    editContextMenu.AddItem("绘制连接地形", () => mapUI.EditorState.CursorAction =
                        new DrawCliffCursorAction(mapUI, theaterMatchingCliffs[0]), null, null, null);
                }
                else
                {
                    editContextMenu.AddItem("绘制最近的连接地形", RepeatLastConnectedTile, null, null, null, () => KeyboardCommands.Instance.RepeatConnectedTile.GetKeyDisplayString());
                    editContextMenu.AddItem("绘制连接地形...", () => windowController.SelectConnectedTileWindow.Open(), null, null, null, () => KeyboardCommands.Instance.PlaceConnectedTile.GetKeyDisplayString());
                }
            }

            editContextMenu.AddItem("Toggle IceGrowth", () => { mapUI.EditorState.CursorAction = toggleIceGrowthCursorAction; toggleIceGrowthCursorAction.ToggleIceGrowth = true; mapUI.EditorState.HighlightIceGrowth = true; }, null, null, null);
            editContextMenu.AddItem("Clear IceGrowth", () => { mapUI.EditorState.CursorAction = toggleIceGrowthCursorAction; toggleIceGrowthCursorAction.ToggleIceGrowth = false; mapUI.EditorState.HighlightIceGrowth = true; }, null, null, null);
            editContextMenu.AddItem(" ", null, () => false, null, null);
            editContextMenu.AddItem("管理基地节点", ManageBaseNodes_Selected, null, null, null);

            if (map.Rules.OverlayTypes.Exists(ot => ot.ININame == Constants.VeinholeMonsterTypeName) && map.Rules.OverlayTypes.Exists(ot => ot.ININame == Constants.VeinholeDummyTypeName))
            {
                editContextMenu.AddItem(" ", null, () => false, null, null);
                editContextMenu.AddItem("放置 Veinhole Monster", () => mapUI.EditorState.CursorAction = placeVeinholeMonsterCursorAction, null, null, null, null);
            }

            var editButton = new MenuButton(WindowManager, editContextMenu);
            editButton.Name = nameof(editButton);
            editButton.X = fileButton.Right;
            editButton.Text = "编辑";
            AddChild(editButton);

            var viewContextMenu = new EditorContextMenu(WindowManager);
            viewContextMenu.Name = nameof(viewContextMenu);
            viewContextMenu.AddItem("配置物体可见性...", () => windowController.RenderedObjectsConfigurationWindow.Open());
            viewContextMenu.AddItem(" ", null, () => false, null, null);
            viewContextMenu.AddItem("切换不可通行可见性", () => mapUI.EditorState.HighlightImpassableCells = !mapUI.EditorState.HighlightImpassableCells, null, null, null);
            viewContextMenu.AddItem("切换IceGrowth可见性", () => mapUI.EditorState.HighlightIceGrowth = !mapUI.EditorState.HighlightIceGrowth, null, null, null);
            viewContextMenu.AddItem(" ", null, () => false, null, null);
            viewContextMenu.AddItem("显示小地图", () => windowController.MinimapWindow.Open());
            viewContextMenu.AddItem(" ", null, () => false, null, null);
            viewContextMenu.AddItem("查找路径点...", () => windowController.FindWaypointWindow.Open());
            viewContextMenu.AddItem("地图中心", () => mapUI.Camera.CenterOnMapCenterCell());
            viewContextMenu.AddItem(" ", null, () => false, null, null);
            viewContextMenu.AddItem("无光照", () => mapUI.EditorState.LightingPreviewState = LightingPreviewMode.NoLighting);
            viewContextMenu.AddItem("正常光照", () => mapUI.EditorState.LightingPreviewState = LightingPreviewMode.Normal);
            if (Constants.IsRA2YR)
            {
                viewContextMenu.AddItem("闪电风暴光照", () => mapUI.EditorState.LightingPreviewState = LightingPreviewMode.IonStorm);
                viewContextMenu.AddItem("心灵控制仪光照", () => mapUI.EditorState.LightingPreviewState = LightingPreviewMode.Dominator);
            }
            else
            {
                viewContextMenu.AddItem("Ion Storm Lighting", () => mapUI.EditorState.LightingPreviewState = LightingPreviewMode.IonStorm);
            }
            viewContextMenu.AddItem(" ", null, () => false, null, null);
            viewContextMenu.AddItem("切换全屏模式", () => KeyboardCommands.Instance.ToggleFullscreen.DoTrigger());

            var viewButton = new MenuButton(WindowManager, viewContextMenu);
            viewButton.Name = nameof(viewButton);
            viewButton.X = editButton.Right;
            viewButton.Text = "视图";
            AddChild(viewButton);

            var toolsContextMenu = new EditorContextMenu(WindowManager);
            toolsContextMenu.Name = nameof(toolsContextMenu);
            // toolsContextMenu.AddItem("Options");
            if (windowController.AutoApplyImpassableOverlayWindow.IsAvailable)
                toolsContextMenu.AddItem("应用不可通行覆盖物...", () => windowController.AutoApplyImpassableOverlayWindow.Open(), null, null, null);

            toolsContextMenu.AddItem("地形生成器选项...", () => windowController.TerrainGeneratorConfigWindow.Open(), null, null, null, () => KeyboardCommands.Instance.ConfigureTerrainGenerator.GetKeyDisplayString());
            toolsContextMenu.AddItem("生成地形", () => EnterTerrainGenerator(), null, null, null, () => KeyboardCommands.Instance.GenerateTerrain.GetKeyDisplayString());
            toolsContextMenu.AddItem(" ", null, () => false, null, null);
            toolsContextMenu.AddItem("应用 INI 代码...", () => windowController.ApplyINICodeWindow.Open(), null, null, null);
            toolsContextMenu.AddItem("运行 WAE 脚本...", () => windowController.RunScriptWindow.Open(), null, null, null, null);
            toolsContextMenu.AddItem(" ", null, () => false, null, null);
            toolsContextMenu.AddItem("删除设置...", () => windowController.DeletionModeConfigurationWindow.Open());
            toolsContextMenu.AddItem(" ", null, () => false, null, null);
            toolsContextMenu.AddItem("更改地图高度...", () => windowController.ChangeHeightWindow.Open(), null, () => !Constants.IsFlatWorld, null, null);
            toolsContextMenu.AddItem(" ", null, () => false, () => !Constants.IsFlatWorld, null);
            toolsContextMenu.AddItem("平滑冰面", SmoothenIce, null, null, null, null);
            toolsContextMenu.AddItem(" ", null, () => false, null, null);
            toolsContextMenu.AddItem("测量距离...", () => mapUI.EditorState.CursorAction = checkDistanceCursorAction, null, null, null, () => KeyboardCommands.Instance.CheckDistance.GetKeyDisplayString());
            toolsContextMenu.AddItem("测量距离(寻路算法)...", () => mapUI.EditorState.CursorAction = checkDistancePathfindingCursorAction, null, null, null, () => KeyboardCommands.Instance.CheckDistancePathfinding.GetKeyDisplayString());
            toolsContextMenu.AddItem(" ", null, () => false, null, null);
            toolsContextMenu.AddItem("计算矿石价值...", () => mapUI.EditorState.CursorAction = calculateTiberiumValueCursorAction, null, null, null, () => KeyboardCommands.Instance.CalculateCredits.GetKeyDisplayString());
            toolsContextMenu.AddItem(" ", null, () => false, null, null);
            toolsContextMenu.AddItem("加载地图范围的叠加...", () => MapWideOverlayLoadRequested?.Invoke(this, EventArgs.Empty), null, null, null, null);
            toolsContextMenu.AddItem(" ", null, () => false, null, null);
            toolsContextMenu.AddItem("配置热键...", () => windowController.HotkeyConfigurationWindow.Open(), null, null, null);
            toolsContextMenu.AddItem(" ", null, () => false, null, null);
            toolsContextMenu.AddItem("关于", () => windowController.AboutWindow.Open(), null, null, null, null);

            var toolsButton = new MenuButton(WindowManager, toolsContextMenu);
            toolsButton.Name = nameof(toolsButton);
            toolsButton.X = viewButton.Right;
            toolsButton.Text = "工具";
            AddChild(toolsButton);

            var scriptingContextMenu = new EditorContextMenu(WindowManager);
            scriptingContextMenu.Name = nameof(scriptingContextMenu);
            scriptingContextMenu.AddItem("所属方", () => windowController.HousesWindow.Open(), null, null, null);
            scriptingContextMenu.AddItem("触发器", () => windowController.TriggersWindow.Open(), null, null, null);
            scriptingContextMenu.AddItem("特遣部队", () => windowController.TaskForcesWindow.Open(), null, null, null);
            scriptingContextMenu.AddItem("动作脚本", () => windowController.ScriptsWindow.Open(), null, null, null);
            scriptingContextMenu.AddItem("作战小队", () => windowController.TeamTypesWindow.Open(), null, null, null);
            scriptingContextMenu.AddItem("局部变量", () => windowController.LocalVariablesWindow.Open(), null, null, null);
            scriptingContextMenu.AddItem("AI触发器", () => windowController.AITriggersWindow.Open(), null, null, null, null);

            var scriptingButton = new MenuButton(WindowManager, scriptingContextMenu);
            scriptingButton.Name = nameof(scriptingButton);
            scriptingButton.X = toolsButton.Right;
            scriptingButton.Text = "脚本编辑";
            AddChild(scriptingButton);

            base.Initialize();

            Height = fileButton.Height;

            menuButtons = new MenuButton[] { fileButton, editButton, viewButton, toolsButton, scriptingButton };
            Array.ForEach(menuButtons, b => b.MouseEnter += MenuButton_MouseEnter);

            KeyboardCommands.Instance.ConfigureCopiedObjects.Triggered += (s, e) => windowController.CopiedEntryTypesWindow.Open();
            KeyboardCommands.Instance.GenerateTerrain.Triggered += (s, e) => EnterTerrainGenerator();
            KeyboardCommands.Instance.ConfigureTerrainGenerator.Triggered += (s, e) => windowController.TerrainGeneratorConfigWindow.Open();
            KeyboardCommands.Instance.PlaceTunnel.Triggered += (s, e) => mapUI.EditorState.CursorAction = placeTubeCursorAction;
            KeyboardCommands.Instance.PlaceConnectedTile.Triggered += (s, e) => windowController.SelectConnectedTileWindow.Open();
            KeyboardCommands.Instance.RepeatConnectedTile.Triggered += (s, e) => RepeatLastConnectedTile();
            KeyboardCommands.Instance.CalculateCredits.Triggered += (s, e) => mapUI.EditorState.CursorAction = calculateTiberiumValueCursorAction;
            KeyboardCommands.Instance.CheckDistance.Triggered += (s, e) => mapUI.EditorState.CursorAction = checkDistanceCursorAction;
            KeyboardCommands.Instance.CheckDistancePathfinding.Triggered += (s, e) => mapUI.EditorState.CursorAction = checkDistancePathfindingCursorAction;
            KeyboardCommands.Instance.Save.Triggered += (s, e) => SaveMap();

            windowController.TerrainGeneratorConfigWindow.ConfigApplied += TerrainGeneratorConfigWindow_ConfigApplied;
        }

        private void TerrainGeneratorConfigWindow_ConfigApplied(object sender, EventArgs e)
        {
            EnterTerrainGenerator();
        }

        private void SaveMap()
        {
            if (string.IsNullOrWhiteSpace(map.LoadedINI.FileName))
            {
                SaveAs();
                return;
            }

            TrySaveMap();
        }

        private void TrySaveMap()
        {
            try
            {
                map.Save();
            }
            catch (Exception ex)
            {
                if (ex is UnauthorizedAccessException || ex is IOException)
                {
                    Logger.Log("无法保存地图文件.返回错误消息: " + ex.Message);

                    EditorMessageBox.Show(WindowManager, "无法保存地图",
                        "写入映射文件失败.请确保WAE具有对路径的写入访问权限." + Environment.NewLine + Environment.NewLine +
                        "此错误的常见来源是尝试将映射保存到 Program Files 或其他" + Environment.NewLine +
                        "写入受保护的目录，但未使用管理权限运行WAE." + Environment.NewLine + Environment.NewLine +
                        "返回的错误: " + ex.Message, Windows.MessageBoxButtons.OK);
                }
                else
                {
                    throw;
                }
            }
        }

        private void WriteMapPreviewConfirmation()
        {
            var messageBox = EditorMessageBox.Show(WindowManager, "确认",
                "这会将当前小地图作为地图预览写入地图文件." + Environment.NewLine + Environment.NewLine +
                "如果地图用作自定义地图，则在 CnCNet 客户端或游戏中" + Environment.NewLine + 
                "这将为地图提供预览，但这会显著增加地图文件的尺寸." + Environment.NewLine +
                "确定继续吗？" + Environment.NewLine + Environment.NewLine +
                "注意：在保存地图之前，预览实际上不会写入地图" + Environment.NewLine
                , Windows.MessageBoxButtons.YesNo);

            messageBox.YesClickedAction = _ => windowController.MegamapGenerationOptionsWindow.Open(true);
        }

        private void RepeatLastConnectedTile()
        {
            if (windowController.SelectConnectedTileWindow.SelectedObject == null)
                windowController.SelectConnectedTileWindow.Open();
            else
                SelectConnectedTileWindow_ObjectSelected(this, EventArgs.Empty);
        }

        private void OpenWithTextEditor()
        {
            string textEditorPath = UserSettings.Instance.TextEditorPath;

            if (string.IsNullOrWhiteSpace(textEditorPath) || !File.Exists(textEditorPath))
            {
                textEditorPath = GetDefaultTextEditorPath();

                if (textEditorPath == null)
                {
                    EditorMessageBox.Show(WindowManager, "未找到文本编辑器!", "未配置有效的文本编辑器，也未找到默认选项。", Windows.MessageBoxButtons.OK);
                    return;
                }
            }

            try
            {
                Process.Start(textEditorPath, "\"" + map.LoadedINI.FileName + "\"");
            }
            catch (Exception ex) when (ex is Win32Exception || ex is ObjectDisposedException)
            {
                Logger.Log("无法启动文本编辑器! 信息: " + ex.Message);
                EditorMessageBox.Show(WindowManager, "无法启动文本编辑器",
                    "尝试使用文本编辑器打开地图文件时出错." + Environment.NewLine + Environment.NewLine +
                    "错误: " + ex.Message, Windows.MessageBoxButtons.OK);
            }
        }

        private string GetDefaultTextEditorPath()
        {
            string programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            string programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);

            var pathsToSearch = new[]
            {
                Path.Combine(programFiles, "Notepad++", "notepad++.exe"),
                Path.Combine(programFilesX86, "Notepad++", "notepad++.exe"),
                Path.Combine(programFiles, "Microsoft VS Code", "vscode.exe"),
                Path.Combine(Environment.SystemDirectory, "notepad.exe"),
            };

            foreach (string path in pathsToSearch)
            {
                if (File.Exists(path))
                    return path;
            }

            return null;
        }

        private void ManageBaseNodes_Selected()
        {
            if (map.Houses.Count == 0)
            {
                EditorMessageBox.Show(WindowManager, "需要所属方",
                    "地图上没有设置所属方。需要先配置所属方，然后才能添加基础节点." + Environment.NewLine + Environment.NewLine +
                    "可从 脚本编辑 -> 所属方 进行配置所属方.", TSMapEditor.UI.Windows.MessageBoxButtons.OK);

                return;
            }

            mapUI.EditorState.CursorAction = manageBaseNodesCursorAction;
        }

        private void SmoothenIce()
        {
            new SmoothenIceScript().Perform(map);
            mapUI.InvalidateMap();
        }

        private void EnterTerrainGenerator()
        {
            if (windowController.TerrainGeneratorConfigWindow.TerrainGeneratorConfig == null)
            {
                notificationManager.AddNotification("需要先配置地形生成器");
                windowController.TerrainGeneratorConfigWindow.Open();
                return;
            }

            var generateTerrainCursorAction = new GenerateTerrainCursorAction(mapUI);
            generateTerrainCursorAction.TerrainGeneratorConfiguration = windowController.TerrainGeneratorConfigWindow.TerrainGeneratorConfig;
            mapUI.CursorAction = generateTerrainCursorAction;
        }

        private void SelectBridge()
        {
            selectBridgeWindow.Open();
        }

        private void SelectBridgeDarkeningPanel_Hidden(object sender, EventArgs e)
        {
            if (selectBridgeWindow.SelectedObject != null)
                mapUI.EditorState.CursorAction = new PlaceBridgeCursorAction(mapUI, selectBridgeWindow.SelectedObject);
        }

        private void SelectConnectedTileWindow_ObjectSelected(object sender, EventArgs e)
        {
            mapUI.EditorState.CursorAction = new DrawCliffCursorAction(mapUI, windowController.SelectConnectedTileWindow.SelectedObject);
        }

        private void Open()
        {
#if WINDOWS
            string initialPath = string.IsNullOrWhiteSpace(UserSettings.Instance.LastScenarioPath.GetValue()) ? UserSettings.Instance.GameDirectory : Path.GetDirectoryName(UserSettings.Instance.LastScenarioPath.GetValue());

            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.InitialDirectory = initialPath;
                openFileDialog.Filter = Constants.OpenFileDialogFilter.Replace(':', ';');
                openFileDialog.RestoreDirectory = true;

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    OnFileSelected?.Invoke(this, new FileSelectedEventArgs(openFileDialog.FileName));
                }
            }
#else
            windowController.OpenMapWindow.Open();
#endif
        }

        private void SaveAs()
        {
#if WINDOWS
            string initialPath = string.IsNullOrWhiteSpace(UserSettings.Instance.LastScenarioPath.GetValue()) ? UserSettings.Instance.GameDirectory : UserSettings.Instance.LastScenarioPath.GetValue();

            using (SaveFileDialog saveFileDialog = new SaveFileDialog())
            {
                saveFileDialog.InitialDirectory = Path.GetDirectoryName(initialPath);
                saveFileDialog.FileName = Path.GetFileName(initialPath);
                saveFileDialog.Filter = Constants.OpenFileDialogFilter.Replace(':', ';');
                saveFileDialog.RestoreDirectory = true;

                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    map.LoadedINI.FileName = saveFileDialog.FileName;
                    TrySaveMap();

                    if (UserSettings.Instance.LastScenarioPath.GetValue() != saveFileDialog.FileName)
                    {
                        UserSettings.Instance.RecentFiles.PutEntry(saveFileDialog.FileName);
                        UserSettings.Instance.LastScenarioPath.UserDefinedValue = saveFileDialog.FileName;
                        _ = UserSettings.Instance.SaveSettingsAsync();
                    }
                }
            }
#else
            windowController.SaveMapAsWindow.Open();
#endif
        }

        private void MenuButton_MouseEnter(object sender, EventArgs e)
        {
            var menuButton = (MenuButton)sender;

            // Is a menu open?
            int openIndex = Array.FindIndex(menuButtons, b => b.ContextMenu.Enabled);
            if (openIndex > -1)
            {
                // Switch to the new button's menu
                menuButtons[openIndex].ContextMenu.Disable();
                menuButton.OpenContextMenu();
            }
        }
    }
}
