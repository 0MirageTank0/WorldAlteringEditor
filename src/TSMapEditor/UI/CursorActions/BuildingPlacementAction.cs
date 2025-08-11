using System;
using Rampastring.XNAUI.Input;
using TSMapEditor.GameMath;
using TSMapEditor.Models;
using TSMapEditor.Mutations.Classes;

namespace TSMapEditor.UI.CursorActions
{
    /// <summary>
    /// A cursor action that allows placing buildings on the map.
    /// </summary>
    public class BuildingPlacementAction : CursorAction
    {
        public BuildingPlacementAction(ICursorActionTarget cursorActionTarget, RKeyboard keyboard) : base(cursorActionTarget)
        {
            this.keyboard = keyboard;
        }

        public override string GetName() => "放置建筑物";
        public override bool DrawMapCrossLine => true;

        private Structure structure;

        private BuildingType buildingType;

        private readonly RKeyboard keyboard;
        public override int CrossLineXBold => buildingXWidth;
        public override int CrossLineYBold => buildingYWidth;
        private int buildingXWidth;
        private int buildingYWidth;
        public BuildingType BuildingType
        {
            get => buildingType;
            set
            {
                if (buildingType != value)
                {
                    buildingType = value;

                    if (buildingType == null)
                    {
                        structure = null;
                    }
                    else
                    {
                        structure = new Structure(buildingType) { Owner = CursorActionTarget.MutationTarget.ObjectOwner };
                    }
                }
            }
        }

        public override void OnActionEnter()
        {
            buildingXWidth = buildingYWidth = 0;
            if (structure != null)
            {
                structure.Owner = CursorActionTarget.MutationTarget.ObjectOwner;
                buildingType.ArtConfig.DoForFoundationCoords(point2D =>
                {
                    buildingXWidth = Math.Max(point2D.X, buildingXWidth);
                    buildingYWidth = Math.Max(point2D.Y, buildingYWidth);
                });
            }
        }

        public override void PreMapDraw(Point2D cellCoords)
        {
            // Assign preview data
            structure.Position = cellCoords;

            bool overlapObjects = KeyboardCommands.Instance.OverlapObjects.AreKeysOrModifiersDown(keyboard);

            bool canPlace = Map.CanPlaceObjectAt(structure, cellCoords, false,
                overlapObjects);

            if (canPlace)
            {
                var tile = CursorActionTarget.Map.GetTile(cellCoords);
                tile.Structures.Add(structure);
                CursorActionTarget.TechnoUnderCursor = structure;
            }
            CursorActionTarget.AddRefreshPoint(cellCoords, 10);
        }

        public override void PostMapDraw(Point2D cellCoords)
        {
            // Clear preview data
            var tile = CursorActionTarget.Map.GetTile(cellCoords);
            if (tile.Structures.Contains(structure))
            {
                tile.Structures.Remove(structure);
                CursorActionTarget.TechnoUnderCursor = null;
                CursorActionTarget.AddRefreshPoint(cellCoords, 10);
            }
        }
        public override void LeftDown(Point2D cellCoords)
        {
            if (BuildingType == null)
                throw new InvalidOperationException(nameof(BuildingType) + " 不能为null");

            bool overlapObjects = KeyboardCommands.Instance.OverlapObjects.AreKeysOrModifiersDown(keyboard);

            bool canPlace = Map.CanPlaceObjectAt(structure, cellCoords, false,
                overlapObjects);

            if (!canPlace)
                return;

            var mutation = new PlaceBuildingMutation(CursorActionTarget.MutationTarget, BuildingType, cellCoords);
            CursorActionTarget.MutationManager.PerformMutation(mutation);
        }

        public override void LeftClick(Point2D cellCoords)
        {
            LeftDown(cellCoords);
        }
    }
}
