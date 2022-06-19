/// <summary>
/// Represents a criterion that is displayed on the squares.
/// </summary>
public enum InfoType
{ 
    Author, 
    ReleaseYear, 
    DefuserDiff, 
    ExpertDiff, 
    TPScore 
}
/// <summary>
/// Represents an icon that one of the squares can be set to.
/// </summary>
public enum IconType
{ 
    Correct, 
    Incorrect, 
    WrongPos, 
    Higher, 
    Lower,
}
/// <summary>
/// Represents the defuser- or expert-difficulty of a module on the repo.
/// </summary>
public enum Difficulty
{
    VeryEasy,
    Easy,
    Medium,
    Hard,
    VeryHard
}

