namespace Dokryun.Entities;

public class Character
{
    public string Name { get; set; }
    public int Hp { get; set; }
    public int MaxHp { get; set; }
    public int Atk { get; set; }
    public int Def { get; set; }
    public bool IsAlive => Hp > 0;

    public int TakeDamage(int rawDamage)
    {
        int damage = Math.Max(1, rawDamage - Def);
        Hp = Math.Max(0, Hp - damage);
        return damage;
    }
}
