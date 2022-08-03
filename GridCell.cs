using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GridCell
{
    public enum OutputCode
    {
        OK,
        OVERLOAD
    }
    public enum Side
    {
        BottomLeft,
        BottomRight,
        TopLeft,
        TopRight,

        Left,
        Right,
        Top,
        Bottom
    }
    public abstract class BoidCell<T> : Cell
    {
        public BoidCell(Vector2 position, Vector2 size, int maxCellDepth) : base(position, size, maxCellDepth) { }
        public abstract void AddNewObject(T obj, Vector3 position);
        public abstract void RemoveObject(T obj, Vector3 position);
        public abstract bool UpdateObjectPosition(T obj, Vector3 oldPosition, Vector3 newPosition);
        public abstract T[] ReturnObjectAt(Vector3 position);
        public abstract T[] ReturnObject();
        //public abstract Vector3 GetNeighborCell(Vector3 position, Vector2Int direction);
    }
    public abstract class Cell
    {
        public Vector2 position { get; }
        public Vector2 size { get; }
        public Vector2 centerPosition { get; }
        public int maxCellDepth { get; }

        public Cell(Vector2 position, Vector2 size, int maxCellDepth)
        {
            this.position = position;
            this.size = size;
            this.centerPosition = new Vector2(this.position.x + (size.x / 2), this.position.y + (size.y / 2));
            this.maxCellDepth = maxCellDepth;
        }
        public Vector3 GetSizeInWorld()
        {
            return new Vector3(size.x, 0, size.y);
        }
        public Vector3 GetCenterPositionInWorld()
        {
            return new Vector3(centerPosition.x, 0, centerPosition.y);
        }
        public virtual Side DetermineSide(Vector3 centerPosition, Vector3 position)
        {
            if (centerPosition.x <= position.x)
            {
                // on the right side
                if (centerPosition.y <= position.z)
                {
                    // on the top-right
                    return Side.TopRight;
                }
                else
                {
                    // on the bottom-right
                    return Side.BottomRight;
                }
            }
            else
            {
                // on the left size
                if (centerPosition.y <= position.z)
                {
                    // on the top-left
                    return Side.TopLeft;
                }
                else
                {
                    // on the bottom-left
                    return Side.BottomLeft;
                }
            }
        }
        public virtual bool IsOutOfCell(Vector3 oldPosition, Vector3 newPosition)
        {
            Vector3 sz = GetSizeInWorld();

            return centerPosition.x + (sz.x / 2) <= newPosition.x ||
                    centerPosition.x - (sz.x / 2) > newPosition.x ||
                    centerPosition.y + (sz.z / 2) <= newPosition.z ||
                    centerPosition.y - (sz.z / 2) > newPosition.z;
        }
        public virtual Vector3 GetNeighborCell(Vector3 position, Vector2Int direction)
        {
            Vector3 sz = GetSizeInWorld();

            switch (direction.x)
            {
                case 1:
                    position.Set(centerPosition.x + (sz.x / 2) + 0.01f, position.y, position.z);
                    break;
                case -1:
                    position.Set(centerPosition.x - (sz.x / 2) - 0.01f, position.y, position.z);
                    break;
                default:
                    break;
            }
            switch (direction.y)
            {
                case 1:
                    position.Set(position.x, position.y, centerPosition.y + (sz.z / 2) + 0.01f);
                    break;
                case -1:
                    position.Set(position.x, position.y, centerPosition.y - (sz.z / 2) - 0.01f);
                    break;
                default:
                    break;
            }

            return position;
        }
    }
    public abstract class Grid
    {
        public Vector3 position { get; protected set; }
        public int gridCellDepth { get; protected set; }
        public Vector2 gridCellSize { get; protected set; }
        //public int objectLimit { get; protected set; }
        public Vector2Int gridSize { get; protected set; }

        public Grid(Vector3 initialPosition, int gridCellDepth, Vector2 gridCellSize, Vector2Int gridSize)
        {
            this.position = initialPosition;

            this.gridCellDepth = gridCellDepth;
            this.gridCellSize = gridCellSize;
            //this.objectLimit = objectLimit;
            this.gridSize = gridSize;

            InstantiateCells();
        }
        protected int DeterminePositionInArray(Vector3 objPosition)
        {
            int xWholePosition = (int)(Mathf.Floor((objPosition.x - this.position.x) / gridCellSize.x) * gridCellSize.x);
            int yWholePosition = (int)(Mathf.Floor((objPosition.z - this.position.z) / gridCellSize.y) * gridCellSize.y);

            // If the object added is outside of the grid
            if (xWholePosition < 0 || xWholePosition > (gridCellSize.x * gridSize.x) || yWholePosition < 0 || yWholePosition > (gridCellSize.y * gridSize.y))
            {
                throw new System.Exception("Object is outside of the Grid");
            }

            int xIndex = (int)(xWholePosition / gridCellSize.x);
            int yIndex = (int)(yWholePosition / gridCellSize.y);

            int positionInArray = xIndex + (yIndex * gridSize.x);

            return positionInArray;
        }
        protected Vector3 TransformCellPosition(Cell cell)
        {
            return new Vector3(cell.centerPosition.x, this.position.y, cell.centerPosition.y);
        }
        public abstract bool IsOutOfTheCell(Vector3 oldPosition, Vector3 newPosition);
        public abstract Vector3 GetNeighborPositionAt(Vector3 position, Vector2Int direction);
        public abstract Vector3 GetNeighborRandomCenterPositionAt(Vector3 position, Vector2Int direction);
        protected abstract void InstantiateCells();
    }
    public class OverloadException<T> : System.Exception
    {
        public T[] objects;
        public OverloadException(T[] objects)
        {
            this.objects = objects;
        }
    }
    public class CannotSplitException : System.Exception
    {
        
    }
    public class DepthLimitReachedException : System.Exception { }
    public class CannotAddObjectException : System.Exception { }
    public class NotInCellException : System.Exception { }
}
