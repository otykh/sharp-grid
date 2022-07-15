using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GridCell;

public abstract class Grid<T>
{
    public Vector3 position { get; protected set; }
    public int gridCellDepth { get; protected set; }
    public Vector2 gridCellSize { get; protected set; }
    public int objectLimit { get; protected set; }
    public Vector2Int gridSize { get; protected set; }

    public Grid(Vector3 initialPosition, int gridCellDepth, Vector2 gridCellSize, int objectLimit, Vector2Int gridSize)
    {
        this.position = initialPosition;

        this.gridCellDepth = gridCellDepth;
        this.gridCellSize = gridCellSize;
        this.objectLimit = objectLimit;
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
    protected Vector3 TransformCellPosition(Cell<T> cell)
    {
        return new Vector3(cell.centerPosition.x, this.position.y, cell.centerPosition.y);
    }
    public abstract void AddObject(T obj, Vector3 objPosition);
    public abstract void RemoveObject(T obj, Vector3 objPosition);
    public abstract bool IsOutOfTheCell(Vector3 oldPosition, Vector3 newPosition);
    public abstract void MoveObject(T obj, Vector3 oldPosition, Vector3 newPosition);
    protected abstract void InstantiateCells();
}
