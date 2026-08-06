using NUnit.Framework;
using UnityEngine;

public sealed class CatControllerTests
{
    private GameObject catObject;
    private CatController cat;

    [SetUp]
    public void SetUp()
    {
        catObject = new GameObject("Cat under test");
        cat = catObject.AddComponent<CatController>();
        cat.BeginBanquet();
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(catObject);
    }

    [Test]
    public void BeginBanquetRestoresHungryState()
    {
        cat.RegisterServedDish(2, false);
        cat.BeginBanquet();

        Assert.That(cat.State, Is.EqualTo(CatVisualState.Hungry));
        Assert.That(cat.Satisfaction, Is.Zero);
        Assert.That(cat.CurrentRequest, Is.Empty);
    }

    [Test]
    public void ReceivingEndsInStableRelaxedState()
    {
        cat.PlayReceiving();
        cat.RegisterServedDish(1, false);
        Assert.That(cat.State, Is.EqualTo(CatVisualState.Receiving));

        cat.AdvanceVisual(2f);

        Assert.That(cat.State, Is.EqualTo(CatVisualState.Relaxed));
        Assert.That(cat.Satisfaction, Is.EqualTo(1));
        Assert.That(cat.IsReceiving, Is.False);
    }

    [Test]
    public void FinalDishEndsSatisfiedAndPurring()
    {
        cat.PlayReceiving();
        cat.RegisterServedDish(3, true);
        cat.AdvanceVisual(2f);
        cat.PlayFinalPurr();

        Assert.That(cat.State, Is.EqualTo(CatVisualState.Satisfied));
        Assert.That(cat.Satisfaction, Is.EqualTo(3));
    }
}
