using System.Text;

namespace antirus.Util;

public static class TextRater
{
    private static readonly string[] russianWords = new string[]
    {
        "росси",
        "русск",
        "россия",
        "путин",
        "russia",
        "russki",
        "вагнер",
        //russian only cyrillic letters 
        "ы",
        "ъ",
        "э",
        "ё"
    };
    private static readonly string[] ukrainianWords = new string[]
    {
        "україн",
        "ukrain",
        "київ",
        "москаль",
        "кацап",
        "україн",
        "ЗСУ",
        "повернись живим",
        "спільнота",    
        //ukrainian only cyrillic letters
        "ї",
        "ґ",
        "є",
        "і"
    };

    public static int RateText(string text)
    {
        text = text.ToLower();
        int russianCount = 0;
        int ukrainianCount = 0;
        foreach (string word in russianWords)
        {
            if (text.Contains(word))
            {
                russianCount++;
            }
        }
        foreach (string word in ukrainianWords)
        {
            if (text.Contains(word))
            {
                ukrainianCount++;
            }
        }
        return russianCount - ukrainianCount;
    }


}