namespace QuizBattle.Models;

public class User
{
    public string Email
    {
        get;
        set;
    } = "";

    public string IdToken
    {
        get;
        set;
    } = "";

    public string LocalId
    {
        get;
        set;
    } = "";

    public string DisplayName
    {
        get;
        set;
    } = "No Name";

    public string PhotoUrl
    {
        get;
        set;
    } = string.Empty;

    public bool EmailVerified
    {
        get;
        set;
    }
}