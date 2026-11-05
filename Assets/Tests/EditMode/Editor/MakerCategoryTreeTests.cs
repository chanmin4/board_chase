using System;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class MakerCategoryTreeTests
{
    private string _scope;
    [SetUp] public void Setup() => _scope = "MakerTreeTests-" + Guid.NewGuid().ToString("N");
    [TearDown] public void Cleanup()
    {
        foreach (string scope in new[] { _scope, _scope + "Other" })
            foreach (string key in new[] { "Weapon", "Bullet", "Consumable" })
                EditorPrefs.DeleteKey(MakerCategoryTreeUi.PreferenceKey(scope, key));
    }

    [Test] public void NewCategoryGroupsStartCollapsed()
    {
        Assert.That(MakerCategoryTreeUi.IsExpanded(_scope, "Weapon"), Is.False);
        Assert.That(EditorPrefs.HasKey(MakerCategoryTreeUi.PreferenceKey(_scope, "Weapon")), Is.False);
    }

    [Test] public void ExpansionIsPersistedAndIndependentAcrossGroupsAndMakers()
    {
        MakerCategoryTreeUi.SetExpanded(_scope, "Weapon", true);
        Assert.That(EditorPrefs.GetBool(MakerCategoryTreeUi.PreferenceKey(_scope, "Weapon")), Is.True);
        Assert.That(MakerCategoryTreeUi.IsExpanded(_scope, "Weapon"), Is.True);
        Assert.That(MakerCategoryTreeUi.IsExpanded(_scope, "Bullet"), Is.False);
        Assert.That(MakerCategoryTreeUi.IsExpanded(_scope + "Other", "Weapon"), Is.False);
        MakerCategoryTreeUi.SetExpanded(_scope, "Weapon", false);
        Assert.That(MakerCategoryTreeUi.IsExpanded(_scope, "Weapon"), Is.False);
    }

    [Test] public void ExpandAndCollapseAllAffectOnlyTheSpecifiedGroups()
    {
        MakerCategoryTreeUi.SetExpanded(_scope, "Consumable", true);
        MakerCategoryTreeUi.SetAll(_scope, new[] { "Weapon", "Bullet" }, true);
        Assert.That(MakerCategoryTreeUi.IsExpanded(_scope, "Weapon"), Is.True);
        Assert.That(MakerCategoryTreeUi.IsExpanded(_scope, "Bullet"), Is.True);
        MakerCategoryTreeUi.SetAll(_scope, new[] { "Weapon", "Bullet" }, false);
        Assert.That(MakerCategoryTreeUi.IsExpanded(_scope, "Weapon"), Is.False);
        Assert.That(MakerCategoryTreeUi.IsExpanded(_scope, "Bullet"), Is.False);
        Assert.That(MakerCategoryTreeUi.IsExpanded(_scope, "Consumable"), Is.True);
    }

    [Test] public void ArrowAndSelectionHitTargetsDoNotOverlap()
    {
        var row = new Rect(10, 20, 210, 25);
        MakerCategoryTreeUi.SplitRow(row, true, 0, out Rect arrow, out Rect select);
        Assert.That(arrow.width, Is.EqualTo(18));
        Assert.That(arrow.Overlaps(select), Is.False);
        Assert.That(arrow.Contains(select.center), Is.False);
        Assert.That(select.Contains(arrow.center), Is.False);
        Assert.That(select.xMax, Is.EqualTo(row.xMax));
    }

    [Test] public void LeafIndentDoesNotReserveAnInvisibleDisclosureButton()
    {
        var row = new Rect(10, 20, 210, 25);
        MakerCategoryTreeUi.SplitRow(row, false, 18, out Rect arrow, out Rect select);
        Assert.That(arrow.width, Is.Zero);
        Assert.That(select.xMin, Is.EqualTo(28));
        Assert.That(select.xMax, Is.EqualTo(row.xMax));
    }

    [Test] public void ItemSidebarUsesAllAvailableVerticalSpace()
    {
        var layout = ItemMakerWindow.CalculateSidebarLayout(756, 110, ItemMakerWindow.DefaultCreatePanelHeight, true);
        Assert.That(layout.Categories.height, Is.GreaterThan(400));
        Assert.That(layout.Categories.yMax, Is.EqualTo(layout.Splitter.yMin));
        Assert.That(layout.Splitter.yMax, Is.EqualTo(layout.CreateHeader.yMin));
        Assert.That(layout.CreateHeader.yMax, Is.EqualTo(layout.CreateBody.yMin));
        Assert.That(layout.CreateBody.yMax, Is.EqualTo(751));
    }

    [Test] public void ItemSidebarDividerTransfersHeightBetweenPanes()
    {
        var before = ItemMakerWindow.CalculateSidebarLayout(756, 110, 184, true);
        var after = ItemMakerWindow.CalculateSidebarLayout(756, 110, 124, true);
        Assert.That(after.Categories.height - before.Categories.height, Is.EqualTo(60));
        Assert.That(before.CreateBody.height - after.CreateBody.height, Is.EqualTo(60));
        Assert.That(after.CreateBody.yMax, Is.EqualTo(before.CreateBody.yMax));
    }

    [Test] public void ItemSidebarCollapsedPaletteReturnsItsHeightToCategories()
    {
        var open = ItemMakerWindow.CalculateSidebarLayout(756, 110, 184, true);
        var closed = ItemMakerWindow.CalculateSidebarLayout(756, 110, 184, false);
        Assert.That(closed.CreateBody.height, Is.Zero);
        Assert.That(closed.Categories.height - open.Categories.height, Is.EqualTo(184));
        Assert.That(closed.CreateHeader.yMax, Is.EqualTo(751));
    }

    [Test] public void ItemSidebarClampsExtremeDragsAndFitsMinimumWindow()
    {
        var up = ItemMakerWindow.CalculateSidebarLayout(416, 110, 10000, true);
        var down = ItemMakerWindow.CalculateSidebarLayout(416, 110, -10000, true);
        Assert.That(up.Categories.height, Is.EqualTo(100));
        Assert.That(down.CreateBody.height, Is.EqualTo(100));
        Assert.That(up.CreateBody.yMax, Is.EqualTo(411));
        Assert.That(down.CreateBody.yMax, Is.EqualTo(411));
    }

    [Test] public void ItemSidebarWindowResizeGrowsCategoriesWithoutChangingDesiredSplit()
    {
        const float desired = 240;
        var small = ItemMakerWindow.CalculateSidebarLayout(416, 110, desired, true);
        var large = ItemMakerWindow.CalculateSidebarLayout(756, 110, desired, true);
        Assert.That(small.CreateBody.height, Is.LessThan(desired));
        Assert.That(large.CreateBody.height, Is.EqualTo(desired));
        Assert.That(large.Categories.height, Is.GreaterThan(small.Categories.height));
    }

    [Test] public void ItemSidebarInvalidStoredHeightFallsBackToDefault()
    {
        foreach (float invalid in new[] { float.NaN, float.PositiveInfinity, float.NegativeInfinity })
        {
            var layout = ItemMakerWindow.CalculateSidebarLayout(756, 110, invalid, true);
            Assert.That(layout.CreateBody.height, Is.EqualTo(ItemMakerWindow.DefaultCreatePanelHeight));
        }
    }
}
