using NUnit.Framework;
using UnityEngine;

public sealed class TypingInputTests
{
    private GameObject gameObject;
    private TypingInput typingInput;

    [SetUp]
    public void SetUp()
    {
        gameObject = new GameObject("TypingInputTests");
        typingInput = gameObject.AddComponent<TypingInput>();
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(gameObject);
    }

    [Test]
    public void ProcessCharacter_CompletesThreeConsecutiveWords()
    {
        string[] words = { "sal", "Azúcar", "piñón" };
        int completedWords = 0;
        typingInput.WordCompleted += _ => completedWords++;

        foreach (string word in words)
        {
            typingInput.SetExpectedWord(word);

            foreach (char character in word)
            {
                typingInput.ProcessCharacter(character);
            }

            Assert.That(typingInput.IsComplete, Is.True);
        }

        Assert.That(completedWords, Is.EqualTo(3));
    }

    [Test]
    public void ProcessCharacter_WrongLetterDoesNotAdvance()
    {
        int mistakes = 0;
        typingInput.IncorrectCharacterEntered += (_, _) => mistakes++;
        typingInput.SetExpectedWord("sal");

        typingInput.ProcessCharacter('x');

        Assert.That(typingInput.Progress, Is.Zero);
        Assert.That(mistakes, Is.EqualTo(1));
    }

    [Test]
    public void ProcessCharacter_EmitsOneEventForEachAttempt()
    {
        int correctCharacters = 0;
        int incorrectCharacters = 0;
        int progressChanges = 0;
        typingInput.SetExpectedWord("sal");
        typingInput.CorrectCharacterEntered += (_, _) => correctCharacters++;
        typingInput.IncorrectCharacterEntered += (_, _) =>
            incorrectCharacters++;
        typingInput.ProgressChanged += (_, _) => progressChanges++;

        typingInput.ProcessCharacter('s');
        typingInput.ProcessCharacter('x');

        Assert.That(correctCharacters, Is.EqualTo(1));
        Assert.That(incorrectCharacters, Is.EqualTo(1));
        Assert.That(progressChanges, Is.EqualTo(1));
    }

    [Test]
    public void ProcessBackspace_RemovesProgressWithoutGoingBelowZero()
    {
        typingInput.SetExpectedWord("sal");
        typingInput.ProcessCharacter('s');
        typingInput.ProcessBackspace();
        typingInput.ProcessBackspace();

        Assert.That(typingInput.Progress, Is.Zero);
    }

    [Test]
    public void ProcessCharacter_ReportsCompletionOnlyOnce()
    {
        int completions = 0;
        typingInput.WordCompleted += _ => completions++;
        typingInput.SetExpectedWord("sal");

        foreach (char character in "sallllll")
        {
            typingInput.ProcessCharacter(character);
        }

        Assert.That(completions, Is.EqualTo(1));
        Assert.That(typingInput.IsInputEnabled, Is.False);
    }

    [Test]
    public void SetInputEnabled_BlocksAndRestoresInput()
    {
        typingInput.SetExpectedWord("sal");
        typingInput.SetInputEnabled(false);
        typingInput.ProcessCharacter('s');

        Assert.That(typingInput.Progress, Is.Zero);

        typingInput.SetInputEnabled(true);
        typingInput.ProcessCharacter('s');

        Assert.That(typingInput.Progress, Is.EqualTo(1));
    }
}
