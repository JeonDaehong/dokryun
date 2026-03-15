namespace Dokryun.Entities;

public class Enemy : Character
{
    public int Index { get; set; } // position index (0 = frontmost)

    public Enemy(string name, int hp, int atk, int def, int index)
    {
        Name = name;
        Hp = hp;
        MaxHp = hp;
        Atk = atk;
        Def = def;
        Index = index;
    }
}
