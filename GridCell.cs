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
        TopRight
    }
    public abstract class Cell<T>
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
        public abstract void AddNewObject(T obj, Vector3 position);
        public abstract void RemoveObject(T obj, Vector3 position);
        public abstract bool IsOutOfCell(Vector3 oldPosition, Vector3 newPosition);
        public abstract T[] ReturnObjects();
    }
    public class ObjectCell : Cell<GameObject>
    {
        private GameObject[] gameObjects;
        public ObjectCell(Vector2 position, Vector2 size, int objectLimit, int maxCellDepth) : base(position, size, maxCellDepth)
        {
            gameObjects = new GameObject[objectLimit];
        }
        public override void AddNewObject(GameObject obj, Vector3 position)
        {
            //@TODO optimise this code
            //Debug.Log("Adding new object! " + obj.name + " " + gameObjects.Length);
            for (int i = 0; i < gameObjects.Length; i++)
            {
                if (gameObjects[i] is null)
                {
                    gameObjects[i] = obj;
                    return;
                }
            }
            throw new OverloadException<GameObject>(gameObjects);
        }
        public override void RemoveObject(GameObject obj, Vector3 position)
        {
            //@TODO optimise this code
            for (int i = 0; i < gameObjects.Length; i++)
            {
                if (gameObjects[i] is null) { continue; }

                if (GameObject.ReferenceEquals(gameObjects[i], obj))
                {
                    // remove that object
                    gameObjects[i] = null;
                    return;
                }
            }
            throw new System.ArgumentException("Obj is not present in the cell, but was kindly asked to be removed!");
        }
        public override bool IsOutOfCell(Vector3 oldPosition, Vector3 newPosition)
        {
            Vector3 sz = GetSizeInWorld();

            return  centerPosition.x + (sz.x / 2) <= newPosition.x ||
                    centerPosition.x - (sz.x / 2) > newPosition.x ||
                    centerPosition.y + (sz.z / 2) <= newPosition.z ||
                    centerPosition.y - (sz.z / 2) > newPosition.z;
        }
        public override GameObject[] ReturnObjects()
        {
            return gameObjects;
        }
    }
    public class ParentCell : Cell<GameObject>
    {
        private Cell<GameObject>[] cells;
        public int objectsInChildrenCells;
        private int objectLimit;
        private int depthLimit;
        public static ParentCell CreateSplitCell(Vector2 position, Vector2 size, int objectLimit, int depthLimit, GameObject[] previousObjects)
        {
            return new ParentCell(position, size, objectLimit, objectLimit, previousObjects);
        }
        public static ParentCell CreateSingleCell(Vector2 position, Vector2 size, int objectLimit, int depthLimit)
        {
            return new ParentCell(position, size, objectLimit, depthLimit);
        }
        private ParentCell(Vector2 position, Vector2 size, int objectLimit, int depthLimit, GameObject[] previousObjects) : base(position, size, depthLimit)
        {
            this.depthLimit = depthLimit;
            this.objectLimit = objectLimit;
            objectsInChildrenCells = 0;

            cells = new Cell<GameObject>[4];
            CreateFourObjectCells(position, size, objectLimit, depthLimit);

            foreach (GameObject obj in previousObjects)
            {
                AddNewObject(obj, obj.transform.position);
            }
        }
        private ParentCell(Vector2 position, Vector2 size, int objectLimit, int depthLimit) : base(position, size, depthLimit)
        {
            this.depthLimit = depthLimit;
            this.objectLimit = objectLimit;
            cells = new Cell<GameObject>[1];
            CreateSingleObjectCell(position, size, objectLimit, depthLimit);
        }
        private void CreateSingleObjectCell(Vector2 position, Vector2 size, int objectLimit, int depthLimit)
        {
            cells[0] = new ObjectCell(position, size, objectLimit, depthLimit);
        }
        private void CreateFourObjectCells(Vector2 position, Vector2 size, int objectLimit, int depthLimit)
        {
            Vector2 smallerCellSize = new Vector2(size.x / 2, size.y / 2);
            for (int i = 0; i < 4; i++)
            {
                float xPosMultimplier = (i + 1) % 2 == 0 ? 1 : 0; // 0 - 0, 1 - 1, 2 - 0, 3 - 1
                float yPosMultimplier = i > 1 ? 1 : 0;            // 0 - 0, 1 - 0, 2 - 1, 3 - 1
                cells[i] = new ObjectCell(
                    new Vector2(position.x + (xPosMultimplier * smallerCellSize.x), position.y + (yPosMultimplier * smallerCellSize.y)),
                    smallerCellSize,
                    depthLimit,
                    objectLimit);
            }
        }

        public static Side DetermineSide(Vector3 centerPosition, Vector3 position)
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
        
        private int GetIndexOfCell(Vector3 position)
        {
            if (cells.Length > 1)
            {
                return (int)DetermineSide(centerPosition, position);
            }
            else
            {
                return 0;
            }
        }
        public void HandleOverload(GameObject[] objects, int overloadedIndex)
        {
            if (cells[overloadedIndex].maxCellDepth <= 0) // cannot split
            {
                throw new CannotSplitException();
            }

            if (cells.Length == 1)
            {
                ObjectCell overloadedCell = cells[overloadedIndex] as ObjectCell;
                cells = new Cell<GameObject>[4];
                CreateFourObjectCells(position, size, objectLimit, depthLimit);
            }
            else
            {
                ObjectCell overloadedCell = cells[overloadedIndex] as ObjectCell;
                cells[overloadedIndex] = ParentCell.CreateSingleCell(overloadedCell.position, overloadedCell.size, objectLimit, depthLimit - 1);
            }

            objectsInChildrenCells -= objects.Length;
            foreach (GameObject obj in objects)
            {
                AddNewObject(obj, obj.transform.position);
            }
        }
        public void HandleUnderload()
        {
            GameObject[] objects = this.GetObjectsFromCellsAndErase();
            cells = new Cell<GameObject>[1];
            CreateSingleObjectCell(position, size, objectLimit, depthLimit);
            objectsInChildrenCells = 0;

            foreach(GameObject g in objects)
            {
                AddNewObject(g, g.transform.position);
            }
        }
        public override void AddNewObject(GameObject obj, Vector3 position)
        {
            int arrayIndex = GetIndexOfCell(position);

            try
            {
                cells[arrayIndex].AddNewObject(obj, position);
                objectsInChildrenCells++;
            }
            catch(OverloadException<GameObject> exc)
            {
                if (cells[arrayIndex].maxCellDepth <= 0) 
                {
                    throw new CannotAddObjectException();
                }
                else
                {
                    HandleOverload(exc.objectArray, arrayIndex);
                    AddNewObject(obj, position);
                }
            }
        }
        public override void RemoveObject(GameObject obj, Vector3 position)
        {
            int arrayIndex = GetIndexOfCell(position);

            cells[arrayIndex].RemoveObject(obj, position);
            objectsInChildrenCells--;

            // if there is not a full ammount of objects in this parent cell that means
            // that the split is not necessary! Example if there is 3 objects MAX in a cell
            // then if parent has only 2 that means there is no overload and the 
            // cells can be merged together to free up the space
            if (objectsInChildrenCells <= objectLimit && cells.Length > 1)
            {
                HandleUnderload();
            }
        }
        public override bool IsOutOfCell(Vector3 oldPosition, Vector3 newPosition)
        {
            int arrayIndex = GetIndexOfCell(oldPosition);
            return cells[arrayIndex].IsOutOfCell(oldPosition, newPosition);
        }
        public int ReturnObjectLimit()
        {
            return objectLimit;
        }
        public GameObject[] GetObjectsFromCellsAndErase()
        {
            List<GameObject> outList = new List<GameObject>();
            for(int i = 0; i < cells.Length; i++)
            {
                // Theoretically this every cell is ObjectCell, since the object limit is the same
                // for every cell. If this cell has less then limit it always is the only parent

                GameObject[] g = cells[i].ReturnObjects();
                for (int z = 0; z < g.Length; z++)
                {
                    if(g[z] == null) { continue; }
                    outList.Add(g[z]);
                }
            }
            return outList.ToArray();
        }
        public Cell<GameObject>[] ReturnCellsForDebuging()
        {
            return cells;
        }
        public override GameObject[] ReturnObjects()
        {
            if(cells.Length > 1)
            {
                throw new System.Exception("This should not be called from a parent object that is a object holder for more then one object cell");
            }
            else
            {
                return cells[0].ReturnObjects();
            }
        }
    }
    public class OverloadException<T> : System.Exception
    {
        public T[] objectArray;
        public OverloadException(T[] objects)
        {
            objectArray = objects;
        }
    }
    public class CannotSplitException : System.Exception
    {
        
    }
    public class DepthLimitReachedException : System.Exception { }
    public class CannotAddObjectException : System.Exception { }
}
