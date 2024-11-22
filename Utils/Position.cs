namespace api.Utils;

public class Position(int x = 0, int y = 0)
{
    public int X { get; set; } = x;
    public int Y { get; set; } = y;
    
    // Operator overload for addition.
    public static Position operator +(Position pos1, Position pos2) => new Position(pos1.X + pos2.X, pos1.Y + pos2.Y);
    // Operator overload for multiplication by a constant.
    public static Position operator *(int constant, Position pos) => new Position(constant * pos.X, constant * pos.Y);
    // Equality overload
    public static bool operator ==(Position pos1, Position pos2) => (pos1.X == pos2.X && pos1.Y == pos2.Y);   
    // Inequality overload
    public static bool operator !=(Position pos1, Position pos2) => (pos1.X != pos2.X || pos1.Y != pos2.Y);   
}
