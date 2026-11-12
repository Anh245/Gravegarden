using System;
using System.Collections.Generic;

public enum PlacementFailure { None, InvalidSize, OutsideBounds, Blocked, Occupied, InvalidReservation }

// Trạng thái ô thuần C#: không phụ thuộc Collider hoặc thứ tự Awake của Unity.
public sealed class PlacementMap
{
    private readonly int width;
    private readonly int height;
    private readonly HashSet<(int x, int y)> allowed;
    private readonly Dictionary<(int x, int y), int> owners = new Dictionary<(int x, int y), int>();
    private readonly Dictionary<int, List<(int x, int y)>> reservations = new Dictionary<int, List<(int x, int y)>>();
    private int nextId = 1;
    public int OccupiedCount => owners.Count;

    public PlacementMap(int width, int height, IEnumerable<(int x, int y)> allowedCells)
    {
        if (width <= 0 || height <= 0) throw new ArgumentOutOfRangeException();
        this.width = width;
        this.height = height;
        allowed = new HashSet<(int x, int y)>(allowedCells);
    }

    public void AllowCells(IEnumerable<(int x, int y)> cells)
    {
        var pending = new List<(int x, int y)>(cells);
        foreach (var cell in pending)
            if (cell.x < 0 || cell.y < 0 || cell.x >= width || cell.y >= height)
                throw new ArgumentOutOfRangeException(nameof(cells));
        foreach (var cell in pending) allowed.Add(cell);
    }

    public bool IsOccupied(int x, int y) => owners.ContainsKey((x, y));

    public bool CanPlace(int x, int y, int size, out PlacementFailure failure)
    {
        if (size <= 0 || size > width || size > height)
        {
            failure = PlacementFailure.InvalidSize;
            return false;
        }
        if (x < 0 || y < 0 || x > width - size || y > height - size)
        {
            failure = PlacementFailure.OutsideBounds;
            return false;
        }
        for (int row = y; row < y + size; row++)
        for (int column = x; column < x + size; column++)
        {
            if (!allowed.Contains((column, row)))
            {
                failure = PlacementFailure.Blocked;
                return false;
            }
            if (owners.ContainsKey((column, row)))
            {
                failure = PlacementFailure.Occupied;
                return false;
            }
        }
        failure = PlacementFailure.None;
        return true;
    }

    public bool TryReserve(int x, int y, int size, out int id, out PlacementFailure failure)
    {
        id = 0;
        if (!CanPlace(x, y, size, out failure))
            return false;
        id = nextId++;
        var cells = new List<(int x, int y)>();
        for (int row = y; row < y + size; row++)
        for (int column = x; column < x + size; column++)
        {
            var cell = (column, row);
            owners.Add(cell, id);
            cells.Add(cell);
        }
        reservations.Add(id, cells);
        return true;
    }

    public bool CanRelocate(int id,int x,int y,int size,out PlacementFailure failure)
    {
        if(size<=0 || size>width || size>height)
        {failure=PlacementFailure.InvalidSize;return false;}
        if(!reservations.TryGetValue(id,out var oldCells) || oldCells.Count!=(long)size*size)
        {failure=PlacementFailure.InvalidReservation;return false;}
        if(x<0 || y<0 || x>width-size || y>height-size)
        {failure=PlacementFailure.OutsideBounds;return false;}
        for(int row=y;row<y+size;row++)
        for(int column=x;column<x+size;column++)
        {
            var cell=(column,row);
            if(!allowed.Contains(cell)) {failure=PlacementFailure.Blocked;return false;}
            if(owners.TryGetValue(cell,out int owner) && owner!=id)
            {failure=PlacementFailure.Occupied;return false;}
        }
        failure=PlacementFailure.None;return true;
    }
    public bool TryRelocate(int id,int x,int y,int size,out PlacementFailure failure)
    {
        // Chỉ thay đổi map sau khi toàn bộ vị trí mới đã hợp lệ.
        if(!CanRelocate(id,x,y,size,out failure)) return false;
        var next=new List<(int x,int y)>();
        for(int row=y;row<y+size;row++)
        for(int column=x;column<x+size;column++) next.Add((column,row));
        foreach(var cell in reservations[id]) owners.Remove(cell);
        foreach(var cell in next) owners.Add(cell,id);
        reservations[id]=next;
        return true;
    }

    public bool Release(int id)
    {
        if (!reservations.TryGetValue(id, out List<(int x, int y)> cells))
            return false;
        foreach (var cell in cells) owners.Remove(cell);
        reservations.Remove(id);
        return true;
    }
}
