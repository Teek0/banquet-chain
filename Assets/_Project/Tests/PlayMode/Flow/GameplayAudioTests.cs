using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class GameplayAudioTests
{
    private readonly List<Object> objectsToDestroy = new();

    [TearDown]
    public void TearDown()
    {
        foreach (Object target in objectsToDestroy)
        {
            if (target != null)
            {
                Object.DestroyImmediate(target);
            }
        }

        objectsToDestroy.Clear();
    }

    [UnityTest]
    public IEnumerator MissingOptionalClips_UsesCompleteFallbackSet()
    {
        Setup setup = CreateSetup();
        setup.Audio.Prepare();
        yield return null;

        Assert.That(
            setup.Audio.HasCompleteFeedbackSet,
            Is.True,
            $"Clips disponibles: {setup.Audio.AvailableClipCount}/11"
        );
    }

    [UnityTest]
    public IEnumerator TypingEvents_ProduceDistinctDiagnosticCues()
    {
        Setup setup = CreateSetup();
        setup.Input.SetExpectedWord("pan");
        setup.Input.ProcessCharacter('p');
        Assert.That(setup.Audio.LastCue, Is.EqualTo(GameplayAudioCue.CorrectLetter));

        setup.Input.ProcessCharacter('x');
        Assert.That(setup.Audio.LastCue, Is.EqualTo(GameplayAudioCue.Error));

        setup.Input.ProcessBackspace();
        Assert.That(setup.Audio.LastCue, Is.EqualTo(GameplayAudioCue.Backspace));
        setup.Input.ProcessCharacter('a');
        setup.Input.ProcessCharacter('n');
        yield return null;

        Assert.That(setup.Audio.LastCue, Is.EqualTo(GameplayAudioCue.WordCompleted));
        Assert.That(setup.Audio.PlayedCueCount, Is.GreaterThanOrEqualTo(3));
    }

    private Setup CreateSetup()
    {
        GameObject runnerObject = new("Runner");
        GameObject audioObject = new("Audio");
        objectsToDestroy.Add(runnerObject);
        objectsToDestroy.Add(audioObject);
        runnerObject.SetActive(false);
        audioObject.SetActive(false);
        TypingInput input = runnerObject.AddComponent<TypingInput>();
        RecipeRunner runner = runnerObject.AddComponent<RecipeRunner>();
        SerializedObject serializedRunner = new(runner);
        serializedRunner.FindProperty("typingInput").objectReferenceValue = input;
        serializedRunner.FindProperty("playOnStart").boolValue = false;
        serializedRunner.ApplyModifiedPropertiesWithoutUndo();

        AudioSource sfx = audioObject.AddComponent<AudioSource>();
        AudioSource music = audioObject.AddComponent<AudioSource>();
        AudioSource purr = audioObject.AddComponent<AudioSource>();
        GameplayAudio audio = audioObject.AddComponent<GameplayAudio>();
        SerializedObject serializedAudio = new(audio);
        serializedAudio.FindProperty("typingInput").objectReferenceValue = input;
        serializedAudio.FindProperty("recipeRunner").objectReferenceValue = runner;
        serializedAudio.FindProperty("sfxSource").objectReferenceValue = sfx;
        serializedAudio.FindProperty("musicSource").objectReferenceValue = music;
        serializedAudio.FindProperty("purrSource").objectReferenceValue = purr;
        serializedAudio.ApplyModifiedPropertiesWithoutUndo();
        runnerObject.SetActive(true);
        audioObject.SetActive(true);
        return new Setup(input, audio);
    }

    private sealed class Setup
    {
        public Setup(TypingInput input, GameplayAudio audio)
        {
            Input = input;
            Audio = audio;
        }

        public TypingInput Input { get; }
        public GameplayAudio Audio { get; }
    }
}
