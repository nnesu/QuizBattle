//This file is unused as of now
//The idea is that each question list in the Data folder represents one subject, referred to as bosses
//For example, if the Data folder contains math.txt, and english.txt, players can access choose between them
//And then ayun start na ng boss battle
//This system needs a main menu, which right now hasn't been implemented yet

namespace QuizBattle.Models
{
    public class Boss
    {
        public string Name { get; set; } = string.Empty;

        public string FileName { get; set; } = string.Empty;
    }
}