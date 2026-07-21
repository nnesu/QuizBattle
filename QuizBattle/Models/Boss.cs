namespace QuizBattle.Models;

public class Boss
{
    public string Name { get; set; } = string.Empty;
    public string IdleImage { get; set; } = string.Empty;
    public string HurtImage { get; set; } = string.Empty;
    public string AttackImage { get; set; } = string.Empty;
    public string DefeatedImage { get; set; } = string.Empty;
    public int MaxHp { get; set; }
    public int Hp { get; set; }
}